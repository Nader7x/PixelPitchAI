"""Custom Football Event Transformer (~112M Base config) — Module C2.

Architecture per docs/revamp/03_hybrid_event_transformer_and_models.md:
- Backbone: 12L, d_model=768, 12Q/4KV Grouped-Query Attention (GQA), RoPE (theta=500,000),
  SwiGLU FFN (d_ff=2048), RMSNorm + QK-Norm, 16,384 event-step context window.
- Conditioning:
  1. Tactical & Roster Memory Encoder: ingests Module B player_cond [B, 22, 128] + 2 Team/GK
     tactical tokens + 4 Global Ball/Match tokens -> 1 bidirectional interaction layer -> M_t [B, 28, 768]
     read via Gated Cross-Attention at every transformer block.
  2. Global Match State s_t [B, 64] via Shared-Trunk + per-layer Rank-32 AdaLN-Zero.
- Intra-Event Causal Micro-Decoder: 2 causal layers (d_dec=512) sequentially sampling the
  7 dependent sub-fields of event e_t under hard FSM/Module-B logit masks and Module D calibration.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Callable, Dict, List, Optional, Tuple

import math
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch import FloatTensor, LongTensor, Tensor

from api.v2.simulator.fsm import (
    ActionType,
    CELL_OUT_OF_BOUNDS,
    CELL_SYSTEM,
    FSMMasks,
    NUM_ACTIONS,
    NUM_ACTOR_SLOTS,
    NUM_BODY_PARTS,
    NUM_CELL_TOKENS,
    GRID_X,
    NUM_DT_BUCKETS,
    NUM_END_Z_BINS,
    NUM_MODIFIER_FLAGS,
    NUM_OUTCOMES,
    NUM_PASS_HEIGHTS,
    NUM_PITCH_CELLS,
    NUM_RESTART_TYPES,
    PITCH_LENGTH,
    PITCH_WIDTH,
    Period,
    PossPhase,
    cell_to_center_xy,
    decode_dt_seconds,
)


@dataclass
class EventTransformerConfig:
    """Configuration for PixelPitchEventTF scaling tiers (78M .. 154M) and tiny test profile."""

    d_model: int = 768
    n_layers: int = 12
    n_heads: int = 12
    n_kv_heads: int = 4
    d_ff: int = 2048
    max_seq_len: int = 16384
    rope_theta: float = 500_000.0
    d_dec: int = 512
    n_dec_layers: int = 2
    n_dec_heads: int = 8
    d_dec_ff: int = 1408
    player_cond_dim: int = 128
    team_tactics_dim: int = 18
    global_ctx_dim: int = 16
    state_dim: int = 64
    adaln_trunk_dim: int = 512
    adaln_rank: int = 32
    mem_ff_dim: int = 1536
    sub_emb_dim: int = 64
    dropout: float = 0.0

    @classmethod
    def small_78m(cls) -> "EventTransformerConfig":
        return cls(d_model=640, n_layers=12, n_heads=10, n_kv_heads=2, d_ff=1792, d_dec=384, n_dec_heads=6, d_dec_ff=1024)

    @classmethod
    def base_112m(cls) -> "EventTransformerConfig":
        """Locked default specification (`PixelPitch-EventTF-112M`)."""
        return cls()

    @classmethod
    def medium_128m(cls) -> "EventTransformerConfig":
        return cls(n_layers=14)

    @classmethod
    def large_154m(cls) -> "EventTransformerConfig":
        return cls(n_layers=16, d_ff=2304)

    @classmethod
    def tiny_test(cls) -> "EventTransformerConfig":
        """Fast lightweight configuration for CPU unit tests and CI integration checks."""
        return cls(
            d_model=128,
            n_layers=2,
            n_heads=4,
            n_kv_heads=2,
            d_ff=256,
            max_seq_len=2048,
            d_dec=96,
            n_dec_layers=1,
            n_dec_heads=4,
            d_dec_ff=192,
            adaln_trunk_dim=128,
            adaln_rank=16,
            mem_ff_dim=256,
            sub_emb_dim=32,
        )


class RMSNorm(nn.Module):
    def __init__(self, dim: int, eps: float = 1e-6):
        super().__init__()
        self.eps = eps
        self.weight = nn.Parameter(torch.ones(dim))

    def forward(self, x: Tensor) -> Tensor:
        w = self.weight if self.weight.dtype == x.dtype else self.weight.to(x.dtype)
        if hasattr(F, "rms_norm") and not torch.onnx.is_in_onnx_export():
            return F.rms_norm(x, (x.shape[-1],), w, self.eps)
        norm = x * torch.rsqrt(x.pow(2).mean(dim=-1, keepdim=True) + self.eps)
        return norm * w


class RotaryEmbedding(nn.Module):
    """Precomputed Rotary Position Embeddings supporting up to 16,384 steps & KV-cache offsets."""

    def __init__(self, head_dim: int, max_seq_len: int = 16384, theta: float = 500_000.0):
        super().__init__()
        self.head_dim = head_dim
        self.max_seq_len = max_seq_len
        inv_freq = 1.0 / (theta ** (torch.arange(0, head_dim, 2, dtype=torch.float32) / head_dim))
        self.register_buffer("inv_freq", inv_freq, persistent=False)
        self._cos_cache: Optional[Tensor] = None
        self._sin_cache: Optional[Tensor] = None

    def _build_cache(self, seq_len: int, device: torch.device, dtype: torch.dtype) -> Tuple[Tensor, Tensor]:
        if isinstance(seq_len, torch.Tensor):
            seq_len_int = int(seq_len.item())
        else:
            seq_len_int = int(seq_len)
        if (
            self._cos_cache is None
            or self._cos_cache.shape[0] < seq_len_int
            or self._cos_cache.device != device
            or self._cos_cache.dtype != dtype
        ):
            t = torch.arange(max(seq_len_int, min(self.max_seq_len, 2048)), device=device, dtype=torch.float32)
            freqs = torch.outer(t, self.inv_freq.to(device))
            emb = torch.cat([freqs, freqs], dim=-1)
            self._cos_cache = emb.cos().to(dtype)
            self._sin_cache = emb.sin().to(dtype)
        return self._cos_cache, self._sin_cache

    @staticmethod
    def _rotate_half(x: Tensor) -> Tensor:
        half = x.shape[-1] // 2
        return torch.cat([-x[..., half:], x[..., :half]], dim=-1)

    def ensure_cache(self, seq_len: int, device: torch.device, dtype: torch.dtype) -> Tuple[Tensor, Tensor]:
        return self._build_cache(seq_len, device, dtype)

    def forward(self, q: Tensor, k: Tensor, pos_offset: int | Tensor = 0) -> Tuple[Tensor, Tensor]:
        # q: [B, H, L, D], k: [B, H_kv, L, D]
        L = q.shape[2]
        if isinstance(pos_offset, torch.Tensor):
            # Static CUDA-graph path (L == 1): index_select avoids .item() sync and dynamic slicing
            cos_cache, sin_cache = self._cos_cache, self._sin_cache
            if cos_cache is None or sin_cache is None or cos_cache.device != q.device or cos_cache.dtype != q.dtype:
                cos_cache, sin_cache = self._build_cache(self.max_seq_len, q.device, q.dtype)
            idx = pos_offset.view(1)
            cos_slice = cos_cache.index_select(0, idx).unsqueeze(0).unsqueeze(0)
            sin_slice = sin_cache.index_select(0, idx).unsqueeze(0).unsqueeze(0)
        else:
            cos, sin = self._build_cache(pos_offset + L, q.device, q.dtype)
            cos_slice = cos[pos_offset: pos_offset + L].unsqueeze(0).unsqueeze(0)
            sin_slice = sin[pos_offset: pos_offset + L].unsqueeze(0).unsqueeze(0)
        q_rot = (q * cos_slice) + (self._rotate_half(q) * sin_slice)
        k_rot = (k * cos_slice) + (self._rotate_half(k) * sin_slice)
        return q_rot, k_rot


class SwiGLU(nn.Module):
    def __init__(self, d_model: int, d_ff: int):
        super().__init__()
        self.w_gate = nn.Linear(d_model, d_ff, bias=False)
        self.w_up = nn.Linear(d_model, d_ff, bias=False)
        self.w_down = nn.Linear(d_ff, d_model, bias=False)

    def forward(self, x: Tensor) -> Tensor:
        return self.w_down(F.silu(self.w_gate(x)) * self.w_up(x))


class GQAAttention(nn.Module):
    """Grouped-Query Attention supporting causal self-attention (RoPE+QK-Norm+KVCache) and gated cross-attn."""

    def __init__(
        self,
        d_model: int,
        n_heads: int,
        n_kv_heads: int,
        use_rope: bool = True,
        use_qk_norm: bool = True,
        gated: bool = False,
        max_seq_len: int = 16384,
        rope_theta: float = 500_000.0,
    ):
        super().__init__()
        assert d_model % n_heads == 0
        assert n_heads % n_kv_heads == 0
        self.n_heads = n_heads
        self.n_kv_heads = n_kv_heads
        self.n_rep = n_heads // n_kv_heads
        self.head_dim = d_model // n_heads
        self.use_rope = use_rope

        self.w_q = nn.Linear(d_model, n_heads * self.head_dim, bias=False)
        self.w_k = nn.Linear(d_model, n_kv_heads * self.head_dim, bias=False)
        self.w_v = nn.Linear(d_model, n_kv_heads * self.head_dim, bias=False)
        self.w_o = nn.Linear(n_heads * self.head_dim, d_model, bias=False)

        self.q_norm = RMSNorm(self.head_dim) if use_qk_norm else None
        self.k_norm = RMSNorm(self.head_dim) if use_qk_norm else None
        self.rotary = RotaryEmbedding(self.head_dim, max_seq_len=max_seq_len, theta=rope_theta) if use_rope else None
        self.gate_alpha = nn.Parameter(torch.zeros(d_model)) if gated else None

    def forward(
        self,
        q_in: Tensor,
        kv_in: Optional[Tensor] = None,
        is_causal: bool = True,
        past_kv: Optional[Tuple[Tensor, Tensor]] = None,
        pos_offset: int | Tensor = 0,
        static_kv: Optional[Tuple[Tensor, Tensor]] = None,
        attn_mask: Optional[Tensor] = None,
    ) -> Tuple[Tensor, Optional[Tuple[Tensor, Tensor]]]:
        B, L_q, _ = q_in.shape
        kv_src = q_in if kv_in is None else kv_in
        L_kv = kv_src.shape[1]

        q = self.w_q(q_in).view(B, L_q, self.n_heads, self.head_dim).transpose(1, 2)
        k = self.w_k(kv_src).view(B, L_kv, self.n_kv_heads, self.head_dim).transpose(1, 2)
        v = self.w_v(kv_src).view(B, L_kv, self.n_kv_heads, self.head_dim).transpose(1, 2)

        if self.q_norm is not None:
            q = self.q_norm(q)
            k = self.k_norm(k)

        if self.rotary is not None:
            q, k = self.rotary(q, k, pos_offset=pos_offset)

        new_kv: Optional[Tuple[Tensor, Tensor]] = None
        if kv_in is None:
            if static_kv is not None:
                pos_idx = (
                    pos_offset.view(1)
                    if isinstance(pos_offset, torch.Tensor)
                    else torch.tensor([int(pos_offset)], device=k.device, dtype=torch.long)
                )
                static_kv[0].index_copy_(2, pos_idx, k.to(static_kv[0].dtype))
                static_kv[1].index_copy_(2, pos_idx, v.to(static_kv[1].dtype))
                k = static_kv[0]
                v = static_kv[1]
            else:
                if past_kv is not None:
                    k = torch.cat([past_kv[0], k], dim=2)
                    v = torch.cat([past_kv[1], v], dim=2)
                new_kv = (k, v)

        # Expand KV heads for GQA
        if self.n_rep > 1:
            k_exp = k.repeat_interleave(self.n_rep, dim=1)
            v_exp = v.repeat_interleave(self.n_rep, dim=1)
        else:
            k_exp, v_exp = k, v

        if attn_mask is not None:
            attn_out = F.scaled_dot_product_attention(q, k_exp, v_exp, attn_mask=attn_mask, is_causal=False)
        else:
            # Causal mask applies when self-attending without past KV cache (works for L_q==1 and L_q>1; pure Python bool for ONNX export)
            causal_flag = bool(is_causal) and (kv_in is None) and (past_kv is None)
            attn_out = F.scaled_dot_product_attention(q, k_exp, v_exp, is_causal=causal_flag)
        out = attn_out.transpose(1, 2).contiguous().view(B, L_q, -1)
        out = self.w_o(out)
        if self.gate_alpha is not None:
            out = out * torch.tanh(self.gate_alpha)
        return out, new_kv


class TacticalMemoryEncoder(nn.Module):
    """Encodes Module B player_cond [B, 22, 128] + Team/GK [B, 2, 18] + Ball/Match [B, 4, 16] -> M_t [B, 28, d_model]."""

    def __init__(self, cfg: EventTransformerConfig):
        super().__init__()
        self.player_proj = nn.Sequential(
            nn.Linear(cfg.player_cond_dim, 512),
            nn.SiLU(),
            nn.Linear(512, cfg.d_model),
        )
        self.team_proj = nn.Sequential(
            nn.Linear(cfg.team_tactics_dim, 512),
            nn.SiLU(),
            nn.Linear(512, cfg.d_model),
        )
        self.ctx_proj = nn.Sequential(
            nn.Linear(cfg.global_ctx_dim, 512),
            nn.SiLU(),
            nn.Linear(512, cfg.d_model),
        )
        self.slot_type_emb = nn.Parameter(torch.randn(28, cfg.d_model) * 0.02)

        # 1 Bidirectional Spatial-Tactical Interaction Layer
        self.norm1 = RMSNorm(cfg.d_model)
        self.attn = GQAAttention(
            cfg.d_model, cfg.n_heads, cfg.n_kv_heads, use_rope=False, use_qk_norm=True, gated=False
        )
        self.norm2 = RMSNorm(cfg.d_model)
        self.ffn = SwiGLU(cfg.d_model, cfg.mem_ff_dim)

    def forward(
        self,
        player_cond: FloatTensor,
        team_tactics: Optional[FloatTensor] = None,
        global_ball_ctx: Optional[FloatTensor] = None,
    ) -> FloatTensor:
        B = player_cond.shape[0]
        dev, dt = player_cond.device, player_cond.dtype

        if team_tactics is None:
            team_tactics = torch.zeros((B, 2, self.team_proj[0].in_features), device=dev, dtype=dt)
        if global_ball_ctx is None:
            global_ball_ctx = torch.zeros((B, 4, self.ctx_proj[0].in_features), device=dev, dtype=dt)

        p_tok = self.player_proj(player_cond)        # [B, 22, d_model]
        t_tok = self.team_proj(team_tactics)         # [B, 2, d_model]
        c_tok = self.ctx_proj(global_ball_ctx)       # [B, 4, d_model]
        mem = torch.cat([p_tok, t_tok, c_tok], dim=1) + self.slot_type_emb.unsqueeze(0)

        attn_out, _ = self.attn(self.norm1(mem), kv_in=self.norm1(mem), is_causal=False)
        mem = mem + attn_out
        mem = mem + self.ffn(self.norm2(mem))
        return mem


class Rank32AdaLNAdapter(nn.Module):
    """Per-layer rank-32 AdaLN-Zero adapter producing 6 modulation vectors from shared c_t."""

    def __init__(self, trunk_dim: int, d_model: int, rank: int = 32):
        super().__init__()
        self.down = nn.Linear(trunk_dim, rank, bias=False)
        self.up = nn.Linear(rank, 6 * d_model, bias=True)
        nn.init.zeros_(self.up.weight)
        nn.init.zeros_(self.up.bias)

    def forward(self, c_t: Tensor) -> Tuple[Tensor, Tensor, Tensor, Tensor, Tensor, Tensor]:
        mods = self.up(F.silu(self.down(c_t)))
        return mods.chunk(6, dim=-1)


class EventTransformerBlock(nn.Module):
    """Backbone layer: AdaLN Self-Attn (GQA+RoPE) -> Gated Tactical Cross-Attn -> AdaLN SwiGLU FFN."""

    def __init__(self, cfg: EventTransformerConfig):
        super().__init__()
        self.norm1 = RMSNorm(cfg.d_model)
        self.self_attn = GQAAttention(
            cfg.d_model, cfg.n_heads, cfg.n_kv_heads,
            use_rope=True, use_qk_norm=True, gated=False,
            max_seq_len=cfg.max_seq_len, rope_theta=cfg.rope_theta,
        )
        self.norm_cross = RMSNorm(cfg.d_model)
        self.cross_attn = GQAAttention(
            cfg.d_model, cfg.n_heads, cfg.n_kv_heads,
            use_rope=False, use_qk_norm=False, gated=True,
        )
        self.norm2 = RMSNorm(cfg.d_model)
        self.ffn = SwiGLU(cfg.d_model, cfg.d_ff)
        self.adaln = Rank32AdaLNAdapter(cfg.adaln_trunk_dim, cfg.d_model, rank=cfg.adaln_rank)

    def forward(
        self,
        x: Tensor,
        mem: Tensor,
        c_t: Tensor,
        past_kv: Optional[Tuple[Tensor, Tensor]] = None,
        pos_offset: int | Tensor = 0,
        static_kv: Optional[Tuple[Tensor, Tensor]] = None,
        attn_mask: Optional[Tensor] = None,
    ) -> Tuple[Tensor, Optional[Tuple[Tensor, Tensor]]]:
        g1, b1, a1, g2, b2, a2 = self.adaln(c_t)
        # Support both [B, D] (step decode) and [B, L, D] (sequence)
        if g1.dim() == 2:
            g1, b1, a1 = g1.unsqueeze(1), b1.unsqueeze(1), a1.unsqueeze(1)
            g2, b2, a2 = g2.unsqueeze(1), b2.unsqueeze(1), a2.unsqueeze(1)

        h1 = self.norm1(x) * (1.0 + g1) + b1
        sa_out, new_kv = self.self_attn(
            h1,
            kv_in=None,
            is_causal=True,
            past_kv=past_kv,
            pos_offset=pos_offset,
            static_kv=static_kv,
            attn_mask=attn_mask,
        )
        x = x + (1.0 + a1) * sa_out

        ca_out, _ = self.cross_attn(self.norm_cross(x), kv_in=mem, is_causal=False)
        x = x + ca_out

        h2 = self.norm2(x) * (1.0 + g2) + b2
        x = x + (1.0 + a2) * self.ffn(h2)
        return x, new_kv


class FactoredEventEmbedder(nn.Module):
    """Embeds a committed 10-field event e_{t-1} into a single d_model composite backbone token."""

    def __init__(self, cfg: EventTransformerConfig):
        super().__init__()
        d = cfg.sub_emb_dim
        self.dt_emb = nn.Embedding(NUM_DT_BUCKETS, d)
        self.period_emb = nn.Embedding(len(Period), d)
        self.phase_emb = nn.Embedding(len(PossPhase), d)
        self.actor_emb = nn.Embedding(NUM_ACTOR_SLOTS, d)
        self.action_emb = nn.Embedding(NUM_ACTIONS, d)
        self.restart_emb = nn.Embedding(NUM_RESTART_TYPES, d)
        self.height_emb = nn.Embedding(NUM_PASS_HEIGHTS, d)
        self.body_emb = nn.Embedding(NUM_BODY_PARTS, d)
        self.start_cell_emb = nn.Embedding(NUM_CELL_TOKENS, d)
        self.end_cell_emb = nn.Embedding(NUM_CELL_TOKENS, d)
        self.end_z_emb = nn.Embedding(NUM_END_Z_BINS, d)
        self.target_emb = nn.Embedding(NUM_ACTOR_SLOTS, d)
        self.outcome_emb = nn.Embedding(NUM_OUTCOMES, d)
        self.flags_proj = nn.Linear(NUM_MODIFIER_FLAGS, d)
        self.cont_proj = nn.Sequential(nn.Linear(10, d * 2), nn.SiLU(), nn.Linear(d * 2, d * 2))

        total_in = 14 * d + d * 2  # 14 discrete/flag slices + 2d continuous
        self.fuse = nn.Sequential(
            nn.Linear(total_in, cfg.d_model),
            nn.SiLU(),
            nn.Linear(cfg.d_model, cfg.d_model),
        )

    def forward(self, event: Dict[str, Tensor]) -> Tensor:
        # Each discrete entry has shape [B, L] (or [B] during 1-step decode)
        squeeze_seq = event["action_type"].dim() == 1
        def _e(key: str, default_val: int = 0) -> Tensor:
            t = event.get(key)
            if t is None:
                t = torch.full_like(event["action_type"], default_val)
            return t.unsqueeze(1) if t.dim() == 1 else t

        cont = event.get("continuous")
        if cont is None:
            b, l = _e("action_type").shape
            cont = torch.zeros((b, l, 10), device=event["action_type"].device, dtype=torch.float32)
        elif cont.dim() == 2:
            cont = cont.unsqueeze(1)

        flags = event.get("flags")
        if flags is None:
            b, l = _e("action_type").shape
            flags = torch.zeros((b, l, NUM_MODIFIER_FLAGS), device=event["action_type"].device, dtype=torch.float32)
        elif flags.dim() == 2:
            flags = flags.unsqueeze(1)

        parts = [
            self.dt_emb(_e("dt_bucket")),
            self.period_emb(_e("period")),
            self.phase_emb(_e("poss_phase")),
            self.actor_emb(_e("actor_slot")),
            self.action_emb(_e("action_type")),
            self.restart_emb(_e("restart_type")),
            self.height_emb(_e("pass_height")),
            self.body_emb(_e("body_part")),
            self.start_cell_emb(_e("start_cell")),
            self.end_cell_emb(_e("end_cell")),
            self.end_z_emb(_e("end_z_bin")),
            self.target_emb(_e("target_slot")),
            self.outcome_emb(_e("outcome")),
            self.flags_proj(flags.to(self.flags_proj.weight.dtype)),
            self.cont_proj(cont.to(self.cont_proj[0].weight.dtype)),
        ]
        out = self.fuse(torch.cat(parts, dim=-1))
        return out.squeeze(1) if squeeze_seq else out


class MicroDecoderLayer(nn.Module):
    def __init__(self, d_dec: int, n_heads: int, d_ff: int):
        super().__init__()
        self.norm1 = RMSNorm(d_dec)
        self.attn = GQAAttention(d_dec, n_heads, n_heads, use_rope=False, use_qk_norm=True, gated=False)
        self.norm2 = RMSNorm(d_dec)
        self.ffn = SwiGLU(d_dec, d_ff)

    def forward(
        self,
        x: Tensor,
        past_kv: Optional[Tuple[Tensor, Tensor]] = None,
    ) -> Tuple[Tensor, Optional[Tuple[Tensor, Tensor]]]:
        a, new_kv = self.attn(self.norm1(x), kv_in=None, is_causal=True, past_kv=past_kv)
        x = x + a
        x = x + self.ffn(self.norm2(x))
        return x, new_kv


class IntraEventMicroDecoder(nn.Module):
    """2-layer causal micro-decoder sampling the 7 dependent sub-fields of event e_t given h_t."""

    def __init__(self, cfg: EventTransformerConfig):
        super().__init__()
        self.d_dec = cfg.d_dec
        self.h_proj = nn.Linear(cfg.d_model, cfg.d_dec)
        self.step_pos_emb = nn.Parameter(torch.randn(7, cfg.d_dec) * 0.02)

        self.layers = nn.ModuleList([
            MicroDecoderLayer(cfg.d_dec, cfg.n_dec_heads, cfg.d_dec_ff)
            for _ in range(cfg.n_dec_layers)
        ])
        self.norm_out = RMSNorm(cfg.d_dec)

        # Sub-token feedback embeddings into next micro-step
        self.fb_dt = nn.Embedding(NUM_DT_BUCKETS, cfg.d_dec)
        self.fb_actor = nn.Embedding(NUM_ACTOR_SLOTS, cfg.d_dec)
        self.fb_action = nn.Embedding(NUM_ACTIONS, cfg.d_dec)
        self.fb_mods = nn.Linear(NUM_RESTART_TYPES + NUM_PASS_HEIGHTS + NUM_BODY_PARTS + NUM_MODIFIER_FLAGS, cfg.d_dec)
        self.fb_start = nn.Embedding(NUM_CELL_TOKENS, cfg.d_dec)
        self.fb_end = nn.Embedding(NUM_CELL_TOKENS, cfg.d_dec)

        # 7 Ordered Prediction Heads
        self.head_dt_bucket = nn.Linear(cfg.d_dec, NUM_DT_BUCKETS)
        self.head_dt_res = nn.Linear(cfg.d_dec, 1)
        self.head_actor = nn.Linear(cfg.d_dec, NUM_ACTOR_SLOTS)
        self.head_action = nn.Linear(cfg.d_dec, NUM_ACTIONS)
        self.head_pass_height = nn.Linear(cfg.d_dec, NUM_PASS_HEIGHTS)
        self.head_body_part = nn.Linear(cfg.d_dec, NUM_BODY_PARTS)
        self.head_flags = nn.Linear(cfg.d_dec, NUM_MODIFIER_FLAGS)
        self.head_start_cell = nn.Linear(cfg.d_dec, NUM_CELL_TOKENS)
        self.head_start_offset = nn.Linear(cfg.d_dec, 2)
        self.head_end_cell = nn.Linear(cfg.d_dec, NUM_CELL_TOKENS)
        self.head_end_offset = nn.Linear(cfg.d_dec, 2)
        self.head_end_z = nn.Linear(cfg.d_dec, NUM_END_Z_BINS)
        self.head_target = nn.Linear(cfg.d_dec, NUM_ACTOR_SLOTS)
        self.head_outcome = nn.Linear(cfg.d_dec, NUM_OUTCOMES)
        self.head_aux_analytics = nn.Linear(cfg.d_dec, 4)  # [xG/PSxG, xPass, xT_delta, VAEP_delta]

        # Precomputed constant buffers to avoid per-step GPU->CPU syncs & host allocations
        self.register_buffer("_dt_all_mask", torch.ones((1, NUM_DT_BUCKETS), dtype=torch.bool), persistent=False)
        dt_bnd = torch.zeros((1, NUM_DT_BUCKETS), dtype=torch.bool)
        dt_bnd[0, 0] = True
        self.register_buffer("_dt_boundary_mask", dt_bnd, persistent=False)
        home_shot = torch.zeros((1, NUM_CELL_TOKENS), dtype=torch.bool)
        away_shot = torch.zeros((1, NUM_CELL_TOKENS), dtype=torch.bool)
        for dy in range(7, 13):
            home_shot[0, dy * GRID_X + (GRID_X - 1)] = True
            away_shot[0, dy * GRID_X + 0] = True
        self.register_buffer("_home_shot_cells", home_shot, persistent=False)
        self.register_buffer("_away_shot_cells", away_shot, persistent=False)
        self.register_buffer("_pitch_center_xy", torch.tensor([[60.0, 40.0]], dtype=torch.float32), persistent=False)

    def _run_decoder(self, seq: Tensor) -> Tensor:
        x = seq
        for layer in self.layers:
            x, _ = layer(x, past_kv=None)
        return self.norm_out(x)

    def _run_decoder_step(
        self,
        tok: Tensor,
        past_kvs: Optional[List[Tuple[Tensor, Tensor]]],
    ) -> Tuple[Tensor, List[Tuple[Tensor, Tensor]]]:
        x = tok.unsqueeze(1)
        new_kvs: List[Tuple[Tensor, Tensor]] = []
        for idx, layer in enumerate(self.layers):
            layer_past = past_kvs[idx] if past_kvs is not None else None
            x, kv = layer(x, past_kv=layer_past)
            new_kvs.append(kv)
        return self.norm_out(x).squeeze(1), new_kvs

    def forward_teacher_forced(self, h_t: Tensor, targets: Dict[str, Tensor]) -> Dict[str, Tensor]:
        """Parallel causal forward across the 7 intra-event sub-steps for training loss computation.

        h_t: [B, L, d_model] -> flattened internally to [B*L, d_dec].
        """
        B, L, _ = h_t.shape
        N = B * L
        h0 = self.h_proj(h_t).reshape(N, self.d_dec)

        dt_b = targets["dt_bucket"].reshape(N)
        actor = targets["actor_slot"].reshape(N)
        action = targets["action_type"].reshape(N)
        mods_oh = torch.cat([
            F.one_hot(targets["restart_type"].reshape(N), NUM_RESTART_TYPES).float(),
            F.one_hot(targets["pass_height"].reshape(N), NUM_PASS_HEIGHTS).float(),
            F.one_hot(targets["body_part"].reshape(N), NUM_BODY_PARTS).float(),
            targets["flags"].reshape(N, NUM_MODIFIER_FLAGS).float(),
        ], dim=-1).to(h0.dtype)
        start_c = targets["start_cell"].reshape(N)
        end_c = targets["end_cell"].reshape(N)

        tokens = torch.stack([
            h0,
            h0 + self.fb_dt(dt_b),
            h0 + self.fb_actor(actor),
            h0 + self.fb_action(action),
            h0 + self.fb_mods(mods_oh),
            h0 + self.fb_start(start_c),
            h0 + self.fb_end(end_c),
        ], dim=1) + self.step_pos_emb.unsqueeze(0)  # [N, 7, d_dec]

        dec = self._run_decoder(tokens)  # [N, 7, d_dec]
        s0, s1, s2, s3, s4, s5, s6 = [dec[:, i].view(B, L, -1) for i in range(7)]

        return {
            "dt_bucket_logits": self.head_dt_bucket(s0),
            "dt_res": self.head_dt_res(s0).squeeze(-1),
            "actor_logits": self.head_actor(s1),
            "action_logits": self.head_action(s2),
            "pass_height_logits": self.head_pass_height(s3),
            "body_part_logits": self.head_body_part(s3),
            "flags_logits": self.head_flags(s3),
            "start_cell_logits": self.head_start_cell(s4),
            "start_offset": torch.tanh(self.head_start_offset(s4)) * 2.0,
            "end_cell_logits": self.head_end_cell(s5),
            "end_offset": torch.tanh(self.head_end_offset(s5)) * 2.0,
            "end_z_logits": self.head_end_z(s5),
            "target_logits": self.head_target(s5),
            "outcome_logits": self.head_outcome(s6),
            "aux_analytics": self.head_aux_analytics(s6),
        }

    @staticmethod
    def _sample_masked(
        logits: Tensor,
        mask: Optional[Tensor],
        temperature: float = 1.0,
        top_p: float = 0.95,
        generator: Optional[torch.Generator] = None,
    ) -> LongTensor:
        scaled = logits.float() / max(temperature, 1e-4)
        if mask is not None:
            scaled = scaled.masked_fill(~mask, -1e9)
        probs = F.softmax(scaled, dim=-1)
        if top_p < 1.0:
            sorted_probs, sorted_idx = torch.sort(probs, descending=True, dim=-1)
            cum_probs = torch.cumsum(sorted_probs, dim=-1)
            remove = (cum_probs - sorted_probs) > top_p
            sorted_probs = sorted_probs.masked_fill(remove, 0.0)
            sorted_probs = sorted_probs / sorted_probs.sum(dim=-1, keepdim=True).clamp(min=1e-9)
            draw = torch.multinomial(sorted_probs, num_samples=1, generator=generator)
            return sorted_idx.gather(-1, draw).squeeze(-1)
        return torch.multinomial(probs, num_samples=1, generator=generator).squeeze(-1)

    def sample_step(
        self,
        h_t: Tensor,
        fsm_masks: FSMMasks,
        poss_team: LongTensor,
        outcome_override_fn: Optional[Callable[[Dict[str, Tensor], Tensor], Tensor]] = None,
        outcome_mask_fn: Optional[Callable[[LongTensor], Tensor]] = None,
        temperature: float = 1.0,
        top_p: float = 0.95,
        generator: Optional[torch.Generator] = None,
    ) -> Dict[str, Tensor]:
        """Autoregressively sample all 7 dependent sub-fields of event e_t under hard FSM masks."""
        B = h_t.shape[0]
        dev = h_t.device
        h0 = self.h_proj(h_t)  # [B, d_dec]

        # Step 0: dt_bucket & dt_res
        d0, dec_kvs = self._run_decoder_step(h0 + self.step_pos_emb[0], None)
        dt_logits = self.head_dt_bucket(d0)
        dt_res = self.head_dt_res(d0).squeeze(-1).clamp(-0.5, 0.5)
        dt_mask = torch.where(
            fsm_masks.force_period_boundary.unsqueeze(-1),
            self._dt_boundary_mask.expand(B, -1),
            self._dt_all_mask.expand(B, -1),
        )
        dt_bucket = self._sample_masked(dt_logits, dt_mask, temperature, top_p, generator)
        dt_seconds = decode_dt_seconds(dt_bucket, dt_res)
        dt_seconds = torch.where(fsm_masks.force_period_boundary, torch.zeros_like(dt_seconds), dt_seconds)

        # Step 1: actor_slot (0..22) masked by actor_mask_23
        d1, dec_kvs = self._run_decoder_step(h0 + self.fb_dt(dt_bucket) + self.step_pos_emb[1], dec_kvs)
        actor_slot = self._sample_masked(self.head_actor(d1), fsm_masks.actor_mask_23, temperature, top_p, generator)

        # Step 2: action_type masked by action_mask + spatial_log_prior[:, :NUM_ACTIONS]
        d2, dec_kvs = self._run_decoder_step(h0 + self.fb_actor(actor_slot) + self.step_pos_emb[2], dec_kvs)
        action_logits = self.head_action(d2) + fsm_masks.spatial_log_prior[:, :NUM_ACTIONS].to(d2.dtype)
        action_type = self._sample_masked(action_logits, fsm_masks.action_mask, temperature, top_p, generator)

        # Step 3: modifiers (restart forced by FSM, pass_height, body_part masked, 8 flags)
        d3, dec_kvs = self._run_decoder_step(h0 + self.fb_action(action_type) + self.step_pos_emb[3], dec_kvs)
        restart_type = fsm_masks.restart_type_forced
        pass_height = self._sample_masked(self.head_pass_height(d3), None, temperature, top_p, generator)
        is_pass = action_type == ActionType.PASS
        pass_height = torch.where(is_pass, pass_height, torch.zeros_like(pass_height))
        body_part = self._sample_masked(self.head_body_part(d3), fsm_masks.body_part_mask, temperature, top_p, generator)
        flag_probs = torch.sigmoid(self.head_flags(d3)).float()
        flags = torch.bernoulli(flag_probs, generator=generator).long()

        mods_oh = torch.cat([
            F.one_hot(restart_type, NUM_RESTART_TYPES).float(),
            F.one_hot(pass_height, NUM_PASS_HEIGHTS).float(),
            F.one_hot(body_part, NUM_BODY_PARTS).float(),
            flags.float(),
        ], dim=-1).to(h0.dtype)

        # Step 4: start_xy masked by start_cell_mask
        d4, dec_kvs = self._run_decoder_step(h0 + self.fb_mods(mods_oh) + self.step_pos_emb[4], dec_kvs)
        start_cell = self._sample_masked(self.head_start_cell(d4), fsm_masks.start_cell_mask, temperature, top_p, generator)
        start_off = torch.tanh(self.head_start_offset(d4)) * 2.0
        center_xy = self._pitch_center_xy
        start_xy = cell_to_center_xy(start_cell) + start_off
        start_xy = torch.where((start_cell >= NUM_PITCH_CELLS).unsqueeze(-1), center_xy, start_xy)
        start_xy[:, 0].clamp_(0.0, PITCH_LENGTH)
        start_xy[:, 1].clamp_(0.0, PITCH_WIDTH)

        # Step 5: end_xy, end_z, target_slot masked by end_cell_mask & target_mask
        d5, dec_kvs = self._run_decoder_step(h0 + self.fb_start(start_cell) + self.step_pos_emb[5], dec_kvs)
        is_shot = action_type == ActionType.SHOT
        home_poss = poss_team == 1
        shot_cells = torch.where(home_poss.unsqueeze(-1), self._home_shot_cells, self._away_shot_cells)
        end_cell_mask = torch.where(is_shot.unsqueeze(-1), shot_cells, fsm_masks.end_cell_mask)

        end_cell = self._sample_masked(self.head_end_cell(d5), end_cell_mask, temperature, top_p, generator)
        end_off = torch.tanh(self.head_end_offset(d5)) * 2.0
        end_xy = cell_to_center_xy(end_cell) + end_off
        is_oob_cell = end_cell == CELL_OUT_OF_BOUNDS
        end_xy = torch.where(is_oob_cell.unsqueeze(-1), start_xy, end_xy)
        end_xy = torch.where((end_cell == CELL_SYSTEM).unsqueeze(-1), center_xy, end_xy)
        end_xy[:, 0].clamp_(0.0, PITCH_LENGTH)
        end_xy[:, 1].clamp_(0.0, PITCH_WIDTH)

        end_z_bin = self._sample_masked(self.head_end_z(d5), None, temperature, top_p, generator)
        end_z = torch.where(is_shot, end_z_bin.float() * (4.0 / NUM_END_Z_BINS), torch.zeros(B, device=dev))

        # Target slot mapped to global 1..22 for passes
        target_mask_23 = torch.zeros((B, NUM_ACTOR_SLOTS), dtype=torch.bool, device=dev)
        target_logits = self.head_target(d5)
        target_mask_23[:, 1:12] = torch.where(home_poss.unsqueeze(-1), fsm_masks.target_mask, False)
        target_mask_23[:, 12:23] = torch.where((~home_poss).unsqueeze(-1), fsm_masks.target_mask, False)
        valid_no_self = target_mask_23.clone()
        valid_no_self.scatter_(1, actor_slot.unsqueeze(-1), False)
        target_mask_23 = torch.where(valid_no_self.any(dim=-1, keepdim=True), valid_no_self, target_mask_23)
        target_mask_23 = torch.where(is_pass.unsqueeze(-1), target_mask_23, False)
        target_mask_23[:, 0] = ~is_pass
        t_prior = fsm_masks.spatial_log_prior[:, NUM_ACTIONS:].to(d5.dtype)
        target_logits = target_logits.clone()
        target_logits[:, 1:12] = target_logits[:, 1:12] + torch.where(home_poss.unsqueeze(-1), t_prior, 0.0)
        target_logits[:, 12:23] = target_logits[:, 12:23] + torch.where((~home_poss).unsqueeze(-1), t_prior, 0.0)
        target_slot = self._sample_masked(target_logits, target_mask_23, temperature, top_p, generator)

        # Step 6: outcome (masked by action_type + calibrated via Module D override callback!)
        d6, _ = self._run_decoder_step(h0 + self.fb_end(end_cell) + self.step_pos_emb[6], dec_kvs)
        outcome_logits = self.head_outcome(d6)
        aux_analytics = self.head_aux_analytics(d6)
        outcome_mask = outcome_mask_fn(action_type) if outcome_mask_fn is not None else None

        proposed_event = {
            "dt_bucket": dt_bucket,
            "dt_seconds": dt_seconds,
            "actor_slot": actor_slot,
            "action_type": action_type,
            "restart_type": restart_type,
            "pass_height": pass_height,
            "body_part": body_part,
            "flags": flags,
            "start_cell": start_cell,
            "start_xy": start_xy,
            "end_cell": end_cell,
            "end_xy": end_xy,
            "end_z_bin": end_z_bin,
            "end_z": end_z,
            "target_slot": target_slot,
        }
        if outcome_override_fn is not None:
            outcome_logits = outcome_override_fn(proposed_event, outcome_logits)

        outcome = self._sample_masked(outcome_logits, outcome_mask, temperature, top_p, generator)
        proposed_event["outcome"] = outcome
        proposed_event["aux_analytics"] = aux_analytics
        return proposed_event


class PixelPitchEventTF(nn.Module):
    """PixelPitchAI v2 Custom Football Event Transformer (`Base-112M` default)."""

    def __init__(self, config: Optional[EventTransformerConfig] = None):
        super().__init__()
        self.config = config or EventTransformerConfig.base_112m()
        cfg = self.config

        self.memory_encoder = TacticalMemoryEncoder(cfg)
        self.event_embedder = FactoredEventEmbedder(cfg)
        self.adaln_trunk = nn.Sequential(
            nn.Linear(cfg.state_dim, cfg.adaln_trunk_dim),
            nn.SiLU(),
            nn.Linear(cfg.adaln_trunk_dim, cfg.adaln_trunk_dim),
        )
        self.layers = nn.ModuleList([EventTransformerBlock(cfg) for _ in range(cfg.n_layers)])
        self.norm_final = RMSNorm(cfg.d_model)
        self.micro_decoder = IntraEventMicroDecoder(cfg)

        # Learned conditioning dropout embeddings for Stage 2 CFG training
        self.null_tactic_emb = nn.Parameter(torch.zeros(cfg.team_tactics_dim))
        self.null_player_emb = nn.Parameter(torch.zeros(cfg.player_cond_dim))

    def count_parameters(self) -> Dict[str, int]:
        backbone = sum(p.numel() for p in self.layers.parameters()) + sum(p.numel() for p in self.norm_final.parameters())
        memory = sum(p.numel() for p in self.memory_encoder.parameters())
        embedder = sum(p.numel() for p in self.event_embedder.parameters())
        adaln = sum(p.numel() for p in self.adaln_trunk.parameters())
        decoder = sum(p.numel() for p in self.micro_decoder.parameters())
        total = sum(p.numel() for p in self.parameters())
        return {
            "total": total,
            "backbone": backbone,
            "memory_encoder": memory,
            "event_embedder": embedder,
            "adaln_trunk": adaln,
            "micro_decoder": decoder,
        }

    def encode_memory_and_state(
        self,
        player_cond: FloatTensor,
        s_t: FloatTensor,
        team_tactics: Optional[FloatTensor] = None,
        global_ball_ctx: Optional[FloatTensor] = None,
        drop_player_mask: Optional[Tensor] = None,
        drop_tactic_mask: Optional[Tensor] = None,
    ) -> Tuple[Tensor, Tensor]:
        """Compute M_t [B, 28, d_model] and AdaLN trunk state c_t [B, adaln_trunk_dim]."""
        if drop_player_mask is not None:
            null_p = self.null_player_emb.view(1, 1, -1).expand_as(player_cond)
            player_cond = torch.where(drop_player_mask.view(-1, 1, 1), null_p, player_cond)
        if team_tactics is not None and drop_tactic_mask is not None:
            null_t = self.null_tactic_emb.view(1, 1, -1).expand_as(team_tactics)
            team_tactics = torch.where(drop_tactic_mask.view(-1, 1, 1), null_t, team_tactics)

        mem = self.memory_encoder(player_cond, team_tactics=team_tactics, global_ball_ctx=global_ball_ctx)
        c_t = self.adaln_trunk(s_t)
        return mem, c_t

    def forward_sequence(
        self,
        prev_events: Dict[str, Tensor],
        targets: Dict[str, Tensor],
        player_cond: FloatTensor,
        s_t: FloatTensor,
        team_tactics: Optional[FloatTensor] = None,
        global_ball_ctx: Optional[FloatTensor] = None,
        drop_player_mask: Optional[Tensor] = None,
        drop_tactic_mask: Optional[Tensor] = None,
    ) -> Dict[str, Tensor]:
        """Teacher-forced training forward pass across full-match packed sequences [B, L]."""
        mem, c_t = self.encode_memory_and_state(
            player_cond, s_t, team_tactics, global_ball_ctx, drop_player_mask, drop_tactic_mask
        )
        if c_t.dim() == 2:
            c_t_seq = c_t.unsqueeze(1).expand(-1, prev_events["action_type"].shape[1], -1)
        else:
            c_t_seq = c_t

        x = self.event_embedder(prev_events)
        for layer in self.layers:
            x, _ = layer(x, mem=mem, c_t=c_t_seq, past_kv=None, pos_offset=0)
        h = self.norm_final(x)
        return self.micro_decoder.forward_teacher_forced(h, targets)

    @torch.inference_mode()
    def step_decode(
        self,
        prev_event: Dict[str, Tensor],
        player_cond: Optional[FloatTensor],
        s_t: FloatTensor,
        fsm_masks: FSMMasks,
        poss_team: LongTensor,
        past_kvs: Optional[List[Tuple[Tensor, Tensor]]] = None,
        pos_offset: int = 0,
        team_tactics: Optional[FloatTensor] = None,
        global_ball_ctx: Optional[FloatTensor] = None,
        mem_override: Optional[FloatTensor] = None,
        outcome_override_fn: Optional[Callable[[Dict[str, Tensor], Tensor], Tensor]] = None,
        outcome_mask_fn: Optional[Callable[[LongTensor], Tensor]] = None,
        temperature: float = 1.0,
        top_p: float = 0.95,
        generator: Optional[torch.Generator] = None,
    ) -> Tuple[Dict[str, Tensor], List[Tuple[Tensor, Tensor]]]:
        """Single-step KV-cached autoregressive decode for B parallel matches."""
        use_bf16 = s_t.is_cuda and torch.cuda.is_bf16_supported()
        with torch.amp.autocast("cuda", enabled=use_bf16, dtype=torch.bfloat16):
            if mem_override is not None:
                mem = mem_override
                c_t = self.adaln_trunk(s_t)
            else:
                assert player_cond is not None, "Either player_cond [B,22,128] or mem_override [B,28,D] is required."
                mem, c_t = self.encode_memory_and_state(player_cond, s_t, team_tactics, global_ball_ctx)
            x = self.event_embedder(prev_event).unsqueeze(1)  # [B, 1, d_model]

            new_kvs: List[Tuple[Tensor, Tensor]] = []
            for idx, layer in enumerate(self.layers):
                layer_past = past_kvs[idx] if past_kvs is not None else None
                x, kv = layer(x, mem=mem, c_t=c_t, past_kv=layer_past, pos_offset=pos_offset)
                new_kvs.append(kv)

            h_t = self.norm_final(x).squeeze(1).float()  # [B, d_model]

        event = self.micro_decoder.sample_step(
            h_t=h_t,
            fsm_masks=fsm_masks,
            poss_team=poss_team,
            outcome_override_fn=outcome_override_fn,
            outcome_mask_fn=outcome_mask_fn,
            temperature=temperature,
            top_p=top_p,
            generator=generator,
        )
        return event, new_kvs

    @torch.no_grad()
    def forward_backbone_static(
        self,
        prev_event: Dict[str, Tensor],
        player_cond: FloatTensor,
        s_t: FloatTensor,
        pos_tensor: LongTensor,
        static_kvs: List[Tuple[Tensor, Tensor]],
        attn_mask: Tensor,
        team_tactics: Optional[FloatTensor] = None,
        global_ball_ctx: Optional[FloatTensor] = None,
    ) -> Tensor:
        """Static-shape single-step backbone forward pass for CUDA Graph capture/replay."""
        use_bf16 = s_t.is_cuda and torch.cuda.is_bf16_supported()
        with torch.amp.autocast("cuda", enabled=use_bf16, dtype=torch.bfloat16):
            mem, c_t = self.encode_memory_and_state(player_cond, s_t, team_tactics, global_ball_ctx)
            x = self.event_embedder(prev_event).unsqueeze(1)
            for idx, layer in enumerate(self.layers):
                x, _ = layer(
                    x,
                    mem=mem,
                    c_t=c_t,
                    past_kv=None,
                    pos_offset=pos_tensor,
                    static_kv=static_kvs[idx],
                    attn_mask=attn_mask,
                )
            return self.norm_final(x).squeeze(1).float()


class StaticBackboneCUDAGraphRunner:
    """Captures and replays PixelPitchEventTF's 12-layer backbone step as a single CUDA Graph."""

    def __init__(
        self,
        model: PixelPitchEventTF,
        batch_size: int,
        max_cache_len: int,
        sample_prev_event: Dict[str, Tensor],
        sample_player_cond: FloatTensor,
        sample_s_t: FloatTensor,
        sample_team_tactics: FloatTensor,
    ):
        self.model = model
        self.batch_size = batch_size
        self.max_cache_len = max_cache_len
        device = sample_s_t.device
        use_bf16 = device.type == "cuda" and torch.cuda.is_bf16_supported()
        cache_dtype = torch.bfloat16 if use_bf16 else torch.float32

        n_kv_heads = model.config.n_kv_heads
        head_dim = model.config.d_model // model.config.n_heads
        for layer in model.layers:
            if layer.self_attn.rotary is not None:
                layer.self_attn.rotary.ensure_cache(max(max_cache_len, model.config.max_seq_len), device, cache_dtype)

        self.prev_event_buf: Dict[str, Tensor] = {
            k: torch.zeros_like(v, device=device) for k, v in sample_prev_event.items()
        }
        self.player_cond_buf = torch.zeros_like(sample_player_cond, device=device)
        self.s_t_buf = torch.zeros_like(sample_s_t, device=device)
        self.team_tactics_buf = torch.zeros_like(sample_team_tactics, device=device)
        self.pos_tensor = torch.zeros((1,), dtype=torch.long, device=device)
        self.attn_mask = torch.zeros((1, 1, 1, max_cache_len), dtype=torch.bool, device=device)
        self.attn_mask[0, 0, 0, 0] = True
        self.static_kvs: List[Tuple[Tensor, Tensor]] = [
            (
                torch.zeros((batch_size, n_kv_heads, max_cache_len, head_dim), dtype=cache_dtype, device=device),
                torch.zeros((batch_size, n_kv_heads, max_cache_len, head_dim), dtype=cache_dtype, device=device),
            )
            for _ in model.layers
        ]

        # Warmup on a side stream before CUDA Graph capture
        with torch.no_grad():
            warmup_stream = torch.cuda.Stream(device=device)
            warmup_stream.wait_stream(torch.cuda.current_stream(device))
            with torch.cuda.stream(warmup_stream):
                for _ in range(3):
                    _ = self.model.forward_backbone_static(
                        prev_event=self.prev_event_buf,
                        player_cond=self.player_cond_buf,
                        s_t=self.s_t_buf,
                        pos_tensor=self.pos_tensor,
                        static_kvs=self.static_kvs,
                        attn_mask=self.attn_mask,
                        team_tactics=self.team_tactics_buf,
                    )
            torch.cuda.current_stream(device).wait_stream(warmup_stream)

            self.graph = torch.cuda.CUDAGraph()
            with torch.cuda.graph(self.graph):
                self.h_t_out = self.model.forward_backbone_static(
                    prev_event=self.prev_event_buf,
                    player_cond=self.player_cond_buf,
                    s_t=self.s_t_buf,
                    pos_tensor=self.pos_tensor,
                    static_kvs=self.static_kvs,
                    attn_mask=self.attn_mask,
                    team_tactics=self.team_tactics_buf,
                )

            self.reset()

    def reset(self) -> None:
        self.attn_mask.zero_()
        for k_buf, v_buf in self.static_kvs:
            k_buf.zero_()
            v_buf.zero_()

    @torch.no_grad()
    def run(
        self,
        prev_event: Dict[str, Tensor],
        player_cond: FloatTensor,
        s_t: FloatTensor,
        team_tactics: FloatTensor,
        step_idx: int,
    ) -> Tensor:
        for k, buf in self.prev_event_buf.items():
            buf.copy_(prev_event[k])
        self.player_cond_buf.copy_(player_cond)
        self.s_t_buf.copy_(s_t)
        self.team_tactics_buf.copy_(team_tactics)
        self.pos_tensor.fill_(step_idx)
        self.attn_mask[0, 0, 0, step_idx] = True
        self.graph.replay()
        return self.h_t_out
