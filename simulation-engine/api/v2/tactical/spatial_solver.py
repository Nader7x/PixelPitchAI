"""Module B: Vectorized 22-Player Off-Ball Spatial Solver, Pitch Control & 360 Bridge.

Implements the 3-layer hybrid spatial model (Formation/Phase Anchor Potential ->
Learned Residual SpatialPredictor -> Kinematic & Laws-of-the-Game Clamps),
vectorized 24x16 Spearman-style Pitch Control, synthetic [B, 22, 7] 360 freeze
frames for Module D, and 11x11 pass-target feasibility + spatial log-priors.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Optional

import torch
import torch.nn as nn

from .config_schema import PITCH_LENGTH_YD, PITCH_WIDTH_YD

DEFAULT_MODEL_V2_DIR: str = "./models_v2"
SPATIAL_PREDICTOR_FILENAME: str = "spatial_predictor.pt"

METERS_TO_YARDS: float = 1.0936133
NUM_PITCH_GRID_X: int = 24
NUM_PITCH_GRID_Y: int = 16
NUM_ACTIONS_DEFAULT: int = 18  # Matches Module C ACTION_TYPE core vocabulary


@dataclass
class SpatialStepOutput:
    """Batched spatial outputs consumed by StateEngine, Module C, Module D, & Module E."""

    positions: torch.Tensor              # [B, 22, 2] canonical [0,120]x[0,80]
    velocities: torch.Tensor             # [B, 22, 2] yards/sec
    phase_ids: torch.Tensor              # [B, 2] (home_phase, away_phase) in 0..3
    pitch_control_grid: torch.Tensor     # [B, 24, 16] Home control probability in [0,1]
    home_voronoi_control: torch.Tensor   # [B] mean pitch control
    local_pitch_control: torch.Tensor    # [B, 22] acting team control at each slot
    dist_to_ball: torch.Tensor           # [B, 22] normalized distance / 144.22
    ball_pressure: torch.Tensor          # [B] continuous defender pressure score
    under_pressure: torch.Tensor         # [B] bool (ball_pressure >= 0.45)
    freeze_frame_360: torch.Tensor       # [B, 22, 7] (x, y, vx, vy, is_teammate, is_actor, is_gk)
    defenders_in_shot_cone: torch.Tensor # [B] int64 count in shot-to-goal triangle
    gk_position_xy: torch.Tensor         # [B, 2] defending GK canonical coords
    target_feasibility: torch.Tensor     # [B, 11] pass-target feasibility in [0,1]
    offside_flags: torch.Tensor          # [B, 11] bool offside status per teammate slot
    target_mask: torch.Tensor            # [B, 11] bool active non-actor teammate mask
    spatial_log_prior: torch.Tensor      # [B, num_actions + 11] additive log-priors
    clamp_hit_fraction: torch.Tensor     # [B] fraction of slots clamped (Tier-4 OOD drift)


class SpatialPredictor(nn.Module):
    """Lightweight residual off-ball spatial predictor (~0.35M params).

    Predicts per-slot displacement residual (dx, dy) around Layer-1 formation anchors
    plus uncertainty (log_sigma_x, log_sigma_y). Output head is zero-initialized so
    cold-start evaluations cleanly follow analytic phase anchors until fine-tuned on 360.
    """

    def __init__(self, in_features: int = 18, hidden_dim: int = 192) -> None:
        super().__init__()
        self.in_proj = nn.Sequential(
            nn.Linear(in_features, hidden_dim),
            nn.SiLU(),
            nn.LayerNorm(hidden_dim),
        )
        # Lightweight 1-step multi-player self-interaction via mean-field pooling + MLP
        self.interact = nn.Sequential(
            nn.Linear(hidden_dim * 2, hidden_dim),
            nn.SiLU(),
            nn.LayerNorm(hidden_dim),
            nn.Linear(hidden_dim, hidden_dim),
            nn.SiLU(),
        )
        self.head = nn.Linear(hidden_dim, 4)
        nn.init.zeros_(self.head.weight)
        nn.init.zeros_(self.head.bias)

    def forward(self, slot_features: torch.Tensor, active_mask: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
        """Compute bounded residual (dx, dy) in yards and std (sigma_x, sigma_y)."""
        h = self.in_proj(slot_features)  # [B, 22, H]
        mask_f = active_mask.to(h.dtype).unsqueeze(-1)
        denom = mask_f.sum(dim=1, keepdim=True).clamp_min(1.0)
        global_ctx = (h * mask_f).sum(dim=1, keepdim=True) / denom  # [B, 1, H]
        h_cat = torch.cat([h, global_ctx.expand_as(h)], dim=-1)
        out = self.head(self.interact(h_cat))
        delta_xy = 6.0 * torch.tanh(out[..., :2])
        sigma_xy = torch.nn.functional.softplus(out[..., 2:]) + 0.5
        return delta_xy, sigma_xy


class OffBallSpatialSolver(nn.Module):
    """Vectorized 3-layer off-ball solver, Pitch Control grid, and pass-target evaluator."""

    def __init__(
        self,
        num_actions: int = NUM_ACTIONS_DEFAULT,
        hidden_dim: int = 192,
        pitch_ctrl_lambda: float = 0.85,
        pressure_threshold: float = 0.45,
        models_dir: Optional[str | Path] = None,
        auto_load: bool = True,
    ) -> None:
        super().__init__()
        self.num_actions = num_actions
        self.pitch_ctrl_lambda = pitch_ctrl_lambda
        self.pressure_threshold = pressure_threshold
        self.spatial_predictor = SpatialPredictor(in_features=18, hidden_dim=hidden_dim)
        self.weights_loaded: bool = False
        self.checkpoint_path: Optional[str] = None

        # Precompute 24x16 cell centers in canonical [0, 120] x [0, 80] coordinates
        xs = torch.linspace(2.5, PITCH_LENGTH_YD - 2.5, NUM_PITCH_GRID_X)
        ys = torch.linspace(2.5, PITCH_WIDTH_YD - 2.5, NUM_PITCH_GRID_Y)
        gx, gy = torch.meshgrid(xs, ys, indexing="ij")
        grid_centers = torch.stack([gx.reshape(-1), gy.reshape(-1)], dim=-1)  # [384, 2]
        self.register_buffer("grid_centers", grid_centers, persistent=False)

        # Reusable constant masks/indices (eliminates per-step tensor allocations)
        is_home_22 = torch.zeros((1, 22), dtype=torch.bool)
        is_home_22[:, :11] = True
        is_outfield_22 = torch.ones((1, 22), dtype=torch.bool)
        is_outfield_22[:, 0] = False
        is_outfield_22[:, 11] = False
        is_gk_22 = (~is_outfield_22).to(torch.float32)
        self.register_buffer("is_home_22", is_home_22, persistent=False)
        self.register_buffer("is_outfield_22", is_outfield_22, persistent=False)
        self.register_buffer("is_gk_22", is_gk_22, persistent=False)
        self.register_buffer("eye_22", torch.eye(22, dtype=torch.bool).unsqueeze(0), persistent=False)
        self.register_buffer("slot_ids_22", torch.arange(22, dtype=torch.long).unsqueeze(0), persistent=False)
        self.register_buffer("slot_ids_11", torch.arange(11, dtype=torch.long).unsqueeze(0), persistent=False)
        self.register_buffer("scale_xy", torch.tensor([PITCH_LENGTH_YD, PITCH_WIDTH_YD], dtype=torch.float32), persistent=False)

        if auto_load:
            base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", DEFAULT_MODEL_V2_DIR))
            ckpt_file = base_dir / SPATIAL_PREDICTOR_FILENAME
            if ckpt_file.is_file():
                self.load_checkpoint(ckpt_file)

    @property
    def predictor(self) -> SpatialPredictor:
        """Alias for spatial_predictor used by training & checkpoint loaders."""
        return self.spatial_predictor

    def save_checkpoint(self, ckpt_path: str | Path) -> Path:
        """Save SpatialPredictor state_dict to ckpt_path (e.g., models_v2/spatial_predictor.pt)."""
        target = Path(ckpt_path)
        target.parent.mkdir(parents=True, exist_ok=True)
        torch.save(self.spatial_predictor.state_dict(), target)
        self.checkpoint_path = str(target)
        return target

    def load_checkpoint(
        self,
        ckpt_path: str | Path,
        device: str | torch.device = "cpu",
    ) -> bool:
        """Load SpatialPredictor state_dict from ckpt_path if present."""
        target = Path(ckpt_path)
        if not target.is_file():
            return False
        raw = torch.load(target, map_location=device, weights_only=False)
        if isinstance(raw, dict) and "state_dict" in raw and isinstance(raw["state_dict"], dict):
            state_dict: Dict[str, Any] = raw["state_dict"]
        elif isinstance(raw, dict) and "model_state_dict" in raw and isinstance(raw["model_state_dict"], dict):
            state_dict = raw["model_state_dict"]
        elif isinstance(raw, dict):
            state_dict = raw
        else:
            return False

        cleaned: Dict[str, Any] = {}
        for k, v in state_dict.items():
            if k.startswith("spatial_predictor."):
                cleaned[k[len("spatial_predictor."):]] = v
            elif k.startswith("predictor."):
                cleaned[k[len("predictor."):]] = v
            elif k != "grid_centers":
                cleaned[k] = v

        self.spatial_predictor.load_state_dict(cleaned, strict=False)
        self.spatial_predictor.to(device)
        self.weights_loaded = True
        self.checkpoint_path = str(target)
        return True

    @classmethod
    def from_models_dir(
        cls,
        models_dir: Optional[str | Path] = None,
        device: str | torch.device = "cpu",
        **kwargs: Any,
    ) -> "OffBallSpatialSolver":
        """Instantiate OffBallSpatialSolver and auto-load spatial_predictor.pt from models_dir."""
        base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", DEFAULT_MODEL_V2_DIR))
        solver = cls(models_dir=base_dir, auto_load=False, **kwargs).to(device)
        solver.load_checkpoint(base_dir / SPATIAL_PREDICTOR_FILENAME, device=device)
        return solver

    @staticmethod
    def determine_team_phases(
        possessing_team: torch.Tensor,
        ball_xy: torch.Tensor,
        seconds_since_turnover: torch.Tensor,
        team_tactics_10d: torch.Tensor,
    ) -> torch.Tensor:
        """Determine active TacticalPhase ID (0..3) for Home (idx 0) and Away (idx 1)."""
        # TacticalPhase order: 0=build_up, 1=in_possession, 2=high_press, 3=out_of_possession
        home_poss = possessing_team == 0
        # Team-relative ball x in [0, 120] from possessing team's own goal
        home_ball_x = ball_xy[:, 0]
        away_ball_x = PITCH_LENGTH_YD - ball_xy[:, 0]

        home_counter_press = team_tactics_10d[:, 0, 6]
        away_counter_press = team_tactics_10d[:, 1, 6]
        home_press_int = team_tactics_10d[:, 0, 1]
        away_press_int = team_tactics_10d[:, 1, 1]

        home_in_poss_phase = torch.where(home_ball_x <= 42.0, 0, 1)
        away_in_poss_phase = torch.where(away_ball_x <= 42.0, 0, 1)

        home_high_press = ((seconds_since_turnover <= 6.0) & (home_counter_press > 0.50)) | (
            (away_ball_x <= 48.0) & (home_press_int > 0.45)
        )
        away_high_press = ((seconds_since_turnover <= 6.0) & (away_counter_press > 0.50)) | (
            (home_ball_x <= 48.0) & (away_press_int > 0.45)
        )

        home_def_phase = torch.where(home_high_press, 2, 3)
        away_def_phase = torch.where(away_high_press, 2, 3)

        home_phase = torch.where(home_poss, home_in_poss_phase, home_def_phase)
        away_phase = torch.where(~home_poss, away_in_poss_phase, away_def_phase)
        return torch.stack([home_phase, away_phase], dim=-1)  # [B, 2]

    def compute_phase_anchors(
        self,
        phase_anchors: torch.Tensor,       # [B, 22, 4, 5] (ax, ay, rad, shift_x, shift_y)
        phase_ids: torch.Tensor,           # [B, 2]
        ball_xy: torch.Tensor,             # [B, 2]
        effective_tactics_10d: torch.Tensor,  # [B, 2, 10]
        gk_sweeper_rate: torch.Tensor,     # [B, 2]
    ) -> torch.Tensor:
        """Layer 1: Compute phase-aware tactical anchor positions in canonical [0,120]x[0,80]."""
        bsz = phase_anchors.shape[0]
        device = phase_anchors.device
        dtype = phase_anchors.dtype

        slot_phase_ids = torch.cat(
            [phase_ids[:, 0:1].expand(bsz, 11), phase_ids[:, 1:2].expand(bsz, 11)],
            dim=1,
        )  # [B, 22]
        gather_idx = slot_phase_ids.unsqueeze(-1).unsqueeze(-1).expand(bsz, 22, 1, 5)
        active_anchor_params = torch.gather(phase_anchors, dim=2, index=gather_idx).squeeze(2)  # [B, 22, 5]

        ax_rel = active_anchor_params[..., 0]
        ay_rel = active_anchor_params[..., 1]
        shift_x = active_anchor_params[..., 3]
        shift_y = active_anchor_params[..., 4]

        # Expand team tactical sliders to [B, 22]
        slot_tactics = torch.cat(
            [
                effective_tactics_10d[:, 0:1, :].expand(bsz, 11, 10),
                effective_tactics_10d[:, 1:2, :].expand(bsz, 11, 10),
            ],
            dim=1,
        )
        line_height = slot_tactics[..., 0]
        att_width = slot_tactics[..., 4]
        def_compact = slot_tactics[..., 5]

        is_outfield = self.is_outfield_22.expand(bsz, 22)

        is_oop_or_press = torch.cat(
            [
                (phase_ids[:, 0:1] >= 2).expand(bsz, 11),
                (phase_ids[:, 1:2] >= 2).expand(bsz, 11),
            ],
            dim=1,
        )

        # Out-of-possession line shift: 22m..52m scale around nominal 0.35 centroid
        line_shift_rel = (line_height - 0.50) * 0.24
        ax_adj = torch.where(is_outfield & is_oop_or_press, ax_rel + line_shift_rel, ax_rel)

        # Out-of-possession block compression toward block centroid
        team_mean_x = torch.cat(
            [
                ax_adj[:, 1:11].mean(dim=1, keepdim=True).expand(bsz, 11),
                ax_adj[:, 12:22].mean(dim=1, keepdim=True).expand(bsz, 11),
            ],
            dim=1,
        )
        compact_scale = 1.15 - 0.38 * def_compact
        ax_adj = torch.where(
            is_outfield & is_oop_or_press,
            team_mean_x + (ax_adj - team_mean_x) * compact_scale,
            ax_adj,
        )

        # In-possession width scaling around y_rel=0.5
        width_scale = torch.where(is_oop_or_press, 1.0 - 0.25 * (def_compact - 0.5), 0.72 + 0.56 * att_width)
        ay_adj = 0.5 + (ay_rel - 0.5) * width_scale

        # Convert team-relative normalized coords to canonical [0,120]x[0,80]
        is_home = self.is_home_22.expand(bsz, 22)

        x_canon = torch.where(is_home, ax_adj * PITCH_LENGTH_YD, (1.0 - ax_adj) * PITCH_LENGTH_YD)
        y_canon = torch.where(is_home, ay_adj * PITCH_WIDTH_YD, (1.0 - ay_adj) * PITCH_WIDTH_YD)

        # Ball attraction shift in canonical coordinates
        x_canon = x_canon + shift_x * (ball_xy[:, 0:1] - 60.0)
        y_canon = y_canon + shift_y * (ball_xy[:, 1:2] - 40.0)

        # Dynamic SweeperKeeper depth relative to deepest outfielder
        home_min_def_x = x_canon[:, 1:11].min(dim=1).values
        away_max_def_x = x_canon[:, 12:22].max(dim=1).values
        home_gk_x = torch.clamp(home_min_def_x - (24.0 - 9.0 * gk_sweeper_rate[:, 0]), min=3.0, max=38.0)
        away_gk_x = torch.clamp(away_max_def_x + (24.0 - 9.0 * gk_sweeper_rate[:, 1]), min=82.0, max=117.0)
        x_canon = x_canon.clone()
        x_canon[:, 0] = home_gk_x
        x_canon[:, 11] = away_gk_x

        anchors = torch.stack([x_canon, y_canon], dim=-1).to(dtype)
        anchors[..., 0] = anchors[..., 0].clamp(2.0, PITCH_LENGTH_YD - 2.0)
        anchors[..., 1] = anchors[..., 1].clamp(2.0, PITCH_WIDTH_YD - 2.0)
        return anchors

    def _apply_kinematic_and_laws_clamps(
        self,
        proposed_xy: torch.Tensor,       # [B, 22, 2]
        prev_xy: torch.Tensor,           # [B, 22, 2]
        stamina: torch.Tensor,           # [B, 22]
        sprint_capacity: torch.Tensor,   # [B, 22]
        active_mask: torch.Tensor,       # [B, 22]
        freedom_to_roam: torch.Tensor,   # [B, 22]
        possessing_team: torch.Tensor,   # [B]
        ball_xy: torch.Tensor,           # [B, 2]
        dt_sec: torch.Tensor,            # [B]
        actor_slot_idx: Optional[torch.Tensor] = None,
        actor_pin_xy: Optional[torch.Tensor] = None,
        receiver_slot_idx: Optional[torch.Tensor] = None,
        receiver_pin_xy: Optional[torch.Tensor] = None,
    ) -> tuple[torch.Tensor, torch.Tensor]:
        """Layer 3: Enforce max sprint envelope, separation, boundary & offside-line bounds."""
        bsz = proposed_xy.shape[0]
        dt_clamped = dt_sec.clamp(0.4, 8.0).unsqueeze(-1)  # [B, 1]

        # Max speed in yards/s: (6.8..9.2 m/s) * stamina factor * 1.0936
        v_max_mps = (6.8 + 2.4 * sprint_capacity) * (0.72 + 0.28 * stamina)
        max_disp = v_max_mps * METERS_TO_YARDS * dt_clamped  # [B, 22]

        diff = proposed_xy - prev_xy
        dist = torch.linalg.norm(diff, dim=-1).clamp_min(1e-6)
        scale = torch.clamp(max_disp / dist, max=1.0)
        speed_clamped_flag = (dist > max_disp + 1e-3) & active_mask

        pos = prev_xy + diff * scale.unsqueeze(-1)

        # Pin explicit actor / receiver coordinates when an on-ball event occurred (branchless, no host sync)
        batch_arange = torch.arange(bsz, device=pos.device)
        if actor_slot_idx is not None and actor_pin_xy is not None:
            valid_a = (actor_slot_idx >= 0) & (actor_slot_idx < 22)
            safe_a = actor_slot_idx.clamp(0, 21)
            pos[batch_arange, safe_a] = torch.where(valid_a.unsqueeze(-1), actor_pin_xy, pos[batch_arange, safe_a])
        if receiver_slot_idx is not None and receiver_pin_xy is not None:
            valid_r = (receiver_slot_idx >= 0) & (receiver_slot_idx < 22)
            safe_r = receiver_slot_idx.clamp(0, 21)
            pos[batch_arange, safe_r] = torch.where(valid_r.unsqueeze(-1), receiver_pin_xy, pos[batch_arange, safe_r])

        # Passive offside line awareness for non-ball attacking outfielders
        # Home defends x=0 (deepest = smallest x); Away defends x=120 (deepest = largest x)
        home_x_active = torch.where(active_mask[:, :11], pos[:, :11, 0], torch.full_like(pos[:, :11, 0], 1e4))
        away_x_active = torch.where(active_mask[:, 11:22], pos[:, 11:22, 0], torch.full_like(pos[:, 11:22, 0], -1e4))
        home_def_sorted_x = torch.sort(home_x_active, dim=1, descending=False).values[:, 1]
        away_def_sorted_x = torch.sort(away_x_active, dim=1, descending=True).values[:, 1]

        is_home_poss = (possessing_team == 0).unsqueeze(-1)  # [B, 1]
        # Home attacks +x: offside threshold = max(60, ball_x, away_2nd_last_def_x)
        home_offside_line = torch.maximum(
            torch.maximum(ball_xy[:, 0], away_def_sorted_x),
            torch.full_like(ball_xy[:, 0], 60.0),
        ).unsqueeze(-1)
        # Away attacks -x: offside threshold = min(60, ball_x, home_2nd_last_def_x)
        away_offside_line = torch.minimum(
            torch.minimum(ball_xy[:, 0], home_def_sorted_x),
            torch.full_like(ball_xy[:, 0], 60.0),
        ).unsqueeze(-1)

        # Pull passive attackers softly back behind line based on (1 - freedom_to_roam)
        home_too_far = is_home_poss & (pos[:, :11, 0] > home_offside_line)
        home_pull_x = home_offside_line - 0.5 + 2.5 * freedom_to_roam[:, :11]
        away_too_far = (~is_home_poss) & (pos[:, 11:22, 0] < away_offside_line)
        away_pull_x = away_offside_line + 0.5 - 2.5 * freedom_to_roam[:, 11:22]

        x_new = pos[..., 0].clone()
        x_new[:, :11] = torch.where(home_too_far, torch.minimum(x_new[:, :11], home_pull_x), x_new[:, :11])
        x_new[:, 11:22] = torch.where(away_too_far, torch.maximum(x_new[:, 11:22], away_pull_x), x_new[:, 11:22])
        pos = torch.stack([x_new, pos[..., 1]], dim=-1)

        # Minimum separation soft repulsion (< 1.2 yd) across active players
        pair_diff = pos.unsqueeze(2) - pos.unsqueeze(1)  # [B, 22, 22, 2]
        pair_dist = torch.linalg.norm(pair_diff, dim=-1)  # [B, 22, 22]
        eye = self.eye_22
        both_active = active_mask.unsqueeze(2) & active_mask.unsqueeze(1) & (~eye)
        too_close = both_active & (pair_dist < 1.2) & (pair_dist > 1e-4)
        repulse_mag = torch.where(too_close, (1.2 - pair_dist) * 0.35, 0.0)
        unit_dir = pair_diff / pair_dist.clamp_min(1e-4).unsqueeze(-1)
        pos = pos + (repulse_mag.unsqueeze(-1) * unit_dir).sum(dim=2)

        # Pitch boundary clamp (allow exact actor pin on touchline/corner if specified)
        pre_bound = pos.clone()
        pos[..., 0] = pos[..., 0].clamp(0.5, PITCH_LENGTH_YD - 0.5)
        pos[..., 1] = pos[..., 1].clamp(0.5, PITCH_WIDTH_YD - 0.5)
        if actor_slot_idx is not None and actor_pin_xy is not None:
            valid_a = (actor_slot_idx >= 0) & (actor_slot_idx < 22)
            safe_a = actor_slot_idx.clamp(0, 21)
            pos[batch_arange, safe_a] = torch.where(valid_a.unsqueeze(-1), actor_pin_xy, pos[batch_arange, safe_a])

        bound_clamped = (torch.linalg.norm(pos - pre_bound, dim=-1) > 1e-3) & active_mask
        clamp_hit_fraction = (speed_clamped_flag | bound_clamped).to(pos.dtype).mean(dim=1)
        return pos, clamp_hit_fraction

    def compute_pitch_control_and_360(
        self,
        positions: torch.Tensor,        # [B, 22, 2]
        velocities: torch.Tensor,       # [B, 22, 2]
        stamina: torch.Tensor,          # [B, 22]
        anticipation: torch.Tensor,     # [B, 22]
        active_mask: torch.Tensor,      # [B, 22]
        possessing_team: torch.Tensor,  # [B]
        ball_xy: torch.Tensor,          # [B, 2]
        actor_slot_idx: torch.Tensor,   # [B] 0..21 (-1 if none)
        def_pressing_int: torch.Tensor, # [B] opponent pressing intensity [0,1]
    ) -> tuple[torch.Tensor, ...]:
        """Compute 24x16 Pitch Control, ball pressure, synthetic 360 frame & shot cone."""
        bsz = positions.shape[0]
        device = positions.device
        dtype = positions.dtype

        centers = self.grid_centers.to(device=device, dtype=dtype)  # [384, 2]
        reaction_delay = (0.55 - 0.15 * anticipation).clamp(0.30, 0.65)  # [B, 22]
        v_cruise = (5.0 + 1.8 * stamina) * METERS_TO_YARDS               # [B, 22]

        future_pos = positions + 0.4 * velocities
        # [B, 22, 384]
        dist_grid = torch.linalg.norm(centers.view(1, 1, 384, 2) - future_pos.unsqueeze(2), dim=-1)
        tau = reaction_delay.unsqueeze(-1) + dist_grid / v_cruise.unsqueeze(-1).clamp_min(1.0)
        influence = torch.exp(-self.pitch_ctrl_lambda * tau) * active_mask.to(dtype).unsqueeze(-1)

        home_inf = influence[:, :11, :].sum(dim=1)
        away_inf = influence[:, 11:22, :].sum(dim=1)
        pc_home_flat = home_inf / (home_inf + away_inf + 1e-6)  # [B, 384]
        pitch_control_grid = pc_home_flat.view(bsz, NUM_PITCH_GRID_X, NUM_PITCH_GRID_Y)
        home_voronoi_control = pc_home_flat.mean(dim=-1)

        # Sample local pitch control at each player's cell
        cell_x = (positions[..., 0] / 5.0).long().clamp(0, NUM_PITCH_GRID_X - 1)
        cell_y = (positions[..., 1] / 5.0).long().clamp(0, NUM_PITCH_GRID_Y - 1)
        cell_flat_idx = cell_x * NUM_PITCH_GRID_Y + cell_y  # [B, 22]
        pc_home_at_player = torch.gather(pc_home_flat, dim=1, index=cell_flat_idx)
        is_home_slot = self.is_home_22.expand(bsz, 22)
        local_pc = torch.where(is_home_slot, pc_home_at_player, 1.0 - pc_home_at_player)

        # Continuous ball-carrier pressure score from defending team
        r_press = (3.5 + 2.0 * def_pressing_int).unsqueeze(-1)  # [B, 1]
        dist_to_ball_raw = torch.linalg.norm(positions - ball_xy.unsqueeze(1), dim=-1)  # [B, 22]
        is_home_poss = (possessing_team == 0).unsqueeze(-1)
        is_defender = torch.where(is_home_poss, ~is_home_slot, is_home_slot) & active_mask
        press_contrib = torch.exp(-(dist_to_ball_raw ** 2) / (2.0 * (r_press ** 2)))
        ball_pressure = (press_contrib * is_defender.to(dtype)).sum(dim=1)
        under_pressure = ball_pressure >= self.pressure_threshold

        # Synthetic 360 freeze frame [B, 22, 7]: (x, y, vx, vy, is_teammate, is_actor, is_gk)
        is_teammate = torch.where(is_home_poss, is_home_slot, ~is_home_slot).to(dtype)
        slot_ids = self.slot_ids_22.expand(bsz, 22)
        is_actor = (slot_ids == actor_slot_idx.unsqueeze(-1)).to(dtype)
        is_gk = self.is_gk_22.to(dtype).expand(bsz, 22)
        freeze_frame_360 = torch.cat(
            [
                positions,
                velocities,
                is_teammate.unsqueeze(-1),
                is_actor.unsqueeze(-1),
                is_gk.unsqueeze(-1),
            ],
            dim=-1,
        )  # [B, 22, 7]

        # Defenders inside shot-to-goal triangle (ball_xy, post_L, post_R)
        target_goal_x = torch.where(
            possessing_team == 0,
            torch.full_like(ball_xy[:, 0], PITCH_LENGTH_YD),
            torch.zeros_like(ball_xy[:, 0]),
        )
        post_l = torch.stack([target_goal_x, torch.full_like(target_goal_x, 36.0)], dim=-1)
        post_r = torch.stack([target_goal_x, torch.full_like(target_goal_x, 44.0)], dim=-1)
        in_cone = _points_in_triangle_2d(positions, ball_xy, post_l, post_r) & is_defender
        defenders_in_shot_cone = in_cone.long().sum(dim=1)

        gk_position_xy = torch.where(is_home_poss, positions[:, 11, :], positions[:, 0, :])
        dist_to_ball_norm = (dist_to_ball_raw / 144.22).clamp(0.0, 1.0)

        return (
            pitch_control_grid,
            home_voronoi_control,
            local_pc,
            dist_to_ball_norm,
            ball_pressure,
            under_pressure,
            freeze_frame_360,
            defenders_in_shot_cone,
            gk_position_xy,
        )

    def compute_pass_targets_and_priors(
        self,
        positions: torch.Tensor,          # [B, 22, 2]
        velocities: torch.Tensor,         # [B, 22, 2]
        active_mask: torch.Tensor,        # [B, 22]
        local_pc: torch.Tensor,           # [B, 22]
        possessing_team: torch.Tensor,    # [B]
        actor_slot_idx: torch.Tensor,     # [B] 0..21 (-1 if unset)
        ball_xy: torch.Tensor,            # [B, 2]
        ball_pressure: torch.Tensor,      # [B]
        poss_tactics_10d: torch.Tensor,   # [B, 10]
        restart_offside_exempt: Optional[torch.Tensor] = None,
    ) -> tuple[torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor]:
        """Compute 11x11 pass lane clearance, offside trap, target mask & spatial log-prior."""
        bsz = positions.shape[0]
        device = positions.device
        dtype = positions.dtype

        is_home_poss = possessing_team == 0  # [B]
        # Partition team (11) vs opponent (11) positions
        team_pos = torch.where(is_home_poss.view(bsz, 1, 1), positions[:, :11, :], positions[:, 11:22, :])
        team_vel = torch.where(is_home_poss.view(bsz, 1, 1), velocities[:, :11, :], velocities[:, 11:22, :])
        opp_pos = torch.where(is_home_poss.view(bsz, 1, 1), positions[:, 11:22, :], positions[:, :11, :])
        team_active = torch.where(is_home_poss.unsqueeze(-1), active_mask[:, :11], active_mask[:, 11:22])
        opp_active = torch.where(is_home_poss.unsqueeze(-1), active_mask[:, 11:22], active_mask[:, :11])
        team_pc = torch.where(is_home_poss.unsqueeze(-1), local_pc[:, :11], local_pc[:, 11:22])

        # Identify local actor index (0..10) or fall back to nearest teammate to ball
        local_actor = torch.where(
            is_home_poss,
            actor_slot_idx.clamp(0, 10),
            (actor_slot_idx - 11).clamp(0, 10),
        )
        valid_actor = (actor_slot_idx >= 0) & (actor_slot_idx < 22)
        nearest_tm = torch.linalg.norm(team_pos - ball_xy.unsqueeze(1), dim=-1).argmin(dim=1)
        local_actor = torch.where(valid_actor, local_actor, nearest_tm)

        actor_pos = team_pos[torch.arange(bsz, device=device), local_actor]  # [B, 2]
        pass_vec = team_pos - actor_pos.unsqueeze(1)  # [B, 11, 2]
        pass_dist = torch.linalg.norm(pass_vec, dim=-1).clamp_min(1e-4)  # [B, 11]
        flight_time = (pass_dist / 19.0).clamp(0.15, 2.8)
        recv_pred_pos = team_pos + team_vel * flight_time.unsqueeze(-1) * 0.4

        # Segment-to-opponent distance across all 11 targets x 11 defenders: [B, 11, 11]
        seg = recv_pred_pos - actor_pos.unsqueeze(1)  # [B, 11, 2]
        seg_len_sq = (seg ** 2).sum(dim=-1).clamp_min(1e-4)  # [B, 11]
        rel_opp = opp_pos.unsqueeze(1) - actor_pos.view(bsz, 1, 1, 2)  # [B, 11, 11, 2]
        t_proj = ((rel_opp * seg.unsqueeze(2)).sum(dim=-1) / seg_len_sq.unsqueeze(2)).clamp(0.0, 1.0)
        closest_pt = actor_pos.view(bsz, 1, 1, 2) + t_proj.unsqueeze(-1) * seg.unsqueeze(2)
        perp_dist = torch.linalg.norm(opp_pos.unsqueeze(1) - closest_pt, dim=-1)  # [B, 11, 11]
        perp_dist = torch.where(opp_active.unsqueeze(1).expand(bsz, 11, 11), perp_dist, torch.full_like(perp_dist, 99.0))
        min_lane_dist = perp_dist.min(dim=-1).values  # [B, 11]

        # Lane clearance score in [0, 1]
        lane_clearance = torch.sigmoid((min_lane_dist - 2.2) * 0.9)

        # Offside check in attacking-relative coordinates (+x = toward opponent goal)
        team_x_att = torch.where(is_home_poss.unsqueeze(-1), team_pos[..., 0], PITCH_LENGTH_YD - team_pos[..., 0])
        ball_x_att = torch.where(is_home_poss, ball_xy[:, 0], PITCH_LENGTH_YD - ball_xy[:, 0])
        opp_x_att = torch.where(is_home_poss.unsqueeze(-1), opp_pos[..., 0], PITCH_LENGTH_YD - opp_pos[..., 0])
        opp_x_active = torch.where(opp_active, opp_x_att, torch.full_like(opp_x_att, -1e4))
        opp_second_last_x = torch.sort(opp_x_active, dim=1, descending=True).values[:, 1]
        offside_thresh = torch.maximum(
            torch.maximum(ball_x_att, opp_second_last_x),
            torch.full_like(ball_x_att, 60.0),
        ).unsqueeze(-1)
        is_offside = team_x_att > (offside_thresh + 0.35)
        if restart_offside_exempt is not None:
            is_offside = is_offside & (~restart_offside_exempt.unsqueeze(-1))

        # Tactical alignment: directness prefers longer forward passes; width prefers wide targets
        directness = poss_tactics_10d[:, 3].unsqueeze(-1)
        att_width = poss_tactics_10d[:, 4].unsqueeze(-1)
        risk_appetite = poss_tactics_10d[:, 8].unsqueeze(-1)

        actor_x_att = torch.where(is_home_poss, actor_pos[:, 0], PITCH_LENGTH_YD - actor_pos[:, 0]).unsqueeze(-1)
        forward_gain = ((team_x_att - actor_x_att) / 40.0).clamp(-1.0, 1.5)
        lateral_offset = ((team_pos[..., 1] - 40.0).abs() / 40.0).clamp(0.0, 1.0)

        tactical_align = torch.sigmoid(
            1.2 * (directness - 0.5) * forward_gain
            + 0.8 * (att_width - 0.5) * lateral_offset
            + 0.5 * risk_appetite * (1.0 - lane_clearance)
        )
        offside_factor = torch.where(is_offside, torch.full_like(lane_clearance, 0.12), torch.ones_like(lane_clearance))

        feasibility = (0.55 * lane_clearance + 0.30 * team_pc + 0.15 * tactical_align) * offside_factor
        # Mask self-target and sent-off teammates
        is_self = self.slot_ids_11 == local_actor.unsqueeze(-1)
        target_mask = team_active & (~is_self)
        feasibility = torch.where(target_mask, feasibility.clamp(1e-4, 1.0), torch.zeros_like(feasibility))

        target_log_prior = torch.where(
            target_mask,
            torch.log(feasibility.clamp_min(1e-4)),
            torch.full_like(feasibility, -1e9),
        )

        # Action priors over |A|=num_actions (PASS=0, CARRY=1, DRIBBLE=2, SHOT=3, CLEARANCE=4, ...)
        action_prior = torch.zeros((bsz, self.num_actions), device=device, dtype=dtype)
        dist_to_goal = torch.hypot(PITCH_LENGTH_YD - ball_x_att, ball_xy[:, 1] - 40.0)
        # Penalize far shots and calibrate box shot frequency toward real ~24-28 shots/match
        if self.num_actions > 3:
            action_prior[:, 3] = (-1.25 + (20.0 - dist_to_goal) * 0.04).clamp(-3.5, -1.05)
        # Directness & pressure slightly shift PASS vs CARRY/CLEARANCE
        action_prior[:, 0] = 0.25 * (1.0 - poss_tactics_10d[:, 2])
        if self.num_actions > 1:
            action_prior[:, 1] = 0.20 * (1.0 - ball_pressure.clamp(0.0, 1.5))
        if self.num_actions > 4:
            action_prior[:, 4] = torch.where(ball_x_att < 32.0, 0.4 * ball_pressure, torch.full_like(ball_pressure, -1.5))

        spatial_log_prior = torch.cat([action_prior, target_log_prior], dim=-1)
        return feasibility, is_offside, target_mask, spatial_log_prior

    def step(
        self,
        prev_positions: torch.Tensor,
        phase_anchors: torch.Tensor,
        possessing_team: torch.Tensor,
        ball_xy: torch.Tensor,
        ball_end_xy: torch.Tensor,
        seconds_since_turnover: torch.Tensor,
        dt_sec: torch.Tensor,
        effective_tactics_10d: torch.Tensor,
        stamina: torch.Tensor,
        sprint_capacity: torch.Tensor,
        anticipation: torch.Tensor,
        freedom_to_roam: torch.Tensor,
        role_forward_bias: torch.Tensor,
        gk_sweeper_rate: torch.Tensor,
        active_mask: torch.Tensor,
        actor_slot_idx: torch.Tensor,
        actor_pin_xy: Optional[torch.Tensor] = None,
        receiver_slot_idx: Optional[torch.Tensor] = None,
        receiver_pin_xy: Optional[torch.Tensor] = None,
        restart_offside_exempt: Optional[torch.Tensor] = None,
    ) -> SpatialStepOutput:
        """Execute Layers 1-3 + Pitch Control + 360 Freeze Frame + Pass Target Feasibility."""
        bsz = prev_positions.shape[0]
        device = prev_positions.device
        dtype = prev_positions.dtype

        phase_ids = self.determine_team_phases(
            possessing_team=possessing_team,
            ball_xy=ball_xy,
            seconds_since_turnover=seconds_since_turnover,
            team_tactics_10d=effective_tactics_10d,
        )
        anchors = self.compute_phase_anchors(
            phase_anchors=phase_anchors,
            phase_ids=phase_ids,
            ball_xy=ball_xy,
            effective_tactics_10d=effective_tactics_10d,
            gk_sweeper_rate=gk_sweeper_rate,
        )

        # Layer 2: Residual SpatialPredictor inputs [B, 22, 18]
        is_home = self.is_home_22.to(dtype).expand(bsz, 22)
        is_gk = self.is_gk_22.to(dtype).expand(bsz, 22)
        poss_flag = torch.where(
            (possessing_team == 0).unsqueeze(-1),
            is_home,
            1.0 - is_home,
        )
        slot_phase_norm = torch.cat(
            [
                (phase_ids[:, 0:1].to(dtype) / 3.0).expand(bsz, 11),
                (phase_ids[:, 1:2].to(dtype) / 3.0).expand(bsz, 11),
            ],
            dim=1,
        )
        slot_press = torch.cat(
            [
                effective_tactics_10d[:, 0:1, 1].expand(bsz, 11),
                effective_tactics_10d[:, 1:2, 1].expand(bsz, 11),
            ],
            dim=1,
        )
        scale_xy = self.scale_xy.to(dtype)
        slot_feats = torch.cat(
            [
                prev_positions / scale_xy,
                anchors / scale_xy,
                (ball_xy / scale_xy).unsqueeze(1).expand(bsz, 22, 2),
                (ball_end_xy / scale_xy).unsqueeze(1).expand(bsz, 22, 2),
                is_home.unsqueeze(-1),
                is_gk.unsqueeze(-1),
                stamina.unsqueeze(-1),
                freedom_to_roam.unsqueeze(-1),
                role_forward_bias.unsqueeze(-1),
                slot_phase_norm.unsqueeze(-1),
                active_mask.to(dtype).unsqueeze(-1),
                poss_flag.unsqueeze(-1),
                (dt_sec.clamp(0.1, 10.0) / 10.0).view(bsz, 1, 1).expand(bsz, 22, 1),
                slot_press.unsqueeze(-1),
            ],
            dim=-1,
        )
        delta_xy, _ = self.spatial_predictor(slot_feats, active_mask)
        proposed_xy = anchors + delta_xy

        # Layer 3: Kinematic & laws-of-the-game clamps
        positions, clamp_hit_fraction = self._apply_kinematic_and_laws_clamps(
            proposed_xy=proposed_xy,
            prev_xy=prev_positions,
            stamina=stamina,
            sprint_capacity=sprint_capacity,
            active_mask=active_mask,
            freedom_to_roam=freedom_to_roam,
            possessing_team=possessing_team,
            ball_xy=ball_xy,
            dt_sec=dt_sec,
            actor_slot_idx=actor_slot_idx,
            actor_pin_xy=actor_pin_xy,
            receiver_slot_idx=receiver_slot_idx,
            receiver_pin_xy=receiver_pin_xy,
        )
        velocities = (positions - prev_positions) / dt_sec.clamp_min(0.4).view(bsz, 1, 1)

        def_pressing_int = torch.where(
            possessing_team == 0,
            effective_tactics_10d[:, 1, 1],
            effective_tactics_10d[:, 0, 1],
        )
        poss_tactics_10d = torch.where(
            (possessing_team == 0).view(bsz, 1),
            effective_tactics_10d[:, 0, :],
            effective_tactics_10d[:, 1, :],
        )

        (
            pitch_control_grid,
            home_voronoi_control,
            local_pc,
            dist_to_ball_norm,
            ball_pressure,
            under_pressure,
            freeze_frame_360,
            defenders_in_shot_cone,
            gk_position_xy,
        ) = self.compute_pitch_control_and_360(
            positions=positions,
            velocities=velocities,
            stamina=stamina,
            anticipation=anticipation,
            active_mask=active_mask,
            possessing_team=possessing_team,
            ball_xy=ball_xy,
            actor_slot_idx=actor_slot_idx,
            def_pressing_int=def_pressing_int,
        )

        feasibility, offside_flags, target_mask, spatial_log_prior = self.compute_pass_targets_and_priors(
            positions=positions,
            velocities=velocities,
            active_mask=active_mask,
            local_pc=local_pc,
            possessing_team=possessing_team,
            actor_slot_idx=actor_slot_idx,
            ball_xy=ball_xy,
            ball_pressure=ball_pressure,
            poss_tactics_10d=poss_tactics_10d,
            restart_offside_exempt=restart_offside_exempt,
        )

        return SpatialStepOutput(
            positions=positions,
            velocities=velocities,
            phase_ids=phase_ids,
            pitch_control_grid=pitch_control_grid,
            home_voronoi_control=home_voronoi_control,
            local_pitch_control=local_pc,
            dist_to_ball=dist_to_ball_norm,
            ball_pressure=ball_pressure,
            under_pressure=under_pressure,
            freeze_frame_360=freeze_frame_360,
            defenders_in_shot_cone=defenders_in_shot_cone,
            gk_position_xy=gk_position_xy,
            target_feasibility=feasibility,
            offside_flags=offside_flags,
            target_mask=target_mask,
            spatial_log_prior=spatial_log_prior,
            clamp_hit_fraction=clamp_hit_fraction,
        )


def _points_in_triangle_2d(
    pts: torch.Tensor,   # [B, 22, 2]
    a: torch.Tensor,     # [B, 2]
    b: torch.Tensor,     # [B, 2]
    c: torch.Tensor,     # [B, 2]
) -> torch.Tensor:
    """Vectorized 2D point-in-triangle check via same-side cross-product signs."""
    a_ = a.unsqueeze(1)
    b_ = b.unsqueeze(1)
    c_ = c.unsqueeze(1)

    def _sign(p1: torch.Tensor, p2: torch.Tensor, p3: torch.Tensor) -> torch.Tensor:
        return (p1[..., 0] - p3[..., 0]) * (p2[..., 1] - p3[..., 1]) - (p2[..., 0] - p3[..., 0]) * (p1[..., 1] - p3[..., 1])

    d1 = _sign(pts, a_, b_)
    d2 = _sign(pts, b_, c_)
    d3 = _sign(pts, c_, a_)
    has_neg = (d1 < 0) | (d2 < 0) | (d3 < 0)
    has_pos = (d1 > 0) | (d2 > 0) | (d3 > 0)
    return ~(has_neg & has_pos)
