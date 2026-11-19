"""
Vectorized ETL, Event Folding, Active 22-Slot Tracking, and 360 Cone Geometry Engine.

Supports:
- DuckDB + PyArrow Zstd-Parquet fast path when `duckdb` / `pyarrow` are installed.
- Pure NumPy/Pandas/SQLite3 vectorized fallback path guaranteed to work in standard `uv.lock` envs.
"""

from __future__ import annotations

import json
import math
import sqlite3
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import numpy as np
import pandas as pd

from statsbombapi_v2.schemas import (
    ActionType,
    BodyPart,
    EventGoldV2,
    EventOutcome,
    FreezeFramePlayer,
    GOAL_CENTER_Y,
    GOAL_MOUTH_Y_MAX,
    GOAL_MOUTH_Y_MIN,
    MatchPeriod,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PassHeight,
    PlayerRole,
    PossPhase,
    RestartType,
    ShotFreezeFrameEntry,
    pack_modifier_bitmask,
    quantize_dt_bucket,
    quantize_end_z,
    quantize_pitch_cell,
    rescale_wyscout,
    to_attacking_relative,
    to_canonical_global,
)

try:
    import duckdb  # type: ignore

    DUCKDB_AVAILABLE = True
except ImportError:
    duckdb = None  # type: ignore
    DUCKDB_AVAILABLE = False

try:
    import pyarrow  # type: ignore
    import pyarrow.parquet as pq  # type: ignore

    PYARROW_AVAILABLE = True
except ImportError:
    pyarrow = None  # type: ignore
    pq = None  # type: ignore
    PYARROW_AVAILABLE = False


# Mapping from StatsBomb position IDs (1..25) to canonical 22 PlayerRole enums (0..21)
STATSBOMB_POSITION_TO_ROLE: Dict[int, PlayerRole] = {
    1: PlayerRole.SHOT_STOPPER_GK,        # Goalkeeper
    2: PlayerRole.OVERLAPPING_FULLBACK,   # Right Back
    3: PlayerRole.BALL_PLAYING_CB,        # Right Center Back
    4: PlayerRole.STOPPER_CB,             # Center Back
    5: PlayerRole.BALL_PLAYING_CB,        # Left Center Back
    6: PlayerRole.OVERLAPPING_FULLBACK,   # Left Back
    7: PlayerRole.COMPLETE_WING_BACK,     # Right Wing Back
    8: PlayerRole.COMPLETE_WING_BACK,     # Left Wing Back
    9: PlayerRole.DESTROYER_DM,           # Right Defensive Midfield
    10: PlayerRole.DEEP_LYING_PLAYMAKER,  # Center Defensive Midfield
    11: PlayerRole.DESTROYER_DM,          # Left Defensive Midfield
    12: PlayerRole.TOUCHLINE_WINGER,      # Right Midfield
    13: PlayerRole.MEZZALA,               # Right Center Midfield
    14: PlayerRole.BOX_TO_BOX_MIDFIELDER, # Center Midfield
    15: PlayerRole.MEZZALA,               # Left Center Midfield
    16: PlayerRole.TOUCHLINE_WINGER,      # Left Midfield
    17: PlayerRole.INVERTED_WINGER,       # Right Wing
    18: PlayerRole.CARRILERO,             # Right Attacking Midfield
    19: PlayerRole.ADVANCED_PLAYMAKER,    # Center Attacking Midfield
    20: PlayerRole.CARRILERO,             # Left Attacking Midfield
    21: PlayerRole.INVERTED_WINGER,       # Left Wing
    22: PlayerRole.INSIDE_FORWARD,        # Right Center Forward
    23: PlayerRole.POACHER,               # Striker
    24: PlayerRole.PRESSING_FORWARD,      # Center Forward
    25: PlayerRole.INSIDE_FORWARD,        # Left Center Forward
}

FORMATION_TO_ID: Dict[str, int] = {
    "442": 1,
    "433": 2,
    "4231": 3,
    "352": 4,
    "343": 5,
    "4141": 6,
    "532": 7,
    "541": 8,
    "4411": 9,
    "41212": 10,
    "3421": 11,
    "3412": 12,
    "4321": 13,
    "4222": 14,
    "3142": 15,
    "3241": 16,
}


def _safe_name(obj: Any, default: str = "") -> str:
    if isinstance(obj, dict):
        return str(obj.get("name", default) or default)
    if obj is None:
        return default
    return str(obj)


def _safe_id(obj: Any, default: int = -1) -> int:
    if isinstance(obj, dict):
        val = obj.get("id")
        return int(val) if val is not None else default
    if obj is None:
        return default
    try:
        return int(obj)
    except (TypeError, ValueError):
        return default


def _parse_timestamp_ms(timestamp_str: Optional[str], period: int, minute: int, second: int) -> int:
    """Convert StatsBomb period clock into monotonic match milliseconds."""
    period_offsets_ms = {
        1: 0,
        2: 45 * 60 * 1000,
        3: 90 * 60 * 1000,
        4: 105 * 60 * 1000,
        5: 120 * 60 * 1000,
    }
    base_offset = period_offsets_ms.get(period, 0)
    if timestamp_str and ":" in str(timestamp_str):
        try:
            parts = str(timestamp_str).split(":")
            hh = int(parts[0])
            mm = int(parts[1])
            ss = float(parts[2])
            return base_offset + int(round((hh * 3600 + mm * 60 + ss) * 1000))
        except (ValueError, IndexError):
            pass
    # Fallback using minute & second
    rel_min = max(0, minute - {1: 0, 2: 45, 3: 90, 4: 105}.get(period, 0))
    return base_offset + (rel_min * 60 + second) * 1000


def map_sb_period(period_int: int) -> MatchPeriod:
    mapping = {
        1: MatchPeriod.FIRST_HALF,
        2: MatchPeriod.SECOND_HALF,
        3: MatchPeriod.EXTRA_TIME_1,
        4: MatchPeriod.EXTRA_TIME_2,
    }
    return mapping.get(int(period_int), MatchPeriod.FIRST_HALF)


def map_sb_restart(play_pattern_name: str, sub_type_name: str = "") -> RestartType:
    st = sub_type_name.lower()
    if "kick off" in st:
        return RestartType.KICK_OFF
    if "throw-in" in st or "throw in" in st:
        return RestartType.THROW_IN
    if "corner" in st:
        return RestartType.CORNER
    if "goal kick" in st:
        return RestartType.GOAL_KICK
    if "penalty" in st:
        return RestartType.PENALTY
    if "free kick" in st:
        return RestartType.FREE_KICK_DIRECT

    pp = play_pattern_name.lower()
    if "throw" in pp:
        return RestartType.THROW_IN
    if "corner" in pp:
        return RestartType.CORNER
    if "free kick" in pp:
        return RestartType.FREE_KICK_DIRECT
    if "goal kick" in pp:
        return RestartType.GOAL_KICK
    if "kick off" in pp:
        return RestartType.KICK_OFF
    return RestartType.OPEN_PLAY


def map_sb_pass_height(height_name: str) -> PassHeight:
    h = height_name.lower()
    if "ground" in h:
        return PassHeight.GROUND
    if "low" in h:
        return PassHeight.LOW
    if "high" in h:
        return PassHeight.HIGH
    return PassHeight.NA


def map_sb_body_part(bp_name: str) -> BodyPart:
    b = bp_name.lower()
    if "right" in b:
        return BodyPart.RIGHT_FOOT
    if "left" in b:
        return BodyPart.LEFT_FOOT
    if "head" in b:
        return BodyPart.HEAD
    if "keeper" in b or "hand" in b:
        return BodyPart.KEEPER_HANDS
    return BodyPart.OTHER


class PitchSlotTracker:
    """
    Deterministic 22-player pitch slot state machine (`slot 1..11` Home, `slot 12..22` Away).
    Tracks Starting XI initialization, Tactical Shifts, Substitutions, and Red/Second-Yellow cards.
    """

    def __init__(self, home_team_id: int, away_team_id: int) -> None:
        self.home_team_id = int(home_team_id)
        self.away_team_id = int(away_team_id)
        self.slots_player_ids: List[int] = [-1] * 22
        self.slots_roles: List[int] = [int(PlayerRole.BOX_TO_BOX_MIDFIELDER)] * 22
        self.sent_off_mask: int = 0
        self.sent_off_player_ids: set[int] = set()
        self.home_formation_id: int = 0
        self.away_formation_id: int = 0
        self.player_to_slot: Dict[int, int] = {}

    def _recompute_sent_off_mask(self) -> None:
        mask = 0
        for idx, pid in enumerate(self.slots_player_ids):
            if pid > 0 and pid in self.sent_off_player_ids:
                mask |= 1 << idx
        self.sent_off_mask = mask

    def apply_tactics(self, team_id: int, tactics: Dict[str, Any]) -> None:
        if not tactics:
            return
        is_home = int(team_id) == self.home_team_id
        offset = 0 if is_home else 11
        formation_str = str(tactics.get("formation", "")).replace("-", "").strip()
        formation_id = FORMATION_TO_ID.get(formation_str, 0)
        if is_home:
            self.home_formation_id = formation_id
        else:
            self.away_formation_id = formation_id

        lineup = tactics.get("lineup") or []
        # Deterministic ordering by StatsBomb position ID (GK=1 first -> defenders -> midfielders -> forwards)
        sorted_lineup = sorted(lineup, key=lambda entry: _safe_id(entry.get("position"), 99))
        team_initialized = any(pid > 0 for pid in self.slots_player_ids[offset : offset + 11])

        if not team_initialized:
            for idx, entry in enumerate(sorted_lineup[:11]):
                slot_zero_idx = offset + idx
                slot_1_22 = slot_zero_idx + 1
                pid = _safe_id(entry.get("player"), -1)
                pos_id = _safe_id(entry.get("position"), 14)
                role = STATSBOMB_POSITION_TO_ROLE.get(pos_id, PlayerRole.BOX_TO_BOX_MIDFIELDER)
                if idx == 0 and role not in (PlayerRole.SWEEPER_KEEPER, PlayerRole.SHOT_STOPPER_GK):
                    role = PlayerRole.SHOT_STOPPER_GK
                self.slots_player_ids[slot_zero_idx] = pid
                self.slots_roles[slot_zero_idx] = int(role)
                if pid > 0:
                    self.player_to_slot[pid] = slot_1_22
        else:
            # Tactical Shift: preserve existing slot assignments (especially sent-off slots) and only update roles
            for entry in sorted_lineup[:11]:
                pid = _safe_id(entry.get("player"), -1)
                if pid <= 0:
                    continue
                pos_id = _safe_id(entry.get("position"), 14)
                role = STATSBOMB_POSITION_TO_ROLE.get(pos_id, PlayerRole.BOX_TO_BOX_MIDFIELDER)
                existing_slot = self.player_to_slot.get(pid)
                if existing_slot is not None and offset + 1 <= existing_slot <= offset + 11:
                    self.slots_roles[existing_slot - 1] = int(role)
                else:
                    for cand_zero in range(offset, offset + 11):
                        if self.slots_player_ids[cand_zero] == -1:
                            self.slots_player_ids[cand_zero] = pid
                            self.slots_roles[cand_zero] = int(role)
                            self.player_to_slot[pid] = cand_zero + 1
                            break
        self._recompute_sent_off_mask()

    def apply_substitution(self, team_id: int, out_player_id: int, in_player_id: int) -> int:
        """Swap outgoing player in-place with incoming substitute, returning assigned slot `1..22`."""
        slot = self.player_to_slot.get(int(out_player_id))
        if slot is None:
            is_home = int(team_id) == self.home_team_id
            start, end = (1, 12) if is_home else (12, 23)
            for candidate in range(start, end):
                if self.slots_player_ids[candidate - 1] == -1:
                    slot = candidate
                    break
        if slot is None:
            return 0
        self.player_to_slot.pop(int(out_player_id), None)
        self.slots_player_ids[slot - 1] = int(in_player_id)
        if int(in_player_id) > 0:
            self.player_to_slot[int(in_player_id)] = slot
        self._recompute_sent_off_mask()
        return slot

    def apply_red_card(self, player_id: int) -> None:
        if int(player_id) > 0:
            self.sent_off_player_ids.add(int(player_id))
        self._recompute_sent_off_mask()

    def resolve_slot(self, player_id: int, team_id: int, position_id: int = 14) -> int:
        """Resolve `1..22` slot for an actor; lazily populates missing slot if Starting XI was omitted."""
        if player_id <= 0:
            return 0
        if player_id in self.player_to_slot:
            return self.player_to_slot[player_id]
        is_home = int(team_id) == self.home_team_id
        start, end = (0, 11) if is_home else (11, 22)
        for i in range(start, end):
            if self.slots_player_ids[i] == -1:
                slot_1_22 = i + 1
                self.slots_player_ids[i] = int(player_id)
                self.slots_roles[i] = int(STATSBOMB_POSITION_TO_ROLE.get(position_id, PlayerRole.BOX_TO_BOX_MIDFIELDER))
                self.player_to_slot[int(player_id)] = slot_1_22
                return slot_1_22
        return 0

    def snapshot(self) -> Tuple[List[int], List[int], int, int, int]:
        return (
            list(self.slots_player_ids),
            list(self.slots_roles),
            int(self.sent_off_mask),
            int(self.home_formation_id),
            int(self.away_formation_id),
        )


def _point_in_triangle(
    px: np.ndarray,
    py: np.ndarray,
    ax: float,
    ay: float,
    bx: float,
    by: float,
    cx: float,
    cy: float,
) -> np.ndarray:
    """Vectorized barycentric / cross-product test for points inside triangle ABC."""
    v0x, v0y = cx - ax, cy - ay
    v1x, v1y = bx - ax, by - ay
    v2x, v2y = px - ax, py - ay
    dot00 = v0x * v0x + v0y * v0y
    dot01 = v0x * v1x + v0y * v1y
    dot02 = v0x * v2x + v0y * v2y
    dot11 = v1x * v1x + v1y * v1y
    dot12 = v1x * v2x + v1y * v2y
    denom = dot00 * dot11 - dot01 * dot01
    if abs(denom) < 1e-12:
        return np.zeros_like(px, dtype=bool)
    inv_denom = 1.0 / denom
    u = (dot11 * dot02 - dot01 * dot12) * inv_denom
    v = (dot00 * dot12 - dot01 * dot02) * inv_denom
    return (u >= 0.0) & (v >= 0.0) & (u + v <= 1.0)


def _merge_angular_intervals(intervals: List[Tuple[float, float]], lo: float, hi: float) -> float:
    """Compute total covered width inside [lo, hi] across possibly overlapping angular intervals."""
    clipped = [(max(lo, a), min(hi, b)) for a, b in intervals if b > lo and a < hi]
    if not clipped:
        return 0.0
    clipped.sort()
    covered = 0.0
    cur_start, cur_end = clipped[0]
    for start, end in clipped[1:]:
        if start <= cur_end:
            cur_end = max(cur_end, end)
        else:
            covered += cur_end - cur_start
            cur_start, cur_end = start, end
    covered += cur_end - cur_start
    return float(covered)


def compute_freeze_frame_geometry(
    x_att: float,
    y_att: float,
    end_x_att: float,
    end_y_att: float,
    is_home_actor: bool,
    action_type: ActionType,
    freeze_players_canon: Sequence[Tuple[float, float, bool, bool]],
) -> Dict[str, Any]:
    """
    Compute exact 360 / shot freeze-frame defender & GK cone geometry.

    Args:
        x_att, y_att, end_x_att, end_y_att: Acting-team attacking-relative coordinates (+x -> 120).
        is_home_actor: Whether acting team is Home.
        action_type: Event ActionType.
        freeze_players_canon: Sequence of `(x_canon, y_canon, teammate, is_gk)` tuples.
    """
    defaults: Dict[str, Any] = {
        "ff_n_defenders_in_shot_triangle": 0,
        "ff_open_goal_fraction": 1.0,
        "ff_gk_dist_goal": 0.0,
        "ff_gk_angle_offset": 0.0,
        "ff_closest_defender_dist": 30.0,
        "ff_n_pressers_3m": 0,
        "ff_min_defender_dist_to_pass_ray": 30.0,
        "ff_n_defenders_in_pass_corridor": 0,
        "ff_receiver_voronoi_area": 0.0,
        "ff_actor_packing_bypassed": 0,
    }
    if not freeze_players_canon:
        return defaults

    # Convert all freeze-frame players into acting-team attacking-relative frame
    opp_coords: List[Tuple[float, float, bool]] = []
    all_coords_att: List[Tuple[float, float, bool]] = []
    for xc, yc, teammate, is_gk in freeze_players_canon:
        xa, ya = to_attacking_relative(xc, yc, is_home_actor)
        all_coords_att.append((xa, ya, teammate))
        if not teammate:
            opp_coords.append((xa, ya, is_gk))

    if not opp_coords:
        return defaults

    opp_arr = np.array([(x, y) for x, y, _ in opp_coords], dtype=np.float64)
    dists_to_actor = np.hypot(opp_arr[:, 0] - x_att, opp_arr[:, 1] - y_att)
    closest_def_dist = float(np.min(dists_to_actor))
    n_pressers_3m = int(np.sum(dists_to_actor <= 3.0))

    # Packing: opponents strictly between shooter/passer and goal that are bypassed horizontally
    packing_bypassed = int(
        np.sum((opp_arr[:, 0] >= x_att) & (opp_arr[:, 0] <= end_x_att))
    ) if end_x_att > x_att else 0

    # Shot Triangle & Open Goal Angle Calculation
    n_in_triangle = 0
    open_goal_fraction = 1.0
    gk_dist_goal = 0.0
    gk_angle_offset = 0.0

    # Goal posts in attacking-relative frame: (120, 36) and (120, 44)
    dx_goal = PITCH_LENGTH_YD - x_att
    if dx_goal > 0.05:
        in_tri = _point_in_triangle(
            opp_arr[:, 0],
            opp_arr[:, 1],
            x_att,
            y_att,
            PITCH_LENGTH_YD,
            GOAL_MOUTH_Y_MIN,
            PITCH_LENGTH_YD,
            GOAL_MOUTH_Y_MAX,
        )
        n_in_triangle = int(np.sum(in_tri))

        theta_post_low = math.atan2(GOAL_MOUTH_Y_MIN - y_att, dx_goal)
        theta_post_high = math.atan2(GOAL_MOUTH_Y_MAX - y_att, dx_goal)
        lo_ang, hi_ang = min(theta_post_low, theta_post_high), max(theta_post_low, theta_post_high)
        goal_span = max(hi_ang - lo_ang, 1e-6)
        center_angle = math.atan2(GOAL_CENTER_Y - y_att, dx_goal)

        cone_intervals: List[Tuple[float, float]] = []
        for ox, oy, is_gk in opp_coords:
            d_actor = math.hypot(ox - x_att, oy - y_att)
            if is_gk:
                gk_dist_goal = float(math.hypot(PITCH_LENGTH_YD - ox, GOAL_CENTER_Y - oy))
                gk_ang = math.atan2(oy - y_att, max(ox - x_att, 1e-4))
                gk_angle_offset = float(gk_ang - center_angle)
            if ox >= x_att and d_actor > 0.25:
                ang = math.atan2(oy - y_att, ox - x_att)
                radius = 0.90 if is_gk else 0.45
                half_span = math.asin(min(0.99, radius / d_actor))
                cone_intervals.append((ang - half_span, ang + half_span))

        occluded = _merge_angular_intervals(cone_intervals, lo_ang, hi_ang)
        open_goal_fraction = float(max(0.0, min(1.0, 1.0 - occluded / goal_span)))

    # Passing Corridor & Receiver Voronoi Area
    min_dist_ray = closest_def_dist
    n_in_corridor = 0
    voronoi_area = 0.0
    seg_dx = end_x_att - x_att
    seg_dy = end_y_att - y_att
    seg_len_sq = seg_dx * seg_dx + seg_dy * seg_dy
    if seg_len_sq > 0.25:
        proj = ((opp_arr[:, 0] - x_att) * seg_dx + (opp_arr[:, 1] - y_att) * seg_dy) / seg_len_sq
        proj_clamped = np.clip(proj, 0.0, 1.0)
        closest_x = x_att + proj_clamped * seg_dx
        closest_y = y_att + proj_clamped * seg_dy
        perp_dists = np.hypot(opp_arr[:, 0] - closest_x, opp_arr[:, 1] - closest_y)
        min_dist_ray = float(np.min(perp_dists))
        n_in_corridor = int(np.sum((perp_dists <= 3.0) & (proj >= 0.0) & (proj <= 1.0)))

    # Bounded pitch Voronoi area around receiver `(end_x_att, end_y_att)` using 2x2 yd coarse grid
    if action_type == ActionType.PASS and all_coords_att:
        gx = np.linspace(2.0, 118.0, 30)
        gy = np.linspace(2.0, 78.0, 20)
        grid_x, grid_y = np.meshgrid(gx, gy)
        flat_x = grid_x.ravel()[:, None]  # [600, 1]
        flat_y = grid_y.ravel()[:, None]  # [600, 1]
        # Candidate points: all freeze frame players + virtual receiver if not already present
        pts = np.array([(px, py) for px, py, _ in all_coords_att], dtype=np.float64)
        d_recv = np.hypot(pts[:, 0] - end_x_att, pts[:, 1] - end_y_att)
        if np.min(d_recv) > 1.5:
            pts = np.vstack([pts, np.array([[end_x_att, end_y_att]])])
            recv_idx = len(pts) - 1
        else:
            recv_idx = int(np.argmin(d_recv))
        d2 = (flat_x - pts[:, 0][None, :]) ** 2 + (flat_y - pts[:, 1][None, :]) ** 2
        nearest = np.argmin(d2, axis=1)
        cell_area = (PITCH_LENGTH_YD / 30.0) * (PITCH_WIDTH_YD / 20.0)  # 16 sq yd
        voronoi_area = float(np.sum(nearest == recv_idx) * cell_area)

    return {
        "ff_n_defenders_in_shot_triangle": n_in_triangle,
        "ff_open_goal_fraction": open_goal_fraction,
        "ff_gk_dist_goal": gk_dist_goal,
        "ff_gk_angle_offset": gk_angle_offset,
        "ff_closest_defender_dist": closest_def_dist,
        "ff_n_pressers_3m": n_pressers_3m,
        "ff_min_defender_dist_to_pass_ray": min_dist_ray,
        "ff_n_defenders_in_pass_corridor": n_in_corridor,
        "ff_receiver_voronoi_area": voronoi_area,
        "ff_actor_packing_bypassed": packing_bypassed,
    }


def _map_sb_event_to_action_outcome(
    raw_event: Dict[str, Any],
) -> Tuple[ActionType, EventOutcome, Dict[str, Any]]:
    """Map raw StatsBomb event dict to canonical (ActionType, EventOutcome, extracted metadata)."""
    ev_type = _safe_name(raw_event.get("type"))
    loc = raw_event.get("location") or [60.0, 40.0]
    x_raw = float(loc[0]) if len(loc) >= 1 else 60.0
    y_raw = float(loc[1]) if len(loc) >= 2 else 40.0
    end_x_raw, end_y_raw, end_z_m = x_raw, y_raw, 0.0

    restart = map_sb_restart(_safe_name(raw_event.get("play_pattern")), "")
    pass_height = PassHeight.NA
    body_part = BodyPart.RIGHT_FOOT
    under_pressure = bool(raw_event.get("under_pressure", False))
    pass_switch = False
    through_ball = False
    cross = False
    cutback = False
    first_time = False
    one_on_one = False
    counter_attack = "counter" in _safe_name(raw_event.get("play_pattern")).lower()
    aerial_won = False
    nutmeg = False
    overrun = False
    target_player_id = -1
    xg_sb: Optional[float] = None
    shot_ff: List[Dict[str, Any]] = []

    if ev_type == "Pass":
        p = raw_event.get("pass") or {}
        end_loc = p.get("end_location") or [x_raw, y_raw]
        end_x_raw = float(end_loc[0]) if len(end_loc) >= 1 else x_raw
        end_y_raw = float(end_loc[1]) if len(end_loc) >= 2 else y_raw
        pass_height = map_sb_pass_height(_safe_name(p.get("height")))
        body_part = map_sb_body_part(_safe_name(p.get("body_part")))
        sub_type = _safe_name(p.get("type"))
        if sub_type:
            restart = map_sb_restart(_safe_name(raw_event.get("play_pattern")), sub_type)
        pass_switch = bool(p.get("switch", False))
        through_ball = bool(p.get("through_ball", False))
        cross = bool(p.get("cross", False))
        cutback = bool(p.get("cut_back", False))
        aerial_won = bool(p.get("aerial_won", False))
        target_player_id = _safe_id(p.get("recipient"), -1)

        out_name = _safe_name(p.get("outcome")).lower()
        if not out_name:
            outcome = EventOutcome.COMPLETE
        elif "out" in out_name:
            if end_x_raw <= 0.2 or end_x_raw >= PITCH_LENGTH_YD - 0.2:
                outcome = EventOutcome.OUT_BYLINE
            else:
                outcome = EventOutcome.OUT_TOUCHLINE
        elif "offside" in out_name:
            outcome = EventOutcome.OFFSIDE
        else:
            outcome = EventOutcome.INCOMPLETE
        action = ActionType.PASS

    elif ev_type == "Carry":
        c = raw_event.get("carry") or {}
        end_loc = c.get("end_location") or [x_raw, y_raw]
        end_x_raw = float(end_loc[0]) if len(end_loc) >= 1 else x_raw
        end_y_raw = float(end_loc[1]) if len(end_loc) >= 2 else y_raw
        action = ActionType.CARRY
        outcome = EventOutcome.COMPLETE

    elif ev_type == "Shot":
        s = raw_event.get("shot") or {}
        end_loc = s.get("end_location") or [PITCH_LENGTH_YD, GOAL_CENTER_Y, 0.0]
        end_x_raw = float(end_loc[0]) if len(end_loc) >= 1 else PITCH_LENGTH_YD
        end_y_raw = float(end_loc[1]) if len(end_loc) >= 2 else GOAL_CENTER_Y
        end_z_m = float(end_loc[2]) if len(end_loc) >= 3 and end_loc[2] is not None else 0.0
        end_z_m = max(0.0, min(end_z_m, 5.0))
        body_part = map_sb_body_part(_safe_name(s.get("body_part")))
        sub_type = _safe_name(s.get("type"))
        if sub_type:
            restart = map_sb_restart(_safe_name(raw_event.get("play_pattern")), sub_type)
        first_time = bool(s.get("first_time", False))
        one_on_one = bool(s.get("one_on_one", False))
        xg_val = s.get("statsbomb_xg")
        xg_sb = float(xg_val) if xg_val is not None else None
        shot_ff = s.get("freeze_frame") or []

        out_name = _safe_name(s.get("outcome")).lower()
        if "goal" in out_name:
            outcome = EventOutcome.GOAL
        elif "saved" in out_name:
            outcome = EventOutcome.SAVED_HELD
        elif "blocked" in out_name:
            outcome = EventOutcome.BLOCKED
        elif "post" in out_name:
            outcome = EventOutcome.POST
        else:
            outcome = EventOutcome.OFF_TARGET
        action = ActionType.SHOT

    elif ev_type == "Dribble":
        d = raw_event.get("dribble") or {}
        nutmeg = bool(d.get("nutmeg", False))
        overrun = bool(d.get("overrun", False))
        out_name = _safe_name(d.get("outcome")).lower()
        outcome = EventOutcome.COMPLETE if "complete" in out_name and "in" not in out_name else EventOutcome.INCOMPLETE
        action = ActionType.DRIBBLE

    elif ev_type == "Clearance":
        cl = raw_event.get("clearance") or {}
        body_part = map_sb_body_part(_safe_name(cl.get("body_part")))
        aerial_won = bool(cl.get("aerial_won", False))
        action = ActionType.CLEARANCE
        outcome = EventOutcome.COMPLETE

    elif ev_type == "Interception":
        ic = raw_event.get("interception") or {}
        out_name = _safe_name(ic.get("outcome")).lower()
        outcome = EventOutcome.WON if ("won" in out_name or "success" in out_name) else EventOutcome.LOST
        action = ActionType.INTERCEPTION

    elif ev_type == "Duel":
        du = raw_event.get("duel") or {}
        out_name = _safe_name(du.get("outcome")).lower()
        outcome = EventOutcome.WON if ("won" in out_name or "success" in out_name) else EventOutcome.LOST
        action = ActionType.TACKLE_DUEL

    elif ev_type == "Ball Recovery":
        br = raw_event.get("ball_recovery") or {}
        outcome = EventOutcome.LOST if br.get("recovery_failure") else EventOutcome.WON
        action = ActionType.BALL_RECOVERY

    elif ev_type == "Goal Keeper":
        gk = raw_event.get("goalkeeper") or {}
        body_part = BodyPart.KEEPER_HANDS
        out_name = _safe_name(gk.get("outcome")).lower()
        if "collected" in out_name or "claim" in out_name or "held" in out_name:
            outcome = EventOutcome.SAVED_HELD
        elif "parried" in out_name or "saved" in out_name or "tipped" in out_name:
            outcome = EventOutcome.SAVED_PARRY
        else:
            outcome = EventOutcome.COMPLETE
        action = ActionType.SAVE

    elif ev_type == "Block":
        action = ActionType.BLOCK
        outcome = EventOutcome.BLOCKED

    elif ev_type == "Pressure":
        action = ActionType.PRESSURE
        outcome = EventOutcome.COMPLETE

    elif ev_type == "Foul Committed":
        fc = raw_event.get("foul_committed") or {}
        card_name = _safe_name(fc.get("card")).lower()
        if "second yellow" in card_name:
            outcome = EventOutcome.SECOND_YELLOW
        elif "red" in card_name:
            outcome = EventOutcome.RED_CARD
        elif "yellow" in card_name:
            outcome = EventOutcome.YELLOW_CARD
        else:
            outcome = EventOutcome.FOUL_CONCEDED
        action = ActionType.FOUL_COMMITTED

    elif ev_type == "Bad Behaviour":
        bb = raw_event.get("bad_behaviour") or {}
        card_name = _safe_name(bb.get("card")).lower()
        if "second yellow" in card_name:
            outcome = EventOutcome.SECOND_YELLOW
        elif "red" in card_name:
            outcome = EventOutcome.RED_CARD
        else:
            outcome = EventOutcome.YELLOW_CARD
        action = ActionType.CARD

    elif ev_type == "Miscontrol":
        action = ActionType.MISCONTROL
        outcome = EventOutcome.LOST

    elif ev_type == "Dispossessed":
        action = ActionType.DISPOSSESSED
        outcome = EventOutcome.LOST

    elif ev_type == "Offside":
        action = ActionType.OFFSIDE
        outcome = EventOutcome.OFFSIDE

    elif ev_type == "Substitution":
        action = ActionType.SUBSTITUTION
        outcome = EventOutcome.SUB_EXECUTED

    elif ev_type in ("Half Start", "Half End"):
        action = ActionType.PERIOD_BOUNDARY
        outcome = EventOutcome.PERIOD_END if ev_type == "Half End" else EventOutcome.NONE

    elif ev_type in ("Own Goal For", "Own Goal Against"):
        action = ActionType.SHOT
        outcome = EventOutcome.OWN_GOAL

    else:
        action = ActionType.BALL_RECOVERY
        outcome = EventOutcome.NONE

    meta = {
        "x_raw": x_raw,
        "y_raw": y_raw,
        "end_x_raw": end_x_raw,
        "end_y_raw": end_y_raw,
        "end_z_m": end_z_m,
        "restart_type": restart,
        "pass_height": pass_height,
        "body_part": body_part,
        "under_pressure": under_pressure,
        "pass_switch": pass_switch,
        "through_ball": through_ball,
        "cross": cross,
        "cutback": cutback,
        "first_time": first_time,
        "one_on_one": one_on_one,
        "counter_attack": counter_attack,
        "aerial_won": aerial_won,
        "nutmeg": nutmeg,
        "overrun": overrun,
        "target_player_id": target_player_id,
        "xg_statsbomb": xg_sb,
        "shot_freeze_frame_raw": shot_ff,
    }
    return action, outcome, meta


def ingest_statsbomb_match(
    match_meta: Dict[str, Any],
    raw_events: List[Dict[str, Any]],
    frames_360: Optional[List[Dict[str, Any]]] = None,
    fold_receipts: bool = True,
    fold_micro_carries: bool = True,
) -> List[EventGoldV2]:
    """
    Normalize a raw StatsBomb Open Data match into a sequence of canonical `EventGoldV2` events.
    Performs:
    - Starting XI / Tactical Shift / Substitution / Red-Card 22-slot tracking
    - `Ball Receipt*` folding onto preceding `Pass`
    - Optional trivial (<0.5 yd & <0.4s) carry folding into `one_touch` / `first_time`
    - Canonical [0, 120] x [0, 80] + attacking-relative coordinate transformation
    - 8-state `PossPhase` classification
    - 360 & shot freeze-frame cone/corridor/Voronoi geometry calculation
    """
    match_id = int(match_meta["match_id"])
    home_team_id = _safe_id(match_meta.get("home_team", {}).get("home_team_id") or match_meta.get("home_team"))
    if home_team_id == -1:
        home_team_id = int(match_meta.get("home_team_id", 1))
    away_team_id = _safe_id(match_meta.get("away_team", {}).get("away_team_id") or match_meta.get("away_team"))
    if away_team_id == -1:
        away_team_id = int(match_meta.get("away_team_id", 2))

    # Index 360 frames by event_uuid
    frames_by_uuid: Dict[str, Dict[str, Any]] = {}
    if frames_360:
        for fr in frames_360:
            ev_id = fr.get("event_uuid")
            if ev_id:
                frames_by_uuid[str(ev_id)] = fr

    # Build lookup of Ball Receipt* by id and related_events so Passes fold seamlessly
    receipt_by_uuid: Dict[str, Dict[str, Any]] = {}
    for ev in raw_events:
        if _safe_name(ev.get("type")) == "Ball Receipt*":
            receipt_by_uuid[str(ev.get("id"))] = ev

    # Sort deterministically by index / period / timestamp
    ordered_raw = sorted(raw_events, key=lambda e: int(e.get("index", 0)))

    slot_tracker = PitchSlotTracker(home_team_id=home_team_id, away_team_id=away_team_id)

    # First pass: ingest non-folded events & fold receipts
    intermediate: List[Dict[str, Any]] = []
    skip_uuids: set[str] = set()

    for idx_raw, ev in enumerate(ordered_raw):
        ev_uuid = str(ev.get("id", f"{match_id}_{idx_raw}"))
        if ev_uuid in skip_uuids:
            continue

        ev_type = _safe_name(ev.get("type"))
        ev_team_id = _safe_id(ev.get("team"), -1)
        is_home = ev_team_id == home_team_id

        if ev_type == "Starting XI":
            slot_tracker.apply_tactics(ev_team_id, ev.get("tactics") or {})
            continue

        if ev_type == "Tactical Shift":
            slot_tracker.apply_tactics(ev_team_id, ev.get("tactics") or {})
            continue

        # Fold standalone Ball Receipt* into preceding Pass when fold_receipts=True
        if fold_receipts and ev_type == "Ball Receipt*":
            continue

        # Ignore passive non-event metadata rows like Injury Stoppage / Camera / Referee Ball-Drop
        if ev_type in ("Camera On", "Camera off", "Referee Ball-Drop", "Player Off", "Player On", "Shield"):
            continue

        period_int = int(ev.get("period", 1))
        minute = int(ev.get("minute", 0))
        second = min(max(int(ev.get("second", 0)), 0), 59)
        ts_ms = _parse_timestamp_ms(ev.get("timestamp"), period_int, minute, second)
        player_id = _safe_id(ev.get("player"), -1)
        pos_id = _safe_id(ev.get("position"), 14)

        action, outcome, meta = _map_sb_event_to_action_outcome(ev)

        # Handle Substitution slot update BEFORE snapshotting
        if action == ActionType.SUBSTITUTION:
            sub = ev.get("substitution") or {}
            in_pid = _safe_id(sub.get("replacement"), -1)
            slot_tracker.apply_substitution(ev_team_id, player_id, in_pid)
            meta["target_player_id"] = in_pid

        # Fold linked Ball Receipt* onto Pass
        if fold_receipts and action == ActionType.PASS:
            for rel_id in ev.get("related_events") or []:
                rec = receipt_by_uuid.get(str(rel_id))
                if rec:
                    rec_pid = _safe_id(rec.get("player"), -1)
                    if rec_pid > 0 and meta["target_player_id"] <= 0:
                        meta["target_player_id"] = rec_pid
                    rec_out = _safe_name((rec.get("ball_receipt") or {}).get("outcome")).lower()
                    if "incomplete" in rec_out and outcome == EventOutcome.COMPLETE:
                        outcome = EventOutcome.INCOMPLETE
                    skip_uuids.add(str(rel_id))
                    break

        # Fold micro placeholder carries (<0.5 yd and <0.4s before immediate pass/shot)
        if fold_micro_carries and action == ActionType.CARRY:
            dist_carry = math.hypot(meta["end_x_raw"] - meta["x_raw"], meta["end_y_raw"] - meta["y_raw"])
            if dist_carry < 0.5 and float(ev.get("duration") or 0.0) < 0.4:
                # Look ahead for same player's immediate action
                if idx_raw + 1 < len(ordered_raw):
                    nxt = ordered_raw[idx_raw + 1]
                    if _safe_id(nxt.get("player"), -2) == player_id and _safe_name(nxt.get("type")) in ("Pass", "Shot"):
                        nxt["_folded_one_touch"] = True
                        continue

        actor_slot = slot_tracker.resolve_slot(player_id, ev_team_id, pos_id) if action != ActionType.PERIOD_BOUNDARY else 0
        target_pid = int(meta["target_player_id"])
        target_slot = slot_tracker.player_to_slot.get(target_pid, 0) if target_pid > 0 else 0

        # Capture pre-event slot & sent-off state (matches pre-event scoreline semantics),
        # then apply Red/Second-Yellow card for all subsequent events.
        slots_pids, slots_roles, sent_mask, h_form, a_form = slot_tracker.snapshot()
        if outcome in (EventOutcome.RED_CARD, EventOutcome.SECOND_YELLOW):
            slot_tracker.apply_red_card(player_id)

        # Coordinates -> Canonical Global & Attacking-Relative
        x_canon, y_canon = to_canonical_global(meta["x_raw"], meta["y_raw"], is_home)
        end_x_canon, end_y_canon = to_canonical_global(meta["end_x_raw"], meta["end_y_raw"], is_home)
        x_att, y_att = meta["x_raw"], meta["y_raw"]
        end_x_att, end_y_att = meta["end_x_raw"], meta["end_y_raw"]

        # 360 / Shot Freeze-Frame Extraction
        fr_360_entry = frames_by_uuid.get(ev_uuid)
        has_360 = fr_360_entry is not None
        ff_360_models: List[FreezeFramePlayer] = []
        vis_poly_canon: List[float] = []
        shot_ff_models: List[ShotFreezeFrameEntry] = []
        geom_input: List[Tuple[float, float, bool, bool]] = []

        if fr_360_entry is not None:
            raw_poly = fr_360_entry.get("visible_area") or []
            for p_i in range(0, len(raw_poly) - 1, 2):
                px_c, py_c = to_canonical_global(float(raw_poly[p_i]), float(raw_poly[p_i + 1]), is_home)
                vis_poly_canon.extend([px_c, py_c])
            for fp in fr_360_entry.get("freeze_frame") or []:
                floc = fp.get("location") or [60.0, 40.0]
                fxc, fyc = to_canonical_global(float(floc[0]), float(floc[1]), is_home)
                teammate = bool(fp.get("teammate", False))
                is_gk = bool(fp.get("keeper", False))
                ff_360_models.append(
                    FreezeFramePlayer(
                        teammate=teammate,
                        actor=bool(fp.get("actor", False)),
                        keeper=is_gk,
                        x_canon=fxc,
                        y_canon=fyc,
                    )
                )
                geom_input.append((fxc, fyc, teammate, is_gk))

        has_shot_ff = bool(meta["shot_freeze_frame_raw"])
        if has_shot_ff:
            for sfp in meta["shot_freeze_frame_raw"]:
                sloc = sfp.get("location") or [60.0, 40.0]
                sxc, syc = to_canonical_global(float(sloc[0]), float(sloc[1]), is_home)
                spid = _safe_id(sfp.get("player"), -1)
                teammate = bool(sfp.get("teammate", False))
                is_gk = _safe_name(sfp.get("position")).lower() == "goalkeeper"
                shot_ff_models.append(
                    ShotFreezeFrameEntry(
                        player_id=spid if spid > 0 else None,
                        slot=slot_tracker.player_to_slot.get(spid),
                        x_canon=sxc,
                        y_canon=syc,
                        teammate=teammate,
                        is_gk=is_gk,
                    )
                )
            if not geom_input:
                geom_input = [(e.x_canon, e.y_canon, e.teammate, e.is_gk) for e in shot_ff_models]

        geom = compute_freeze_frame_geometry(
            x_att=x_att,
            y_att=y_att,
            end_x_att=end_x_att,
            end_y_att=end_y_att,
            is_home_actor=is_home,
            action_type=action,
            freeze_players_canon=geom_input,
        )

        one_touch = bool(ev.get("_folded_one_touch", False))
        intermediate.append(
            {
                "match_id": match_id,
                "event_uuid": ev_uuid,
                "provider_source": "statsbomb",
                "period": map_sb_period(period_int),
                "minute": minute,
                "second": second,
                "timestamp_ms": ts_ms,
                "possession_id": int(ev.get("possession", 0)),
                "possession_team_id": _safe_id(ev.get("possession_team"), -1),
                "actor_slot": actor_slot,
                "actor_player_id": player_id,
                "actor_team_id": ev_team_id,
                "is_home_actor": is_home,
                "target_slot": target_slot,
                "target_player_id": target_pid,
                "active_slots_player_ids": slots_pids,
                "active_slots_roles": slots_roles,
                "active_slots_sent_off": sent_mask,
                "home_formation_id": h_form,
                "away_formation_id": a_form,
                "action_type": action,
                "outcome": outcome,
                "x_canon": x_canon,
                "y_canon": y_canon,
                "end_x_canon": end_x_canon,
                "end_y_canon": end_y_canon,
                "end_z_m": meta["end_z_m"],
                "x_att": x_att,
                "y_att": y_att,
                "end_x_att": end_x_att,
                "end_y_att": end_y_att,
                "restart_type": meta["restart_type"],
                "pass_height": meta["pass_height"],
                "body_part": meta["body_part"],
                "under_pressure": meta["under_pressure"],
                "pass_switch": meta["pass_switch"],
                "through_ball": meta["through_ball"],
                "cross": meta["cross"],
                "cutback": meta["cutback"],
                "first_time": meta["first_time"] or one_touch,
                "one_on_one": meta["one_on_one"],
                "one_touch": one_touch,
                "counter_attack": meta["counter_attack"],
                "aerial_won": meta["aerial_won"],
                "nutmeg": meta["nutmeg"],
                "overrun": meta["overrun"],
                "has_360_frame": has_360,
                "has_shot_freeze_frame": has_shot_ff,
                "shot_freeze_frame": shot_ff_models,
                "freeze_frame_360": ff_360_models,
                "visible_area_poly": vis_poly_canon,
                "xg_statsbomb": meta["xg_statsbomb"],
                **geom,
            }
        )

    # Second pass: dt_sec, monotonic clocks, scoreline tracking, and 8-state POSS_PHASE FSM
    gold_events: List[EventGoldV2] = []
    score_home = 0
    score_away = 0
    prev_ts_ms = 0
    prev_poss_id = -1
    poss_start_ms = 0
    last_turnover_ms = -999999
    last_set_piece_ms = -999999
    last_set_piece_team = -1

    for seq_idx, row in enumerate(intermediate):
        ts_ms = max(prev_ts_ms, row["timestamp_ms"])
        dt_sec = max(0.0, (ts_ms - prev_ts_ms) / 1000.0) if seq_idx > 0 else 0.0
        prev_ts_ms = ts_ms
        dt_bucket, dt_res = quantize_dt_bucket(dt_sec)

        poss_id = row["possession_id"]
        poss_team = row["possession_team_id"]
        if poss_id != prev_poss_id:
            if prev_poss_id != -1:
                last_turnover_ms = ts_ms
            poss_start_ms = ts_ms
            prev_poss_id = poss_id

        if row["restart_type"] in (
            RestartType.CORNER,
            RestartType.FREE_KICK_DIRECT,
            RestartType.FREE_KICK_INDIRECT,
            RestartType.THROW_IN,
            RestartType.GOAL_KICK,
            RestartType.PENALTY,
        ):
            last_set_piece_ms = ts_ms
            last_set_piece_team = row["actor_team_id"]

        # Determine 8-state POSS_PHASE
        action = row["action_type"]
        if action in (ActionType.PERIOD_BOUNDARY, ActionType.SUBSTITUTION, ActionType.CARD):
            poss_phase = PossPhase.DEAD_BALL
        elif (ts_ms - last_set_piece_ms) <= 6000 and row["restart_type"] != RestartType.KICK_OFF:
            poss_phase = (
                PossPhase.SET_PIECE_ATTACK
                if row["actor_team_id"] == last_set_piece_team
                else PossPhase.SET_PIECE_DEFENSE
            )
        elif (
            row["actor_team_id"] != poss_team
            and (ts_ms - last_turnover_ms) <= 5000
            and row["x_att"] <= 80.0
            and action in (ActionType.PRESSURE, ActionType.TACKLE_DUEL, ActionType.FOUL_COMMITTED, ActionType.INTERCEPTION)
        ):
            poss_phase = PossPhase.TRANSITION_COUNTERPRESS
        elif (ts_ms - poss_start_ms) <= 8000 and (
            row["counter_attack"]
            or ((row["end_x_att"] - row["x_att"]) / max(dt_sec, 0.5) > 3.5)
        ):
            poss_phase = PossPhase.TRANSITION_FAST
        elif row["x_att"] < 40.0:
            poss_phase = PossPhase.OPEN_BUILDUP
        elif row["x_att"] < 80.0:
            poss_phase = PossPhase.OPEN_PROGRESSION
        else:
            poss_phase = PossPhase.OPEN_FINAL_THIRD

        # Quantize spatial tokens & compute distance/angle
        start_cell, dx_s, dy_s = quantize_pitch_cell(row["x_canon"], row["y_canon"])
        end_cell, dx_e, dy_e = quantize_pitch_cell(row["end_x_canon"], row["end_y_canon"])
        end_z_bin = quantize_end_z(row["end_z_m"]) if action == ActionType.SHOT else -1
        dist_yd = math.hypot(row["end_x_att"] - row["x_att"], row["end_y_att"] - row["y_att"])
        angle_rad = math.atan2(row["end_y_att"] - row["y_att"], row["end_x_att"] - row["x_att"])
        mod_mask = pack_modifier_bitmask(
            under_pressure=row["under_pressure"],
            through_ball=row["through_ball"],
            pass_switch=row["pass_switch"],
            cross=row["cross"],
            cutback=row["cutback"],
            first_time=row["first_time"],
            one_touch=row["one_touch"],
            counter_attack=row["counter_attack"],
        )

        event = EventGoldV2(
            match_id=row["match_id"],
            event_seq_idx=seq_idx,
            event_uuid=row["event_uuid"],
            provider_source="statsbomb",
            period=row["period"],
            minute=row["minute"],
            second=row["second"],
            timestamp_ms=ts_ms,
            dt_sec=dt_sec,
            dt_bucket_id=dt_bucket,
            dt_res=dt_res,
            possession_id=poss_id,
            possession_team_id=poss_team,
            poss_phase=poss_phase,
            score_home=score_home,
            score_away=score_away,
            actor_slot=row["actor_slot"],
            actor_player_id=row["actor_player_id"],
            actor_team_id=row["actor_team_id"],
            is_home_actor=row["is_home_actor"],
            target_slot=row["target_slot"],
            target_player_id=row["target_player_id"],
            active_slots_player_ids=row["active_slots_player_ids"],
            active_slots_roles=row["active_slots_roles"],
            active_slots_sent_off=row["active_slots_sent_off"],
            home_formation_id=row["home_formation_id"],
            away_formation_id=row["away_formation_id"],
            action_type=action,
            outcome=row["outcome"],
            x_canon=row["x_canon"],
            y_canon=row["y_canon"],
            end_x_canon=row["end_x_canon"],
            end_y_canon=row["end_y_canon"],
            end_z_m=row["end_z_m"],
            x_att=row["x_att"],
            y_att=row["y_att"],
            end_x_att=row["end_x_att"],
            end_y_att=row["end_y_att"],
            start_cell_id=start_cell,
            end_cell_id=end_cell,
            dx_res_start=dx_s,
            dy_res_start=dy_s,
            dx_res_end=dx_e,
            dy_res_end=dy_e,
            end_z_bin=end_z_bin,
            distance_yd=dist_yd,
            angle_rad=angle_rad,
            restart_type=row["restart_type"],
            pass_height=row["pass_height"],
            body_part=row["body_part"],
            under_pressure=row["under_pressure"],
            pass_switch=row["pass_switch"],
            through_ball=row["through_ball"],
            cross=row["cross"],
            cutback=row["cutback"],
            first_time=row["first_time"],
            one_on_one=row["one_on_one"],
            one_touch=row["one_touch"],
            counter_attack=row["counter_attack"],
            aerial_won=row["aerial_won"],
            nutmeg=row["nutmeg"],
            overrun=row["overrun"],
            modifier_bitmask_8=mod_mask,
            has_360_frame=row["has_360_frame"],
            has_shot_freeze_frame=row["has_shot_freeze_frame"],
            shot_freeze_frame=row["shot_freeze_frame"],
            freeze_frame_360=row["freeze_frame_360"],
            visible_area_poly=row["visible_area_poly"],
            ff_n_defenders_in_shot_triangle=row["ff_n_defenders_in_shot_triangle"],
            ff_open_goal_fraction=row["ff_open_goal_fraction"],
            ff_gk_dist_goal=row["ff_gk_dist_goal"],
            ff_gk_angle_offset=row["ff_gk_angle_offset"],
            ff_closest_defender_dist=row["ff_closest_defender_dist"],
            ff_n_pressers_3m=row["ff_n_pressers_3m"],
            ff_min_defender_dist_to_pass_ray=row["ff_min_defender_dist_to_pass_ray"],
            ff_n_defenders_in_pass_corridor=row["ff_n_defenders_in_pass_corridor"],
            ff_receiver_voronoi_area=row["ff_receiver_voronoi_area"],
            ff_actor_packing_bypassed=row["ff_actor_packing_bypassed"],
            xg_statsbomb=row["xg_statsbomb"],
        )
        gold_events.append(event)

        # Update cumulative score AFTER storing pre-event state
        if row["outcome"] == EventOutcome.GOAL:
            if row["is_home_actor"]:
                score_home += 1
            else:
                score_away += 1
        elif row["outcome"] == EventOutcome.OWN_GOAL:
            if row["is_home_actor"]:
                score_away += 1
            else:
                score_home += 1

    return gold_events


def ingest_wyscout_match(
    match_meta: Dict[str, Any],
    raw_events: List[Dict[str, Any]],
    home_lineup_ids: Optional[List[int]] = None,
    away_lineup_ids: Optional[List[int]] = None,
) -> List[EventGoldV2]:
    """
    Normalize a Wyscout Open Data match into canonical `EventGoldV2` events.
    Rescales [0,100] coords -> [0,120]x[0,80], maps tags (1801, 1802, 101, 1901, 2101),
    synthesizes implicit CARRY events (>2.0 yd displacement within same possession/player),
    and sets has_360_frame=False.
    """
    match_id = int(match_meta.get("wyId", match_meta.get("match_id", 0)))
    home_team_id = int(match_meta.get("home_team_id", 1))
    away_team_id = int(match_meta.get("away_team_id", 2))

    slot_tracker = PitchSlotTracker(home_team_id=home_team_id, away_team_id=away_team_id)
    if home_lineup_ids:
        slot_tracker.apply_tactics(
            home_team_id,
            {
                "formation": "433",
                "lineup": [
                    {"player": {"id": pid}, "position": {"id": 1 if i == 0 else 14}}
                    for i, pid in enumerate(home_lineup_ids[:11])
                ],
            },
        )
    if away_lineup_ids:
        slot_tracker.apply_tactics(
            away_team_id,
            {
                "formation": "433",
                "lineup": [
                    {"player": {"id": pid}, "position": {"id": 1 if i == 0 else 14}}
                    for i, pid in enumerate(away_lineup_ids[:11])
                ],
            },
        )

    ordered = sorted(raw_events, key=lambda e: (int(e.get("matchPeriod", "1H") != "1H"), float(e.get("eventSec", 0.0))))
    gold_events: List[EventGoldV2] = []
    score_home, score_away = 0, 0
    prev_ts_ms = 0
    prev_player_id = -1
    prev_end_att = (60.0, 40.0)
    prev_is_home: Optional[bool] = None
    possession_id = 1
    prev_team = -1

    for raw_i, ev in enumerate(ordered):
        team_id = int(ev.get("teamId", home_team_id))
        is_home = team_id == home_team_id
        player_id = int(ev.get("playerId", -1))
        period_str = str(ev.get("matchPeriod", "1H"))
        period = MatchPeriod.FIRST_HALF if period_str == "1H" else MatchPeriod.SECOND_HALF
        sec_in_period = float(ev.get("eventSec", 0.0))
        ts_ms = (0 if period == MatchPeriod.FIRST_HALF else 45 * 60 * 1000) + int(round(sec_in_period * 1000))
        ts_ms = max(ts_ms, prev_ts_ms)
        minute = ts_ms // 60000
        second = (ts_ms // 1000) % 60

        positions = ev.get("positions") or [{"x": 50, "y": 50}, {"x": 50, "y": 50}]
        x_att, y_att = rescale_wyscout(positions[0]["x"], positions[0]["y"])
        if len(positions) > 1:
            end_x_att, end_y_att = rescale_wyscout(positions[1]["x"], positions[1]["y"])
        else:
            end_x_att, end_y_att = x_att, y_att

        tags = {int(t["id"]) if isinstance(t, dict) else int(t) for t in (ev.get("tags") or [])}
        ev_name = str(ev.get("eventName", "Pass")).lower()
        sub_name = str(ev.get("subEventName", "")).lower()

        if team_id != prev_team and prev_team != -1:
            possession_id += 1
        prev_team = team_id

        # Synthesize implicit CARRY when same player moves >2.0 yd between previous end and new start
        if (
            prev_player_id == player_id
            and prev_is_home == is_home
            and player_id > 0
            and math.hypot(x_att - prev_end_att[0], y_att - prev_end_att[1]) > 2.0
        ):
            c_x_c, c_y_c = to_canonical_global(prev_end_att[0], prev_end_att[1], is_home)
            c_ex_c, c_ey_c = to_canonical_global(x_att, y_att, is_home)
            sc, dxs, dys = quantize_pitch_cell(c_x_c, c_y_c)
            ec, dxe, dye = quantize_pitch_cell(c_ex_c, c_ey_c)
            dt_c = max(0.2, (ts_ms - prev_ts_ms) / 2000.0)
            dtb_c, dtr_c = quantize_dt_bucket(dt_c)
            slots_p, slots_r, sent_m, hf, af = slot_tracker.snapshot()
            gold_events.append(
                EventGoldV2(
                    match_id=match_id,
                    event_seq_idx=len(gold_events),
                    event_uuid=f"wy_{match_id}_{raw_i}_carry",
                    provider_source="wyscout",
                    period=period,
                    minute=minute,
                    second=second,
                    timestamp_ms=max(prev_ts_ms, ts_ms - int(dt_c * 1000)),
                    dt_sec=dt_c,
                    dt_bucket_id=dtb_c,
                    dt_res=dtr_c,
                    possession_id=possession_id,
                    possession_team_id=team_id,
                    poss_phase=PossPhase.OPEN_PROGRESSION if x_att < 80 else PossPhase.OPEN_FINAL_THIRD,
                    score_home=score_home,
                    score_away=score_away,
                    actor_slot=slot_tracker.resolve_slot(player_id, team_id),
                    actor_player_id=player_id,
                    actor_team_id=team_id,
                    is_home_actor=is_home,
                    target_slot=0,
                    target_player_id=-1,
                    active_slots_player_ids=slots_p,
                    active_slots_roles=slots_r,
                    active_slots_sent_off=sent_m,
                    home_formation_id=hf,
                    away_formation_id=af,
                    action_type=ActionType.CARRY,
                    outcome=EventOutcome.COMPLETE,
                    x_canon=c_x_c,
                    y_canon=c_y_c,
                    end_x_canon=c_ex_c,
                    end_y_canon=c_ey_c,
                    x_att=prev_end_att[0],
                    y_att=prev_end_att[1],
                    end_x_att=x_att,
                    end_y_att=y_att,
                    start_cell_id=sc,
                    end_cell_id=ec,
                    dx_res_start=dxs,
                    dy_res_start=dys,
                    dx_res_end=dxe,
                    dy_res_end=dye,
                    distance_yd=math.hypot(x_att - prev_end_att[0], y_att - prev_end_att[1]),
                    angle_rad=math.atan2(y_att - prev_end_att[1], x_att - prev_end_att[0]),
                    has_360_frame=False,
                )
            )

        # Map Wyscout event type & modifiers
        through_ball = "smart pass" in sub_name or 901 in tags
        cross = "cross" in sub_name
        counter = 1901 in tags
        pass_height = PassHeight.HIGH if ("high" in sub_name or "launch" in sub_name or cross) else PassHeight.GROUND
        body_part = BodyPart.KEEPER_HANDS if "hand pass" in sub_name else (BodyPart.HEAD if "head" in sub_name else BodyPart.RIGHT_FOOT)

        if "shot" in ev_name:
            action = ActionType.SHOT
            end_x_att = PITCH_LENGTH_YD
            if 101 in tags:
                outcome = EventOutcome.GOAL
            elif 2101 in tags:
                outcome = EventOutcome.BLOCKED
            elif 1801 in tags:
                outcome = EventOutcome.SAVED_HELD
            else:
                outcome = EventOutcome.OFF_TARGET
        elif "duel" in ev_name:
            action = ActionType.TACKLE_DUEL
            outcome = EventOutcome.WON if 1801 in tags else EventOutcome.LOST
        elif "foul" in ev_name:
            action = ActionType.FOUL_COMMITTED
            outcome = EventOutcome.RED_CARD if 1701 in tags else (EventOutcome.YELLOW_CARD if 1702 in tags else EventOutcome.FOUL_CONCEDED)
        elif "free kick" in ev_name:
            action = ActionType.PASS
            outcome = EventOutcome.COMPLETE if 1801 in tags else EventOutcome.INCOMPLETE
        else:
            action = ActionType.PASS
            outcome = EventOutcome.COMPLETE if 1801 in tags else EventOutcome.INCOMPLETE

        restart = RestartType.OPEN_PLAY
        if "corner" in sub_name:
            restart = RestartType.CORNER
        elif "throw" in sub_name:
            restart = RestartType.THROW_IN
        elif "goal kick" in sub_name:
            restart = RestartType.GOAL_KICK
        elif "penalty" in sub_name:
            restart = RestartType.PENALTY
        elif "free kick" in ev_name:
            restart = RestartType.FREE_KICK_DIRECT

        actor_slot = slot_tracker.resolve_slot(player_id, team_id)
        if outcome == EventOutcome.RED_CARD:
            slot_tracker.apply_red_card(player_id)

        x_canon, y_canon = to_canonical_global(x_att, y_att, is_home)
        end_x_canon, end_y_canon = to_canonical_global(end_x_att, end_y_att, is_home)
        sc, dxs, dys = quantize_pitch_cell(x_canon, y_canon)
        ec, dxe, dye = quantize_pitch_cell(end_x_canon, end_y_canon)
        dt_sec = max(0.0, (ts_ms - prev_ts_ms) / 1000.0) if gold_events else 0.0
        dtb, dtr = quantize_dt_bucket(dt_sec)
        slots_p, slots_r, sent_m, hf, af = slot_tracker.snapshot()

        gold_events.append(
            EventGoldV2(
                match_id=match_id,
                event_seq_idx=len(gold_events),
                event_uuid=str(ev.get("id", f"wy_{match_id}_{raw_i}")),
                provider_source="wyscout",
                period=period,
                minute=minute,
                second=second,
                timestamp_ms=ts_ms,
                dt_sec=dt_sec,
                dt_bucket_id=dtb,
                dt_res=dtr,
                possession_id=possession_id,
                possession_team_id=team_id,
                poss_phase=PossPhase.TRANSITION_FAST if counter else (PossPhase.OPEN_BUILDUP if x_att < 40 else (PossPhase.OPEN_PROGRESSION if x_att < 80 else PossPhase.OPEN_FINAL_THIRD)),
                score_home=score_home,
                score_away=score_away,
                actor_slot=actor_slot,
                actor_player_id=player_id,
                actor_team_id=team_id,
                is_home_actor=is_home,
                target_slot=0,
                target_player_id=-1,
                active_slots_player_ids=slots_p,
                active_slots_roles=slots_r,
                active_slots_sent_off=sent_m,
                home_formation_id=hf,
                away_formation_id=af,
                action_type=action,
                outcome=outcome,
                x_canon=x_canon,
                y_canon=y_canon,
                end_x_canon=end_x_canon,
                end_y_canon=end_y_canon,
                x_att=x_att,
                y_att=y_att,
                end_x_att=end_x_att,
                end_y_att=end_y_att,
                start_cell_id=sc,
                end_cell_id=ec,
                dx_res_start=dxs,
                dy_res_start=dys,
                dx_res_end=dxe,
                dy_res_end=dye,
                distance_yd=math.hypot(end_x_att - x_att, end_y_att - y_att),
                angle_rad=math.atan2(end_y_att - y_att, end_x_att - x_att),
                restart_type=restart,
                pass_height=pass_height,
                body_part=body_part,
                through_ball=through_ball,
                cross=cross,
                counter_attack=counter,
                modifier_bitmask_8=pack_modifier_bitmask(
                    through_ball=through_ball, cross=cross, counter_attack=counter
                ),
                has_360_frame=False,
            )
        )

        if outcome == EventOutcome.GOAL:
            if is_home:
                score_home += 1
            else:
                score_away += 1

        prev_ts_ms = ts_ms
        prev_player_id = player_id
        prev_end_att = (end_x_att, end_y_att)
        prev_is_home = is_home

    return gold_events


class LakehouseEngine:
    """
    Analytical SQL & Storage wrapper over DuckDB (when installed) or SQLite3 + Pandas fallback.
    Supports vectorized queries on Gold events and Parquet/JSONL persistence.
    """

    def __init__(self, db_path: str = ":memory:") -> None:
        self.db_path = db_path
        self.backend = "duckdb" if DUCKDB_AVAILABLE else "sqlite_pandas"
        if DUCKDB_AVAILABLE:
            self._conn = duckdb.connect(db_path)
        else:
            self._conn = sqlite3.connect(db_path)

    def close(self) -> None:
        self._conn.close()

    @staticmethod
    def events_to_dataframe(events: Sequence[EventGoldV2]) -> pd.DataFrame:
        """Convert structured EventGoldV2 sequence into a flat analytical DataFrame."""
        if not events:
            return pd.DataFrame()
        rows = []
        for ev in events:
            d = ev.model_dump(exclude={"shot_freeze_frame", "freeze_frame_360", "visible_area_poly"})
            d["active_slots_player_ids_json"] = json.dumps(d.pop("active_slots_player_ids"))
            d["active_slots_roles_json"] = json.dumps(d.pop("active_slots_roles"))
            d["action_type"] = int(ev.action_type)
            d["outcome"] = int(ev.outcome)
            d["period"] = int(ev.period)
            d["poss_phase"] = int(ev.poss_phase)
            d["restart_type"] = int(ev.restart_type)
            d["pass_height"] = int(ev.pass_height)
            d["body_part"] = int(ev.body_part)
            rows.append(d)
        return pd.DataFrame(rows)

    def register_events(self, events: Sequence[EventGoldV2], table_name: str = "events_gold_v2") -> pd.DataFrame:
        df = self.events_to_dataframe(events)
        if DUCKDB_AVAILABLE:
            self._conn.register("tmp_events_df", df)
            self._conn.execute(f"CREATE OR REPLACE TABLE {table_name} AS SELECT * FROM tmp_events_df")
        else:
            df.to_sql(table_name, self._conn, if_exists="replace", index=False)
        return df

    def query(self, sql: str) -> pd.DataFrame:
        if DUCKDB_AVAILABLE:
            return self._conn.execute(sql).df()
        return pd.read_sql_query(sql, self._conn)

    def export_table(self, table_name: str, output_path: Path) -> Path:
        """Write table as Zstd Parquet when PyArrow/DuckDB are installed, else gzipped JSONL."""
        output_path.parent.mkdir(parents=True, exist_ok=True)
        if DUCKDB_AVAILABLE:
            pq_path = output_path.with_suffix(".parquet")
            self._conn.execute(f"COPY {table_name} TO '{pq_path.as_posix()}' (FORMAT PARQUET, COMPRESSION ZSTD)")
            return pq_path
        df = self.query(f"SELECT * FROM {table_name}")
        if PYARROW_AVAILABLE:
            pq_path = output_path.with_suffix(".parquet")
            df.to_parquet(pq_path, compression="zstd", index=False)
            return pq_path
        jsonl_path = output_path.with_suffix(".jsonl.gz")
        df.to_json(jsonl_path, orient="records", lines=True, compression="gzip")
        return jsonl_path


def ingest_open_data_dir(
    open_data_root: Path,
    competition_ids: Optional[Iterable[int]] = None,
    max_matches: Optional[int] = None,
) -> List[EventGoldV2]:
    """Stream local `hudl/open-data` directory into normalized EventGoldV2 records."""
    data_dir = open_data_root / "data"
    comp_file = data_dir / "competitions.json"
    if not comp_file.exists():
        raise FileNotFoundError(f"StatsBomb competitions.json not found at {comp_file}")

    competitions = json.loads(comp_file.read_text(encoding="utf-8-sig"))
    allowed_comps = set(competition_ids) if competition_ids is not None else None

    all_events: List[EventGoldV2] = []
    matches_processed = 0
    seen_match_ids: set[int] = set()

    for comp in competitions:
        cid = int(comp["competition_id"])
        sid = int(comp["season_id"])
        if allowed_comps is not None and cid not in allowed_comps:
            continue
        match_file = data_dir / "matches" / str(cid) / f"{sid}.json"
        if not match_file.exists():
            continue
        matches = json.loads(match_file.read_text(encoding="utf-8-sig"))
        for m in matches:
            mid = int(m["match_id"])
            if mid in seen_match_ids:
                continue
            seen_match_ids.add(mid)
            ev_file = data_dir / "events" / f"{mid}.json"
            if not ev_file.exists():
                continue
            raw_ev = json.loads(ev_file.read_text(encoding="utf-8-sig"))
            f360_file = data_dir / "three-sixty" / f"{mid}.json"
            frames_360 = json.loads(f360_file.read_text(encoding="utf-8-sig")) if f360_file.exists() else None
            all_events.extend(ingest_statsbomb_match(m, raw_ev, frames_360=frames_360))
            matches_processed += 1
            if max_matches is not None and matches_processed >= max_matches:
                return all_events

    return all_events
