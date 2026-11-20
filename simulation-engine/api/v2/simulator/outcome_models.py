"""Module D: Dedicated Calibrated Outcome & Analytics Models (xG, PSxG, xPass, xT, VAEP, Turnover).

Implements the Tier-2 GPU-resident architecture per docs/revamp/03_hybrid_event_transformer_and_models.md §3:
- Zero CPU-GPU PCIe synchronization during B=128..256 Monte Carlo rollouts.
- FreezeFrame360FeatureExtractor: extracts defender shot cones, GK angle/reach, and passing
  corridor clearances directly from Module B's [B, 22, 7] synthetic 360 freeze-frame tensor.
- D1..D6 calibrated sub-models combining calibrated physical/geometric priors with trainable
  residual MLPs (distilled from offline CatBoost/LightGBM teachers) + temperature scaling.
- Bayesian Log-Odds Calibration Bridge integrating player skill residuals and fatigue cleanly.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Optional, Tuple

import math
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch import FloatTensor, LongTensor, Tensor

from api.v2.simulator.fsm import (
    ActionType,
    BodyPart,
    EventOutcome,
    GOAL_Y_MAX,
    GOAL_Y_MIN,
    GOAL_Z_MAX,
    NUM_OUTCOMES,
    PITCH_LENGTH,
    PITCH_WIDTH,
    PassHeight,
    RestartType,
)


# ---------------------------------------------------------------------------
# Calibration & Math Utilities
# ---------------------------------------------------------------------------

def safe_logit(p: Tensor, eps: float = 1e-5) -> Tensor:
    p_c = p.clamp(min=eps, max=1.0 - eps)
    return torch.log(p_c / (1.0 - p_c))


def expected_calibration_error(probs: Tensor, targets: Tensor, n_bins: int = 15) -> float:
    """Compute Expected Calibration Error (ECE) on binary probabilities and 0/1 targets."""
    p = probs.detach().float().reshape(-1)
    y = targets.detach().float().reshape(-1)
    if p.numel() == 0:
        return 0.0
    bin_edges = torch.linspace(0.0, 1.0, n_bins + 1, device=p.device)
    ece = torch.tensor(0.0, device=p.device)
    for b in range(n_bins):
        lo, hi = bin_edges[b], bin_edges[b + 1]
        mask = (p >= lo) & (p <= hi if b == n_bins - 1 else p < hi)
        if mask.any():
            ece = ece + (mask.float().mean() * torch.abs(y[mask].mean() - p[mask].mean()))
    return float(ece.item())


def apply_bayesian_log_odds(
    base_prob: FloatTensor,
    actor_residual: Optional[FloatTensor] = None,
    opp_residual: Optional[FloatTensor] = None,
    actor_stamina: Optional[FloatTensor] = None,
    w_actor: float = 1.0,
    w_opp: float = 1.0,
    lambda_fatigue: float = 0.35,
) -> FloatTensor:
    """Zero-centered log-odds calibration bridge (03 §3.4).

    logit(p_sim) = logit(p_base) + w_actor * delta_actor - w_opp * delta_opp
                   - lambda_fatigue * max(0, fatigue - 0.5)
    where fatigue = 1.0 - stamina (aligned with 02 §3.6 stamina in [0, 1]).
    """
    logit = safe_logit(base_prob)
    if actor_residual is not None:
        logit = logit + w_actor * actor_residual.clamp(-0.65, 0.65)
    if opp_residual is not None:
        logit = logit - w_opp * opp_residual.clamp(-0.65, 0.65)
    if actor_stamina is not None:
        fatigue = (1.0 - actor_stamina).clamp(0.0, 1.0)
        logit = logit - lambda_fatigue * torch.clamp(fatigue - 0.5, min=0.0)
    return torch.sigmoid(logit)


# ---------------------------------------------------------------------------
# 360 Freeze-Frame Vectorized Geometry Extractor ([B, 22, 7])
# ---------------------------------------------------------------------------

class FreezeFrame360FeatureExtractor(nn.Module):
    """Extracts 360 shot-cone, GK line-of-sight, and passing-corridor features from [B, 22, 7].

    Expected freeze_frame_360 last-dim layout (per 02_tactical_state_and_experimentation.md §5):
    [x, y, vx, vy, is_teammate, is_actor, is_gk] in Canonical Global Pitch Frame [0..120, 0..80].
    """

    def forward_shot_features(
        self,
        freeze_frame: FloatTensor,  # [B, 22, 7]
        start_xy: FloatTensor,      # [B, 2]
        end_xy: FloatTensor,        # [B, 2]
        end_z: FloatTensor,         # [B]
        poss_team: LongTensor,      # [B] 1=home, 2=away
    ) -> Dict[str, Tensor]:
        B = start_xy.shape[0]
        dev = start_xy.device

        # Target goal center and posts in canonical coordinates
        goal_x = torch.where(poss_team == 1, PITCH_LENGTH, 0.0)
        goal_center = torch.stack([goal_x, torch.full((B,), 40.0, device=dev)], dim=-1)
        post_l = torch.stack([goal_x, torch.full((B,), GOAL_Y_MIN, device=dev)], dim=-1)
        post_r = torch.stack([goal_x, torch.full((B,), GOAL_Y_MAX, device=dev)], dim=-1)

        vec_to_goal = goal_center - start_xy
        dist_to_goal = torch.linalg.norm(vec_to_goal, dim=-1).clamp(min=0.5)

        # Visible goal aperture angle (law of cosines between posts)
        v1 = post_l - start_xy
        v2 = post_r - start_xy
        n1 = torch.linalg.norm(v1, dim=-1).clamp(min=0.5)
        n2 = torch.linalg.norm(v2, dim=-1).clamp(min=0.5)
        cos_ang = ((v1 * v2).sum(dim=-1) / (n1 * n2)).clamp(-1.0 + 1e-6, 1.0 - 1e-6)
        shot_angle = torch.acos(cos_ang)  # radians in (0, pi)

        # Extract player coordinates & roles from [B, 22, 7]
        pxy = freeze_frame[..., :2]             # [B, 22, 2]
        is_teammate = freeze_frame[..., 4] > 0.5
        is_gk = freeze_frame[..., 6] > 0.5
        is_opp = ~is_teammate
        is_opp_gk = is_opp & is_gk

        # Opposing GK position (fallback to goal line if mask absent)
        gk_weights = is_opp_gk.float()
        has_gk = gk_weights.sum(dim=-1, keepdim=True) > 0
        gk_xy = (pxy * gk_weights.unsqueeze(-1)).sum(dim=1) / gk_weights.sum(dim=-1, keepdim=True).clamp(min=1.0)
        gk_xy = torch.where(has_gk, gk_xy, goal_center)

        gk_dist_goal = torch.linalg.norm(gk_xy - goal_center, dim=-1)
        # Perpendicular offset of GK from the shot ray (start_xy -> goal_center)
        ray_dir = vec_to_goal / dist_to_goal.unsqueeze(-1)
        gk_rel = gk_xy - start_xy
        gk_proj = (gk_rel * ray_dir).sum(dim=-1)
        gk_perp = torch.abs(gk_rel[:, 0] * ray_dir[:, 1] - gk_rel[:, 1] * ray_dir[:, 0])
        gk_angle_offset = torch.atan2(gk_perp, gk_proj.clamp(min=0.5))

        # Shot triangle / cone containment for defenders
        # Triangle vertices: A=start_xy, B=post_l, C=post_r
        def _cross2(u: Tensor, v: Tensor) -> Tensor:
            return u[..., 0] * v[..., 1] - u[..., 1] * v[..., 0]

        p_a = start_xy.unsqueeze(1)
        p_b = post_l.unsqueeze(1)
        p_c = post_r.unsqueeze(1)
        v_ab = p_b - p_a
        v_bc = p_c - p_b
        v_ca = p_a - p_c
        s1 = _cross2(v_ab, pxy - p_a)
        s2 = _cross2(v_bc, pxy - p_b)
        s3 = _cross2(v_ca, pxy - p_c)
        in_tri = ((s1 >= 0) & (s2 >= 0) & (s3 >= 0)) | ((s1 <= 0) & (s2 <= 0) & (s3 <= 0))
        def_in_cone = (in_tri & is_opp & (~is_opp_gk)).float().sum(dim=-1)

        # Closest opponent defender distance to shooter
        opp_dists = torch.linalg.norm(pxy - start_xy.unsqueeze(1), dim=-1)
        opp_dists = torch.where(is_opp, opp_dists, torch.full_like(opp_dists, 120.0))
        closest_def_dist = opp_dists.min(dim=-1).values

        # Placement metrics for PSxG (end_y in [36, 44], end_z in [0, 2.67])
        in_frame_y = (end_xy[:, 1] >= GOAL_Y_MIN) & (end_xy[:, 1] <= GOAL_Y_MAX)
        in_frame_z = (end_z >= 0.0) & (end_z <= GOAL_Z_MAX)
        on_target = in_frame_y & in_frame_z
        dist_nearest_post = torch.minimum(
            torch.abs(end_xy[:, 1] - GOAL_Y_MIN),
            torch.abs(end_xy[:, 1] - GOAL_Y_MAX),
        )
        dist_crossbar = torch.abs(GOAL_Z_MAX - end_z)
        corner_precision = torch.exp(-0.4 * torch.minimum(dist_nearest_post, dist_crossbar))
        gk_reach_dist = torch.sqrt(
            (end_xy[:, 1] - gk_xy[:, 1]).pow(2) + (end_z - 1.1).clamp(min=0.0).pow(2)
        )

        return {
            "dist_to_goal": dist_to_goal,
            "shot_angle": shot_angle,
            "def_in_cone": def_in_cone,
            "closest_def_dist": closest_def_dist,
            "gk_dist_goal": gk_dist_goal,
            "gk_angle_offset": gk_angle_offset,
            "on_target": on_target,
            "corner_precision": corner_precision,
            "gk_reach_dist": gk_reach_dist,
        }

    def forward_pass_features(
        self,
        freeze_frame: FloatTensor,  # [B, 22, 7]
        start_xy: FloatTensor,      # [B, 2]
        end_xy: FloatTensor,        # [B, 2]
        poss_team: LongTensor,      # [B]
    ) -> Dict[str, Tensor]:
        vec = end_xy - start_xy
        pass_len = torch.linalg.norm(vec, dim=-1).clamp(min=0.1)
        # Forward progress toward opponent goal
        dx_att = torch.where(poss_team == 1, vec[:, 0], -vec[:, 0])
        pass_angle = torch.atan2(torch.abs(vec[:, 1]), dx_att + 1e-4)

        pxy = freeze_frame[..., :2]
        is_teammate = freeze_frame[..., 4] > 0.5
        is_opp = ~is_teammate

        # Distance of opponents to pass segment [start_xy -> end_xy]
        seg_u = vec / pass_len.unsqueeze(-1)
        rel_p = pxy - start_xy.unsqueeze(1)
        proj = (rel_p * seg_u.unsqueeze(1)).sum(dim=-1)
        t_clamped = (proj / pass_len.unsqueeze(-1)).clamp(0.0, 1.0)
        closest_pt = start_xy.unsqueeze(1) + t_clamped.unsqueeze(-1) * vec.unsqueeze(1)
        perp_dist = torch.linalg.norm(pxy - closest_pt, dim=-1)

        # Ignore opponents behind passer or way beyond target
        in_corridor = is_opp & (proj >= -1.0) & (proj <= pass_len.unsqueeze(-1) + 2.0)
        corr_dists = torch.where(in_corridor, perp_dist, torch.full_like(perp_dist, 60.0))
        min_def_to_ray = corr_dists.min(dim=-1).values
        n_def_in_corridor = (in_corridor & (perp_dist < 3.5)).float().sum(dim=-1)

        # Receiver openness & passer pressure
        opp_to_end = torch.linalg.norm(pxy - end_xy.unsqueeze(1), dim=-1)
        opp_to_end = torch.where(is_opp, opp_to_end, torch.full_like(opp_to_end, 60.0))
        receiver_closest_def = opp_to_end.min(dim=-1).values

        opp_to_start = torch.linalg.norm(pxy - start_xy.unsqueeze(1), dim=-1)
        opp_to_start = torch.where(is_opp, opp_to_start, torch.full_like(opp_to_start, 60.0))
        n_pressers_3m = (opp_to_start <= 3.5).float().sum(dim=-1)
        pressure_score = torch.exp(-(opp_to_start.pow(2)) / (2.0 * 4.0**2)).sum(dim=-1)

        return {
            "pass_len": pass_len,
            "dx_att": dx_att,
            "pass_angle": pass_angle,
            "min_def_to_ray": min_def_to_ray,
            "n_def_in_corridor": n_def_in_corridor,
            "receiver_closest_def": receiver_closest_def,
            "n_pressers_3m": n_pressers_3m,
            "pressure_score": pressure_score,
        }


# ---------------------------------------------------------------------------
# D4: Bellman Expected Threat (24 x 16) + Continuous Bilinear Interpolation
# ---------------------------------------------------------------------------

class BellmanXTGrid(nn.Module):
    """24 x 16 canonical Expected Threat grid solved via Bellman value iteration."""

    def __init__(self, nx: int = 24, ny: int = 16):
        super().__init__()
        self.nx = nx
        self.ny = ny
        grid = self._build_analytical_prior_grid(nx, ny)
        self.register_buffer("xt_grid", grid)  # [24, 16]

    @staticmethod
    def _build_analytical_prior_grid(nx: int, ny: int) -> FloatTensor:
        """Solve 25-iteration Bellman fixed point on realistic football move/shoot/xG priors."""
        gx = (torch.arange(nx, dtype=torch.float32) + 0.5) * (PITCH_LENGTH / nx)
        gy = (torch.arange(ny, dtype=torch.float32) + 0.5) * (PITCH_WIDTH / ny)
        xx, yy = torch.meshgrid(gx, gy, indexing="ij")

        dist_goal = torch.sqrt((PITCH_LENGTH - xx).pow(2) + (40.0 - yy).pow(2)).clamp(min=1.0)
        # Shoot probability rises sharply inside ~24 yards
        shoot_p = torch.sigmoid((22.0 - dist_goal) * 0.22) * 0.60
        move_p = 1.0 - shoot_p
        # Zone xG prior calibrated to Karun Singh xT scale (~0.25-0.40 inside 6-12 yds)
        xg_zone = torch.sigmoid(-0.55 - 0.115 * dist_goal + 0.9 * torch.exp(-((yy - 40.0) / 14.0).pow(2)))

        # Local transition kernel toward adjacent forward/central cells
        n_cells = nx * ny
        flat_x = xx.reshape(-1)
        flat_y = yy.reshape(-1)
        dx = flat_x.unsqueeze(0) - flat_x.unsqueeze(1)  # [N, N]
        dy = flat_y.unsqueeze(0) - flat_y.unsqueeze(1)
        step_dist = torch.sqrt(dx.pow(2) + dy.pow(2))
        # Realistic progressive pass/carry kernel (5..25 yards, slight forward bias)
        T = torch.exp(-((step_dist - 10.0) / 8.0).pow(2)) * torch.sigmoid(dx * 0.08 + 0.4)
        T.fill_diagonal_(0.0)
        T = T / T.sum(dim=-1, keepdim=True).clamp(min=1e-6)

        xt = BellmanXTGrid.solve_bellman(
            shoot_probs=shoot_p.reshape(-1),
            move_probs=move_p.reshape(-1),
            xg_values=xg_zone.reshape(-1),
            transition_matrix=T,
            n_iters=30,
        )
        return xt.view(nx, ny)

    @staticmethod
    def solve_bellman(
        shoot_probs: FloatTensor,
        move_probs: FloatTensor,
        xg_values: FloatTensor,
        transition_matrix: FloatTensor,
        n_iters: int = 30,
        tol: float = 1e-6,
    ) -> FloatTensor:
        """Bellman fixed-point iteration: xT = s * xG + m * (T @ xT)."""
        xt = torch.zeros_like(xg_values)
        shoot_term = shoot_probs * xg_values
        for _ in range(n_iters):
            nxt = shoot_term + move_probs * (transition_matrix @ xt)
            if torch.max(torch.abs(nxt - xt)).item() < tol:
                return nxt
            xt = nxt
        return xt

    def fit_from_transitions(
        self,
        shoot_probs: FloatTensor,
        move_probs: FloatTensor,
        xg_values: FloatTensor,
        transition_matrix: FloatTensor,
        n_iters: int = 50,
    ) -> float:
        solved = self.solve_bellman(shoot_probs, move_probs, xg_values, transition_matrix, n_iters=n_iters)
        residual = float(
            torch.max(torch.abs(solved - (shoot_probs * xg_values + move_probs * (transition_matrix @ solved)))).item()
        )
        self.xt_grid.copy_(solved.view(self.nx, self.ny))
        return residual

    def bilinear_query(self, xy: FloatTensor, poss_team: LongTensor) -> FloatTensor:
        """Continuous bilinear xT interpolation in canonical attacking direction."""
        x_att = torch.where(poss_team == 1, xy[:, 0], PITCH_LENGTH - xy[:, 0]).clamp(0.0, PITCH_LENGTH - 1e-4)
        y_att = torch.where(poss_team == 1, xy[:, 1], PITCH_WIDTH - xy[:, 1]).clamp(0.0, PITCH_WIDTH - 1e-4)

        fx = (x_att / PITCH_LENGTH) * (self.nx - 1)
        fy = (y_att / PITCH_WIDTH) * (self.ny - 1)
        x0 = fx.long().clamp(0, self.nx - 2)
        y0 = fy.long().clamp(0, self.ny - 2)
        x1 = x0 + 1
        y1 = y0 + 1
        wx = (fx - x0.float()).clamp(0.0, 1.0)
        wy = (fy - y0.float()).clamp(0.0, 1.0)

        g = self.xt_grid
        v00 = g[x0, y0]
        v01 = g[x0, y1]
        v10 = g[x1, y0]
        v11 = g[x1, y1]
        return (1 - wx) * (1 - wy) * v00 + (1 - wx) * wy * v01 + wx * (1 - wy) * v10 + wx * wy * v11

    def compute_xt_delta(self, start_xy: FloatTensor, end_xy: FloatTensor, poss_team: LongTensor) -> FloatTensor:
        return self.bilinear_query(end_xy, poss_team) - self.bilinear_query(start_xy, poss_team)


# ---------------------------------------------------------------------------
# Residual Calibrated MLP Head (Tier-2 Distilled Student)
# ---------------------------------------------------------------------------

class ResidualCalibratedMLP(nn.Module):
    """Compact 3-layer residual MLP with temperature scaling for GPU-resident Module D inference."""

    def __init__(self, in_dim: int, out_dim: int, hidden_dim: int = 128):
        super().__init__()
        self.fc_in = nn.Linear(in_dim, hidden_dim)
        self.res1 = nn.Sequential(nn.LayerNorm(hidden_dim), nn.SiLU(), nn.Linear(hidden_dim, hidden_dim))
        self.res2 = nn.Sequential(nn.LayerNorm(hidden_dim), nn.SiLU(), nn.Linear(hidden_dim, hidden_dim))
        self.fc_out = nn.Linear(hidden_dim, out_dim)
        # Zero-init final residual head so pre-distillation physics relies cleanly on geometric priors
        nn.init.zeros_(self.fc_out.weight)
        nn.init.zeros_(self.fc_out.bias)
        self.log_temp = nn.Parameter(torch.zeros(1))

    def forward(self, features: FloatTensor) -> FloatTensor:
        h = F.silu(self.fc_in(features))
        h = h + self.res1(h)
        h = h + self.res2(h)
        temp = torch.exp(self.log_temp).clamp(0.25, 4.0)
        return self.fc_out(h) / temp


# ---------------------------------------------------------------------------
# Complete Module D Outcome & Analytics Suite (D1..D6)
# ---------------------------------------------------------------------------

class ModuleDOutcomeSuite(nn.Module):
    """Calibrated GPU-resident D1..D6 outcome & analytics suite."""

    def __init__(self):
        super().__init__()
        self.extractor = FreezeFrame360FeatureExtractor()
        self.xt_grid = BellmanXTGrid()

        # Trainable residual student heads (distilled from CatBoost/LightGBM teachers)
        self.xg_head = ResidualCalibratedMLP(in_dim=12, out_dim=1)
        self.psxg_head = ResidualCalibratedMLP(in_dim=15, out_dim=6)  # Goal, SavedHeld, SavedParry, Blocked, OffTarget, Post
        self.xpass_head = ResidualCalibratedMLP(in_dim=13, out_dim=5) # Complete, Incomplete, OutTouch, OutByline, Offside
        self.vaep_score_head = ResidualCalibratedMLP(in_dim=10, out_dim=2)  # [P_score, P_concede]
        self.turnover_head = ResidualCalibratedMLP(in_dim=8, out_dim=3)     # [Retain/Complete, Loss/Incomplete, Foul]

    def evaluate_shot(
        self,
        freeze_frame: FloatTensor,       # [B, 22, 7]
        start_xy: FloatTensor,           # [B, 2]
        end_xy: FloatTensor,             # [B, 2]
        end_z: FloatTensor,              # [B]
        poss_team: LongTensor,           # [B]
        body_part: LongTensor,           # [B]
        restart_type: LongTensor,        # [B]
        flags: FloatTensor,              # [B, 8]
        shooter_residual: Optional[FloatTensor] = None,
        gk_residual: Optional[FloatTensor] = None,
        shooter_stamina: Optional[FloatTensor] = None,
    ) -> Dict[str, Tensor]:
        """Compute D1 Pre-Shot xG, D2 Post-Shot PSxG, and calibrated 6-class shot outcome logits."""
        f = self.extractor.forward_shot_features(freeze_frame, start_xy, end_xy, end_z, poss_team)
        is_pen = (restart_type == RestartType.PENALTY).float()
        is_head = (body_part == BodyPart.HEAD).float()
        under_press = flags[:, 0].float()
        first_time = flags[:, 5].float() if flags.shape[-1] > 5 else torch.zeros_like(under_press)

        # 1. Calibrated Pre-Shot xG geometric logit prior (calibrated to Big-5 ~0.115 avg xG/shot, ~1.35 xG/team)
        xg_prior_logit = (
            -1.30
            - 0.135 * f["dist_to_goal"]
            + 1.85 * f["shot_angle"]
            - 0.32 * f["def_in_cone"]
            + 0.08 * f["closest_def_dist"].clamp(max=10.0)
            - 0.35 * under_press
            - 0.45 * is_head
            + 0.25 * first_time
            + 0.45 * f["gk_angle_offset"]
        )
        # Penalty fixed ~0.775 prior
        xg_prior_logit = torch.where(is_pen > 0.5, torch.full_like(xg_prior_logit, 1.24), xg_prior_logit)

        atk_x = torch.where(poss_team == 1, start_xy[:, 0], PITCH_LENGTH - start_xy[:, 0])
        xg_feats = torch.stack([
            f["dist_to_goal"] / 40.0,
            f["shot_angle"],
            f["def_in_cone"] / 5.0,
            f["closest_def_dist"] / 15.0,
            f["gk_dist_goal"] / 15.0,
            f["gk_angle_offset"],
            is_pen,
            is_head,
            under_press,
            first_time,
            atk_x / PITCH_LENGTH,
            start_xy[:, 1] / PITCH_WIDTH,
        ], dim=-1)
        xg_residual = self.xg_head(xg_feats).squeeze(-1).clamp(-1.5, 1.5)
        xg = torch.sigmoid(xg_prior_logit + xg_residual)

        # 2. D2 Post-Shot PSxG & 6-class outcome logits
        on_t = f["on_target"].float()
        psxg_prior_logit = (
            safe_logit(xg)
            + 0.95 * f["corner_precision"]
            + 0.45 * (f["gk_reach_dist"] - 1.4).clamp(-1.5, 2.5)
            - 6.0 * (1.0 - on_t)
        )
        psxg_base = torch.sigmoid(psxg_prior_logit)
        psxg_sim = apply_bayesian_log_odds(
            psxg_base,
            actor_residual=shooter_residual,
            opp_residual=gk_residual,
            actor_stamina=shooter_stamina,
            w_actor=1.0,
            w_opp=1.0,
        )

        # Build calibrated 6-class probabilities [Goal, SavedHeld, SavedParry, Blocked, OffTarget, Post]
        p_block = (0.14 + 0.07 * f["def_in_cone"]).clamp(0.05, 0.45) * (1.0 - is_pen)
        p_off = torch.where(is_pen > 0.5, torch.full_like(p_block, 0.10), torch.where(on_t > 0.5, torch.full_like(p_block, 0.24), torch.full_like(p_block, 0.62)))
        p_post = torch.full_like(p_block, 0.035)
        rem_on_target = (1.0 - p_block - p_off - p_post).clamp(min=0.08)
        p_goal = (rem_on_target * psxg_sim).clamp(min=1e-4, max=0.92)
        p_save_total = (rem_on_target - p_goal).clamp(min=0.01)
        p_saved_held = p_save_total * 0.58
        p_saved_parry = p_save_total * 0.42

        probs_6 = torch.stack([p_goal, p_saved_held, p_saved_parry, p_block, p_off, p_post], dim=-1)
        probs_6 = probs_6 / probs_6.sum(dim=-1, keepdim=True)

        psxg_feats = torch.cat([
            xg_feats,
            torch.stack([f["corner_precision"], f["gk_reach_dist"] / 4.0, on_t], dim=-1),
        ], dim=-1)
        shot_logits_6 = torch.log(probs_6.clamp(min=1e-6)) + self.psxg_head(psxg_feats)
        # Calibrate open-play realized goal probability to pre-shot xG (leaving penalties untouched)
        shot_logits_6 = shot_logits_6.clone()
        shot_logits_6[:, 0] = shot_logits_6[:, 0] - 0.72 * (1.0 - is_pen)

        return {
            "xg": xg,
            "psxg": psxg_sim * on_t,
            "shot_logits_6": shot_logits_6,
        }

    def evaluate_pass(
        self,
        freeze_frame: FloatTensor,       # [B, 22, 7]
        start_xy: FloatTensor,           # [B, 2]
        end_xy: FloatTensor,             # [B, 2]
        poss_team: LongTensor,           # [B]
        pass_height: LongTensor,         # [B]
        restart_type: LongTensor,        # [B]
        flags: FloatTensor,              # [B, 8]
        passer_residual: Optional[FloatTensor] = None,
        passer_stamina: Optional[FloatTensor] = None,
    ) -> Dict[str, Tensor]:
        """Compute D3 xPass completion probability and 5-class pass outcome logits."""
        f = self.extractor.forward_pass_features(freeze_frame, start_xy, end_xy, poss_team)
        is_high = (pass_height == PassHeight.HIGH).float()
        is_low = (pass_height == PassHeight.LOW).float()
        is_cross = flags[:, 3].float() if flags.shape[-1] > 3 else torch.zeros_like(is_high)
        is_tb = flags[:, 1].float() if flags.shape[-1] > 1 else torch.zeros_like(is_high)
        under_press = flags[:, 0].float()
        is_ko = (restart_type == RestartType.KICK_OFF).float()

        # Calibrated geometric xPass logit
        xpass_prior_logit = (
            2.35
            - 0.045 * f["pass_len"]
            - 0.025 * f["dx_att"].clamp(min=0.0)
            - 0.65 * is_high
            - 0.25 * is_low
            - 0.85 * is_cross
            - 0.65 * is_tb
            - 0.45 * under_press
            - 0.25 * f["n_def_in_corridor"]
            + 0.14 * f["min_def_to_ray"].clamp(max=8.0)
            + 0.12 * f["receiver_closest_def"].clamp(max=8.0)
            + 2.5 * is_ko
        )
        xpass_base = torch.sigmoid(xpass_prior_logit)
        xpass_sim = apply_bayesian_log_odds(
            xpass_base,
            actor_residual=passer_residual,
            actor_stamina=passer_stamina,
            w_actor=1.0,
        )

        # Out-of-bounds & offside residual splits from incomplete mass
        fail_mass = (1.0 - xpass_sim).clamp(min=1e-4)
        near_touch = (end_xy[:, 1] <= 3.0) | (end_xy[:, 1] >= PITCH_WIDTH - 3.0)
        near_byline = (end_xy[:, 0] <= 3.0) | (end_xy[:, 0] >= PITCH_LENGTH - 3.0)
        p_touch = fail_mass * torch.where(near_touch, 0.28, 0.06)
        p_byline = fail_mass * torch.where(near_byline, 0.22, 0.03)
        p_offside = fail_mass * torch.where((f["dx_att"] > 8.0) & (restart_type == RestartType.OPEN_PLAY), 0.07, 0.005)
        p_inc = (fail_mass - p_touch - p_byline - p_offside).clamp(min=1e-4)

        probs_5 = torch.stack([xpass_sim, p_inc, p_touch, p_byline, p_offside], dim=-1)
        probs_5 = probs_5 / probs_5.sum(dim=-1, keepdim=True)

        pass_feats = torch.stack([
            f["pass_len"] / 50.0,
            f["dx_att"] / 50.0,
            f["pass_angle"],
            f["min_def_to_ray"] / 15.0,
            f["n_def_in_corridor"] / 4.0,
            f["receiver_closest_def"] / 15.0,
            f["pressure_score"],
            is_high,
            is_low,
            is_cross,
            is_tb,
            under_press,
            is_ko,
        ], dim=-1)
        pass_logits_5 = torch.log(probs_5.clamp(min=1e-6)) + self.xpass_head(pass_feats)
        return {
            "xpass": xpass_sim,
            "pass_logits_5": pass_logits_5,
            "pressure_score": f["pressure_score"],
        }

    def evaluate_pressure_turnover(
        self,
        freeze_frame: FloatTensor,
        start_xy: FloatTensor,
        poss_team: LongTensor,
        pressing_intensity: FloatTensor,
        carrier_press_resistance: Optional[FloatTensor] = None,
        carrier_stamina: Optional[FloatTensor] = None,
    ) -> Dict[str, Tensor]:
        """D6 Pressure Turnover model returning turnover_risk and [Retain, Loss, Foul] logits."""
        f = self.extractor.forward_pass_features(freeze_frame, start_xy, start_xy, poss_team)
        x_att = torch.where(poss_team == 1, start_xy[:, 0], PITCH_LENGTH - start_xy[:, 0])
        zone_risk = (x_att / PITCH_LENGTH).clamp(0.0, 1.0)
        fatigue = (1.0 - carrier_stamina).clamp(0.0, 1.0) if carrier_stamina is not None else torch.zeros_like(zone_risk)

        base_turnover_logit = (
            -1.95
            + 0.75 * f["pressure_score"].clamp(max=2.5)
            + 0.45 * f["n_pressers_3m"].clamp(max=3.0)
            + 0.55 * pressing_intensity
            + 0.35 * zone_risk
        )
        # Positive press_resistance lowers turnover risk
        resist_mod = -carrier_press_resistance if carrier_press_resistance is not None else None
        p_turn = apply_bayesian_log_odds(
            torch.sigmoid(base_turnover_logit),
            actor_residual=resist_mod,
            actor_stamina=carrier_stamina,
            lambda_fatigue=-0.45,  # low stamina INCREASES turnover logit
        )
        p_foul = (0.08 + 0.05 * f["n_pressers_3m"] + 0.06 * fatigue).clamp(0.03, 0.25)
        p_retain = (1.0 - p_turn - p_foul).clamp(min=0.08)
        probs_3 = torch.stack([p_retain, p_turn, p_foul], dim=-1)
        probs_3 = probs_3 / probs_3.sum(dim=-1, keepdim=True)

        turn_feats = torch.stack([
            f["pressure_score"],
            f["n_pressers_3m"] / 4.0,
            pressing_intensity,
            zone_risk,
            fatigue,
            start_xy[:, 0] / PITCH_LENGTH,
            start_xy[:, 1] / PITCH_WIDTH,
            carrier_press_resistance if carrier_press_resistance is not None else torch.zeros_like(zone_risk),
        ], dim=-1)
        logits_3 = torch.log(probs_3.clamp(min=1e-6)) + self.turnover_head(turn_feats)
        return {"turnover_risk": p_turn, "turnover_logits_3": logits_3}

    def evaluate_vaep(
        self,
        action_type: LongTensor,
        start_xy: FloatTensor,
        end_xy: FloatTensor,
        poss_team: LongTensor,
        xg: FloatTensor,
        xpass: FloatTensor,
        xt_delta: FloatTensor,
        score_diff: FloatTensor,
    ) -> FloatTensor:
        """D5 VAEP action value: Delta P_score - Delta P_concede over next 10 actions."""
        xt_end = self.xt_grid.bilinear_query(end_xy, poss_team)
        is_shot = (action_type == ActionType.SHOT).float()
        is_def = (
            (action_type == ActionType.INTERCEPTION)
            | (action_type == ActionType.TACKLE_DUEL)
            | (action_type == ActionType.CLEARANCE)
            | (action_type == ActionType.BLOCK)
            | (action_type == ActionType.SAVE)
        ).float()

        vaep_feats = torch.stack([
            xt_end,
            xt_delta,
            xg,
            xpass,
            is_shot,
            is_def,
            score_diff / 3.0,
            end_xy[:, 0] / PITCH_LENGTH,
            end_xy[:, 1] / PITCH_WIDTH,
            start_xy[:, 0] / PITCH_LENGTH,
        ], dim=-1)
        delta_scores = self.vaep_score_head(vaep_feats)
        # Prior analytic VAEP + learned residual correction
        prior_vaep = xt_delta + 0.85 * xg * is_shot + 0.04 * is_def
        return prior_vaep + 0.25 * torch.tanh(delta_scores[:, 0] - delta_scores[:, 1])

    @torch.inference_mode()
    def calibrate_and_override_outcomes(
        self,
        proposed_event: Dict[str, Tensor],
        raw_outcome_logits: FloatTensor,
        freeze_frame: FloatTensor,
        poss_team: LongTensor,
        pressing_intensity: FloatTensor,
        score_diff: FloatTensor,
        actor_finishing_res: Optional[FloatTensor] = None,
        opp_gk_res: Optional[FloatTensor] = None,
        actor_pass_res: Optional[FloatTensor] = None,
        actor_press_res: Optional[FloatTensor] = None,
        actor_stamina: Optional[FloatTensor] = None,
    ) -> Tuple[FloatTensor, Dict[str, FloatTensor]]:
        """Inject calibrated Module D D1..D6 probabilities into raw Transformer outcome logits.

        Returns:
            calibrated_logits: [B, NUM_OUTCOMES]
            analytics: dict with xg, psxg, xpass, xt_delta, vaep_value, turnover_risk
        """
        B = raw_outcome_logits.shape[0]
        dev = raw_outcome_logits.device
        logits = raw_outcome_logits.clone()

        action_type = proposed_event["action_type"]
        start_xy = proposed_event["start_xy"]
        end_xy = proposed_event["end_xy"]
        end_z = proposed_event["end_z"]
        flags = proposed_event["flags"].float()
        restart_type = proposed_event["restart_type"]
        pass_height = proposed_event["pass_height"]
        body_part = proposed_event["body_part"]

        xg = torch.zeros(B, device=dev)
        psxg = torch.zeros(B, device=dev)
        xpass = torch.zeros(B, device=dev)

        # D4: Continuous bilinear xT delta
        xt_delta = self.xt_grid.compute_xt_delta(start_xy, end_xy, poss_team)

        # D1 & D2: Shots
        is_shot = action_type == ActionType.SHOT
        if is_shot.any():
            shot_out = self.evaluate_shot(
                freeze_frame=freeze_frame,
                start_xy=start_xy,
                end_xy=end_xy,
                end_z=end_z,
                poss_team=poss_team,
                body_part=body_part,
                restart_type=restart_type,
                flags=flags,
                shooter_residual=actor_finishing_res,
                gk_residual=opp_gk_res,
                shooter_stamina=actor_stamina,
            )
            xg = torch.where(is_shot, shot_out["xg"], xg)
            psxg = torch.where(is_shot, shot_out["psxg"], psxg)
            shot_outcome_indices = [
                EventOutcome.GOAL, EventOutcome.SAVED_HELD, EventOutcome.SAVED_PARRY,
                EventOutcome.BLOCKED, EventOutcome.OFF_TARGET, EventOutcome.POST,
            ]
            for j, out_idx in enumerate(shot_outcome_indices):
                logits[is_shot, out_idx] = shot_out["shot_logits_6"][is_shot, j].to(logits.dtype)

        # D3: Passes
        is_pass = action_type == ActionType.PASS
        if is_pass.any():
            pass_out = self.evaluate_pass(
                freeze_frame=freeze_frame,
                start_xy=start_xy,
                end_xy=end_xy,
                poss_team=poss_team,
                pass_height=pass_height,
                restart_type=restart_type,
                flags=flags,
                passer_residual=actor_pass_res,
                passer_stamina=actor_stamina,
            )
            xpass = torch.where(is_pass, pass_out["xpass"], xpass)
            pass_outcome_indices = [
                EventOutcome.COMPLETE, EventOutcome.INCOMPLETE, EventOutcome.OUT_TOUCHLINE,
                EventOutcome.OUT_BYLINE, EventOutcome.OFFSIDE,
            ]
            for j, out_idx in enumerate(pass_outcome_indices):
                logits[is_pass, out_idx] = pass_out["pass_logits_5"][is_pass, j].to(logits.dtype)

        # D6: Pressure Turnover & Duels
        turn_out = self.evaluate_pressure_turnover(
            freeze_frame=freeze_frame,
            start_xy=start_xy,
            poss_team=poss_team,
            pressing_intensity=pressing_intensity,
            carrier_press_resistance=actor_press_res,
            carrier_stamina=actor_stamina,
        )
        turnover_risk = turn_out["turnover_risk"]
        is_carry_dribble = (action_type == ActionType.CARRY) | (action_type == ActionType.DRIBBLE)
        if is_carry_dribble.any():
            logits[is_carry_dribble, EventOutcome.COMPLETE] = turn_out["turnover_logits_3"][is_carry_dribble, 0]
            logits[is_carry_dribble, EventOutcome.INCOMPLETE] = turn_out["turnover_logits_3"][is_carry_dribble, 1]
            logits[is_carry_dribble, EventOutcome.FOUL_CONCEDED] = turn_out["turnover_logits_3"][is_carry_dribble, 2]

        is_duel = action_type == ActionType.TACKLE_DUEL
        if is_duel.any():
            logits[is_duel, EventOutcome.WON] = turn_out["turnover_logits_3"][is_duel, 0]
            logits[is_duel, EventOutcome.LOST] = turn_out["turnover_logits_3"][is_duel, 1]
            logits[is_duel, EventOutcome.FOUL_CONCEDED] = turn_out["turnover_logits_3"][is_duel, 2]

        # D5: VAEP action value
        vaep_value = self.evaluate_vaep(
            action_type=action_type,
            start_xy=start_xy,
            end_xy=end_xy,
            poss_team=poss_team,
            xg=xg,
            xpass=xpass,
            xt_delta=xt_delta,
            score_diff=score_diff,
        )

        return logits, {
            "xg": xg,
            "psxg": psxg,
            "xpass": xpass,
            "xt_delta": xt_delta,
            "vaep_value": vaep_value,
            "turnover_risk": turnover_risk,
        }


def distill_mlp_from_teacher(
    student: ResidualCalibratedMLP,
    features: FloatTensor,
    teacher_probs: FloatTensor,
    steps: int = 100,
    lr: float = 1e-2,
) -> float:
    """Distill a Tier-1 CatBoost/LightGBM teacher probability distribution into a GPU-resident MLP."""
    optimizer = torch.optim.AdamW(student.parameters(), lr=lr, weight_decay=1e-4)
    target = teacher_probs.clamp(min=1e-6)
    final_loss = 0.0
    student.train()
    for _ in range(steps):
        optimizer.zero_grad()
        logits = student(features)
        if logits.shape[-1] == 1:
            pred = torch.sigmoid(logits.squeeze(-1)).clamp(1e-6, 1.0 - 1e-6)
            t = target.squeeze(-1).clamp(1e-6, 1.0 - 1e-6)
            loss = F.binary_cross_entropy(pred, t)
        else:
            log_p = F.log_softmax(logits, dim=-1)
            loss = F.kl_div(log_p, target, reduction="batchmean")
        loss.backward()
        optimizer.step()
        final_loss = float(loss.item())
    student.eval()
    return final_loss
