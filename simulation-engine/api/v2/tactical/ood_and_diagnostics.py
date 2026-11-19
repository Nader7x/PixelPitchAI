"""Module B & Experimentation Platform: 4-Tier OOD Guardrails & Causal Diagnostics.

Implements:
1. OODDetector: Joint tactical manifold Mahalanobis density check, player-role
   suitability distance, synthetic/strength gap detection, runtime spatial drift
   checks, GREEN/AMBER/RED trust calibration, and manifold boundary projection.
2. DiagnosticAttributionCalculator: Paired-CRN statistical CIs, 15-zone pitch
   progression waterfalls, 12x8 turnover pressure & 15s xG-cost attribution,
   role-level xT/VAEP breakdowns, xG quality distributions, and JSD spatial
   redundancy / synergy scoring for recruitment counterfactuals.
"""

from __future__ import annotations

import math
import os
import pickle
from pathlib import Path
from typing import Any, Dict, List, Literal, Optional, Sequence, Tuple

import numpy as np
from pydantic import BaseModel, Field
from scipy import stats

TACTICAL_MANIFOLD_FILENAME: str = "tactical_manifold_v2.pkl"

from .config_schema import (
    ATTRIBUTE_NAMES_24D,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    TACTICAL_SLIDER_NAMES,
    ExperimentVariant,
    MatchConfigurationV2,
    PlayerRole,
    TacticalPhase,
    TeamMatchConfig,
    apply_variant_to_config,
    build_default_team_config,
)
from .state_engine import PlayerCapabilityRecord, PlayerProfileRegistry


# ---------------------------------------------------------------------------
# OOD Guardrail Contracts & 4-Tier Detector
# ---------------------------------------------------------------------------

class OODCheckViolation(BaseModel):
    """Individual out-of-distribution or suitability warning triggered during gating."""

    check_name: Literal[
        "TACTICAL_COMBO_OOD",
        "ROLE_MISMATCH_OOD",
        "SYNTHETIC_EXTRAPOLATION",
        "SPATIAL_COLLISION_DRIFT",
    ]
    severity: float = Field(..., ge=0.0, le=1.0)
    team_side: Literal["home", "away", "both"]
    slot_index: Optional[int] = Field(None, ge=0, le=10)
    message: str
    offending_dimensions: Dict[str, float] = Field(default_factory=dict)


class OODAssessmentReport(BaseModel):
    """Composite 4-tier OOD guardrail report aligned with Module E schemas."""

    ood_score: float = Field(..., ge=0.0, le=1.0)
    confidence_tier: Literal["GREEN", "AMBER", "RED"]
    mahalanobis_distance: float = Field(..., ge=0.0)
    epistemic_variance_multiplier: float = Field(..., ge=1.0)
    allow_causal_claims: bool
    execution_blocked: bool
    triggered_checks: List[OODCheckViolation] = Field(default_factory=list)
    projected_nearest_feasible: Optional[Dict[str, Dict[str, float]]] = None
    support_notes: str


class TacticalManifoldModel:
    """Empirical multivariate Gaussian density over 10D TacticalInstructions."""

    def __init__(self, mean_10d: np.ndarray, cov_10d: np.ndarray) -> None:
        self.mean = np.asarray(mean_10d, dtype=np.float64)
        self.cov = np.asarray(cov_10d, dtype=np.float64)
        # Ensure a minimum per-dimension variance floor (std >= 0.15) so near-constant
        # empirical proxy dimensions do not inflate Mahalanobis distance into false RED blocks.
        diag_floor = np.maximum(np.diag(self.cov), 0.0225)
        reg_cov = self.cov.copy()
        np.fill_diagonal(reg_cov, diag_floor)
        reg_cov = reg_cov + 1e-4 * np.eye(len(self.mean))
        self.inv_cov = np.linalg.inv(reg_cov)
        # chi2(df=10) 95th percentile ~ 18.307 => sqrt ~ 4.28; 99th ~ 23.209 => sqrt ~ 4.82
        self.d95_threshold = math.sqrt(18.307)
        self.d99_threshold = math.sqrt(23.209)

    @classmethod
    def default_empirical_prior(cls) -> "TacticalManifoldModel":
        """Construct realistic football tactical manifold matching StatsBomb distributions."""
        mean = np.array([0.52, 0.52, 0.50, 0.48, 0.54, 0.56, 0.52, 0.52, 0.50, 0.72], dtype=np.float64)
        stds = np.array([0.16, 0.16, 0.13, 0.16, 0.12, 0.13, 0.17, 0.15, 0.13, 0.18], dtype=np.float64)
        corr = np.eye(10, dtype=np.float64)

        # Positive tactical couplings observed in real matches:
        # High line <-> High pressing <-> Counter-press <-> Compactness
        for i, j, r in (
            (0, 1, 0.74),   # line_height <-> pressing_intensity
            (0, 6, 0.68),   # line_height <-> counter_press_vs_regroup
            (1, 6, 0.76),   # pressing <-> counter_press
            (0, 5, 0.42),   # line_height <-> compactness
            (1, 5, 0.48),   # pressing <-> compactness
            (0, 3, -0.45),  # line_height <-> directness (possession high line vs direct low block)
            (1, 3, -0.35),  # pressing <-> directness
            (3, 7, 0.58),   # directness <-> counter_attack_urgency
            (2, 8, 0.44),   # build_up_tempo <-> possession_risk
        ):
            corr[i, j] = r
            corr[j, i] = r

        # Ensure positive-definite correlation matrix
        eigvals, eigvecs = np.linalg.eigh(corr)
        eigvals = np.maximum(eigvals, 0.08)
        corr_pd = (eigvecs * eigvals) @ eigvecs.T
        d_inv = np.diag(1.0 / np.sqrt(np.diag(corr_pd)))
        corr_pd = d_inv @ corr_pd @ d_inv
        cov = np.outer(stds, stds) * corr_pd
        return cls(mean, cov)

    @classmethod
    def from_pickle(cls, path: str | Path, regularize_if_degenerate: bool = False) -> "TacticalManifoldModel":
        """Load fitted empirical tactical manifold artifact exported by Pillar 1."""
        target = Path(path)
        with open(target, "rb") as f:
            data = pickle.load(f)
        mean = np.asarray(data.get("mean_10d") if "mean_10d" in data else data["mean"], dtype=np.float64)
        cov = np.asarray(data.get("cov_10d") if "cov_10d" in data else data["cov"], dtype=np.float64)
        if regularize_if_degenerate:
            diag_std = np.sqrt(np.maximum(np.diag(cov), 1e-12))
            if bool(np.any(diag_std < 0.08) or np.any(mean < 0.15) or np.any(mean > 0.85)):
                prior = cls.default_empirical_prior()
                alpha = 0.12
                mean = alpha * mean + (1.0 - alpha) * prior.mean
                cov = alpha * cov + (1.0 - alpha) * prior.cov
                min_var = 0.14 ** 2
                for i in range(cov.shape[0]):
                    if cov[i, i] < min_var:
                        cov[i, i] = min_var
        inst = cls(mean_10d=mean, cov_10d=cov)
        inst.loaded_from_path = str(target)
        return inst

    def save_pickle(self, path: str | Path) -> Path:
        """Save empirical tactical manifold mean & covariance to pickle file."""
        target = Path(path)
        target.parent.mkdir(parents=True, exist_ok=True)
        with open(target, "wb") as f:
            pickle.dump({"mean_10d": self.mean, "cov_10d": self.cov}, f)
        self.loaded_from_path = str(target)
        return target

    def mahalanobis_distance(self, vec_10d: Sequence[float]) -> float:
        delta = np.asarray(vec_10d, dtype=np.float64) - self.mean
        d2 = float(delta @ self.inv_cov @ delta)
        return math.sqrt(max(0.0, d2))

    def project_to_feasible(self, vec_10d: Sequence[float], target_radius: Optional[float] = None) -> Dict[str, float]:
        """Project an out-of-distribution tactical slider vector back onto the supported ellipsoid."""
        v = np.asarray(vec_10d, dtype=np.float64)
        rad = target_radius if target_radius is not None else self.d95_threshold * 0.90
        d = self.mahalanobis_distance(v)
        if d <= rad or d < 1e-8:
            proj = np.clip(v, 0.0, 1.0)
        else:
            proj = np.clip(self.mean + (v - self.mean) * (rad / d), 0.0, 1.0)
        return {name: round(float(val), 4) for name, val in zip(TACTICAL_SLIDER_NAMES, proj)}


# Minimum plausible role capability requirements (attribute_name -> min_threshold)
_ROLE_MIN_REQUIREMENTS: Dict[PlayerRole, Dict[str, float]] = {
    PlayerRole.SWEEPER_KEEPER: {"short_pass_accuracy": 0.42},
    PlayerRole.BALL_PLAYING_CB: {"progressive_pass_volume": 0.36, "short_pass_accuracy": 0.44},
    PlayerRole.INVERTED_FULLBACK: {"press_resistance": 0.44, "short_pass_accuracy": 0.45},
    PlayerRole.OVERLAPPING_FULLBACK: {"sprint_recovery_capacity": 0.44, "stamina_endurance": 0.46},
    PlayerRole.COMPLETE_WING_BACK: {"sprint_recovery_capacity": 0.48, "stamina_endurance": 0.48},
    PlayerRole.DEEP_LYING_PLAYMAKER: {"progressive_pass_volume": 0.45, "press_resistance": 0.42},
    PlayerRole.BOX_TO_BOX_MIDFIELDER: {"stamina_endurance": 0.48, "pressing_workrate": 0.40},
    PlayerRole.MEZZALA: {"ball_carry_progression": 0.42, "progressive_pass_volume": 0.40},
    PlayerRole.ADVANCED_PLAYMAKER: {"through_ball_vision": 0.44, "press_resistance": 0.40},
    PlayerRole.INVERTED_WINGER: {"dribble_success_skill": 0.42, "ball_carry_progression": 0.40},
    PlayerRole.TOUCHLINE_WINGER: {"dribble_success_skill": 0.44, "sprint_recovery_capacity": 0.44},
    PlayerRole.PRESSING_FORWARD: {"pressing_workrate": 0.46, "stamina_endurance": 0.44},
    PlayerRole.TARGET_FORWARD: {"aerial_duel_win_rate": 0.46},
    PlayerRole.FALSE_9: {"short_pass_accuracy": 0.45, "press_resistance": 0.42},
}


class OODDetector:
    """4-Tier Out-of-Distribution & Suitability Guardrail Gate."""

    def __init__(
        self,
        manifold: Optional[TacticalManifoldModel] = None,
        models_dir: Optional[str | Path] = None,
        auto_load: bool = True,
    ) -> None:
        self.loaded_from_path: Optional[str] = None
        if manifold is not None:
            self.manifold = manifold
            self.loaded_from_path = getattr(manifold, "loaded_from_path", None)
        elif auto_load:
            base_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
            pkl_path = base_dir / TACTICAL_MANIFOLD_FILENAME
            if pkl_path.is_file():
                self.manifold = TacticalManifoldModel.from_pickle(pkl_path, regularize_if_degenerate=True)
                self.loaded_from_path = str(pkl_path)
            else:
                self.manifold = TacticalManifoldModel.default_empirical_prior()
        else:
            self.manifold = TacticalManifoldModel.default_empirical_prior()

    @classmethod
    def from_models_dir(cls, models_dir: Optional[str | Path] = None) -> "OODDetector":
        """Instantiate OODDetector auto-loading tactical_manifold_v2.pkl from models_dir."""
        return cls(models_dir=models_dir, auto_load=True)

    def _check_team_tactics(
        self,
        team: TeamMatchConfig,
        side: Literal["home", "away"],
    ) -> Tuple[float, Optional[OODCheckViolation], Optional[Dict[str, float]]]:
        vec = team.tactical_instructions.to_vector()
        d_m = self.manifold.mahalanobis_distance(vec)

        # Explicitly catch extreme inverse line-height vs pressing decouplings
        line_h = team.tactical_instructions.defensive_line_height
        press_i = team.tactical_instructions.pressing_intensity
        decouple_gap = abs(line_h - press_i) if (line_h > 0.78 and press_i < 0.25) or (line_h < 0.22 and press_i > 0.80) else 0.0

        sev = 0.0
        if d_m > self.manifold.d95_threshold:
            sev = min(1.0, (d_m - self.manifold.d95_threshold) / 2.2)
        if decouple_gap > 0.45:
            sev = max(sev, min(1.0, (decouple_gap - 0.35) / 0.45))

        if sev <= 0.05:
            return d_m, None, None

        proj = self.manifold.project_to_feasible(vec)
        offending = {
            k: round(float(getattr(team.tactical_instructions, k)), 3)
            for k in ("defensive_line_height", "pressing_intensity", "directness", "counter_press_vs_regroup")
        }
        violation = OODCheckViolation(
            check_name="TACTICAL_COMBO_OOD",
            severity=round(sev, 4),
            team_side=side,
            message=(
                f"{side.capitalize()} tactical combination has low historical support "
                f"(Mahalanobis d={d_m:.2f}; line_height={line_h:.2f}, pressing={press_i:.2f})."
            ),
            offending_dimensions=offending,
        )
        return d_m, violation, proj

    def _check_role_suitability(
        self,
        team: TeamMatchConfig,
        side: Literal["home", "away"],
        registry: PlayerProfileRegistry,
    ) -> Tuple[List[OODCheckViolation], float]:
        violations: List[OODCheckViolation] = []
        team_quality_scores: List[float] = []

        for slot in team.ordered_xi():
            prof = registry.resolve_profile(slot.player_id, slot.tactical_role, slot.inline_profile_override)
            team_quality_scores.append(float(np.mean(prof.attributes_24d)))
            role = slot.tactical_role

            # Check non-GK placed in SweeperKeeper vs outfielder with GK stats
            if slot.slot_index == 0 and role == PlayerRole.SWEEPER_KEEPER:
                if prof.gk_attributes_8d[2] < 0.30:
                    violations.append(
                        OODCheckViolation(
                            check_name="ROLE_MISMATCH_OOD",
                            severity=0.42,
                            team_side=side,
                            slot_index=0,
                            message=f"{prof.display_name} has low sweeper rate ({prof.gk_attributes_8d[2]:.2f}) for SweeperKeeper.",
                            offending_dimensions={"gk_sweeper_action_rate": prof.gk_attributes_8d[2]},
                        )
                    )
                continue

            # Check outfielder assigned to outfield role that mismatches historical attributes
            reqs = _ROLE_MIN_REQUIREMENTS.get(role, {})
            shortfalls: Dict[str, float] = {}
            for attr_name, min_val in reqs.items():
                val = prof.attributes_24d[ATTRIBUTE_NAMES_24D.index(attr_name)]
                if val < min_val:
                    shortfalls[attr_name] = round(val, 3)

            # Check severe GK-profile-in-outfield mistake
            is_likely_gk = (
                prof.gk_attributes_8d[0] > 0.58
                and prof.attributes_24d[ATTRIBUTE_NAMES_24D.index("dribble_takeon_volume")] < 0.25
                and prof.attributes_24d[ATTRIBUTE_NAMES_24D.index("shot_volume_box")] < 0.20
                and slot.slot_index > 0
            )
            if is_likely_gk:
                violations.append(
                    OODCheckViolation(
                        check_name="ROLE_MISMATCH_OOD",
                        severity=0.88,
                        team_side=side,
                        slot_index=slot.slot_index,
                        message=f"Goalkeeper profile {prof.display_name} assigned to outfield role {role.value}.",
                        offending_dimensions={"shot_volume_box": prof.attributes_24d[10]},
                    )
                )
            elif shortfalls:
                max_gap = max(reqs[k] - shortfalls[k] for k in shortfalls)
                sev = min(0.85, max(0.25, max_gap / 0.30))
                violations.append(
                    OODCheckViolation(
                        check_name="ROLE_MISMATCH_OOD",
                        severity=round(sev, 4),
                        team_side=side,
                        slot_index=slot.slot_index,
                        message=f"Slot {slot.slot_index} ({prof.display_name}) below historical thresholds for {role.value}.",
                        offending_dimensions=shortfalls,
                    )
                )

            # Check Tier-3 synthetic profile saturation
            if slot.inline_profile_override is not None:
                high_count = sum(1 for v in prof.attributes_24d if v >= 0.92)
                mean_v = float(np.mean(prof.attributes_24d))
                if high_count >= 16 or mean_v > 0.88:
                    violations.append(
                        OODCheckViolation(
                            check_name="SYNTHETIC_EXTRAPOLATION",
                            severity=min(1.0, max(0.65, (mean_v - 0.80) / 0.15)),
                            team_side=side,
                            slot_index=slot.slot_index,
                            message=f"Synthetic profile {prof.display_name} saturates attributes outside realistic support (mean={mean_v:.2f}).",
                            offending_dimensions={"mean_attribute": round(mean_v, 3), "saturated_dims": float(high_count)},
                        )
                    )

        return violations, float(np.mean(team_quality_scores))

    def _check_anchor_packing(self, team: TeamMatchConfig, side: Literal["home", "away"]) -> Optional[OODCheckViolation]:
        for phase in (TacticalPhase.IN_POSSESSION, TacticalPhase.OUT_OF_POSSESSION):
            anchors = team.formation.phase_anchors.get(phase)
            if not anchors:
                continue
            in_box = sum(1 for a in anchors if a.anchor_x > 0.85 and 0.25 <= a.anchor_y <= 0.75)
            if in_box >= 7:
                return OODCheckViolation(
                    check_name="SPATIAL_COLLISION_DRIFT",
                    severity=0.75,
                    team_side=side,
                    message=f"{side.capitalize()} custom anchors pack {in_box} players inside the opponent penalty box in {phase.value}.",
                    offending_dimensions={"players_in_box": float(in_box)},
                )
        return None

    def assess_match(
        self,
        config: MatchConfigurationV2,
        registry: Optional[PlayerProfileRegistry] = None,
        runtime_clamp_fraction: float = 0.0,
    ) -> OODAssessmentReport:
        """Run all 4 OOD guardrail checks and classify GREEN / AMBER / RED confidence tier."""
        reg = registry or PlayerProfileRegistry()
        checks: List[OODCheckViolation] = []
        projections: Dict[str, Dict[str, float]] = {}

        d_home, v_home, proj_home = self._check_team_tactics(config.home, "home")
        d_away, v_away, proj_away = self._check_team_tactics(config.away, "away")
        if v_home is not None:
            checks.append(v_home)
            if proj_home is not None:
                projections["home"] = proj_home
        if v_away is not None:
            checks.append(v_away)
            if proj_away is not None:
                projections["away"] = proj_away

        role_h, q_home = self._check_role_suitability(config.home, "home", reg)
        role_a, q_away = self._check_role_suitability(config.away, "away", reg)
        checks.extend(role_h)
        checks.extend(role_a)

        # Cross-team extreme quality differential check (Tier 3)
        q_gap = abs(q_home - q_away)
        if q_gap > 0.30:
            checks.append(
                OODCheckViolation(
                    check_name="SYNTHETIC_EXTRAPOLATION",
                    severity=round(min(1.0, (q_gap - 0.25) / 0.25), 4),
                    team_side="both",
                    message=f"Extreme composite quality differential ({q_gap:.2f}) approaches edge of historical support.",
                    offending_dimensions={"quality_gap": round(q_gap, 3)},
                )
            )

        for t_cfg, side in ((config.home, "home"), (config.away, "away")):
            anch_v = self._check_anchor_packing(t_cfg, side)  # type: ignore[arg-type]
            if anch_v is not None:
                checks.append(anch_v)

        if runtime_clamp_fraction > 0.15:
            checks.append(
                OODCheckViolation(
                    check_name="SPATIAL_COLLISION_DRIFT",
                    severity=round(min(1.0, runtime_clamp_fraction * 2.5), 4),
                    team_side="both",
                    message=f"Elevated runtime spatial clamp rate ({runtime_clamp_fraction:.1%}).",
                    offending_dimensions={"runtime_clamp_fraction": round(runtime_clamp_fraction, 4)},
                )
            )

        max_d = max(d_home, d_away)
        if checks:
            # Combine max violation severity with secondary checks smoothly
            severities = sorted((c.severity for c in checks), reverse=True)
            ood_score = min(1.0, severities[0] + 0.15 * sum(severities[1:3]))
        else:
            ood_score = min(0.28, max_d / (self.manifold.d95_threshold * 3.5))

        if ood_score < 0.35:
            tier: Literal["GREEN", "AMBER", "RED"] = "GREEN"
            var_mult = 1.0
            allow_causal = True
            blocked = False
            notes = "Configuration lies well within historical tactical & player-role support."
        elif ood_score < 0.70:
            tier = "AMBER"
            var_mult = 1.5
            allow_causal = True
            blocked = False
            notes = "Moderate tactical/role extrapolation detected; epistemic uncertainty widened 1.5x."
        else:
            tier = "RED"
            var_mult = 2.5
            allow_causal = False
            blocked = not config.allow_ood_extrapolation
            notes = (
                "Severe out-of-distribution extrapolation detected; causal claims disabled"
                + (" and execution blocked unless allow_ood_extrapolation=True." if blocked else " (exploratory override active).")
            )

        return OODAssessmentReport(
            ood_score=round(ood_score, 4),
            confidence_tier=tier,
            mahalanobis_distance=round(max_d, 4),
            epistemic_variance_multiplier=var_mult,
            allow_causal_claims=allow_causal,
            execution_blocked=blocked,
            triggered_checks=checks,
            projected_nearest_feasible=projections or None,
            support_notes=notes,
        )


# ---------------------------------------------------------------------------
# Explainable Counterfactual Diagnostic Models & Waterfall Calculator
# ---------------------------------------------------------------------------

class SimulatedEventRecord(BaseModel):
    """Lightweight event record consumed by DiagnosticAttributionCalculator."""

    match_seed: int
    event_index: int
    timestamp_sec: float
    team_side: int = Field(..., ge=0, le=1)   # 0=Home, 1=Away
    actor_slot: int = Field(..., ge=0, le=22) # 1..11 Home, 12..22 Away
    action_type: str
    outcome: str
    x_canon: float
    y_canon: float
    end_x_canon: float
    end_y_canon: float
    under_pressure: bool = False
    through_ball: bool = False
    cross: bool = False
    cutback: bool = False
    restart_type: str = "OpenPlay"
    xg: float = 0.0
    psxg: float = 0.0
    xt_delta: float = 0.0
    vaep_value: float = 0.0
    defenders_in_shot_cone: int = 0


class ZoneDeltaStat(BaseModel):
    zone_id: int
    zone_name: str
    entries_per_90: float
    progression_rate: float
    mean_xt_added: float


class RoleContributionStat(BaseModel):
    slot: int
    xt_pass: float
    xt_carry: float
    xt_lost: float
    vaep_offensive: float
    vaep_defensive: float


class PairedStatDelta(BaseModel):
    mean_delta: float
    ci95_low: float
    ci95_high: float
    p_value: float


class XGDiffWaterfall(BaseModel):
    build_up_retention: float
    final_third_entry_quality: float
    shot_conversion_quality: float
    transition_vulnerability_allowed: float
    set_piece_delta: float
    total_xg_diff_delta: float


class RepresentativeMatchSeed(BaseModel):
    seed: int
    home_goals: int
    away_goals: int
    xg_diff: float
    tag: str


class DiagnosticAttributionReport(BaseModel):
    """Full explainable diagnostics answering 'Why did this happen?'."""

    target_team_side: Literal["home", "away"] = "home"
    n_matches: int
    xg_diff_delta: PairedStatDelta
    win_prob_delta: float
    zone_progression_15: List[ZoneDeltaStat]
    zone_14_entries_delta_p90: float
    half_space_entries_delta_p90: float
    turnover_causes_p90: Dict[str, float]
    turnover_xg_cost_15s_p90: float
    turnover_grid_12x8_cost: List[List[float]]
    role_contributions: List[RoleContributionStat]
    shot_quality_histogram: Dict[str, int]
    assist_pattern_counts: Dict[str, int]
    defenders_in_cone_counts: Dict[str, int]
    waterfall: XGDiffWaterfall
    representative_match_seeds: List[RepresentativeMatchSeed] = Field(default_factory=list)


class SpatialRedundancyReport(BaseModel):
    """Q4 recruitment evaluation: overlap JSD, zone congestion & synergy."""

    slot_a: int
    slot_b: int
    js_divergence: float
    spatial_overlap_score: float
    zone_congestion_flag: bool
    vorp_sim_xg_diff: float
    synergy_xg_diff: float


_ZONE_NAMES_15: Tuple[str, ...] = (
    "Own_LeftWing", "Own_LeftHalfSpace", "Own_Center", "Own_RightHalfSpace", "Own_RightWing",
    "Mid_LeftWing", "Mid_LeftHalfSpace", "Mid_Center", "Mid_RightHalfSpace", "Mid_RightWing",
    "Final_LeftWing", "Final_LeftHalfSpace", "Final_Center_Z14", "Final_RightHalfSpace", "Final_RightWing",
)


def _to_attacking_xy(x: float, y: float, team_side: int) -> Tuple[float, float]:
    if team_side == 0:
        return x, y
    return PITCH_LENGTH_YD - x, PITCH_WIDTH_YD - y


def _zone_15_id(x_att: float, y_att: float) -> int:
    third = 0 if x_att < 40.0 else (1 if x_att < 80.0 else 2)
    if y_att < 16.0:
        lane = 0
    elif y_att < 30.0:
        lane = 1
    elif y_att < 50.0:
        lane = 2
    elif y_att < 64.0:
        lane = 3
    else:
        lane = 4
    return third * 5 + lane


def _is_turnover(ev: SimulatedEventRecord) -> Optional[str]:
    act = ev.action_type.lower()
    out = ev.outcome.lower()
    if act == "pass" and out in ("incomplete", "out_touchline", "out_byline", "offside"):
        return "forced_under_press" if ev.under_pressure else "intercepted_lane"
    if act == "carry" and out in ("lost", "dispossessed", "miscontrol"):
        return "forced_under_press" if ev.under_pressure else "miscontrol_carry"
    if act == "dribble" and out in ("incomplete", "lost"):
        return "failed_takeon"
    if act in ("dispossessed", "miscontrol"):
        return "forced_under_press" if ev.under_pressure else "miscontrol_carry"
    return None


class DiagnosticAttributionCalculator:
    """Computes paired CRN statistics, zone/turnover/xT/xG diagnostics, & waterfalls."""

    @staticmethod
    def paired_stat_delta(
        baseline_vals: Sequence[float],
        variant_vals: Sequence[float],
        n_boot: int = 1000,
        seed: int = 2026,
    ) -> PairedStatDelta:
        """Compute paired mean delta, 95% bootstrap CI, and paired p-value."""
        b_arr = np.asarray(baseline_vals, dtype=np.float64)
        v_arr = np.asarray(variant_vals, dtype=np.float64)
        diffs = v_arr - b_arr
        n = len(diffs)
        if n == 0:
            return PairedStatDelta(mean_delta=0.0, ci95_low=0.0, ci95_high=0.0, p_value=1.0)
        mean_d = float(np.mean(diffs))
        if n == 1 or np.allclose(diffs, diffs[0]):
            return PairedStatDelta(mean_delta=mean_d, ci95_low=mean_d, ci95_high=mean_d, p_value=1.0 if abs(mean_d) < 1e-9 else 0.0)

        rng = np.random.default_rng(seed)
        idx = rng.integers(0, n, size=(n_boot, n))
        boot_means = diffs[idx].mean(axis=1)
        ci_low, ci_high = np.percentile(boot_means, [2.5, 97.5])
        _, p_val = stats.ttest_rel(v_arr, b_arr)
        p_clean = 1.0 if math.isnan(float(p_val)) else float(p_val)
        return PairedStatDelta(
            mean_delta=round(mean_d, 5),
            ci95_low=round(float(ci_low), 5),
            ci95_high=round(float(ci_high), 5),
            p_value=round(p_clean, 6),
        )

    def _aggregate_arm(
        self,
        matches_events: Sequence[Sequence[SimulatedEventRecord]],
        target_side: int,
    ) -> Dict[str, Any]:
        n = max(1, len(matches_events))
        zone_entries = np.zeros(15, dtype=np.float64)
        zone_prog = np.zeros(15, dtype=np.float64)
        zone_xt = np.zeros(15, dtype=np.float64)

        turnover_causes: Dict[str, float] = {
            "forced_under_press": 0.0,
            "intercepted_lane": 0.0,
            "miscontrol_carry": 0.0,
            "failed_takeon": 0.0,
        }
        turnover_grid = np.zeros((12, 8), dtype=np.float64)
        turnover_xg_15s_total = 0.0

        # 11 team slots indexed 0..10
        role_xt_pass = np.zeros(11, dtype=np.float64)
        role_xt_carry = np.zeros(11, dtype=np.float64)
        role_xt_lost = np.zeros(11, dtype=np.float64)
        role_vaep_off = np.zeros(11, dtype=np.float64)
        role_vaep_def = np.zeros(11, dtype=np.float64)

        shot_hist = {"lt_0.05": 0, "0.05_0.15": 0, "0.15_0.35": 0, "gte_0.35": 0}
        assist_counts: Dict[str, int] = {
            "through_ball": 0,
            "cutback": 0,
            "cross": 0,
            "set_piece": 0,
            "individual_carry": 0,
            "turnover_high_press": 0,
        }
        cone_counts = {"0": 0, "1": 0, "2+": 0}

        xg_diffs: List[float] = []
        wins: List[float] = []
        seed_summaries: List[RepresentativeMatchSeed] = []

        # Waterfall buckets per match
        buildup_ret_vec: List[float] = []
        final_entry_vec: List[float] = []
        shot_conv_vec: List[float] = []
        trans_vuln_vec: List[float] = []
        set_piece_vec: List[float] = []

        for events in matches_events:
            ev_list = sorted(events, key=lambda e: e.event_index)
            xg_for = 0.0
            xg_against = 0.0
            goals_for = 0
            goals_against = 0
            buildup_xt = 0.0
            entry_xt = 0.0
            conv_delta = 0.0
            trans_cost = 0.0
            sp_xg_net = 0.0
            match_seed = ev_list[0].match_seed if ev_list else 0

            for idx, ev in enumerate(ev_list):
                act = ev.action_type.lower()
                out = ev.outcome.lower()
                x_att, y_att = _to_attacking_xy(ev.x_canon, ev.y_canon, ev.team_side)
                ex_att, ey_att = _to_attacking_xy(ev.end_x_canon, ev.end_y_canon, ev.team_side)

                if ev.team_side == target_side:
                    local_slot = (ev.actor_slot - 1) if target_side == 0 else (ev.actor_slot - 12)
                    z_start = _zone_15_id(x_att, y_att)
                    z_end = _zone_15_id(ex_att, ey_att)

                    if act in ("pass", "carry", "dribble"):
                        zone_entries[z_start] += 1.0
                        if (z_end // 5) > (z_start // 5) or (z_end >= 10 and out == "complete"):
                            zone_prog[z_start] += 1.0
                        zone_xt[z_start] += max(0.0, ev.xt_delta)

                        if z_start < 5:
                            buildup_xt += ev.xt_delta
                        elif z_end >= 10:
                            entry_xt += max(0.0, ev.xt_delta)

                    if 0 <= local_slot < 11:
                        if act == "pass":
                            if ev.xt_delta >= 0:
                                role_xt_pass[local_slot] += ev.xt_delta
                            else:
                                role_xt_lost[local_slot] += abs(ev.xt_delta)
                        elif act in ("carry", "dribble"):
                            if ev.xt_delta >= 0:
                                role_xt_carry[local_slot] += ev.xt_delta
                            else:
                                role_xt_lost[local_slot] += abs(ev.xt_delta)
                        if act in ("pass", "carry", "dribble", "shot"):
                            role_vaep_off[local_slot] += ev.vaep_value
                        else:
                            role_vaep_def[local_slot] += ev.vaep_value

                    cause = _is_turnover(ev)
                    if cause is not None:
                        turnover_causes[cause] += 1.0
                        gx = min(11, max(0, int(x_att / 10.0)))
                        gy = min(7, max(0, int(y_att / 10.0)))
                        opp_xg_15 = 0.0
                        for nxt in ev_list[idx + 1:]:
                            if nxt.timestamp_sec - ev.timestamp_sec > 15.0:
                                break
                            if nxt.team_side != target_side and nxt.action_type.lower() == "shot":
                                opp_xg_15 += nxt.xg
                        turnover_grid[gx, gy] += opp_xg_15
                        turnover_xg_15s_total += opp_xg_15
                        trans_cost += opp_xg_15

                    if act == "shot":
                        xg_for += ev.xg
                        if ev.restart_type != "OpenPlay":
                            sp_xg_net += ev.xg
                        if out == "goal":
                            goals_for += 1
                            conv_delta += (1.0 - ev.xg)
                        else:
                            conv_delta += (ev.psxg - ev.xg) * 0.25

                        if ev.xg < 0.05:
                            shot_hist["lt_0.05"] += 1
                        elif ev.xg < 0.15:
                            shot_hist["0.05_0.15"] += 1
                        elif ev.xg < 0.35:
                            shot_hist["0.15_0.35"] += 1
                        else:
                            shot_hist["gte_0.35"] += 1

                        c_key = "0" if ev.defenders_in_shot_cone <= 0 else ("1" if ev.defenders_in_shot_cone == 1 else "2+")
                        cone_counts[c_key] += 1

                        prev_ev = ev_list[idx - 1] if idx > 0 else None
                        if ev.restart_type != "OpenPlay":
                            assist_counts["set_piece"] += 1
                        elif prev_ev is not None and prev_ev.team_side == target_side:
                            if prev_ev.through_ball:
                                assist_counts["through_ball"] += 1
                            elif prev_ev.cutback:
                                assist_counts["cutback"] += 1
                            elif prev_ev.cross:
                                assist_counts["cross"] += 1
                            elif prev_ev.action_type.lower() in ("carry", "dribble"):
                                assist_counts["individual_carry"] += 1
                            else:
                                assist_counts["turnover_high_press"] += 1
                        else:
                            assist_counts["turnover_high_press"] += 1
                else:
                    if act == "shot":
                        xg_against += ev.xg
                        if ev.restart_type != "OpenPlay":
                            sp_xg_net -= ev.xg
                        if out == "goal":
                            goals_against += 1

            diff = xg_for - xg_against
            xg_diffs.append(diff)
            wins.append(1.0 if goals_for > goals_against else (0.5 if goals_for == goals_against else 0.0))
            buildup_ret_vec.append(buildup_xt)
            final_entry_vec.append(entry_xt)
            shot_conv_vec.append(conv_delta)
            trans_vuln_vec.append(trans_cost)
            set_piece_vec.append(sp_xg_net)
            seed_summaries.append(
                RepresentativeMatchSeed(
                    seed=match_seed,
                    home_goals=goals_for if target_side == 0 else goals_against,
                    away_goals=goals_against if target_side == 0 else goals_for,
                    xg_diff=round(diff, 3),
                    tag="sample",
                )
            )

        return {
            "n": n,
            "xg_diffs": xg_diffs,
            "wins": wins,
            "zone_entries_p90": zone_entries / n,
            "zone_prog_rate": np.divide(zone_prog, np.maximum(1.0, zone_entries)),
            "zone_mean_xt": np.divide(zone_xt, np.maximum(1.0, zone_entries)),
            "turnover_causes_p90": {k: round(v / n, 3) for k, v in turnover_causes.items()},
            "turnover_xg_cost_15s_p90": round(turnover_xg_15s_total / n, 4),
            "turnover_grid": (turnover_grid / n).round(4).tolist(),
            "role_stats": [
                RoleContributionStat(
                    slot=i + (1 if target_side == 0 else 12),
                    xt_pass=round(float(role_xt_pass[i] / n), 4),
                    xt_carry=round(float(role_xt_carry[i] / n), 4),
                    xt_lost=round(float(role_xt_lost[i] / n), 4),
                    vaep_offensive=round(float(role_vaep_off[i] / n), 4),
                    vaep_defensive=round(float(role_vaep_def[i] / n), 4),
                )
                for i in range(11)
            ],
            "shot_hist": shot_hist,
            "assist_counts": assist_counts,
            "cone_counts": cone_counts,
            "waterfall_means": (
                float(np.mean(buildup_ret_vec)),
                float(np.mean(final_entry_vec)),
                float(np.mean(shot_conv_vec)),
                float(np.mean(trans_vuln_vec)),
                float(np.mean(set_piece_vec)),
            ),
            "seed_summaries": seed_summaries,
        }

    def compare_arms(
        self,
        baseline_matches: Sequence[Sequence[SimulatedEventRecord]],
        variant_matches: Sequence[Sequence[SimulatedEventRecord]],
        target_team_side: Literal["home", "away"] = "home",
        n_boot: int = 1000,
    ) -> DiagnosticAttributionReport:
        """Compare Variant vs Baseline across paired CRN runs and build full diagnostics."""
        t_side = 0 if target_team_side == "home" else 1
        base = self._aggregate_arm(baseline_matches, t_side)
        var = self._aggregate_arm(variant_matches, t_side)

        xg_delta = self.paired_stat_delta(base["xg_diffs"], var["xg_diffs"], n_boot=n_boot)
        win_delta = float(np.mean(var["wins"]) - np.mean(base["wins"]))

        zone_stats = [
            ZoneDeltaStat(
                zone_id=z,
                zone_name=_ZONE_NAMES_15[z],
                entries_per_90=round(float(var["zone_entries_p90"][z] - base["zone_entries_p90"][z]), 3),
                progression_rate=round(float(var["zone_prog_rate"][z] - base["zone_prog_rate"][z]), 4),
                mean_xt_added=round(float(var["zone_mean_xt"][z] - base["zone_mean_xt"][z]), 4),
            )
            for z in range(15)
        ]

        z14_delta = float(var["zone_entries_p90"][12] - base["zone_entries_p90"][12])
        hs_delta = float(
            (var["zone_entries_p90"][11] + var["zone_entries_p90"][13])
            - (base["zone_entries_p90"][11] + base["zone_entries_p90"][13])
        )

        # Additive xGDiff Decomposition Waterfall
        b_wf = base["waterfall_means"]
        v_wf = var["waterfall_means"]
        d_buildup = v_wf[0] - b_wf[0]
        d_entry = v_wf[1] - b_wf[1]
        d_conv = v_wf[2] - b_wf[2]
        d_trans = v_wf[3] - b_wf[3]
        d_sp = v_wf[4] - b_wf[4]

        # Normalize residual onto exact observed delta_xg_diff so waterfall bridges cleanly
        raw_sum = d_buildup + d_entry + d_conv - d_trans + d_sp
        residual = xg_delta.mean_delta - raw_sum
        waterfall = XGDiffWaterfall(
            build_up_retention=round(d_buildup + 0.35 * residual, 4),
            final_third_entry_quality=round(d_entry + 0.35 * residual, 4),
            shot_conversion_quality=round(d_conv + 0.30 * residual, 4),
            transition_vulnerability_allowed=round(d_trans, 4),
            set_piece_delta=round(d_sp, 4),
            total_xg_diff_delta=round(xg_delta.mean_delta, 4),
        )

        # Pick representative median, upside, and downside match seeds
        reps: List[RepresentativeMatchSeed] = []
        sorted_seeds = sorted(var["seed_summaries"], key=lambda s: s.xg_diff)
        if sorted_seeds:
            low_s = sorted_seeds[max(0, len(sorted_seeds) // 10)].model_copy(update={"tag": "risk_tail_p10"})
            med_s = sorted_seeds[len(sorted_seeds) // 2].model_copy(update={"tag": "median_outcome"})
            high_s = sorted_seeds[min(len(sorted_seeds) - 1, (9 * len(sorted_seeds)) // 10)].model_copy(
                update={"tag": "upside_tail_p90"}
            )
            reps = [med_s, low_s, high_s]

        return DiagnosticAttributionReport(
            target_team_side=target_team_side,
            n_matches=len(variant_matches),
            xg_diff_delta=xg_delta,
            win_prob_delta=round(win_delta, 4),
            zone_progression_15=zone_stats,
            zone_14_entries_delta_p90=round(z14_delta, 3),
            half_space_entries_delta_p90=round(hs_delta, 3),
            turnover_causes_p90={
                k: round(var["turnover_causes_p90"][k] - base["turnover_causes_p90"][k], 3)
                for k in var["turnover_causes_p90"]
            },
            turnover_xg_cost_15s_p90=round(var["turnover_xg_cost_15s_p90"] - base["turnover_xg_cost_15s_p90"], 4),
            turnover_grid_12x8_cost=var["turnover_grid"],
            role_contributions=var["role_stats"],
            shot_quality_histogram=var["shot_hist"],
            assist_pattern_counts=var["assist_counts"],
            defenders_in_cone_counts=var["cone_counts"],
            waterfall=waterfall,
            representative_match_seeds=reps,
        )

    @staticmethod
    def compute_spatial_redundancy(
        coplay_matches: Sequence[Sequence[SimulatedEventRecord]],
        slot_a: int,
        slot_b: int,
        target_team_side: int = 0,
        solo_a_xg_diff_delta: float = 0.0,
        solo_b_xg_diff_delta: float = 0.0,
        coplay_xg_diff_delta: float = 0.0,
    ) -> SpatialRedundancyReport:
        """Compute 12x8 touch-heatmap JSD, spatial overlap, VORP_sim, and synergy (Q4)."""
        grid_a = np.full((12, 8), 1e-6, dtype=np.float64)
        grid_b = np.full((12, 8), 1e-6, dtype=np.float64)
        for events in coplay_matches:
            for ev in events:
                if ev.team_side != target_team_side:
                    continue
                x_att, y_att = _to_attacking_xy(ev.x_canon, ev.y_canon, ev.team_side)
                gx = min(11, max(0, int(x_att / 10.0)))
                gy = min(7, max(0, int(y_att / 10.0)))
                if ev.actor_slot == slot_a:
                    grid_a[gx, gy] += 1.0
                elif ev.actor_slot == slot_b:
                    grid_b[gx, gy] += 1.0

        p = (grid_a / grid_a.sum()).ravel()
        q = (grid_b / grid_b.sum()).ravel()
        m = 0.5 * (p + q)
        jsd = 0.5 * float(np.sum(p * np.log2(p / m)) + np.sum(q * np.log2(q / m)))
        jsd = max(0.0, min(1.0, jsd))
        overlap = 1.0 - jsd
        synergy = coplay_xg_diff_delta - (solo_a_xg_diff_delta + solo_b_xg_diff_delta)

        return SpatialRedundancyReport(
            slot_a=slot_a,
            slot_b=slot_b,
            js_divergence=round(jsd, 4),
            spatial_overlap_score=round(overlap, 4),
            zone_congestion_flag=overlap > 0.78,
            vorp_sim_xg_diff=round(coplay_xg_diff_delta, 4),
            synergy_xg_diff=round(synergy, 4),
        )


def _coerce_match_config(raw: Any) -> MatchConfigurationV2:
    """Coerce dict or MatchConfigurationV2 into a validated MatchConfigurationV2."""
    if isinstance(raw, MatchConfigurationV2):
        return raw
    data = dict(raw or {})
    if "home" in data and "away" in data:
        return MatchConfigurationV2.model_validate(data)
    home_raw = data.get("home_team") or {}
    away_raw = data.get("away_team") or {}
    home_cfg = build_default_team_config(
        team_name=str(home_raw.get("name", "Home")),
        base_player_id=100,
        team_id=int(home_raw.get("team_id") or 1),
        formation_name=str(home_raw.get("in_possession_formation") or "4-3-3"),
    )
    away_cfg = build_default_team_config(
        team_name=str(away_raw.get("name", "Away")),
        base_player_id=200,
        team_id=int(away_raw.get("team_id") or 2),
        formation_name=str(away_raw.get("in_possession_formation") or "4-4-2"),
    )
    for t_raw, t_cfg in ((home_raw, home_cfg), (away_raw, away_cfg)):
        tactics_raw = t_raw.get("tactics") or {}
        for k, v in tactics_raw.items():
            if hasattr(t_cfg.tactical_instructions, k):
                setattr(t_cfg.tactical_instructions, k, max(0.0, min(1.0, float(v))))
    return MatchConfigurationV2(
        match_id=int(data.get("match_id") or 1),
        home=home_cfg,
        away=away_cfg,
        allow_ood_extrapolation=bool(data.get("allow_ood_extrapolation", False)),
    )


def assess_experiment_ood(
    baseline_config: Any,
    interventions: Optional[Sequence[Dict[str, Any]]] = None,
    registry: Optional[PlayerProfileRegistry] = None,
    models_dir: Optional[str | Path] = None,
) -> Dict[str, Any]:
    """Module E bridge assessing OOD guardrails across baseline & intervention arms."""
    detector = OODDetector(models_dir=models_dir, auto_load=True)
    reg = registry or PlayerProfileRegistry(models_dir=models_dir, auto_load=True)
    base_cfg = _coerce_match_config(baseline_config)
    base_rep = detector.assess_match(base_cfg, reg)

    per_arm: Dict[str, Any] = {"baseline": base_rep.model_dump(mode="json")}
    max_score = base_rep.ood_score
    worst_tier = base_rep.confidence_tier
    all_reasons = [c.message for c in base_rep.triggered_checks]

    tier_rank = {"GREEN": 0, "AMBER": 1, "RED": 2}
    for idx, raw_arm in enumerate(interventions or ()):
        arm_id = str(raw_arm.get("arm_id") or raw_arm.get("variant_id") or f"arm_{idx}")
        t_ov = dict(raw_arm.get("tactical_overrides") or {})
        for patch in raw_arm.get("patches") or ():
            target = str(patch.get("target") or "")
            val = patch.get("value")
            if isinstance(val, (int, float)) and "." in target:
                slider = target.split(".")[-1]
                if slider in TACTICAL_SLIDER_NAMES:
                    t_ov[slider] = float(val)
        var = ExperimentVariant(
            variant_id=arm_id,
            label=str(raw_arm.get("label") or arm_id),
            team_side="away" if raw_arm.get("team_side") == "away" else "home",
            tactical_overrides=t_ov or None,
        )
        arm_cfg = apply_variant_to_config(base_cfg, var)
        arm_rep = detector.assess_match(arm_cfg, reg)
        per_arm[arm_id] = arm_rep.model_dump(mode="json")
        max_score = max(max_score, arm_rep.ood_score)
        if tier_rank[arm_rep.confidence_tier] > tier_rank[worst_tier]:
            worst_tier = arm_rep.confidence_tier
        all_reasons.extend(c.message for c in arm_rep.triggered_checks)

    warning_level = "ok" if worst_tier == "GREEN" else ("caution" if worst_tier == "AMBER" else "blocked")
    return {
        "in_distribution": worst_tier != "RED",
        "confidence_tier": worst_tier,
        "ood_score": round(max_score, 4),
        "warning_level": warning_level,
        "ood_reasons": all_reasons,
        "per_arm": per_arm,
    }
