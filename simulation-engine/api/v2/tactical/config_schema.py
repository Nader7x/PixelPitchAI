"""Module A: Match Configuration Schema, Formations, Roles, & Tactical Presets.

Defines the strongly-typed Pydantic v2 contracts for PixelPitchAI v2 single-match
simulations and counterfactual Monte Carlo experimentation batches.
"""

from __future__ import annotations

from copy import deepcopy
from enum import Enum
from typing import Dict, List, Literal, Optional, Sequence, Tuple

from pydantic import BaseModel, Field, model_validator

PITCH_LENGTH_YD: float = 120.0
PITCH_WIDTH_YD: float = 80.0

ATTRIBUTE_NAMES_24D: Tuple[str, ...] = (
    # Passing & Progression (6)
    "short_pass_accuracy",
    "progressive_pass_volume",
    "long_switch_propensity",
    "through_ball_vision",
    "cross_frequency",
    "press_resistance",
    # Ball Manipulation & Carrying (4)
    "dribble_takeon_volume",
    "dribble_success_skill",
    "ball_carry_progression",
    "first_touch_retention",
    # Shooting & Threat (5)
    "shot_volume_box",
    "shot_volume_long",
    "finishing_skill_psxg_delta",
    "aerial_box_threat",
    "weak_foot_balance",
    # Defending & Pressing (6)
    "pressing_workrate",
    "tackle_duel_win_rate",
    "interception_reading",
    "aerial_duel_win_rate",
    "foul_proneness",
    "recovery_speed_proxy",
    # Physical & Specialist (3)
    "stamina_endurance",
    "sprint_recovery_capacity",
    "set_piece_delivery",
)

GK_ATTRIBUTE_NAMES_8D: Tuple[str, ...] = (
    "gk_shot_stopping_psxg_prevented",
    "gk_save_hold_ratio",
    "gk_sweeper_action_rate",
    "gk_claim_cross_rate",
    "gk_short_buildup_share",
    "gk_long_kick_accuracy",
    "left_vs_right_lane_bias",
    "positional_verticality",
)

TACTICAL_SLIDER_NAMES: Tuple[str, ...] = (
    "defensive_line_height",
    "pressing_intensity",
    "build_up_tempo",
    "directness",
    "attacking_width",
    "defensive_compactness",
    "counter_press_vs_regroup",
    "counter_attack_urgency",
    "possession_risk_appetite",
    "game_state_adaptivity",
)


class TacticalPhase(str, Enum):
    """Canonical four-phase tactical states for formation shape modulation."""

    BUILD_UP = "build_up"
    IN_POSSESSION = "in_possession"
    HIGH_PRESS = "high_press"
    OUT_OF_POSSESSION = "out_of_possession"


class PlayerRole(str, Enum):
    """Canonical 22 tactical player roles (IDs 0..21)."""

    SWEEPER_KEEPER = "SweeperKeeper"
    SHOT_STOPPER_GK = "ShotStopperGK"
    BALL_PLAYING_CB = "BallPlayingCB"
    STOPPER_CB = "StopperCB"
    WIDE_CENTER_BACK = "WideCenterBack"
    INVERTED_FULLBACK = "InvertedFullback"
    OVERLAPPING_FULLBACK = "OverlappingFullback"
    DEFENSIVE_FULLBACK = "DefensiveFullback"
    COMPLETE_WING_BACK = "CompleteWingBack"
    DEEP_LYING_PLAYMAKER = "DeepLyingPlaymaker"
    DESTROYER_DM = "DestroyerDM"
    BOX_TO_BOX_MIDFIELDER = "BoxToBoxMidfielder"
    MEZZALA = "Mezzala"
    CARRILERO = "Carrilero"
    ADVANCED_PLAYMAKER = "AdvancedPlaymaker"
    INVERTED_WINGER = "InvertedWinger"
    TOUCHLINE_WINGER = "TouchlineWinger"
    INSIDE_FORWARD = "InsideForward"
    FALSE_9 = "False9"
    PRESSING_FORWARD = "PressingForward"
    TARGET_FORWARD = "TargetForward"
    POACHER = "Poacher"


ROLE_ORDER: Tuple[PlayerRole, ...] = tuple(PlayerRole)
ROLE_TO_ID: Dict[PlayerRole, int] = {role: idx for idx, role in enumerate(ROLE_ORDER)}
ID_TO_ROLE: Dict[int, PlayerRole] = {idx: role for idx, role in enumerate(ROLE_ORDER)}

GK_ROLES: Tuple[PlayerRole, ...] = (
    PlayerRole.SWEEPER_KEEPER,
    PlayerRole.SHOT_STOPPER_GK,
)

# 16-dim Behavioral Prior Vector per PlayerRole:
# [forward_line, width, inward_halfspace, prog_pass, long_switch, carry, dribble_1v1,
#  box_entry, shot_appetite, press_aggr, intercept, rest_def_hold, aerial_target,
#  drop_deep, sprint_load, buildup_involvement]
ROLE_PRIOR_VECTORS: Dict[PlayerRole, Tuple[float, ...]] = {
    PlayerRole.SWEEPER_KEEPER: (0.08, 0.50, 0.05, 0.45, 0.35, 0.10, 0.02, 0.00, 0.00, 0.18, 0.40, 0.95, 0.25, 0.00, 0.22, 0.85),
    PlayerRole.SHOT_STOPPER_GK: (0.04, 0.50, 0.02, 0.20, 0.60, 0.04, 0.01, 0.00, 0.00, 0.05, 0.25, 0.98, 0.35, 0.00, 0.12, 0.35),
    PlayerRole.BALL_PLAYING_CB: (0.28, 0.42, 0.20, 0.78, 0.72, 0.62, 0.18, 0.08, 0.08, 0.48, 0.65, 0.88, 0.62, 0.05, 0.45, 0.88),
    PlayerRole.STOPPER_CB: (0.25, 0.44, 0.15, 0.38, 0.35, 0.22, 0.06, 0.06, 0.05, 0.78, 0.72, 0.92, 0.80, 0.02, 0.52, 0.42),
    PlayerRole.WIDE_CENTER_BACK: (0.38, 0.68, 0.35, 0.65, 0.50, 0.68, 0.28, 0.18, 0.12, 0.62, 0.68, 0.72, 0.55, 0.05, 0.68, 0.74),
    PlayerRole.INVERTED_FULLBACK: (0.48, 0.30, 0.85, 0.74, 0.42, 0.55, 0.32, 0.20, 0.18, 0.65, 0.68, 0.74, 0.25, 0.18, 0.62, 0.86),
    PlayerRole.OVERLAPPING_FULLBACK: (0.66, 0.92, 0.12, 0.58, 0.38, 0.66, 0.56, 0.42, 0.18, 0.62, 0.55, 0.42, 0.22, 0.05, 0.90, 0.62),
    PlayerRole.DEFENSIVE_FULLBACK: (0.36, 0.66, 0.22, 0.44, 0.40, 0.28, 0.16, 0.10, 0.06, 0.58, 0.72, 0.88, 0.45, 0.04, 0.54, 0.52),
    PlayerRole.COMPLETE_WING_BACK: (0.68, 0.90, 0.20, 0.62, 0.45, 0.72, 0.62, 0.50, 0.26, 0.68, 0.56, 0.36, 0.26, 0.08, 0.95, 0.68),
    PlayerRole.DEEP_LYING_PLAYMAKER: (0.45, 0.48, 0.50, 0.90, 0.88, 0.42, 0.24, 0.14, 0.20, 0.50, 0.66, 0.76, 0.25, 0.35, 0.52, 0.96),
    PlayerRole.DESTROYER_DM: (0.40, 0.48, 0.45, 0.42, 0.28, 0.26, 0.14, 0.12, 0.14, 0.88, 0.90, 0.86, 0.55, 0.18, 0.72, 0.56),
    PlayerRole.BOX_TO_BOX_MIDFIELDER: (0.60, 0.50, 0.55, 0.66, 0.45, 0.68, 0.46, 0.62, 0.48, 0.78, 0.66, 0.48, 0.45, 0.25, 0.90, 0.72),
    PlayerRole.MEZZALA: (0.68, 0.38, 0.82, 0.72, 0.48, 0.76, 0.64, 0.58, 0.54, 0.66, 0.48, 0.35, 0.26, 0.24, 0.78, 0.74),
    PlayerRole.CARRILERO: (0.52, 0.66, 0.46, 0.58, 0.42, 0.52, 0.34, 0.28, 0.24, 0.74, 0.70, 0.66, 0.35, 0.16, 0.78, 0.68),
    PlayerRole.ADVANCED_PLAYMAKER: (0.74, 0.44, 0.76, 0.86, 0.52, 0.66, 0.62, 0.52, 0.54, 0.46, 0.34, 0.20, 0.18, 0.45, 0.58, 0.80),
    PlayerRole.INVERTED_WINGER: (0.76, 0.64, 0.84, 0.62, 0.36, 0.80, 0.82, 0.68, 0.72, 0.56, 0.34, 0.20, 0.18, 0.28, 0.78, 0.62),
    PlayerRole.TOUCHLINE_WINGER: (0.76, 0.94, 0.14, 0.52, 0.34, 0.82, 0.86, 0.50, 0.46, 0.56, 0.32, 0.22, 0.18, 0.12, 0.86, 0.56),
    PlayerRole.INSIDE_FORWARD: (0.84, 0.54, 0.80, 0.46, 0.22, 0.70, 0.70, 0.88, 0.84, 0.58, 0.30, 0.14, 0.38, 0.14, 0.84, 0.44),
    PlayerRole.FALSE_9: (0.78, 0.48, 0.68, 0.78, 0.40, 0.68, 0.68, 0.56, 0.60, 0.54, 0.32, 0.16, 0.20, 0.88, 0.66, 0.78),
    PlayerRole.PRESSING_FORWARD: (0.86, 0.50, 0.52, 0.42, 0.18, 0.50, 0.46, 0.74, 0.68, 0.94, 0.54, 0.18, 0.48, 0.28, 0.92, 0.44),
    PlayerRole.TARGET_FORWARD: (0.88, 0.50, 0.45, 0.36, 0.14, 0.26, 0.24, 0.82, 0.74, 0.44, 0.22, 0.14, 0.96, 0.24, 0.48, 0.48),
    PlayerRole.POACHER: (0.92, 0.50, 0.52, 0.24, 0.08, 0.30, 0.34, 0.95, 0.92, 0.38, 0.18, 0.08, 0.62, 0.06, 0.68, 0.24),
}

# Role spatial anchor shifts applied in normalized team-relative coords:
# (dx, inward_y_toward_center) per phase. inward_y > 0 pulls toward y=0.5.
ROLE_ANCHOR_OFFSETS: Dict[PlayerRole, Dict[TacticalPhase, Tuple[float, float]]] = {
    PlayerRole.SWEEPER_KEEPER: {
        TacticalPhase.BUILD_UP: (0.04, 0.0),
        TacticalPhase.IN_POSSESSION: (0.09, 0.0),
        TacticalPhase.HIGH_PRESS: (0.07, 0.0),
        TacticalPhase.OUT_OF_POSSESSION: (0.03, 0.0),
    },
    PlayerRole.INVERTED_FULLBACK: {
        TacticalPhase.BUILD_UP: (0.08, 0.14),
        TacticalPhase.IN_POSSESSION: (0.12, 0.22),
    },
    PlayerRole.OVERLAPPING_FULLBACK: {
        TacticalPhase.IN_POSSESSION: (0.18, -0.06),
    },
    PlayerRole.DEFENSIVE_FULLBACK: {
        TacticalPhase.IN_POSSESSION: (-0.06, 0.08),
    },
    PlayerRole.COMPLETE_WING_BACK: {
        TacticalPhase.IN_POSSESSION: (0.15, -0.04),
    },
    PlayerRole.DEEP_LYING_PLAYMAKER: {
        TacticalPhase.BUILD_UP: (-0.06, 0.02),
    },
    PlayerRole.MEZZALA: {
        TacticalPhase.IN_POSSESSION: (0.06, -0.04),
    },
    PlayerRole.INVERTED_WINGER: {
        TacticalPhase.IN_POSSESSION: (0.03, 0.14),
    },
    PlayerRole.TOUCHLINE_WINGER: {
        TacticalPhase.IN_POSSESSION: (0.04, -0.06),
    },
    PlayerRole.INSIDE_FORWARD: {
        TacticalPhase.IN_POSSESSION: (0.08, 0.18),
    },
    PlayerRole.FALSE_9: {
        TacticalPhase.IN_POSSESSION: (-0.10, 0.0),
    },
    PlayerRole.PRESSING_FORWARD: {
        TacticalPhase.HIGH_PRESS: (0.05, 0.0),
    },
}


class PhaseSlotAnchor(BaseModel):
    """Canonical tactical anchor & leash parameters for one slot in a phase."""

    slot_index: int = Field(..., ge=0, le=10)
    anchor_x: float = Field(..., ge=0.0, le=1.0)
    anchor_y: float = Field(..., ge=0.0, le=1.0)
    zone_radius: float = Field(0.12, ge=0.04, le=0.30)
    ball_shift_x: float = Field(0.35, ge=0.0, le=0.75)
    ball_shift_y: float = Field(0.25, ge=0.0, le=0.75)


class FormationConfig(BaseModel):
    """Dual-phase formation specification across four tactical phases."""

    base_name: str = Field(..., examples=["4-3-3", "4-2-3-1", "3-5-2"])
    phase_anchors: Dict[TacticalPhase, List[PhaseSlotAnchor]] = Field(
        default_factory=dict,
        description="Optional explicit 11-slot anchors per phase; auto-populated from preset + roles if omitted.",
    )

    @model_validator(mode="after")
    def validate_phase_lengths(self) -> "FormationConfig":
        for phase, anchors in self.phase_anchors.items():
            if len(anchors) != 11:
                raise ValueError(f"Phase {phase.value} must have exactly 11 slot anchors, got {len(anchors)}.")
            indices = {a.slot_index for a in anchors}
            if indices != set(range(11)):
                raise ValueError(f"Phase {phase.value} must cover slot_index 0..10 exactly once.")
        return self


FormationPhaseConfig = FormationConfig


class PlayerInstructionOverrides(BaseModel):
    """Per-slot tactical instruction adjustments."""

    freedom_to_roam: float = Field(0.5, ge=0.0, le=1.0)
    width_bias: float = Field(0.5, ge=0.0, le=1.0)
    forward_run_appetite: float = Field(0.5, ge=0.0, le=1.0)
    pressing_aggressiveness: float = Field(0.5, ge=0.0, le=1.0)
    passing_risk_bias: float = Field(0.5, ge=0.0, le=1.0)


class PlayerAttributeOverride(BaseModel):
    """Enables custom hypothetical players or counterfactual attribute perturbations."""

    display_name: str = Field(..., min_length=1, max_length=120)
    clone_from_player_id: Optional[int] = Field(None, ge=1)
    attributes_24d: Optional[Dict[str, float]] = None
    attribute_deltas: Optional[Dict[str, float]] = None
    gk_attributes_8d: Optional[Dict[str, float]] = None
    finishing_log_odds: Optional[float] = Field(None, ge=-1.0, le=1.0)
    latent_style_64d: Optional[List[float]] = None

    @model_validator(mode="after")
    def validate_override_bounds(self) -> "PlayerAttributeOverride":
        if self.attributes_24d:
            unknown = set(self.attributes_24d) - set(ATTRIBUTE_NAMES_24D)
            if unknown:
                raise ValueError(f"Unknown attributes_24d keys: {sorted(unknown)}")
            for k, v in self.attributes_24d.items():
                if not (0.0 <= v <= 1.0):
                    raise ValueError(f"Attribute {k}={v} must lie in [0, 1].")
        if self.attribute_deltas:
            unknown = set(self.attribute_deltas) - set(ATTRIBUTE_NAMES_24D)
            if unknown:
                raise ValueError(f"Unknown attribute_deltas keys: {sorted(unknown)}")
        if self.gk_attributes_8d:
            unknown = set(self.gk_attributes_8d) - set(GK_ATTRIBUTE_NAMES_8D)
            if unknown:
                raise ValueError(f"Unknown gk_attributes_8d keys: {sorted(unknown)}")
        if self.latent_style_64d is not None and len(self.latent_style_64d) != 64:
            raise ValueError(f"latent_style_64d must have length 64, got {len(self.latent_style_64d)}.")
        return self


class LineupSlotConfig(BaseModel):
    """Configuration for one starting XI tactical slot (`0..10` within team)."""

    slot_index: int = Field(..., ge=0, le=10)
    player_id: Optional[int] = Field(None, ge=1)
    inline_profile_override: Optional[PlayerAttributeOverride] = None
    shirt_number: int = Field(..., ge=1, le=99)
    preferred_foot: Literal["Right", "Left", "Both"] = "Right"
    base_position: str = Field(..., min_length=2, max_length=6)
    tactical_role: PlayerRole
    player_instructions: PlayerInstructionOverrides = Field(default_factory=PlayerInstructionOverrides)
    initial_fitness: float = Field(1.0, ge=0.4, le=1.0)

    @model_validator(mode="after")
    def validate_player_source(self) -> "LineupSlotConfig":
        if self.player_id is None and self.inline_profile_override is None:
            raise ValueError("Each slot requires either player_id or inline_profile_override.")
        if self.slot_index == 0 and self.tactical_role not in GK_ROLES:
            raise ValueError("Slot 0 must be assigned a Goalkeeper role (SweeperKeeper or ShotStopperGK).")
        return self


class BenchPlayerConfig(BaseModel):
    """Configuration for one substitute player on the matchday bench."""

    player_id: Optional[int] = Field(None, ge=1)
    inline_profile_override: Optional[PlayerAttributeOverride] = None
    shirt_number: int = Field(..., ge=1, le=99)
    preferred_foot: Literal["Right", "Left", "Both"] = "Right"
    natural_positions: List[str] = Field(..., min_length=1)
    default_role: PlayerRole
    initial_fitness: float = Field(1.0, ge=0.4, le=1.0)

    @model_validator(mode="after")
    def validate_bench_source(self) -> "BenchPlayerConfig":
        if self.player_id is None and self.inline_profile_override is None:
            raise ValueError("Each bench slot requires either player_id or inline_profile_override.")
        return self


class TacticalInstructions(BaseModel):
    """Continuous [0, 1] team-level tactical control sliders."""

    defensive_line_height: float = Field(0.55, ge=0.0, le=1.0)
    pressing_intensity: float = Field(0.55, ge=0.0, le=1.0)
    build_up_tempo: float = Field(0.50, ge=0.0, le=1.0)
    directness: float = Field(0.45, ge=0.0, le=1.0)
    attacking_width: float = Field(0.55, ge=0.0, le=1.0)
    defensive_compactness: float = Field(0.55, ge=0.0, le=1.0)
    counter_press_vs_regroup: float = Field(0.55, ge=0.0, le=1.0)
    counter_attack_urgency: float = Field(0.50, ge=0.0, le=1.0)
    possession_risk_appetite: float = Field(0.50, ge=0.0, le=1.0)
    game_state_adaptivity: float = Field(0.75, ge=0.0, le=1.0)

    def to_vector(self) -> List[float]:
        """Return ordered 10-element tactical instruction vector."""
        return [float(getattr(self, name)) for name in TACTICAL_SLIDER_NAMES]

    def defensive_line_meters(self) -> float:
        """Convert normalized line height [0, 1] to canonical meters [22m, 52m]."""
        return 22.0 + 30.0 * self.defensive_line_height

    def target_ppda(self) -> float:
        """Convert normalized pressing intensity [0, 1] to target PPDA [18.0, 5.5]."""
        return 18.0 - 12.5 * self.pressing_intensity


class ScriptedSubstitutionRule(BaseModel):
    """Deterministic or conditional counterfactual substitution trigger."""

    trigger_minute: int = Field(..., ge=1, le=120)
    condition_score_diff_max: Optional[int] = None
    condition_score_diff_min: Optional[int] = None
    out_slot_index: int = Field(..., ge=0, le=10)
    in_player_id: Optional[int] = Field(None, ge=1)
    in_bench_index: Optional[int] = Field(
        None,
        ge=0,
        le=11,
        description="Bench slot index when substituting a custom inline_profile_override player with player_id=None.",
    )
    new_role: Optional[PlayerRole] = None
    new_formation: Optional[str] = None
    tactical_patch: Optional[Dict[str, float]] = None

    @model_validator(mode="after")
    def validate_sub_target(self) -> "ScriptedSubstitutionRule":
        if self.in_player_id is None and self.in_bench_index is None:
            raise ValueError("ScriptedSubstitutionRule requires either in_player_id or in_bench_index.")
        if self.tactical_patch:
            unknown = set(self.tactical_patch) - set(TACTICAL_SLIDER_NAMES)
            if unknown:
                raise ValueError(f"Unknown tactical_patch sliders: {sorted(unknown)}")
        return self


class SubstitutionPolicy(BaseModel):
    """Automated, rule-based, or scripted substitution controller."""

    mode: Literal["scripted", "rule_based", "auto_coach", "hybrid"] = "hybrid"
    max_substitutions: int = Field(5, ge=0, le=6)
    max_windows: int = Field(3, ge=0, le=4)
    stamina_sub_threshold: float = Field(0.48, ge=0.20, le=0.80)
    protect_yellow_card_threshold_pressing: float = Field(0.70, ge=0.30, le=1.0)
    scripted_rules: List[ScriptedSubstitutionRule] = Field(default_factory=list)


class TeamMatchConfig(BaseModel):
    """Complete team configuration for one side (Home or Away)."""

    team_id: Optional[int] = Field(None, ge=1)
    team_name: str = Field(..., min_length=1, max_length=100)
    season_label: Optional[str] = Field(None, pattern=r"^\d{4}/\d{4}$")
    coach_id: Optional[int] = Field(None, ge=1)
    formation: FormationConfig
    tactical_instructions: TacticalInstructions = Field(default_factory=TacticalInstructions)
    starting_xi: List[LineupSlotConfig] = Field(..., min_length=11, max_length=11)
    bench: List[BenchPlayerConfig] = Field(default_factory=list, max_length=12)
    substitution_policy: SubstitutionPolicy = Field(default_factory=SubstitutionPolicy)

    @model_validator(mode="after")
    def validate_team_consistency(self) -> "TeamMatchConfig":
        slot_indices = [s.slot_index for s in self.starting_xi]
        if set(slot_indices) != set(range(11)):
            raise ValueError("starting_xi must contain unique slot_index values covering 0..10.")
        bench_ids = {b.player_id for b in self.bench if b.player_id is not None}
        for rule in self.substitution_policy.scripted_rules:
            if rule.in_bench_index is not None and rule.in_bench_index >= len(self.bench):
                raise ValueError(f"in_bench_index={rule.in_bench_index} exceeds bench size {len(self.bench)}.")
            if rule.in_player_id is not None and rule.in_player_id not in bench_ids:
                raise ValueError(f"in_player_id={rule.in_player_id} is not present in bench roster.")
        return self

    def ordered_xi(self) -> List[LineupSlotConfig]:
        """Return starting XI sorted deterministically by slot_index 0..10."""
        return sorted(self.starting_xi, key=lambda s: s.slot_index)


TeamMatchConfigV2 = TeamMatchConfig


class MatchContextConfig(BaseModel):
    """Environmental and officiating priors for the simulated match."""

    competition_tier: Literal["top_5_league", "european_knockout", "domestic_cup", "international"] = "top_5_league"
    home_advantage_strength: float = Field(0.5, ge=0.0, le=1.0)
    referee_strictness: float = Field(0.5, ge=0.0, le=1.0)
    pitch_surface: Literal["Grass", "Hybrid", "Artificial"] = "Hybrid"
    weather_wetness: float = Field(0.1, ge=0.0, le=1.0)
    extra_time_if_draw: bool = False


class MatchConfigurationV2(BaseModel):
    """Top-level Module A configuration schema for single matches & batch variants."""

    match_id: int = Field(..., ge=1)
    home: TeamMatchConfig
    away: TeamMatchConfig
    context: MatchContextConfig = Field(default_factory=MatchContextConfig)
    random_seed: Optional[int] = None
    allow_ood_extrapolation: bool = False


class ExperimentVariant(BaseModel):
    """Single counterfactual intervention relative to a baseline match setup."""

    variant_id: str = Field(..., min_length=1, max_length=64)
    label: str = Field(..., min_length=1, max_length=160)
    team_side: Literal["home", "away"] = "home"
    lineup_swaps: Optional[Dict[int, LineupSlotConfig]] = None
    tactical_overrides: Optional[Dict[str, float]] = None
    role_overrides: Optional[Dict[int, PlayerRole]] = None
    formation_override: Optional[FormationConfig] = None
    sub_policy_override: Optional[SubstitutionPolicy] = None


class OpponentArchetype(BaseModel):
    """Stratified opponent archetype for generalizing player/tactic fit studies."""

    archetype_id: str
    label: str
    away_config: TeamMatchConfig
    weight: float = Field(1.0, gt=0.0)


class CounterfactualExperimentRequest(BaseModel):
    """Batch Monte Carlo counterfactual experiment contract."""

    experiment_id: str = Field(..., min_length=1, max_length=64)
    question_type: Literal[
        "midfielder_fit",
        "tactical_block_comparison",
        "striker_system_fit",
        "signing_upgrade_vs_redundancy",
        "custom",
    ] = "custom"
    baseline: MatchConfigurationV2
    variants: List[ExperimentVariant] = Field(..., min_length=1, max_length=8)
    opponent_suite: Optional[List[OpponentArchetype]] = None
    n_simulations: int = Field(250, ge=50, le=1000)
    use_paired_crn: bool = True
    sample_player_epistemic_uncertainty: bool = True
    lock_game_state_adaptivity: Optional[float] = Field(None, ge=0.0, le=1.0)
    allow_ood_extrapolation: bool = False


# ---------------------------------------------------------------------------
# Formation Preset Library (11 canonical formations across all 4 phases)
# ---------------------------------------------------------------------------

def _make_anchors(coords: Sequence[Tuple[float, float]]) -> List[PhaseSlotAnchor]:
    anchors: List[PhaseSlotAnchor] = []
    for idx, (ax, ay) in enumerate(coords):
        if idx == 0:
            anchors.append(PhaseSlotAnchor(slot_index=0, anchor_x=ax, anchor_y=ay, zone_radius=0.06, ball_shift_x=0.18, ball_shift_y=0.12))
        elif ax < 0.35:
            anchors.append(PhaseSlotAnchor(slot_index=idx, anchor_x=ax, anchor_y=ay, zone_radius=0.11, ball_shift_x=0.36, ball_shift_y=0.22))
        elif ax < 0.65:
            anchors.append(PhaseSlotAnchor(slot_index=idx, anchor_x=ax, anchor_y=ay, zone_radius=0.13, ball_shift_x=0.40, ball_shift_y=0.28))
        else:
            anchors.append(PhaseSlotAnchor(slot_index=idx, anchor_x=ax, anchor_y=ay, zone_radius=0.14, ball_shift_x=0.38, ball_shift_y=0.25))
    return anchors


def _build_4phase_preset(
    base_coords: Sequence[Tuple[float, float]],
    buildup_dx: Sequence[float] | None = None,
    poss_dx: Sequence[float] | None = None,
    press_dx: Sequence[float] | None = None,
    oop_dx: Sequence[float] | None = None,
) -> Dict[TacticalPhase, List[PhaseSlotAnchor]]:
    buildup = [
        (max(0.04, min(0.95, x + (buildup_dx[i] if buildup_dx else (-0.06 if i > 0 else 0.02)))), y)
        for i, (x, y) in enumerate(base_coords)
    ]
    in_poss = [
        (max(0.05, min(0.96, x + (poss_dx[i] if poss_dx else (0.08 if i > 0 else 0.05)))), y)
        for i, (x, y) in enumerate(base_coords)
    ]
    high_press = [
        (max(0.05, min(0.94, x + (press_dx[i] if press_dx else (0.04 if i > 0 else 0.04)))), y)
        for i, (x, y) in enumerate(base_coords)
    ]
    out_poss = [
        (max(0.03, min(0.88, x + (oop_dx[i] if oop_dx else (-0.12 if i > 0 else 0.0)))), y)
        for i, (x, y) in enumerate(base_coords)
    ]
    return {
        TacticalPhase.BUILD_UP: _make_anchors(buildup),
        TacticalPhase.IN_POSSESSION: _make_anchors(in_poss),
        TacticalPhase.HIGH_PRESS: _make_anchors(high_press),
        TacticalPhase.OUT_OF_POSSESSION: _make_anchors(out_poss),
    }


_FORMATION_BASE_COORDS: Dict[str, Tuple[Tuple[float, float], ...]] = {
    "4-3-3": (
        (0.05, 0.50),
        (0.26, 0.14), (0.22, 0.37), (0.22, 0.63), (0.26, 0.86),
        (0.45, 0.50), (0.54, 0.30), (0.54, 0.70),
        (0.75, 0.16), (0.81, 0.50), (0.75, 0.84),
    ),
    "4-2-3-1": (
        (0.05, 0.50),
        (0.25, 0.14), (0.22, 0.37), (0.22, 0.63), (0.25, 0.86),
        (0.43, 0.36), (0.43, 0.64),
        (0.64, 0.18), (0.66, 0.50), (0.64, 0.82),
        (0.82, 0.50),
    ),
    "4-4-2": (
        (0.05, 0.50),
        (0.24, 0.14), (0.21, 0.37), (0.21, 0.63), (0.24, 0.86),
        (0.48, 0.14), (0.46, 0.38), (0.46, 0.62), (0.48, 0.86),
        (0.76, 0.36), (0.76, 0.64),
    ),
    "4-4-2_diamond": (
        (0.05, 0.50),
        (0.25, 0.14), (0.21, 0.37), (0.21, 0.63), (0.25, 0.86),
        (0.39, 0.50), (0.52, 0.28), (0.52, 0.72), (0.65, 0.50),
        (0.80, 0.37), (0.80, 0.63),
    ),
    "4-1-4-1": (
        (0.05, 0.50),
        (0.25, 0.14), (0.22, 0.37), (0.22, 0.63), (0.25, 0.86),
        (0.40, 0.50),
        (0.58, 0.14), (0.56, 0.36), (0.56, 0.64), (0.58, 0.86),
        (0.81, 0.50),
    ),
    "3-5-2": (
        (0.05, 0.50),
        (0.23, 0.26), (0.21, 0.50), (0.23, 0.74),
        (0.50, 0.10), (0.46, 0.35), (0.44, 0.50), (0.46, 0.65), (0.50, 0.90),
        (0.78, 0.37), (0.78, 0.63),
    ),
    "3-4-2-1": (
        (0.05, 0.50),
        (0.23, 0.26), (0.21, 0.50), (0.23, 0.74),
        (0.49, 0.10), (0.44, 0.38), (0.44, 0.62), (0.49, 0.90),
        (0.67, 0.32), (0.67, 0.68),
        (0.83, 0.50),
    ),
    "3-4-3": (
        (0.05, 0.50),
        (0.23, 0.26), (0.21, 0.50), (0.23, 0.74),
        (0.50, 0.10), (0.46, 0.38), (0.46, 0.62), (0.50, 0.90),
        (0.74, 0.22), (0.81, 0.50), (0.74, 0.78),
    ),
    "5-3-2": (
        (0.05, 0.50),
        (0.30, 0.10), (0.21, 0.29), (0.19, 0.50), (0.21, 0.71), (0.30, 0.90),
        (0.46, 0.30), (0.43, 0.50), (0.46, 0.70),
        (0.75, 0.37), (0.75, 0.63),
    ),
    "5-4-1": (
        (0.05, 0.50),
        (0.29, 0.10), (0.20, 0.29), (0.19, 0.50), (0.20, 0.71), (0.29, 0.90),
        (0.45, 0.16), (0.43, 0.39), (0.43, 0.61), (0.45, 0.84),
        (0.74, 0.50),
    ),
    "4-2-2-2": (
        (0.05, 0.50),
        (0.25, 0.14), (0.22, 0.37), (0.22, 0.63), (0.25, 0.86),
        (0.43, 0.35), (0.43, 0.65),
        (0.63, 0.26), (0.63, 0.74),
        (0.81, 0.37), (0.81, 0.63),
    ),
}

FORMATION_PRESETS: Dict[str, Dict[TacticalPhase, List[PhaseSlotAnchor]]] = {
    name: _build_4phase_preset(coords) for name, coords in _FORMATION_BASE_COORDS.items()
}


def resolve_formation_anchors(
    formation_cfg: FormationConfig,
    roles: Sequence[PlayerRole],
) -> Dict[TacticalPhase, List[PhaseSlotAnchor]]:
    """Resolve complete 4-phase anchors, blending preset coordinates with role shifts."""
    if len(roles) != 11:
        raise ValueError(f"Expected 11 roles, got {len(roles)}.")
    preset_key = formation_cfg.base_name if formation_cfg.base_name in FORMATION_PRESETS else "4-3-3"
    preset = FORMATION_PRESETS[preset_key]

    resolved: Dict[TacticalPhase, List[PhaseSlotAnchor]] = {}
    for phase in TacticalPhase:
        explicit = formation_cfg.phase_anchors.get(phase)
        base_list = explicit if explicit is not None else preset[phase]
        ordered = sorted(base_list, key=lambda a: a.slot_index)

        if explicit is not None:
            resolved[phase] = [deepcopy(a) for a in ordered]
            continue

        adjusted: List[PhaseSlotAnchor] = []
        for anchor, role in zip(ordered, roles):
            dx, inward_y = ROLE_ANCHOR_OFFSETS.get(role, {}).get(phase, (0.0, 0.0))
            side_sign = 1.0 if anchor.anchor_y < 0.5 else (-1.0 if anchor.anchor_y > 0.5 else 0.0)
            new_x = max(0.03, min(0.97, anchor.anchor_x + dx))
            new_y = max(0.04, min(0.96, anchor.anchor_y + side_sign * inward_y))
            adjusted.append(
                PhaseSlotAnchor(
                    slot_index=anchor.slot_index,
                    anchor_x=new_x,
                    anchor_y=new_y,
                    zone_radius=anchor.zone_radius,
                    ball_shift_x=anchor.ball_shift_x,
                    ball_shift_y=anchor.ball_shift_y,
                )
            )
        resolved[phase] = adjusted
    return resolved


# ---------------------------------------------------------------------------
# Coach Auto-Hydration & Counterfactual Variant Helpers
# ---------------------------------------------------------------------------

_COACHING_STYLE_PRESETS: Dict[str, Dict[str, float]] = {
    "gegenpressing": {
        "defensive_line_height": 0.78,
        "pressing_intensity": 0.86,
        "build_up_tempo": 0.68,
        "directness": 0.58,
        "attacking_width": 0.64,
        "defensive_compactness": 0.74,
        "counter_press_vs_regroup": 0.90,
        "counter_attack_urgency": 0.82,
        "possession_risk_appetite": 0.60,
    },
    "tiki-taka": {
        "defensive_line_height": 0.74,
        "pressing_intensity": 0.76,
        "build_up_tempo": 0.56,
        "directness": 0.22,
        "attacking_width": 0.68,
        "defensive_compactness": 0.72,
        "counter_press_vs_regroup": 0.82,
        "counter_attack_urgency": 0.42,
        "possession_risk_appetite": 0.52,
    },
    "possession": {
        "defensive_line_height": 0.68,
        "pressing_intensity": 0.70,
        "build_up_tempo": 0.48,
        "directness": 0.28,
        "attacking_width": 0.66,
        "defensive_compactness": 0.66,
        "counter_press_vs_regroup": 0.72,
        "counter_attack_urgency": 0.44,
        "possession_risk_appetite": 0.54,
    },
    "low block counter": {
        "defensive_line_height": 0.24,
        "pressing_intensity": 0.30,
        "build_up_tempo": 0.72,
        "directness": 0.82,
        "attacking_width": 0.52,
        "defensive_compactness": 0.82,
        "counter_press_vs_regroup": 0.16,
        "counter_attack_urgency": 0.88,
        "possession_risk_appetite": 0.40,
    },
    "balanced": {},
}


class CoachHydrator:
    """Hydrates Module A formation & tactics from legacy Coach DB fields."""

    @staticmethod
    def hydrate_tactics(
        preferred_formation: Optional[str] = None,
        coaching_style: Optional[str] = None,
    ) -> Tuple[FormationConfig, TacticalInstructions]:
        formation_name = preferred_formation if preferred_formation in FORMATION_PRESETS else "4-3-3"
        style_key = (coaching_style or "balanced").strip().lower()
        preset_dict = _COACHING_STYLE_PRESETS.get(style_key)
        if preset_dict is None:
            for candidate, values in _COACHING_STYLE_PRESETS.items():
                if candidate in style_key:
                    preset_dict = values
                    break
            else:
                preset_dict = {}
        return FormationConfig(base_name=formation_name), TacticalInstructions(**preset_dict)


def apply_variant_to_config(
    baseline: MatchConfigurationV2,
    variant: ExperimentVariant,
    lock_game_state_adaptivity: Optional[float] = None,
) -> MatchConfigurationV2:
    """Apply an ExperimentVariant deterministically without mutating baseline."""
    cfg = baseline.model_copy(deep=True)
    team = cfg.home if variant.team_side == "home" else cfg.away

    if variant.formation_override is not None:
        team.formation = variant.formation_override.model_copy(deep=True)

    if variant.tactical_overrides:
        data = team.tactical_instructions.model_dump()
        for k, v in variant.tactical_overrides.items():
            if k not in data:
                raise ValueError(f"Unknown tactical slider override: {k}")
            data[k] = max(0.0, min(1.0, float(v)))
        team.tactical_instructions = TacticalInstructions(**data)

    if lock_game_state_adaptivity is not None:
        cfg.home.tactical_instructions.game_state_adaptivity = lock_game_state_adaptivity
        cfg.away.tactical_instructions.game_state_adaptivity = lock_game_state_adaptivity

    if variant.lineup_swaps:
        xi_map = {slot.slot_index: slot for slot in team.starting_xi}
        for slot_idx, new_slot in variant.lineup_swaps.items():
            if slot_idx not in xi_map:
                raise ValueError(f"Invalid lineup_swaps slot_index {slot_idx}.")
            xi_map[slot_idx] = new_slot.model_copy(deep=True)
        team.starting_xi = [xi_map[i] for i in range(11)]

    if variant.role_overrides:
        xi_map = {slot.slot_index: slot for slot in team.starting_xi}
        for slot_idx, new_role in variant.role_overrides.items():
            if slot_idx not in xi_map:
                raise ValueError(f"Invalid role_overrides slot_index {slot_idx}.")
            xi_map[slot_idx].tactical_role = new_role
        team.starting_xi = [xi_map[i] for i in range(11)]

    if variant.sub_policy_override is not None:
        team.substitution_policy = variant.sub_policy_override.model_copy(deep=True)

    return MatchConfigurationV2.model_validate(cfg.model_dump())


_DEFAULT_433_SLOTS: Tuple[Tuple[str, PlayerRole, int], ...] = (
    ("GK", PlayerRole.SWEEPER_KEEPER, 1),
    ("LB", PlayerRole.OVERLAPPING_FULLBACK, 3),
    ("LCB", PlayerRole.BALL_PLAYING_CB, 4),
    ("RCB", PlayerRole.STOPPER_CB, 5),
    ("RB", PlayerRole.INVERTED_FULLBACK, 2),
    ("CDM", PlayerRole.DEEP_LYING_PLAYMAKER, 6),
    ("LCM", PlayerRole.MEZZALA, 8),
    ("RCM", PlayerRole.BOX_TO_BOX_MIDFIELDER, 10),
    ("LW", PlayerRole.INVERTED_WINGER, 11),
    ("ST", PlayerRole.FALSE_9, 9),
    ("RW", PlayerRole.INSIDE_FORWARD, 7),
)


def build_default_team_config(
    team_name: str,
    base_player_id: int = 1000,
    team_id: int = 1,
    formation_name: str = "4-3-3",
    coaching_style: str = "balanced",
) -> TeamMatchConfig:
    """Build a valid 11-starter + 5-bench TeamMatchConfig for hydration or testing."""
    formation_cfg, tactics = CoachHydrator.hydrate_tactics(formation_name, coaching_style)
    xi = [
        LineupSlotConfig(
            slot_index=idx,
            player_id=base_player_id + idx,
            shirt_number=shirt,
            base_position=pos,
            tactical_role=role,
        )
        for idx, (pos, role, shirt) in enumerate(_DEFAULT_433_SLOTS)
    ]
    bench = [
        BenchPlayerConfig(
            player_id=base_player_id + 11 + b,
            shirt_number=12 + b,
            natural_positions=["CM" if b < 2 else ("RW" if b < 4 else "CB")],
            default_role=PlayerRole.BOX_TO_BOX_MIDFIELDER if b < 2 else (PlayerRole.INVERTED_WINGER if b < 4 else PlayerRole.STOPPER_CB),
        )
        for b in range(5)
    ]
    return TeamMatchConfig(
        team_id=team_id,
        team_name=team_name,
        formation=formation_cfg,
        tactical_instructions=tactics,
        starting_xi=xi,
        bench=bench,
    )
