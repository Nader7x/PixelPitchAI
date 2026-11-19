"""Module B: Vectorized Tactical State & Player Capabilities Engine.

Bridges Approach A (learned player capability/style vectors + Transformer conditioning)
and Approach B (explicit substitution laws, fatigue physics, card compaction, and
formation/anchor geometry) across batched GPU/CPU timelines [B, 22, ...].
"""

from __future__ import annotations

import os
import pickle
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Literal, Optional, Sequence, Tuple

import torch
import torch.nn as nn
from pydantic import BaseModel, Field

PLAYER_PROFILES_FILENAME: str = "player_profiles_v2.parquet"
TACTICAL_ENGINE_FILENAME: str = "tactical_state_engine.pt"

from .config_schema import (
    ATTRIBUTE_NAMES_24D,
    GK_ATTRIBUTE_NAMES_8D,
    ID_TO_ROLE,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    ROLE_PRIOR_VECTORS,
    ROLE_TO_ID,
    TACTICAL_SLIDER_NAMES,
    FormationConfig,
    LineupSlotConfig,
    MatchConfigurationV2,
    PlayerAttributeOverride,
    PlayerRole,
    TacticalPhase,
    TeamMatchConfig,
    resolve_formation_anchors,
)
from .spatial_solver import NUM_ACTIONS_DEFAULT, OffBallSpatialSolver, SpatialStepOutput


class PlayerCapabilityRecord(BaseModel):
    """Historical Empirical-Bayes player capability & style profile (from Pillar 1)."""

    player_id: int = Field(..., ge=1)
    display_name: str = "Unknown Player"
    attributes_24d: List[float] = Field(default_factory=lambda: [0.5] * 24, min_length=24, max_length=24)
    sigma_24d: List[float] = Field(default_factory=lambda: [0.06] * 24, min_length=24, max_length=24)
    gk_attributes_8d: List[float] = Field(default_factory=lambda: [0.5] * 8, min_length=8, max_length=8)
    finishing_log_odds: float = 0.0
    latent_style_64d: List[float] = Field(default_factory=lambda: [0.0] * 64, min_length=64, max_length=64)
    minutes_played: float = Field(1800.0, ge=0.0)
    role_suitability_scores: List[float] = Field(default_factory=lambda: [0.5] * 22, min_length=22, max_length=22)


def _deterministic_style_from_attrs(attr_24d: Sequence[float], role: PlayerRole) -> List[float]:
    """Synthesize an L2-normalized 64-dim style embedding when none is cached."""
    role_id = ROLE_TO_ID[role]
    vec = torch.zeros(64, dtype=torch.float32)
    a = torch.tensor(attr_24d, dtype=torch.float32)
    vec[:24] = a - 0.5
    vec[24:40] = torch.tensor(ROLE_PRIOR_VECTORS[role], dtype=torch.float32) - 0.5
    vec[40 + (role_id % 22)] = 0.65
    norm = torch.linalg.norm(vec).clamp_min(1e-6)
    return (vec / norm).tolist()


def _default_record_for_slot(
    player_id: int,
    role: PlayerRole,
    display_name: str = "Synthesized Player",
) -> PlayerCapabilityRecord:
    """Create a sensible positional-prior PlayerCapabilityRecord for cold-start or testing."""
    role_prior = ROLE_PRIOR_VECTORS[role]
    # Map role prior tendencies gently into [0.40, 0.78] neutral-plus capability baseline
    attrs = [0.50] * 24
    attrs[0] = 0.52 + 0.18 * role_prior[15]   # short_pass_accuracy
    attrs[1] = 0.42 + 0.30 * role_prior[3]    # progressive_pass_volume
    attrs[2] = 0.40 + 0.30 * role_prior[4]    # long_switch_propensity
    attrs[5] = 0.48 + 0.22 * role_prior[15]   # press_resistance
    attrs[7] = 0.45 + 0.25 * role_prior[6]    # dribble_success_skill
    attrs[8] = 0.42 + 0.28 * role_prior[5]    # ball_carry_progression
    attrs[10] = 0.38 + 0.32 * role_prior[8]   # shot_volume_box
    attrs[15] = 0.42 + 0.30 * role_prior[9]   # pressing_workrate
    attrs[17] = 0.42 + 0.30 * role_prior[10]  # interception_reading
    attrs[18] = 0.40 + 0.32 * role_prior[12]  # aerial_duel_win_rate
    attrs[21] = 0.55 + 0.20 * role_prior[14]  # stamina_endurance
    attrs[22] = 0.48 + 0.26 * role_prior[14]  # sprint_recovery_capacity

    gk_attrs = [0.50] * 8
    if role == PlayerRole.SWEEPER_KEEPER:
        gk_attrs = [0.55, 0.55, 0.78, 0.60, 0.76, 0.62, 0.50, 0.14]
    elif role == PlayerRole.SHOT_STOPPER_GK:
        gk_attrs = [0.65, 0.68, 0.28, 0.64, 0.35, 0.58, 0.50, 0.06]

    suitability = [0.35] * 22
    suitability[ROLE_TO_ID[role]] = 0.90

    return PlayerCapabilityRecord(
        player_id=max(1, player_id),
        display_name=display_name,
        attributes_24d=[round(max(0.05, min(0.95, x)), 4) for x in attrs],
        sigma_24d=[0.06] * 24,
        gk_attributes_8d=gk_attrs,
        finishing_log_odds=0.0,
        latent_style_64d=_deterministic_style_from_attrs(attrs, role),
        minutes_played=1800.0,
        role_suitability_scores=suitability,
    )


class PlayerProfileRegistry:
    """In-memory or Parquet-backed store resolving player IDs & custom inline overrides."""

    def __init__(
        self,
        records: Optional[Dict[int, PlayerCapabilityRecord]] = None,
        models_dir: Optional[str | Path] = None,
        auto_load: bool = True,
    ) -> None:
        self._records: Dict[int, PlayerCapabilityRecord] = dict(records or {})
        self.loaded_from_path: Optional[str] = None
        if not self._records and auto_load:
            base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
            pq_path = base_dir / PLAYER_PROFILES_FILENAME
            if pq_path.is_file():
                loaded = self.from_parquet(pq_path)
                self._records = loaded._records
                self.loaded_from_path = str(pq_path)

    def __len__(self) -> int:
        return len(self._records)

    def register(self, record: PlayerCapabilityRecord) -> None:
        self._records[record.player_id] = record

    def get(self, player_id: int) -> Optional[PlayerCapabilityRecord]:
        return self._records.get(player_id)

    def save_parquet(self, parquet_path: str | Path) -> Path:
        """Persist registered player capability records to Parquet (with pickle fallback)."""
        target = Path(parquet_path)
        target.parent.mkdir(parents=True, exist_ok=True)
        rows = [rec.model_dump() for rec in self._records.values()]
        try:
            import pandas as pd
            pd.DataFrame(rows).to_parquet(target, index=False)
        except Exception:
            try:
                import duckdb  # type: ignore[import-not-found]
                import pandas as pd
                df_tmp = pd.DataFrame(rows)
                con = duckdb.connect()
                con.register("df_tmp", df_tmp)
                escaped = str(target).replace("'", "''")
                con.execute(f"COPY df_tmp TO '{escaped}' (FORMAT PARQUET)")
                con.close()
            except Exception:
                with open(target, "wb") as f:
                    pickle.dump(rows, f)
        self.loaded_from_path = str(target)
        return target

    @classmethod
    def from_parquet(cls, parquet_path: str | Path) -> "PlayerProfileRegistry":
        """Load player_profiles_v2.parquet exported by Pillar 1 (statsbombapi_v2)."""
        target = Path(parquet_path)
        try:
            import pandas as pd
            df = pd.read_parquet(target)
            rows = df.to_dict(orient="records")
        except Exception:
            try:
                import duckdb  # type: ignore[import-not-found]
                rel = duckdb.read_parquet(str(target))
                cols = rel.columns
                rows = [dict(zip(cols, rec)) for rec in rel.fetchall()]
            except Exception:
                with open(target, "rb") as f:
                    rows = pickle.load(f)

        def _as_list(val: Any, fallback: List[float]) -> List[float]:
            if val is None:
                return list(fallback)
            return val.tolist() if hasattr(val, "tolist") else list(val)

        records: Dict[int, PlayerCapabilityRecord] = {}
        for row in rows:
            pid = int(row["player_id"])
            records[pid] = PlayerCapabilityRecord(
                player_id=pid,
                display_name=str(row.get("display_name") or f"Player_{pid}"),
                attributes_24d=_as_list(row.get("attributes_24d"), [0.5] * 24),
                sigma_24d=_as_list(row.get("sigma_24d"), [0.05] * 24),
                gk_attributes_8d=_as_list(row.get("gk_attributes_8d"), [0.5] * 8),
                finishing_log_odds=float(row.get("finishing_log_odds") or 0.0),
                latent_style_64d=_as_list(row.get("latent_style_64d"), [0.0] * 64),
                minutes_played=float(row.get("minutes_played") or 900.0),
                role_suitability_scores=_as_list(row.get("role_suitability_scores"), [0.5] * 22),
            )
        inst = cls(records, auto_load=False)
        inst.loaded_from_path = str(target)
        return inst

    def resolve_profile(
        self,
        player_id: Optional[int],
        role: PlayerRole,
        inline_override: Optional[PlayerAttributeOverride] = None,
    ) -> PlayerCapabilityRecord:
        """Resolve a base record from registry/priors and apply any counterfactual override."""
        base_id = player_id
        if inline_override is not None and inline_override.clone_from_player_id is not None:
            base_id = inline_override.clone_from_player_id

        if base_id is not None and base_id in self._records:
            base = self._records[base_id].model_copy(deep=True)
        else:
            base = _default_record_for_slot(
                player_id=base_id or 999999,
                role=role,
                display_name=inline_override.display_name if inline_override else f"Player_{base_id or 'custom'}",
            )

        if inline_override is None:
            return base

        attrs = list(base.attributes_24d)
        if inline_override.attributes_24d:
            for k, v in inline_override.attributes_24d.items():
                attrs[ATTRIBUTE_NAMES_24D.index(k)] = float(v)
        if inline_override.attribute_deltas:
            for k, dv in inline_override.attribute_deltas.items():
                idx = ATTRIBUTE_NAMES_24D.index(k)
                attrs[idx] = max(0.0, min(1.0, attrs[idx] + float(dv)))

        gk_attrs = list(base.gk_attributes_8d)
        if inline_override.gk_attributes_8d:
            for k, v in inline_override.gk_attributes_8d.items():
                gk_attrs[GK_ATTRIBUTE_NAMES_8D.index(k)] = float(v)

        style = (
            list(inline_override.latent_style_64d)
            if inline_override.latent_style_64d is not None
            else _deterministic_style_from_attrs(attrs, role)
        )
        finishing_lo = (
            float(inline_override.finishing_log_odds)
            if inline_override.finishing_log_odds is not None
            else base.finishing_log_odds
        )

        return PlayerCapabilityRecord(
            player_id=player_id or base.player_id,
            display_name=inline_override.display_name,
            attributes_24d=attrs,
            sigma_24d=base.sigma_24d,
            gk_attributes_8d=gk_attrs,
            finishing_log_odds=finishing_lo,
            latent_style_64d=style,
            minutes_played=base.minutes_played,
            role_suitability_scores=base.role_suitability_scores,
        )


@dataclass
class _TeamSubRuntime:
    """Lightweight per-batch team metadata needed for scripted/rule-based subs."""

    policy_mode: str
    max_subs: int
    max_windows: int
    stamina_threshold: float
    yellow_press_threshold: float
    scripted_rules: List[Dict[str, Any]] = field(default_factory=list)
    executed_rule_indices: set[int] = field(default_factory=set)
    last_sub_window_clock: float = -1.0


@dataclass
class BatchTacticalState:
    """Contiguous tensor state for B parallel matches across 22 active pitch slots."""

    positions: torch.Tensor               # [B, 22, 2]
    velocities: torch.Tensor              # [B, 22, 2]
    stamina: torch.Tensor                 # [B, 22]
    yellow_cards: torch.Tensor            # [B, 22] bool
    red_cards: torch.Tensor               # [B, 22] bool
    active_mask: torch.Tensor             # [B, 22] bool
    player_ids: torch.Tensor              # [B, 22] int64
    base_attr_24d: torch.Tensor           # [B, 22, 24]
    gk_attr_8d: torch.Tensor              # [B, 22, 8]
    finishing_log_odds: torch.Tensor      # [B, 22]
    style_64d: torch.Tensor               # [B, 22, 64]
    role_ids: torch.Tensor                # [B, 22] int64
    role_priors_16d: torch.Tensor         # [B, 22, 16]
    player_instr_5d: torch.Tensor         # [B, 22, 5]
    team_tactics_10d: torch.Tensor        # [B, 2, 10]
    effective_tactics_10d: torch.Tensor   # [B, 2, 10]
    phase_anchors: torch.Tensor           # [B, 22, 4, 5]
    subs_used: torch.Tensor               # [B, 2] int64
    sub_windows_used: torch.Tensor        # [B, 2] int64
    bench_used_mask: torch.Tensor         # [B, 2, 12] bool
    bench_valid_mask: torch.Tensor        # [B, 2, 12] bool
    bench_player_ids: torch.Tensor        # [B, 2, 12] int64
    bench_attr_24d: torch.Tensor          # [B, 2, 12, 24]
    bench_gk_attr_8d: torch.Tensor        # [B, 2, 12, 8]
    bench_finishing_lo: torch.Tensor      # [B, 2, 12]
    bench_style_64d: torch.Tensor         # [B, 2, 12, 64]
    bench_role_ids: torch.Tensor          # [B, 2, 12] int64
    bench_fitness: torch.Tensor           # [B, 2, 12]
    clock_seconds: torch.Tensor           # [B]
    period: torch.Tensor                  # [B] int64 (1=1H, 2=HT, 3=2H, 6=FT)
    score: torch.Tensor                   # [B, 2] int64
    possessing_team: torch.Tensor         # [B] int64 (0=Home, 1=Away)
    ball_xy: torch.Tensor                 # [B, 2]
    ball_z: torch.Tensor                  # [B]
    seconds_since_turnover: torch.Tensor  # [B]
    poss_duration_sec: torch.Tensor       # [B]
    rolling_ppda: torch.Tensor            # [B, 2]
    rolling_momentum: torch.Tensor        # [B]
    halftime_recovered: torch.Tensor      # [B] bool
    sub_runtimes: List[Tuple[_TeamSubRuntime, _TeamSubRuntime]] = field(default_factory=list)


@dataclass
class TacticalStepResult:
    """Outputs emitted by TacticalStateEngine at every step for Modules C, D, and E."""

    state: BatchTacticalState
    spatial: SpatialStepOutput
    player_cond: torch.Tensor     # float32[B, 22, 128]
    memory_tokens: torch.Tensor   # float32[B, 28, 768] (M_t)
    global_state: torch.Tensor    # float32[B, 64] (s_t)
    actor_mask: torch.Tensor      # bool[B, 22]
    executed_subs: List[Dict[str, Any]] = field(default_factory=list)


class TacticalStateEngine(nn.Module):
    """Stateful GPU/CPU Module B engine assembling M_t, s_t, player_cond & spatial tensors."""

    def __init__(
        self,
        profile_registry: Optional[PlayerProfileRegistry] = None,
        num_actions: int = NUM_ACTIONS_DEFAULT,
        d_model: int = 768,
        models_dir: Optional[str | Path] = None,
        auto_load: bool = True,
    ) -> None:
        super().__init__()
        base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
        self.models_dir = str(base_dir)
        self.registry = profile_registry or PlayerProfileRegistry(models_dir=base_dir, auto_load=auto_load)
        self.num_actions = num_actions
        self.d_model = d_model

        self.spatial_solver = OffBallSpatialSolver(
            num_actions=num_actions,
            models_dir=base_dir,
            auto_load=auto_load,
        )

        # Module C conditioning projection heads (128 -> 512 -> 768)
        self.slot_mem_proj = nn.Sequential(
            nn.Linear(128, 512),
            nn.SiLU(),
            nn.Linear(512, d_model),
        )
        self.slot_pos_emb = nn.Embedding(22, d_model)
        self.team_mem_proj = nn.Sequential(
            nn.Linear(26, 256),
            nn.SiLU(),
            nn.Linear(256, d_model),
        )
        self.ctx_mem_proj = nn.Linear(16, d_model)
        self.ctx_token_queries = nn.Parameter(torch.randn(4, d_model) * 0.02)
        self.mem_norm = nn.LayerNorm(d_model)

        self.weights_loaded: bool = bool(
            self.spatial_solver.weights_loaded or self.registry.loaded_from_path is not None
        )
        if auto_load and (base_dir / TACTICAL_ENGINE_FILENAME).is_file():
            self.load_from_models_dir(base_dir)

    def load_from_models_dir(
        self,
        models_dir: str | Path = "./models_v2",
        device: str | torch.device = "cpu",
    ) -> Dict[str, Any]:
        """Load player_profiles_v2.parquet & spatial_predictor.pt from models_dir."""
        base_dir = Path(models_dir)
        loaded_files: List[str] = []

        pq_path = base_dir / PLAYER_PROFILES_FILENAME
        if pq_path.is_file():
            self.registry = PlayerProfileRegistry.from_parquet(pq_path)
            loaded_files.append(pq_path.name)

        sp_path = base_dir / "spatial_predictor.pt"
        if self.spatial_solver.load_checkpoint(sp_path, device=device):
            loaded_files.append(sp_path.name)

        eng_path = base_dir / TACTICAL_ENGINE_FILENAME
        if eng_path.is_file():
            ckpt = torch.load(eng_path, map_location=device, weights_only=False)
            sd = ckpt.get("state_dict", ckpt) if isinstance(ckpt, dict) else ckpt
            if isinstance(sd, dict):
                self.load_state_dict(sd, strict=False)
                loaded_files.append(eng_path.name)

        self.to(device)
        self.weights_loaded = len(loaded_files) > 0
        return {
            "models_dir": str(base_dir),
            "loaded_files": loaded_files,
            "weights_loaded": self.weights_loaded,
            "profiles_count": len(self.registry),
        }

    @classmethod
    def from_models_dir(
        cls,
        models_dir: str | Path = "./models_v2",
        device: str | torch.device = "cpu",
        **kwargs: Any,
    ) -> "TacticalStateEngine":
        """Instantiate TacticalStateEngine and load available artifacts from models_dir."""
        engine = cls(models_dir=models_dir, auto_load=False, **kwargs)
        engine.load_from_models_dir(models_dir=models_dir, device=device)
        return engine

    def init_batch(
        self,
        configs: MatchConfigurationV2 | Sequence[MatchConfigurationV2],
        batch_size: Optional[int] = None,
        seeds: Optional[Sequence[int]] = None,
        sample_epistemic: bool = False,
        device: torch.device | str = "cpu",
    ) -> TacticalStepResult:
        """Initialize a batch of B parallel matches at kickoff (t=0, ball at (60, 40))."""
        if isinstance(configs, MatchConfigurationV2):
            cfg_list = [configs] * (batch_size or 1)
        else:
            cfg_list = list(configs)
            if batch_size is not None and len(cfg_list) != batch_size:
                raise ValueError(f"Expected {batch_size} configs, got {len(cfg_list)}.")
        bsz = len(cfg_list)
        dev = torch.device(device)

        positions = torch.zeros((bsz, 22, 2), device=dev, dtype=torch.float32)
        velocities = torch.zeros((bsz, 22, 2), device=dev, dtype=torch.float32)
        stamina = torch.ones((bsz, 22), device=dev, dtype=torch.float32)
        player_ids = torch.full((bsz, 22), -1, device=dev, dtype=torch.int64)
        base_attr = torch.zeros((bsz, 22, 24), device=dev, dtype=torch.float32)
        gk_attr = torch.zeros((bsz, 22, 8), device=dev, dtype=torch.float32)
        finishing_lo = torch.zeros((bsz, 22), device=dev, dtype=torch.float32)
        style_64d = torch.zeros((bsz, 22, 64), device=dev, dtype=torch.float32)
        role_ids = torch.zeros((bsz, 22), device=dev, dtype=torch.int64)
        role_priors = torch.zeros((bsz, 22, 16), device=dev, dtype=torch.float32)
        player_instr = torch.zeros((bsz, 22, 5), device=dev, dtype=torch.float32)
        team_tactics = torch.zeros((bsz, 2, 10), device=dev, dtype=torch.float32)
        phase_anchors = torch.zeros((bsz, 22, 4, 5), device=dev, dtype=torch.float32)

        bench_valid = torch.zeros((bsz, 2, 12), device=dev, dtype=torch.bool)
        bench_pids = torch.full((bsz, 2, 12), -1, device=dev, dtype=torch.int64)
        bench_attr = torch.zeros((bsz, 2, 12, 24), device=dev, dtype=torch.float32)
        bench_gk = torch.zeros((bsz, 2, 12, 8), device=dev, dtype=torch.float32)
        bench_fin = torch.zeros((bsz, 2, 12), device=dev, dtype=torch.float32)
        bench_style = torch.zeros((bsz, 2, 12, 64), device=dev, dtype=torch.float32)
        bench_roles = torch.zeros((bsz, 2, 12), device=dev, dtype=torch.int64)
        bench_fitness = torch.ones((bsz, 2, 12), device=dev, dtype=torch.float32)

        sub_runtimes: List[Tuple[_TeamSubRuntime, _TeamSubRuntime]] = []

        for b, cfg in enumerate(cfg_list):
            rng = None
            if sample_epistemic:
                seed_val = int(seeds[b]) if seeds is not None else (cfg.random_seed or (1000 + b))
                rng = torch.Generator(device="cpu").manual_seed(seed_val)

            team_pair_rt: List[_TeamSubRuntime] = []
            for t_idx, team in enumerate((cfg.home, cfg.away)):
                team_tactics[b, t_idx] = torch.tensor(team.tactical_instructions.to_vector(), device=dev)
                xi = team.ordered_xi()
                roles = [s.tactical_role for s in xi]
                resolved_phases = resolve_formation_anchors(team.formation, roles)
                slot_offset = 0 if t_idx == 0 else 11

                for s_idx, slot_cfg in enumerate(xi):
                    g_idx = slot_offset + s_idx
                    prof = self.registry.resolve_profile(
                        slot_cfg.player_id,
                        slot_cfg.tactical_role,
                        slot_cfg.inline_profile_override,
                    )
                    attr_t = torch.tensor(prof.attributes_24d, dtype=torch.float32)
                    if rng is not None:
                        sig_t = torch.tensor(prof.sigma_24d, dtype=torch.float32)
                        noise = torch.randn(24, generator=rng) * sig_t
                        attr_t = (attr_t + noise).clamp(0.0, 1.0)

                    player_ids[b, g_idx] = prof.player_id
                    base_attr[b, g_idx] = attr_t.to(dev)
                    gk_attr[b, g_idx] = torch.tensor(prof.gk_attributes_8d, device=dev)
                    finishing_lo[b, g_idx] = prof.finishing_log_odds
                    style_64d[b, g_idx] = torch.tensor(prof.latent_style_64d, device=dev)
                    role_ids[b, g_idx] = ROLE_TO_ID[slot_cfg.tactical_role]
                    role_priors[b, g_idx] = torch.tensor(ROLE_PRIOR_VECTORS[slot_cfg.tactical_role], device=dev)
                    stamina[b, g_idx] = slot_cfg.initial_fitness

                    pi = slot_cfg.player_instructions
                    player_instr[b, g_idx] = torch.tensor(
                        [
                            pi.freedom_to_roam,
                            pi.width_bias,
                            pi.forward_run_appetite,
                            pi.pressing_aggressiveness,
                            pi.passing_risk_bias,
                        ],
                        device=dev,
                    )

                    for p_idx, phase in enumerate(TacticalPhase):
                        anc = resolved_phases[phase][s_idx]
                        phase_anchors[b, g_idx, p_idx] = torch.tensor(
                            [anc.anchor_x, anc.anchor_y, anc.zone_radius, anc.ball_shift_x, anc.ball_shift_y],
                            device=dev,
                        )

                # Populate bench tensors
                for bi, b_cfg in enumerate(team.bench):
                    b_prof = self.registry.resolve_profile(
                        b_cfg.player_id,
                        b_cfg.default_role,
                        b_cfg.inline_profile_override,
                    )
                    b_attr_t = torch.tensor(b_prof.attributes_24d, dtype=torch.float32)
                    if rng is not None:
                        b_sig_t = torch.tensor(b_prof.sigma_24d, dtype=torch.float32)
                        b_attr_t = (b_attr_t + torch.randn(24, generator=rng) * b_sig_t).clamp(0.0, 1.0)
                    bench_valid[b, t_idx, bi] = True
                    bench_pids[b, t_idx, bi] = b_prof.player_id
                    bench_attr[b, t_idx, bi] = b_attr_t.to(dev)
                    bench_gk[b, t_idx, bi] = torch.tensor(b_prof.gk_attributes_8d, device=dev)
                    bench_fin[b, t_idx, bi] = b_prof.finishing_log_odds
                    bench_style[b, t_idx, bi] = torch.tensor(b_prof.latent_style_64d, device=dev)
                    bench_roles[b, t_idx, bi] = ROLE_TO_ID[b_cfg.default_role]
                    bench_fitness[b, t_idx, bi] = b_cfg.initial_fitness

                pol = team.substitution_policy
                team_pair_rt.append(
                    _TeamSubRuntime(
                        policy_mode=pol.mode,
                        max_subs=pol.max_substitutions,
                        max_windows=pol.max_windows,
                        stamina_threshold=pol.stamina_sub_threshold,
                        yellow_press_threshold=pol.protect_yellow_card_threshold_pressing,
                        scripted_rules=[r.model_dump() for r in pol.scripted_rules],
                    )
                )
            sub_runtimes.append((team_pair_rt[0], team_pair_rt[1]))

        state = BatchTacticalState(
            positions=positions,
            velocities=velocities,
            stamina=stamina,
            yellow_cards=torch.zeros((bsz, 22), device=dev, dtype=torch.bool),
            red_cards=torch.zeros((bsz, 22), device=dev, dtype=torch.bool),
            active_mask=torch.ones((bsz, 22), device=dev, dtype=torch.bool),
            player_ids=player_ids,
            base_attr_24d=base_attr,
            gk_attr_8d=gk_attr,
            finishing_log_odds=finishing_lo,
            style_64d=style_64d,
            role_ids=role_ids,
            role_priors_16d=role_priors,
            player_instr_5d=player_instr,
            team_tactics_10d=team_tactics,
            effective_tactics_10d=team_tactics.clone(),
            phase_anchors=phase_anchors,
            subs_used=torch.zeros((bsz, 2), device=dev, dtype=torch.int64),
            sub_windows_used=torch.zeros((bsz, 2), device=dev, dtype=torch.int64),
            bench_used_mask=torch.zeros((bsz, 2, 12), device=dev, dtype=torch.bool),
            bench_valid_mask=bench_valid,
            bench_player_ids=bench_pids,
            bench_attr_24d=bench_attr,
            bench_gk_attr_8d=bench_gk,
            bench_finishing_lo=bench_fin,
            bench_style_64d=bench_style,
            bench_role_ids=bench_roles,
            bench_fitness=bench_fitness,
            clock_seconds=torch.zeros((bsz,), device=dev, dtype=torch.float32),
            period=torch.ones((bsz,), device=dev, dtype=torch.int64),
            score=torch.zeros((bsz, 2), device=dev, dtype=torch.int64),
            possessing_team=torch.zeros((bsz,), device=dev, dtype=torch.int64),
            ball_xy=torch.tensor([[60.0, 40.0]], device=dev).expand(bsz, 2).clone(),
            ball_z=torch.zeros((bsz,), device=dev, dtype=torch.float32),
            seconds_since_turnover=torch.full((bsz,), 10.0, device=dev, dtype=torch.float32),
            poss_duration_sec=torch.zeros((bsz,), device=dev, dtype=torch.float32),
            rolling_ppda=torch.full((bsz, 2), 11.0, device=dev, dtype=torch.float32),
            rolling_momentum=torch.zeros((bsz,), device=dev, dtype=torch.float32),
            halftime_recovered=torch.zeros((bsz,), device=dev, dtype=torch.bool),
            sub_runtimes=sub_runtimes,
        )

        # Initialize kickoff positions directly from Layer 1 phase anchors (no dt clamp on t=0)
        init_phase_ids = self.spatial_solver.determine_team_phases(
            state.possessing_team, state.ball_xy, state.seconds_since_turnover, state.effective_tactics_10d
        )
        gk_sweep = torch.stack([state.gk_attr_8d[:, 0, 2], state.gk_attr_8d[:, 11, 2]], dim=-1)
        state.positions = self.spatial_solver.compute_phase_anchors(
            state.phase_anchors, init_phase_ids, state.ball_xy, state.effective_tactics_10d, gk_sweep
        )
        # Put Home kickoff striker at (60, 40)
        state.positions[:, 9, 0] = 60.0
        state.positions[:, 9, 1] = 40.0

        spatial_out = self._run_spatial(state, dt_sec=torch.ones((bsz,), device=dev), actor_slot_idx=torch.full((bsz,), 9, device=dev, dtype=torch.int64))
        player_cond, memory_tokens, global_state = self.compute_conditioning(state, spatial_out)
        return TacticalStepResult(
            state=state,
            spatial=spatial_out,
            player_cond=player_cond,
            memory_tokens=memory_tokens,
            global_state=global_state,
            actor_mask=state.active_mask.clone(),
        )

    @staticmethod
    def slot_1_22_to_idx(actor_slot_1_22: torch.Tensor) -> torch.Tensor:
        """Convert 1-based wire slot (0=SYSTEM, 1..22=players) to 0..21 index (-1=SYSTEM)."""
        return torch.where(actor_slot_1_22 > 0, actor_slot_1_22 - 1, torch.full_like(actor_slot_1_22, -1))

    def _run_spatial(
        self,
        state: BatchTacticalState,
        dt_sec: torch.Tensor,
        actor_slot_idx: torch.Tensor,
        actor_pin_xy: Optional[torch.Tensor] = None,
        receiver_slot_idx: Optional[torch.Tensor] = None,
        receiver_pin_xy: Optional[torch.Tensor] = None,
        ball_end_xy: Optional[torch.Tensor] = None,
        restart_offside_exempt: Optional[torch.Tensor] = None,
    ) -> SpatialStepOutput:
        gk_sweep = torch.stack([state.gk_attr_8d[:, 0, 2], state.gk_attr_8d[:, 11, 2]], dim=-1)
        return self.spatial_solver.step(
            prev_positions=state.positions,
            phase_anchors=state.phase_anchors,
            possessing_team=state.possessing_team,
            ball_xy=state.ball_xy,
            ball_end_xy=ball_end_xy if ball_end_xy is not None else state.ball_xy,
            seconds_since_turnover=state.seconds_since_turnover,
            dt_sec=dt_sec,
            effective_tactics_10d=state.effective_tactics_10d,
            stamina=state.stamina,
            sprint_capacity=state.base_attr_24d[..., 22],
            anticipation=state.base_attr_24d[..., 17],
            freedom_to_roam=state.player_instr_5d[..., 0],
            role_forward_bias=state.role_priors_16d[..., 0],
            gk_sweeper_rate=gk_sweep,
            active_mask=state.active_mask,
            actor_slot_idx=actor_slot_idx,
            actor_pin_xy=actor_pin_xy,
            receiver_slot_idx=receiver_slot_idx,
            receiver_pin_xy=receiver_pin_xy,
            restart_offside_exempt=restart_offside_exempt,
        )

    def compute_effective_attributes(self, state: BatchTacticalState) -> torch.Tensor:
        """Apply fatigue degradation and yellow-card defensive caution to 24d attributes."""
        s = state.stamina
        tech_mult = 1.0 - 0.24 * torch.clamp((0.65 - s) / 0.65, min=0.0).pow(1.4)
        foul_mult = 1.0 + 0.35 * torch.clamp((0.60 - s) / 0.60, min=0.0)

        mult = torch.ones_like(state.base_attr_24d)
        # Technical & physical attributes degrade smoothly when fatigued
        tech_indices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 15, 16, 17, 20, 22, 23]
        mult[..., tech_indices] = tech_mult.unsqueeze(-1)
        mult[..., 19] = foul_mult  # foul_proneness rises with fatigue

        # Yellow card induces cautious tackle/press engagement (0.65x pressing/tackling)
        yellow_f = state.yellow_cards.to(mult.dtype)
        caution = 1.0 - 0.35 * yellow_f
        mult[..., 15] = mult[..., 15] * caution
        mult[..., 16] = mult[..., 16] * caution

        return (state.base_attr_24d * mult).clamp(0.0, 1.0)

    def compute_conditioning(
        self,
        state: BatchTacticalState,
        spatial_out: SpatialStepOutput,
    ) -> Tuple[torch.Tensor, torch.Tensor, torch.Tensor]:
        """Build player_cond [B,22,128], memory_tokens M_t [B,28,768], and s_t [B,64]."""
        bsz = state.positions.shape[0]
        device = state.positions.device
        dtype = state.positions.dtype

        a_eff = self.compute_effective_attributes(state)  # [B, 22, 24]
        tau_expanded = torch.cat(
            [
                state.effective_tactics_10d[:, 0:1, :].expand(bsz, 11, 10),
                state.effective_tactics_10d[:, 1:2, :].expand(bsz, 11, 10),
            ],
            dim=1,
        )  # [B, 22, 10]

        scale_xy = torch.tensor([PITCH_LENGTH_YD, PITCH_WIDTH_YD], device=device, dtype=dtype)
        pos_norm = spatial_out.positions / scale_xy
        vel_norm = (spatial_out.velocities / 12.0).clamp(-1.0, 1.0)
        s_pos = torch.cat(
            [
                pos_norm,
                vel_norm,
                spatial_out.dist_to_ball.unsqueeze(-1),
                spatial_out.local_pitch_control.unsqueeze(-1),
            ],
            dim=-1,
        )  # [B, 22, 6]

        card_state = (state.yellow_cards.to(dtype) * 0.5 + state.red_cards.to(dtype)).clamp(0.0, 1.0)
        s_stat = torch.stack(
            [
                state.active_mask.to(dtype),
                state.stamina,
                card_state,
            ],
            dim=-1,
        )  # [B, 22, 3]

        # Exact 128-dim slot tensor: 64 + 24 + 16 + 10 + 5 + 6 + 3 = 128
        player_cond = torch.cat(
            [
                state.style_64d,
                a_eff,
                state.role_priors_16d,
                tau_expanded,
                state.player_instr_5d,
                s_pos,
                s_stat,
            ],
            dim=-1,
        )

        # Tokens 0..21 of M_t [B, 28, 768]
        slot_ids = torch.arange(22, device=device)
        slot_tokens = self.slot_mem_proj(player_cond) + self.slot_pos_emb(slot_ids).unsqueeze(0)

        # Tokens 22..23: Home & Away tactical tokens + active_gk_attributes_6d
        team_toks = []
        for t_idx in (0, 1):
            sl = slice(0, 11) if t_idx == 0 else slice(11, 22)
            gk_idx = 0 if t_idx == 0 else 11
            gk_6d = state.gk_attr_8d[:, gk_idx, :6]
            anchor_summary = state.phase_anchors[:, sl, 1, :2].mean(dim=1)  # [B, 2]
            anchor_spread = state.phase_anchors[:, sl, 1, :2].std(dim=1)   # [B, 2]
            reds_norm = state.red_cards[:, sl].to(dtype).sum(dim=1, keepdim=True) / 3.0
            subs_rem = (5.0 - state.subs_used[:, t_idx:t_idx + 1].to(dtype)) / 5.0
            phase_norm = spatial_out.phase_ids[:, t_idx:t_idx + 1].to(dtype) / 3.0
            avg_stam = state.stamina[:, sl].mean(dim=1, keepdim=True)
            ppda_norm = (state.rolling_ppda[:, t_idx:t_idx + 1] / 20.0).clamp(0.0, 1.5)
            side_flag = torch.full((bsz, 1), float(t_idx), device=device, dtype=dtype)
            t_vec = torch.cat(
                [
                    state.effective_tactics_10d[:, t_idx, :],
                    gk_6d,
                    anchor_summary,
                    anchor_spread,
                    reds_norm,
                    subs_rem,
                    phase_norm,
                    avg_stam,
                    ppda_norm,
                    side_flag,
                ],
                dim=-1,
            )  # [B, 26]
            team_toks.append(self.team_mem_proj(t_vec))
        team_tokens = torch.stack(team_toks, dim=1)  # [B, 2, 768]

        # Tokens 24..27: Global Ball & Match Context Tokens [B, 4, 768]
        minute_norm = (state.clock_seconds / 5400.0).clamp(0.0, 1.4)
        score_diff = (state.score[:, 0] - state.score[:, 1]).to(dtype) / 5.0
        ctx_vec = torch.cat(
            [
                state.ball_xy / scale_xy,
                (state.ball_z / 5.0).unsqueeze(-1),
                (state.possessing_team == 0).to(dtype).unsqueeze(-1),
                minute_norm.unsqueeze(-1),
                score_diff.unsqueeze(-1),
                state.rolling_momentum.unsqueeze(-1),
                spatial_out.home_voronoi_control.unsqueeze(-1),
                spatial_out.ball_pressure.clamp(0.0, 2.0).unsqueeze(-1),
                (state.seconds_since_turnover / 10.0).clamp(0.0, 1.0).unsqueeze(-1),
                (state.poss_duration_sec / 60.0).clamp(0.0, 1.0).unsqueeze(-1),
                (state.period.to(dtype) / 6.0).unsqueeze(-1),
                torch.zeros((bsz, 4), device=device, dtype=dtype),
            ],
            dim=-1,
        )  # [B, 16]
        ctx_tokens = self.ctx_mem_proj(ctx_vec).unsqueeze(1) + self.ctx_token_queries.unsqueeze(0)  # [B, 4, 768]

        memory_tokens = self.mem_norm(torch.cat([slot_tokens, team_tokens, ctx_tokens], dim=1))  # [B, 28, 768]

        # Global match state s_t [B, 64] for Low-Rank AdaLN-Zero
        period_oh = torch.nn.functional.one_hot(state.period.clamp(0, 6), num_classes=7).to(dtype)
        home_reds = state.red_cards[:, :11].to(dtype).sum(dim=1)
        away_reds = state.red_cards[:, 11:22].to(dtype).sum(dim=1)
        home_stam = state.stamina[:, :11].mean(dim=1)
        away_stam = state.stamina[:, 11:22].mean(dim=1)
        s_core = torch.cat(
            [
                minute_norm.unsqueeze(-1),
                period_oh,
                score_diff.unsqueeze(-1),
                (home_reds / 3.0).unsqueeze(-1),
                (away_reds / 3.0).unsqueeze(-1),
                (state.poss_duration_sec / 60.0).clamp(0.0, 1.0).unsqueeze(-1),
                home_stam.unsqueeze(-1),
                away_stam.unsqueeze(-1),
                (state.rolling_ppda / 20.0).clamp(0.0, 1.5),
                state.rolling_momentum.unsqueeze(-1),
                spatial_out.home_voronoi_control.unsqueeze(-1),
                spatial_out.ball_pressure.clamp(0.0, 2.0).unsqueeze(-1),
                state.effective_tactics_10d[:, 0, :],
                state.effective_tactics_10d[:, 1, :],
            ],
            dim=-1,
        )  # 1+7+1+1+1+1+1+1+2+1+1+1+10+10 = 39 dims
        pad = torch.zeros((bsz, 64 - s_core.shape[-1]), device=device, dtype=dtype)
        global_state = torch.cat([s_core, pad], dim=-1)  # [B, 64]

        return player_cond, memory_tokens, global_state

    def _update_game_state_elasticity(self, state: BatchTacticalState) -> None:
        """Modulate effective_tactics_10d by minute & score_diff scaled by adaptivity."""
        minute = state.clock_seconds / 60.0
        w_late = ((minute - 55.0) / 35.0).clamp(0.0, 1.0)
        eff = state.team_tactics_10d.clone()

        for t_idx in (0, 1):
            opp = 1 - t_idx
            score_diff = state.score[:, t_idx] - state.score[:, opp]
            adapt = state.team_tactics_10d[:, t_idx, 9]
            factor = w_late * adapt

            trailing = score_diff < 0
            leading_one = score_diff == 1

            # Trailing pushes line height, pressing, and directness upward
            eff[:, t_idx, 0] = torch.where(trailing, eff[:, t_idx, 0] + 0.16 * factor, eff[:, t_idx, 0])
            eff[:, t_idx, 1] = torch.where(trailing, eff[:, t_idx, 1] + 0.15 * factor, eff[:, t_idx, 1])
            eff[:, t_idx, 3] = torch.where(trailing, eff[:, t_idx, 3] + 0.12 * factor, eff[:, t_idx, 3])

            # Protecting 1-goal lead drops line, slows tempo, and favors regrouping
            eff[:, t_idx, 0] = torch.where(leading_one, eff[:, t_idx, 0] - 0.14 * factor, eff[:, t_idx, 0])
            eff[:, t_idx, 2] = torch.where(leading_one, eff[:, t_idx, 2] - 0.15 * factor, eff[:, t_idx, 2])
            eff[:, t_idx, 6] = torch.where(leading_one, eff[:, t_idx, 6] - 0.18 * factor, eff[:, t_idx, 6])

            # Red-card caution adjustment (fewer than 11 active players on team)
            sl = slice(0, 11) if t_idx == 0 else slice(11, 22)
            has_red = (~state.active_mask[:, sl]).any(dim=1)
            eff[:, t_idx, 1] = torch.where(has_red, eff[:, t_idx, 1] - 0.25, eff[:, t_idx, 1])
            eff[:, t_idx, 0] = torch.where(has_red & (~trailing), eff[:, t_idx, 0] - 0.15, eff[:, t_idx, 0])

        state.effective_tactics_10d = eff.clamp(0.0, 1.0)

    def _rebalance_on_red_card(self, state: BatchTacticalState, b: int, sent_off_global_slot: int) -> None:
        """Compact active outfielders on the penalized team into an emergency 4-4-1/5-3-1 shape."""
        t_idx = 0 if sent_off_global_slot < 11 else 1
        offset = 0 if t_idx == 0 else 11
        state.red_cards[b, sent_off_global_slot] = True
        state.active_mask[b, sent_off_global_slot] = False
        state.velocities[b, sent_off_global_slot] = 0.0

        # Compact OOP & high-press anchors slightly deeper & narrower for remaining outfielders
        for local_s in range(1, 11):
            g_s = offset + local_s
            if not state.active_mask[b, g_s]:
                continue
            for p_idx in (2, 3):  # HIGH_PRESS, OUT_OF_POSSESSION
                ax = state.phase_anchors[b, g_s, p_idx, 0]
                ay = state.phase_anchors[b, g_s, p_idx, 1]
                state.phase_anchors[b, g_s, p_idx, 0] = (ax * 0.88).clamp(0.14, 0.72)
                state.phase_anchors[b, g_s, p_idx, 1] = 0.5 + (ay - 0.5) * 0.85

    @staticmethod
    def _apply_formation_to_state(
        state: BatchTacticalState,
        b: int,
        t_idx: int,
        formation_cfg: FormationConfig,
    ) -> None:
        offset = 0 if t_idx == 0 else 11
        roles_team = [ID_TO_ROLE[int(state.role_ids[b, offset + i].item())] for i in range(11)]
        resolved = resolve_formation_anchors(formation_cfg, roles_team)
        for p_idx, phase in enumerate(TacticalPhase):
            for s_i, anc in enumerate(resolved[phase]):
                state.phase_anchors[b, offset + s_i, p_idx] = torch.tensor(
                    [anc.anchor_x, anc.anchor_y, anc.zone_radius, anc.ball_shift_x, anc.ball_shift_y],
                    device=state.positions.device,
                )

    def _execute_slot_sub(
        self,
        state: BatchTacticalState,
        b: int,
        t_idx: int,
        out_local_slot: int,
        bench_idx: int,
        new_role: Optional[PlayerRole] = None,
        new_formation: Optional[str] = None,
        tactical_patch: Optional[Dict[str, float]] = None,
        consume_window: bool = True,
    ) -> Dict[str, Any]:
        offset = 0 if t_idx == 0 else 11
        g_slot = offset + out_local_slot
        out_pid = int(state.player_ids[b, g_slot].item())

        state.player_ids[b, g_slot] = state.bench_player_ids[b, t_idx, bench_idx]
        state.base_attr_24d[b, g_slot] = state.bench_attr_24d[b, t_idx, bench_idx]
        state.gk_attr_8d[b, g_slot] = state.bench_gk_attr_8d[b, t_idx, bench_idx]
        state.finishing_log_odds[b, g_slot] = state.bench_finishing_lo[b, t_idx, bench_idx]
        state.style_64d[b, g_slot] = state.bench_style_64d[b, t_idx, bench_idx]
        state.stamina[b, g_slot] = state.bench_fitness[b, t_idx, bench_idx]
        state.yellow_cards[b, g_slot] = False

        role_id = ROLE_TO_ID[new_role] if new_role is not None else int(state.bench_role_ids[b, t_idx, bench_idx].item())
        role_enum = ID_TO_ROLE[role_id]
        state.role_ids[b, g_slot] = role_id
        state.role_priors_16d[b, g_slot] = torch.tensor(ROLE_PRIOR_VECTORS[role_enum], device=state.positions.device)

        if tactical_patch:
            for k, val in tactical_patch.items():
                s_i = TACTICAL_SLIDER_NAMES.index(k)
                state.team_tactics_10d[b, t_idx, s_i] = max(0.0, min(1.0, float(val)))

        if new_formation is not None:
            self._apply_formation_to_state(state, b, t_idx, FormationConfig(base_name=new_formation))

        state.bench_used_mask[b, t_idx, bench_idx] = True
        state.subs_used[b, t_idx] += 1
        if consume_window:
            rt = state.sub_runtimes[b][t_idx]
            cur_clk = float(state.clock_seconds[b].item())
            if abs(cur_clk - rt.last_sub_window_clock) > 5.0:
                state.sub_windows_used[b, t_idx] += 1
                rt.last_sub_window_clock = cur_clk

        return {
            "batch_idx": b,
            "team_side": t_idx,
            "slot": g_slot + 1,  # Canonical ACTOR_SLOT_1_22
            "out_player_id": out_pid,
            "in_player_id": int(state.player_ids[b, g_slot].item()),
            "bench_index": bench_idx,
            "minute": float(state.clock_seconds[b].item() / 60.0),
        }

    def evaluate_substitutions(self, state: BatchTacticalState, is_dead_ball: torch.Tensor) -> List[Dict[str, Any]]:
        """Evaluate FIFA-compliant scripted & reactive substitutions at dead-ball moments."""
        executed: List[Dict[str, Any]] = []
        bsz = state.positions.shape[0]
        for b in range(bsz):
            if not bool(is_dead_ball[b].item()):
                continue
            minute = float(state.clock_seconds[b].item() / 60.0)
            is_halftime = int(state.period[b].item()) == 2
            for t_idx in (0, 1):
                rt = state.sub_runtimes[b][t_idx]
                if int(state.subs_used[b, t_idx].item()) >= rt.max_subs:
                    continue
                if not is_halftime and int(state.sub_windows_used[b, t_idx].item()) >= rt.max_windows:
                    continue

                score_diff = int((state.score[b, t_idx] - state.score[b, 1 - t_idx]).item())
                offset = 0 if t_idx == 0 else 11

                # 1. Scripted rules (evaluated first in scripted/hybrid modes)
                if rt.policy_mode in ("scripted", "hybrid"):
                    for r_idx, rule in enumerate(rt.scripted_rules):
                        if r_idx in rt.executed_rule_indices:
                            continue
                        if minute < rule["trigger_minute"]:
                            continue
                        if rule["condition_score_diff_max"] is not None and score_diff > rule["condition_score_diff_max"]:
                            continue
                        if rule["condition_score_diff_min"] is not None and score_diff < rule["condition_score_diff_min"]:
                            continue
                        out_local = int(rule["out_slot_index"])
                        if not bool(state.active_mask[b, offset + out_local].item()):
                            continue
                        b_idx = rule["in_bench_index"]
                        if b_idx is None and rule["in_player_id"] is not None:
                            matches = (state.bench_player_ids[b, t_idx] == rule["in_player_id"]) & state.bench_valid_mask[b, t_idx]
                            idxs = torch.nonzero(matches, as_tuple=False)
                            if len(idxs) > 0:
                                b_idx = int(idxs[0, 0].item())
                        if b_idx is None or not bool(state.bench_valid_mask[b, t_idx, b_idx].item()) or bool(state.bench_used_mask[b, t_idx, b_idx].item()):
                            continue

                        rt.executed_rule_indices.add(r_idx)
                        new_r = PlayerRole(rule["new_role"]) if rule.get("new_role") else None
                        executed.append(
                            self._execute_slot_sub(
                                state,
                                b,
                                t_idx,
                                out_local,
                                int(b_idx),
                                new_role=new_r,
                                new_formation=rule.get("new_formation"),
                                tactical_patch=rule.get("tactical_patch"),
                                consume_window=not is_halftime,
                            )
                        )
                        if int(state.subs_used[b, t_idx].item()) >= rt.max_subs:
                            break

                if int(state.subs_used[b, t_idx].item()) >= rt.max_subs:
                    continue

                # 2. Rule-based & auto_coach reactive triggers after minute 55
                if rt.policy_mode in ("rule_based", "hybrid", "auto_coach") and minute >= 55.0:
                    press_int = float(state.effective_tactics_10d[b, t_idx, 1].item())
                    avail_bench = torch.nonzero(
                        state.bench_valid_mask[b, t_idx] & (~state.bench_used_mask[b, t_idx]),
                        as_tuple=False,
                    )
                    if len(avail_bench) == 0:
                        continue

                    candidate_out: Optional[int] = None
                    # Yellow card risk under high press
                    if press_int > rt.yellow_press_threshold:
                        booked = [
                            s for s in range(1, 11)
                            if bool(state.active_mask[b, offset + s].item()) and bool(state.yellow_cards[b, offset + s].item())
                        ]
                        if booked:
                            candidate_out = booked[0]

                    # Fatigue threshold check
                    if candidate_out is None:
                        stams = state.stamina[b, offset + 1:offset + 11]
                        act = state.active_mask[b, offset + 1:offset + 11]
                        masked_stams = torch.where(act, stams, torch.ones_like(stams))
                        min_idx = int(masked_stams.argmin().item())
                        if float(masked_stams[min_idx].item()) < rt.stamina_threshold:
                            candidate_out = min_idx + 1

                    if candidate_out is not None:
                        out_role_id = int(state.role_ids[b, offset + candidate_out].item())
                        # Pick bench player closest to same positional line (role_id proximity)
                        best_bi = int(avail_bench[0, 0].item())
                        best_dist = 999
                        for row in avail_bench:
                            bi = int(row[0].item())
                            br = int(state.bench_role_ids[b, t_idx, bi].item())
                            if br in (0, 1):  # Do not sub outfielder for GK
                                continue
                            dist = abs(br - out_role_id)
                            if dist < best_dist:
                                best_dist = dist
                                best_bi = bi
                        if best_dist < 999:
                            executed.append(
                                self._execute_slot_sub(
                                    state, b, t_idx, candidate_out, best_bi, consume_window=not is_halftime
                                )
                            )
        return executed

    def step(
        self,
        state: BatchTacticalState,
        dt_sec: torch.Tensor,
        ball_xy: torch.Tensor,
        possessing_team: torch.Tensor,
        period: Optional[torch.Tensor] = None,
        score: Optional[torch.Tensor] = None,
        actor_slot_idx: Optional[torch.Tensor] = None,
        actor_pin_xy: Optional[torch.Tensor] = None,
        receiver_slot_idx: Optional[torch.Tensor] = None,
        receiver_pin_xy: Optional[torch.Tensor] = None,
        ball_end_xy: Optional[torch.Tensor] = None,
        yellow_card_slots: Optional[torch.Tensor] = None,
        red_card_slots: Optional[torch.Tensor] = None,
        is_dead_ball: Optional[torch.Tensor] = None,
        restart_offside_exempt: Optional[torch.Tensor] = None,
    ) -> TacticalStepResult:
        """Advance tactical state by dt_sec across batch B and return Module C/D/E tensors."""
        bsz = state.positions.shape[0]
        device = state.positions.device

        dt_f = dt_sec.to(device=device, dtype=torch.float32).clamp_min(0.0)
        state.clock_seconds = state.clock_seconds + dt_f
        turnover = possessing_team != state.possessing_team
        state.seconds_since_turnover = torch.where(turnover, torch.zeros_like(dt_f), state.seconds_since_turnover + dt_f)
        state.poss_duration_sec = torch.where(turnover, dt_f, state.poss_duration_sec + dt_f)
        state.possessing_team = possessing_team
        state.ball_xy = ball_xy.to(device=device, dtype=torch.float32)

        if score is not None:
            state.score = score.to(device=device, dtype=torch.int64)
        if period is not None:
            new_period = period.to(device=device, dtype=torch.int64)
            # Apply halftime physiological recovery (+0.16) once upon entering 2nd half (period==3)
            just_2h = (new_period == 3) & (~state.halftime_recovered)
            if just_2h.any():
                state.stamina[just_2h] = (state.stamina[just_2h] + 0.16).clamp(max=1.0)
                state.halftime_recovered = state.halftime_recovered | just_2h
            state.period = new_period

        # Stamina drain per step
        is_home_poss = (state.possessing_team == 0).unsqueeze(-1)
        is_home_slot = torch.zeros((bsz, 22), device=device, dtype=torch.bool)
        is_home_slot[:, :11] = True
        is_oop = torch.where(is_home_poss, ~is_home_slot, is_home_slot)
        press_slot = torch.cat(
            [
                state.effective_tactics_10d[:, 0:1, 1].expand(bsz, 11),
                state.effective_tactics_10d[:, 1:2, 1].expand(bsz, 11),
            ],
            dim=1,
        )
        vel_mag = torch.linalg.norm(state.velocities, dim=-1)
        stamina_end = state.base_attr_24d[..., 21]
        drain = (
            dt_f.unsqueeze(-1)
            * (1.15e-4 + 1.45e-4 * press_slot * is_oop.to(dt_f.dtype) + 4.2e-5 * vel_mag)
            * (1.25 - 0.50 * stamina_end)
        )
        state.stamina = torch.where(state.active_mask, (state.stamina - drain).clamp(0.15, 1.0), state.stamina)

        # Pack card & dead-ball short-circuit guards into a single GPU->CPU sync per step
        has_yellow_t = (yellow_card_slots >= 0).any() if yellow_card_slots is not None else torch.tensor(False, device=device)
        has_red_t = (red_card_slots >= 0).any() if red_card_slots is not None else torch.tensor(False, device=device)
        has_dead_t = is_dead_ball.any() if is_dead_ball is not None else torch.tensor(False, device=device)
        has_yellow, has_red, has_dead = torch.stack([has_yellow_t, has_red_t, has_dead_t]).tolist()

        if has_yellow and yellow_card_slots is not None:
            for b in range(bsz):
                ys = int(yellow_card_slots[b].item())
                if 0 <= ys < 22 and bool(state.active_mask[b, ys].item()):
                    if bool(state.yellow_cards[b, ys].item()):
                        self._rebalance_on_red_card(state, b, ys)
                    else:
                        state.yellow_cards[b, ys] = True
        if has_red and red_card_slots is not None:
            for b in range(bsz):
                rs = int(red_card_slots[b].item())
                if 0 <= rs < 22 and bool(state.active_mask[b, rs].item()):
                    self._rebalance_on_red_card(state, b, rs)

        # Evaluate scripted/rule-based substitutions at dead balls
        executed_subs: List[Dict[str, Any]] = []
        if has_dead and is_dead_ball is not None:
            executed_subs = self.evaluate_substitutions(state, is_dead_ball)

        # Update dynamic game-state elasticity
        self._update_game_state_elasticity(state)

        # Update 22-player off-ball positions, Pitch Control, synthetic 360 & pass feasibility
        if actor_slot_idx is None:
            actor_slot_idx = torch.full((bsz,), -1, device=device, dtype=torch.int64)
        spatial_out = self._run_spatial(
            state=state,
            dt_sec=dt_f.clamp_min(0.4),
            actor_slot_idx=actor_slot_idx,
            actor_pin_xy=actor_pin_xy,
            receiver_slot_idx=receiver_slot_idx,
            receiver_pin_xy=receiver_pin_xy,
            ball_end_xy=ball_end_xy,
            restart_offside_exempt=restart_offside_exempt,
        )
        state.positions = spatial_out.positions
        state.velocities = spatial_out.velocities

        player_cond, memory_tokens, global_state = self.compute_conditioning(state, spatial_out)
        return TacticalStepResult(
            state=state,
            spatial=spatial_out,
            player_cond=player_cond,
            memory_tokens=memory_tokens,
            global_state=global_state,
            actor_mask=state.active_mask.clone(),
            executed_subs=executed_subs,
        )

    def apply_live_override(
        self,
        state: BatchTacticalState,
        team_side: Literal["home", "away"] | int,
        tactical_patch: Optional[Dict[str, float]] = None,
        new_formation: Optional[FormationConfig] = None,
        role_updates: Optional[Dict[int, PlayerRole]] = None,
        sub_request: Optional[Tuple[int, int]] = None,
        batch_idx: int = 0,
    ) -> Optional[Dict[str, Any]]:
        """Apply an interactive mid-match tactical tweak or manual sub (from Module E)."""
        t_idx = 0 if team_side in ("home", 0) else 1
        offset = 0 if t_idx == 0 else 11
        dev = state.positions.device

        if tactical_patch:
            for k, val in tactical_patch.items():
                if k not in TACTICAL_SLIDER_NAMES:
                    raise ValueError(f"Unknown tactical slider: {k}")
                idx = TACTICAL_SLIDER_NAMES.index(k)
                state.team_tactics_10d[batch_idx, t_idx, idx] = max(0.0, min(1.0, float(val)))

        if role_updates:
            for local_slot, new_role in role_updates.items():
                if not (0 <= local_slot <= 10):
                    raise ValueError(f"Invalid local slot index: {local_slot}")
                if local_slot == 0 and new_role not in (PlayerRole.SWEEPER_KEEPER, PlayerRole.SHOT_STOPPER_GK):
                    raise ValueError("Slot 0 must keep a Goalkeeper role.")
                g_slot = offset + local_slot
                state.role_ids[batch_idx, g_slot] = ROLE_TO_ID[new_role]
                state.role_priors_16d[batch_idx, g_slot] = torch.tensor(ROLE_PRIOR_VECTORS[new_role], device=dev)

        if new_formation is not None:
            self._apply_formation_to_state(state, batch_idx, t_idx, new_formation)

        sub_event = None
        if sub_request is not None:
            out_local, bench_idx = sub_request
            rt = state.sub_runtimes[batch_idx][t_idx]
            if int(state.subs_used[batch_idx, t_idx].item()) < rt.max_subs and bool(
                state.bench_valid_mask[batch_idx, t_idx, bench_idx].item()
            ) and not bool(state.bench_used_mask[batch_idx, t_idx, bench_idx].item()):
                sub_event = self._execute_slot_sub(state, batch_idx, t_idx, out_local, bench_idx, consume_window=True)

        self._update_game_state_elasticity(state)
        return sub_event


def apply_live_tactical_override(
    state_handle: Any,
    override_payload: Dict[str, Any],
) -> Dict[str, Any]:
    """Module E bridge applying LiveTacticalOverrideRequest payloads to a live engine/state."""
    if isinstance(state_handle, tuple) and len(state_handle) == 2:
        engine, state = state_handle
    elif isinstance(state_handle, BatchTacticalState):
        engine = TacticalStateEngine()
        state = state_handle
    else:
        raise TypeError("state_handle must be BatchTacticalState or (TacticalStateEngine, BatchTacticalState).")

    team_side = override_payload.get("team_side", "home")
    patch = override_payload.get("tactical_instructions_patch")
    form_name = override_payload.get("formation_base_name")
    sub_dict = override_payload.get("substitution")
    sub_req: Optional[Tuple[int, int]] = None
    if isinstance(sub_dict, dict) and "out_slot_index" in sub_dict and "in_bench_index" in sub_dict:
        sub_req = (int(sub_dict["out_slot_index"]), int(sub_dict["in_bench_index"]))

    sub_ev = engine.apply_live_override(
        state=state,
        team_side=team_side,
        tactical_patch=patch,
        new_formation=FormationConfig(base_name=form_name) if form_name else None,
        sub_request=sub_req,
        batch_idx=0,
    )
    t_idx = 0 if team_side in ("home", 0) else 1
    eff_vec = state.effective_tactics_10d[0, t_idx].tolist()
    return {
        team_side: {name: round(float(val), 4) for name, val in zip(TACTICAL_SLIDER_NAMES, eff_vec)},
        "sub_executed": sub_ev,
    }


def load_from_models_dir(
    models_dir: Optional[str | Path] = None,
    device: str | torch.device = "cpu",
) -> Dict[str, Any]:
    """Auto-load all Module B tactical checkpoints from models_dir (default MODEL_V2_DIR / ./models_v2).

    Loads when present:
    - player_profiles_v2.parquet -> PlayerProfileRegistry
    - tactical_manifold_v2.pkl   -> TacticalManifoldModel / OODDetector
    - spatial_predictor.pt       -> OffBallSpatialSolver.predictor
    """
    from .ood_and_diagnostics import OODDetector

    base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
    engine = TacticalStateEngine.from_models_dir(models_dir=base_dir, device=device)
    ood_detector = OODDetector.from_models_dir(models_dir=base_dir)

    loaded_files: List[str] = []
    if engine.registry.loaded_from_path is not None:
        loaded_files.append(Path(engine.registry.loaded_from_path).name)
    if ood_detector.loaded_from_path is not None:
        loaded_files.append(Path(ood_detector.loaded_from_path).name)
    if engine.spatial_solver.checkpoint_path is not None:
        loaded_files.append(Path(engine.spatial_solver.checkpoint_path).name)

    return {
        "models_dir": str(base_dir),
        "engine": engine,
        "registry": engine.registry,
        "spatial_solver": engine.spatial_solver,
        "ood_detector": ood_detector,
        "manifold": ood_detector.manifold,
        "loaded_files": loaded_files,
        "weights_loaded": len(loaded_files) > 0,
    }
