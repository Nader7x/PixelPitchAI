"""
Canonical V2 Data Schemas, Enums, and Spatial Normalization Utilities.

Aligns 1:1 with:
- docs/revamp/01_data_and_feature_pipeline.md (Pillar 1)
- docs/revamp/02_tactical_state_and_experimentation.md (22 canonical PlayerRole enums)
- docs/revamp/03_hybrid_event_transformer_and_models.md (10-field e_t schema & 32d/24d+8d vectors)
"""

from __future__ import annotations

import math
from enum import IntEnum
from typing import Dict, List, Literal, Optional, Tuple

from pydantic import BaseModel, Field, field_validator, model_validator

# --- Pitch Geometry Constants (StatsBomb Canonical Yard Frame) ---
PITCH_LENGTH_YD: float = 120.0
PITCH_WIDTH_YD: float = 80.0
PITCH_LENGTH_M: float = 105.0
PITCH_WIDTH_M: float = 68.0
YD_TO_M_X: float = PITCH_LENGTH_M / PITCH_LENGTH_YD  # 0.875 m/yd
YD_TO_M_Y: float = PITCH_WIDTH_M / PITCH_WIDTH_YD    # 0.850 m/yd

GOAL_MOUTH_Y_MIN: float = 36.0
GOAL_MOUTH_Y_MAX: float = 44.0
GOAL_CENTER_Y: float = 40.0
CROSSBAR_HEIGHT_M: float = 2.44
MAX_SHOT_Z_M: float = 5.0
END_Z_BINS: int = 32

# Discretized Pitch Grids
CELL_GRID_COLS: int = 30  # 4.0 yd per cell along X
CELL_GRID_ROWS: int = 20  # 4.0 yd per cell along Y
NUM_PITCH_CELLS: int = CELL_GRID_COLS * CELL_GRID_ROWS  # 600
CELL_OFF_PITCH: int = 600
CELL_NONE: int = 601

XT_GRID_COLS: int = 24  # 5.0 yd per cell along X
XT_GRID_ROWS: int = 16  # 5.0 yd per cell along Y

# Log-spaced time delta buckets (64 bins from 0s to 120s+)
DT_MAX_SEC: float = 120.0
DT_BUCKETS: int = 64

# 8-bit modifier bitmask flags (Module C MODIFIERS field)
MODIFIER_BIT_MASKS: Dict[str, int] = {
    "under_pressure": 1 << 0,
    "through_ball": 1 << 1,
    "pass_switch": 1 << 2,
    "cross": 1 << 3,
    "cutback": 1 << 4,
    "first_time": 1 << 5,
    "one_touch": 1 << 6,
    "counter_attack": 1 << 7,
}


class MatchPeriod(IntEnum):
    PRE = 0
    FIRST_HALF = 1
    HALFTIME = 2
    SECOND_HALF = 3
    EXTRA_TIME_1 = 4
    EXTRA_TIME_2 = 5
    FULL_TIME = 6


class PossPhase(IntEnum):
    """8-state possession phase taxonomy aligned 1:1 with Module C (`api/v2/simulator/fsm.py`)."""

    OPEN_BUILDUP = 0
    OPEN_PROGRESSION = 1
    OPEN_FINAL_THIRD = 2
    TRANSITION_FAST = 3
    TRANSITION_COUNTERPRESS = 4
    SET_PIECE_ATTACK = 5
    SET_PIECE_DEFENSE = 6
    DEAD_BALL = 7


class PlayerRole(IntEnum):
    """22 canonical tactical roles aligned 1:1 with Module A (02_tactical_state_and_experimentation.md)."""

    SWEEPER_KEEPER = 0
    SHOT_STOPPER_GK = 1
    BALL_PLAYING_CB = 2
    STOPPER_CB = 3
    WIDE_CENTER_BACK = 4
    INVERTED_FULLBACK = 5
    OVERLAPPING_FULLBACK = 6
    DEFENSIVE_FULLBACK = 7
    COMPLETE_WING_BACK = 8
    DEEP_LYING_PLAYMAKER = 9
    DESTROYER_DM = 10
    BOX_TO_BOX_MIDFIELDER = 11
    MEZZALA = 12
    CARRILERO = 13
    ADVANCED_PLAYMAKER = 14
    INVERTED_WINGER = 15
    TOUCHLINE_WINGER = 16
    INSIDE_FORWARD = 17
    FALSE_9 = 18
    PRESSING_FORWARD = 19
    TARGET_FORWARD = 20
    POACHER = 21


PLAYER_ROLE_NAMES: Tuple[str, ...] = (
    "SweeperKeeper",
    "ShotStopperGK",
    "BallPlayingCB",
    "StopperCB",
    "WideCenterBack",
    "InvertedFullback",
    "OverlappingFullback",
    "DefensiveFullback",
    "CompleteWingBack",
    "DeepLyingPlaymaker",
    "DestroyerDM",
    "BoxToBoxMidfielder",
    "Mezzala",
    "Carrilero",
    "AdvancedPlaymaker",
    "InvertedWinger",
    "TouchlineWinger",
    "InsideForward",
    "False9",
    "PressingForward",
    "TargetForward",
    "Poacher",
)


class ActionType(IntEnum):
    PASS = 0
    CARRY = 1
    DRIBBLE = 2
    SHOT = 3
    CLEARANCE = 4
    INTERCEPTION = 5
    TACKLE_DUEL = 6
    BALL_RECOVERY = 7
    SAVE = 8
    BLOCK = 9
    PRESSURE = 10
    FOUL_COMMITTED = 11
    MISCONTROL = 12
    DISPOSSESSED = 13
    OFFSIDE = 14
    CARD = 15
    SUBSTITUTION = 16
    PERIOD_BOUNDARY = 17


class EventOutcome(IntEnum):
    """Unified 24-token outcome taxonomy aligned 1:1 with Module C (`api/v2/simulator/fsm.py`)."""

    COMPLETE = 0
    INCOMPLETE = 1
    OUT_TOUCHLINE = 2
    OUT_BYLINE = 3
    OFFSIDE = 4
    GOAL = 5
    SAVED_HELD = 6
    SAVED_PARRY = 7
    BLOCKED = 8
    OFF_TARGET = 9
    POST = 10
    WON = 11
    LOST = 12
    YELLOW_CARD = 13
    SECOND_YELLOW = 14
    RED_CARD = 15
    FOUL_CONCEDED = 16
    PENALTY_WON = 17
    SUB_EXECUTED = 18
    PERIOD_END = 19
    NEUTRAL = 20
    NONE = 20
    OWN_GOAL = 21
    RETAIN = 22
    FOUL_WON = 23


class RestartType(IntEnum):
    OPEN_PLAY = 0
    KICK_OFF = 1
    THROW_IN = 2
    CORNER = 3
    FREE_KICK_DIRECT = 4
    FREE_KICK_INDIRECT = 5
    GOAL_KICK = 6
    PENALTY = 7


class PassHeight(IntEnum):
    NA = 0
    GROUND = 1
    LOW = 2
    HIGH = 3


class BodyPart(IntEnum):
    RIGHT_FOOT = 0
    LEFT_FOOT = 1
    HEAD = 2
    KEEPER_HANDS = 3
    OTHER = 4


ATTRIBUTES_24D_NAMES: Tuple[str, ...] = (
    "short_pass_accuracy",
    "progressive_pass_volume",
    "long_switch_propensity",
    "through_ball_vision",
    "cross_frequency",
    "press_resistance",
    "dribble_takeon_volume",
    "dribble_success_skill",
    "ball_carry_progression",
    "first_touch_retention",
    "shot_volume_box",
    "shot_volume_long",
    "finishing_skill_psxg_delta",
    "aerial_box_threat",
    "weak_foot_balance",
    "pressing_workrate",
    "tackle_duel_win_rate",
    "interception_reading",
    "aerial_duel_win_rate",
    "foul_proneness",
    "recovery_speed_proxy",
    "stamina_endurance",
    "sprint_recovery_capacity",
    "set_piece_delivery",
)

GK_ATTRIBUTES_8D_NAMES: Tuple[str, ...] = (
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


# --- Coordinate & Quantization Helpers ---

def to_canonical_global(x_raw: float, y_raw: float, is_home: bool) -> Tuple[float, float]:
    """
    Convert acting-team-oriented StatsBomb [0, 120] x [0, 80] coordinates into
    Canonical Global Pitch Frame (Home attacks +x toward 120, Away attacks -x toward 0).
    """
    if is_home:
        return float(x_raw), float(y_raw)
    return float(PITCH_LENGTH_YD - x_raw), float(PITCH_WIDTH_YD - y_raw)


def to_attacking_relative(x_canon: float, y_canon: float, is_home: bool) -> Tuple[float, float]:
    """Convert Canonical Global Pitch coordinates into acting-team attacking-relative (+x -> 120)."""
    if is_home:
        return float(x_canon), float(y_canon)
    return float(PITCH_LENGTH_YD - x_canon), float(PITCH_WIDTH_YD - y_canon)


def rescale_wyscout(x_pct: float, y_pct: float) -> Tuple[float, float]:
    """Convert Wyscout [0, 100] x [0, 100] percentage coordinates into StatsBomb [0, 120] x [0, 80]."""
    return float(x_pct) * (PITCH_LENGTH_YD / 100.0), float(y_pct) * (PITCH_WIDTH_YD / 100.0)


def rescale_opta(x_pct: float, y_pct: float) -> Tuple[float, float]:
    """Convert Opta [0, 100] x [0, 100] (y=0 bottom) into StatsBomb [0, 120] x [0, 80] (y=0 top)."""
    return float(x_pct) * (PITCH_LENGTH_YD / 100.0), (100.0 - float(y_pct)) * (PITCH_WIDTH_YD / 100.0)


def rescale_skillcorner(
    x_m: float,
    y_m: float,
    pitch_length_m: float = PITCH_LENGTH_M,
    pitch_width_m: float = PITCH_WIDTH_M,
) -> Tuple[float, float]:
    """Convert SkillCorner metric center-origin coordinates into StatsBomb [0, 120] x [0, 80]."""
    x_sb = (float(x_m) + pitch_length_m / 2.0) * (PITCH_LENGTH_YD / pitch_length_m)
    y_sb = (pitch_width_m / 2.0 - float(y_m)) * (PITCH_WIDTH_YD / pitch_width_m)
    return x_sb, y_sb


def quantize_pitch_cell(x_canon: Optional[float], y_canon: Optional[float]) -> Tuple[int, float, float]:
    """
    Discretize canonical coordinates into a 30x20 grid (600 4x4-yd cells + 2 special tokens)
    plus continuous sub-cell residuals (dx, dy) in [-2.0, 2.0].
    """
    if x_canon is None or y_canon is None or math.isnan(x_canon) or math.isnan(y_canon):
        return CELL_NONE, 0.0, 0.0
    if x_canon < -0.5 or x_canon > PITCH_LENGTH_YD + 0.5 or y_canon < -0.5 or y_canon > PITCH_WIDTH_YD + 0.5:
        return CELL_OFF_PITCH, 0.0, 0.0

    xc = min(max(float(x_canon), 0.0), PITCH_LENGTH_YD - 1e-6)
    yc = min(max(float(y_canon), 0.0), PITCH_WIDTH_YD - 1e-6)
    cell_w = PITCH_LENGTH_YD / CELL_GRID_COLS  # 4.0
    cell_h = PITCH_WIDTH_YD / CELL_GRID_ROWS   # 4.0
    col = min(int(xc // cell_w), CELL_GRID_COLS - 1)
    row = min(int(yc // cell_h), CELL_GRID_ROWS - 1)
    cell_id = row * CELL_GRID_COLS + col
    center_x = (col + 0.5) * cell_w
    center_y = (row + 0.5) * cell_h
    return cell_id, float(xc - center_x), float(yc - center_y)


def _build_dt_centers() -> Tuple[float, ...]:
    """Match `api/v2/simulator/fsm.py` logarithmic centers: 0.0s, then exp(linspace(log(0.2), log(120.0), 63))."""
    lo = math.log(0.2)
    hi = math.log(DT_MAX_SEC)
    centers = [0.0]
    n_pos = DT_BUCKETS - 1
    for i in range(n_pos):
        frac = i / (n_pos - 1)
        centers.append(math.exp(lo + frac * (hi - lo)))
    return tuple(centers)


_DT_CENTERS: Tuple[float, ...] = _build_dt_centers()


def quantize_dt_bucket(dt_sec: float) -> Tuple[int, float]:
    """Log-spaced discretization of event time delta [0, 120s+] aligned 1:1 with Module C `fsm.py`."""
    dt = max(0.0, float(dt_sec))
    if dt <= 0.0:
        return 0, 0.0
    dt_clamped = min(dt, DT_MAX_SEC)
    best_idx = 0
    best_dist = abs(dt_clamped - _DT_CENTERS[0])
    for idx in range(1, DT_BUCKETS):
        d = abs(dt_clamped - _DT_CENTERS[idx])
        if d < best_dist:
            best_dist = d
            best_idx = idx
    return best_idx, float(dt - _DT_CENTERS[best_idx])


def quantize_end_z(end_z_m: Optional[float]) -> int:
    """Discretize shot end z-coordinate [0.0, 5.0m] into 32 bins, or -1 if N/A."""
    if end_z_m is None or math.isnan(end_z_m):
        return -1
    zc = min(max(float(end_z_m), 0.0), MAX_SHOT_Z_M - 1e-6)
    return min(int((zc / MAX_SHOT_Z_M) * END_Z_BINS), END_Z_BINS - 1)


def pack_modifier_bitmask(
    under_pressure: bool = False,
    through_ball: bool = False,
    pass_switch: bool = False,
    cross: bool = False,
    cutback: bool = False,
    first_time: bool = False,
    one_touch: bool = False,
    counter_attack: bool = False,
) -> int:
    mask = 0
    if under_pressure:
        mask |= MODIFIER_BIT_MASKS["under_pressure"]
    if through_ball:
        mask |= MODIFIER_BIT_MASKS["through_ball"]
    if pass_switch:
        mask |= MODIFIER_BIT_MASKS["pass_switch"]
    if cross:
        mask |= MODIFIER_BIT_MASKS["cross"]
    if cutback:
        mask |= MODIFIER_BIT_MASKS["cutback"]
    if first_time:
        mask |= MODIFIER_BIT_MASKS["first_time"]
    if one_touch:
        mask |= MODIFIER_BIT_MASKS["one_touch"]
    if counter_attack:
        mask |= MODIFIER_BIT_MASKS["counter_attack"]
    return mask


# --- Pydantic V2 Gold Data Models ---

class FreezeFramePlayer(BaseModel):
    teammate: bool
    actor: bool = False
    keeper: bool = False
    x_canon: float
    y_canon: float


class ShotFreezeFrameEntry(BaseModel):
    player_id: Optional[int] = None
    slot: Optional[int] = None
    x_canon: float
    y_canon: float
    teammate: bool
    is_gk: bool = False


class EventGoldV2(BaseModel):
    """Unified Gold event record matching 01_data_and_feature_pipeline.md §3.3 & Module C/D."""

    # Keys & Clock
    match_id: int
    event_seq_idx: int = Field(..., ge=0)
    event_uuid: str
    provider_source: Literal["statsbomb", "wyscout", "opta", "skillcorner", "scraped_event"] = "statsbomb"
    period: MatchPeriod
    minute: int = Field(..., ge=0)
    second: int = Field(..., ge=0, le=59)
    timestamp_ms: int = Field(..., ge=0)
    dt_sec: float = Field(0.0, ge=0.0)
    dt_bucket_id: int = Field(0, ge=0, le=63)
    dt_res: float = 0.0

    # Possession & Score
    possession_id: int = Field(0, ge=0)
    possession_team_id: int = -1
    poss_phase: PossPhase = PossPhase.DEAD_BALL
    score_home: int = Field(0, ge=0)
    score_away: int = Field(0, ge=0)

    # 22-Player Roster
    actor_slot: int = Field(0, ge=0, le=22)
    actor_player_id: int = -1
    actor_team_id: int = -1
    is_home_actor: bool = True
    target_slot: int = Field(0, ge=0, le=22)
    target_player_id: int = -1
    active_slots_player_ids: List[int] = Field(..., min_length=22, max_length=22)
    active_slots_roles: List[int] = Field(..., min_length=22, max_length=22)
    active_slots_sent_off: int = Field(0, ge=0)
    home_formation_id: int = 0
    away_formation_id: int = 0

    # Action & Outcome
    action_type: ActionType
    outcome: EventOutcome = EventOutcome.NONE

    # Spatial Coordinates
    x_canon: float = Field(..., ge=-5.0, le=125.0)
    y_canon: float = Field(..., ge=-5.0, le=85.0)
    end_x_canon: float = Field(..., ge=-5.0, le=125.0)
    end_y_canon: float = Field(..., ge=-5.0, le=85.0)
    end_z_m: float = Field(0.0, ge=0.0, le=5.0)
    x_att: float
    y_att: float
    end_x_att: float
    end_y_att: float
    start_cell_id: int = Field(..., ge=0, le=601)
    end_cell_id: int = Field(..., ge=0, le=601)
    dx_res_start: float = 0.0
    dy_res_start: float = 0.0
    dx_res_end: float = 0.0
    dy_res_end: float = 0.0
    end_z_bin: int = Field(-1, ge=-1, le=31)
    distance_yd: float = Field(0.0, ge=0.0)
    angle_rad: float = 0.0

    # Modifiers & Tactics
    restart_type: RestartType = RestartType.OPEN_PLAY
    pass_height: PassHeight = PassHeight.NA
    body_part: BodyPart = BodyPart.RIGHT_FOOT
    under_pressure: bool = False
    pass_switch: bool = False
    through_ball: bool = False
    cross: bool = False
    cutback: bool = False
    first_time: bool = False
    one_time_flag_placeholder: Optional[bool] = Field(None, exclude=True)
    one_on_one: bool = False
    one_touch: bool = False
    counter_attack: bool = False
    aerial_won: bool = False
    nutmeg: bool = False
    overrun: bool = False
    modifier_bitmask_8: int = Field(0, ge=0, le=255)

    # 360 Freeze-Frames & Geometry
    has_360_frame: bool = False
    has_shot_freeze_frame: bool = False
    shot_freeze_frame: List[ShotFreezeFrameEntry] = Field(default_factory=list)
    freeze_frame_360: List[FreezeFramePlayer] = Field(default_factory=list)
    visible_area_poly: List[float] = Field(default_factory=list)
    ff_n_defenders_in_shot_triangle: int = Field(0, ge=0, le=11)
    ff_open_goal_fraction: float = Field(1.0, ge=0.0, le=1.0)
    ff_gk_dist_goal: float = Field(0.0, ge=0.0)
    ff_gk_angle_offset: float = 0.0
    ff_closest_defender_dist: float = Field(30.0, ge=0.0)
    ff_n_pressers_3m: int = Field(0, ge=0, le=11)
    ff_min_defender_dist_to_pass_ray: float = Field(30.0, ge=0.0)
    ff_n_defenders_in_pass_corridor: int = Field(0, ge=0, le=11)
    ff_receiver_voronoi_area: float = Field(0.0, ge=0.0)
    ff_actor_packing_bypassed: int = Field(0, ge=0, le=11)

    # Derived Analytics Labels (xG, PSxG, xPass, xT, VAEP)
    xg_statsbomb: Optional[float] = Field(None, ge=0.0, le=1.0)
    xg_v2_pre_shot: float = Field(0.0, ge=0.0, le=1.0)
    psxg_v2: float = Field(0.0, ge=0.0, le=1.0)
    xpass_prob: float = Field(0.0, ge=0.0, le=1.0)
    xpass_progressive_value: float = 0.0
    xt_start: float = 0.0
    xt_end: float = 0.0
    xt_delta: float = 0.0
    vaep_p_score_before: float = Field(0.0, ge=0.0, le=1.0)
    vaep_p_score_after: float = Field(0.0, ge=0.0, le=1.0)
    vaep_p_concede_before: float = Field(0.0, ge=0.0, le=1.0)
    vaep_p_concede_after: float = Field(0.0, ge=0.0, le=1.0)
    vaep_value: float = 0.0


class PlayerProfileV2(BaseModel):
    """Historical + 2022-2026 fused Player Capability & Role Profiler record."""

    player_id: int
    season_id: str  # e.g., "2020/2021" or "career_latest"
    player_name: str = ""
    team_id: Optional[int] = None
    as_of_date: Optional[str] = None
    minutes_played: float = Field(0.0, ge=0.0)
    primary_position: str = "CM"
    preferred_foot: Literal["Right", "Left", "Both"] = "Right"
    natural_roles: List[PlayerRole] = Field(default_factory=list)
    role_suitability_scores: List[float] = Field(..., min_length=22, max_length=22)

    attributes_24d: List[float] = Field(..., min_length=24, max_length=24)
    sigma_24d: List[float] = Field(..., min_length=24, max_length=24)
    gk_attributes_8d: List[float] = Field(..., min_length=8, max_length=8)
    finishing_log_odds: float = Field(0.0, ge=-1.0, le=1.0)
    latent_style_64d: List[float] = Field(..., min_length=64, max_length=64)

    # Behavioral style vectors (24x16 = 384 cells)
    touch_heatmap_24x16: List[float] = Field(default_factory=lambda: [1.0 / 384.0] * 384)
    pressing_zone_heatmap_24x16: List[float] = Field(default_factory=lambda: [1.0 / 384.0] * 384)
    pass_vector_summary_16d: List[float] = Field(default_factory=lambda: [0.0] * 16)
    carry_tendency_8d: List[float] = Field(default_factory=lambda: [0.0] * 8)
    shot_tendency_8d: List[float] = Field(default_factory=lambda: [0.0] * 8)

    source_provenance_flags: int = Field(0, ge=0)

    @field_validator("attributes_24d", "gk_attributes_8d", "role_suitability_scores")
    @classmethod
    def check_unit_range(cls, vals: List[float]) -> List[float]:
        return [min(max(float(v), 0.0), 1.0) for v in vals]

    @property
    def player_capability_vec_32d(self) -> List[float]:
        return list(self.attributes_24d) + list(self.gk_attributes_8d)

    @property
    def active_gk_attributes_6d(self) -> List[float]:
        return list(self.gk_attributes_8d[:6])


class PlayerIdCrosswalk(BaseModel):
    """Unified multi-source player entity crosswalk."""

    pixelpitch_player_id: int = Field(..., ge=1)
    statsbomb_id: Optional[int] = None
    wyscout_id: Optional[int] = None
    fbref_hex_id: Optional[str] = None
    understat_id: Optional[int] = None
    sofascore_id: Optional[int] = None
    fotmob_id: Optional[int] = None
    whoscored_id: Optional[int] = None
    transfermarkt_id: Optional[int] = None
    eafc_id: Optional[int] = None
    full_name_norm: str
    birth_year: Optional[int] = None
    nationality_iso: Optional[str] = None
    current_team_name: Optional[str] = None
    position_group: Optional[str] = None
    match_confidence: float = Field(1.0, ge=0.0, le=1.0)
    resolution_method: Literal[
        "exact_id_table",
        "deterministic_name_dob_club",
        "fuzzy_bipartite_hungarian",
        "manual_override",
    ] = "exact_id_table"


class TeamMatchTactics(BaseModel):
    """Observed 10-slider tactical descriptor vector extracted per team-half."""

    match_id: int
    team_id: int
    half: int = Field(1, ge=1, le=2)
    squad_elo: float = 1500.0
    opponent_elo: float = 1500.0
    defensive_line_height: float = Field(0.55, ge=0.0, le=1.0)
    pressing_intensity: float = Field(0.55, ge=0.0, le=1.0)
    build_up_tempo: float = Field(0.50, ge=0.0, le=1.0)
    directness: float = Field(0.45, ge=0.0, le=1.0)
    attacking_width: float = Field(0.55, ge=0.0, le=1.0)
    defensive_compactness: float = Field(0.55, ge=0.0, le=1.0)
    counter_press_vs_regroup: float = Field(0.55, ge=0.0, le=1.0)
    counter_attack_urgency: float = Field(0.50, ge=0.0, le=1.0)
    possession_risk_appetite: float = Field(0.50, ge=0.0, le=1.0)
    game_state_adaptivity: float = Field(0.50, ge=0.0, le=1.0)

    def as_vector_10d(self) -> List[float]:
        return [
            self.defensive_line_height,
            self.pressing_intensity,
            self.build_up_tempo,
            self.directness,
            self.attacking_width,
            self.defensive_compactness,
            self.counter_press_vs_regroup,
            self.counter_attack_urgency,
            self.possession_risk_appetite,
            self.game_state_adaptivity,
        ]
