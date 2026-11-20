"""Explicit Football State Machine (FSM) and canonical event vocabularies (Module C1).

Implements Approach B deterministic football invariants:
- Canonical Global Pitch Frame: Home attacks +x (0 -> 120), Away attacks -x (120 -> 0), y in [0, 80].
- Active 22-slot addressing: 0=SYSTEM, 1..11=Home XI, 12..22=Away XI.
- Match clock, period boundaries, stoppage time, score, cards, substitutions, and set-piece restarts.
- Hard logit masks guaranteeing 100% legal actor, action, modifier, coordinate, and outcome transitions.
"""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum
from typing import Dict, Optional, Tuple

import math
import torch
from torch import BoolTensor, FloatTensor, LongTensor

# ---------------------------------------------------------------------------
# Canonical Pitch & Vocabulary Constants
# ---------------------------------------------------------------------------

PITCH_LENGTH: float = 120.0
PITCH_WIDTH: float = 80.0
GOAL_Y_MIN: float = 36.0
GOAL_Y_MAX: float = 44.0
GOAL_Z_MAX: float = 2.67  # 8ft crossbar height in meters

GRID_X: int = 30  # 4-yard cells along [0, 120]
GRID_Y: int = 20  # 4-yard cells along [0, 80]
NUM_PITCH_CELLS: int = GRID_X * GRID_Y  # 600
CELL_OUT_OF_BOUNDS: int = 600
CELL_SYSTEM: int = 601
NUM_CELL_TOKENS: int = 602

NUM_DT_BUCKETS: int = 64
NUM_ACTOR_SLOTS: int = 23  # 0=SYSTEM, 1..11 Home, 12..22 Away
NUM_ACTIONS: int = 24      # 18 defined + 6 reserved
NUM_RESTART_TYPES: int = 8
NUM_PASS_HEIGHTS: int = 4
NUM_BODY_PARTS: int = 5
NUM_MODIFIER_FLAGS: int = 8
NUM_OUTCOMES: int = 24
NUM_END_Z_BINS: int = 32


class Period(IntEnum):
    PRE = 0
    FIRST_HALF = 1
    HALFTIME = 2
    SECOND_HALF = 3
    ET1 = 4
    ET2 = 5
    FULLTIME = 6


class BallState(IntEnum):
    IN_PLAY = 0
    KICK_OFF = 1
    THROW_IN = 2
    CORNER_KICK = 3
    GOAL_KICK = 4
    FREE_KICK_DIRECT = 5
    FREE_KICK_INDIRECT = 6
    PENALTY = 7
    HALFTIME = 8
    FULLTIME = 9


class PossPhase(IntEnum):
    OPEN_BUILDUP = 0
    OPEN_PROGRESSION = 1
    OPEN_FINAL_THIRD = 2
    TRANSITION_FAST = 3
    TRANSITION_COUNTERPRESS = 4
    SET_PIECE_ATTACK = 5
    SET_PIECE_DEFENSE = 6
    DEAD_BALL = 7


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


class EventOutcome(IntEnum):
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


# Logarithmic dt bucket centers in seconds: 0.0s .. 120.0s
_DT_CENTERS = torch.cat([
    torch.tensor([0.0]),
    torch.exp(torch.linspace(math.log(0.2), math.log(120.0), NUM_DT_BUCKETS - 1)),
])


def dt_bucket_centers(device: torch.device | str = "cpu") -> FloatTensor:
    return _DT_CENTERS.to(device=device, dtype=torch.float32)


def decode_dt_seconds(dt_bucket: LongTensor, dt_residual: Optional[FloatTensor] = None) -> FloatTensor:
    """Convert discrete dt bucket + continuous residual into clamped non-negative seconds."""
    centers = dt_bucket_centers(dt_bucket.device)
    dt = centers[dt_bucket.clamp(0, NUM_DT_BUCKETS - 1)]
    if dt_residual is not None:
        dt = dt + dt_residual
    return dt.clamp(min=0.0, max=150.0)


def xy_to_cell(xy: FloatTensor) -> LongTensor:
    """Map canonical (x, y) in [0, 120] x [0, 80] to row-major pitch cell index [0..599], or 600 if out."""
    x = xy[..., 0]
    y = xy[..., 1]
    oob = (x < 0.0) | (x > PITCH_LENGTH) | (y < 0.0) | (y > PITCH_WIDTH)
    gx = torch.clamp((x / PITCH_LENGTH * GRID_X).long(), 0, GRID_X - 1)
    gy = torch.clamp((y / PITCH_WIDTH * GRID_Y).long(), 0, GRID_Y - 1)
    cell = gy * GRID_X + gx
    return torch.where(oob, torch.full_like(cell, CELL_OUT_OF_BOUNDS), cell)


def cell_to_center_xy(cell: LongTensor) -> FloatTensor:
    """Map row-major pitch cell index [0..599] back to cell center (x, y) in canonical [0, 120] x [0, 80]."""
    safe_cell = cell.clamp(0, NUM_PITCH_CELLS - 1)
    gy = torch.div(safe_cell, GRID_X, rounding_mode="floor").float()
    gx = (safe_cell % GRID_X).float()
    cx = (gx + 0.5) * (PITCH_LENGTH / GRID_X)
    cy = (gy + 0.5) * (PITCH_WIDTH / GRID_Y)
    return torch.stack([cx, cy], dim=-1)


@dataclass
class BatchedFSMState:
    """Vectorized state of B concurrent matches managed deterministically by FootballFSM."""

    period: LongTensor               # [B] Period enum
    clock_ms: LongTensor             # [B] total match elapsed ms
    period_elapsed_ms: LongTensor    # [B] elapsed ms within current period
    stoppage_1h_ms: LongTensor       # [B] added time for 1H
    stoppage_2h_ms: LongTensor       # [B] added time for 2H
    ball_state: LongTensor           # [B] BallState enum
    poss_team: LongTensor            # [B] 0=none, 1=home, 2=away
    poss_phase: LongTensor           # [B] PossPhase enum
    poss_phase_id: LongTensor        # [B] monotonic possession counter
    ball_xy: FloatTensor             # [B, 2] canonical [0..120, 0..80]
    ball_z: FloatTensor              # [B] meters
    home_score: LongTensor           # [B]
    away_score: LongTensor           # [B]
    active_mask: BoolTensor          # [B, 22] False if sent off (slots 1..22 -> idx 0..21)
    yellow_cards: BoolTensor         # [B, 22]
    red_cards: BoolTensor            # [B, 22]
    subs_used: LongTensor            # [B, 2] home, away
    sub_windows_used: LongTensor     # [B, 2] home, away
    first_half_kickoff_team: LongTensor  # [B] 1 or 2
    last_actor_slot: LongTensor      # [B] 0..22
    last_action: LongTensor          # [B] ActionType enum
    extra_time_enabled: BoolTensor   # [B]
    finished: BoolTensor             # [B]

    @property
    def batch_size(self) -> int:
        return int(self.period.shape[0])

    @property
    def device(self) -> torch.device:
        return self.period.device

    def clone(self) -> "BatchedFSMState":
        return BatchedFSMState(
            period=self.period.clone(),
            clock_ms=self.clock_ms.clone(),
            period_elapsed_ms=self.period_elapsed_ms.clone(),
            stoppage_1h_ms=self.stoppage_1h_ms.clone(),
            stoppage_2h_ms=self.stoppage_2h_ms.clone(),
            ball_state=self.ball_state.clone(),
            poss_team=self.poss_team.clone(),
            poss_phase=self.poss_phase.clone(),
            poss_phase_id=self.poss_phase_id.clone(),
            ball_xy=self.ball_xy.clone(),
            ball_z=self.ball_z.clone(),
            home_score=self.home_score.clone(),
            away_score=self.away_score.clone(),
            active_mask=self.active_mask.clone(),
            yellow_cards=self.yellow_cards.clone(),
            red_cards=self.red_cards.clone(),
            subs_used=self.subs_used.clone(),
            sub_windows_used=self.sub_windows_used.clone(),
            first_half_kickoff_team=self.first_half_kickoff_team.clone(),
            last_actor_slot=self.last_actor_slot.clone(),
            last_action=self.last_action.clone(),
            extra_time_enabled=self.extra_time_enabled.clone(),
            finished=self.finished.clone(),
        )


@dataclass
class FSMMasks:
    """Hard legality masks and forced restart enums emitted by FootballFSM before sampling."""

    actor_mask: BoolTensor           # [B, 22] slots 1..22 active & legal
    actor_mask_23: BoolTensor        # [B, 23] includes slot 0 (SYSTEM) for boundary events
    action_mask: BoolTensor          # [B, 24] legal ActionType per ball_state & match state
    target_mask: BoolTensor          # [B, 11] active teammate targets for possessing team
    spatial_log_prior: FloatTensor   # [B, 35] |A| + 11 additive prior
    start_cell_mask: BoolTensor      # [B, 602] legal start pitch cells
    end_cell_mask: BoolTensor        # [B, 602] legal end pitch cells
    body_part_mask: BoolTensor       # [B, 5] legal body parts
    restart_type_forced: LongTensor  # [B] deterministic RestartType enum
    force_period_boundary: BoolTensor  # [B] True if match must emit PERIOD_BOUNDARY now


class FootballFSM:
    """Deterministic Football State Machine enforcing Laws of the Game & coordinate invariants."""

    # Prebuilt action legality table: [len(BallState), NUM_ACTIONS]
    _LEGAL_ACTIONS_TABLE: BoolTensor
    # Prebuilt outcome legality table: [NUM_ACTIONS, NUM_OUTCOMES]
    _LEGAL_OUTCOMES_TABLE: BoolTensor
    # Prebuilt ball-state -> restart-type map
    _RESTART_MAP: LongTensor

    def __init__(self, shot_max_distance_yards: float = 42.0, in_play_continuity_radius: float = 6.5):
        self.shot_max_distance_yards = shot_max_distance_yards
        self.in_play_continuity_radius = in_play_continuity_radius
        self._LEGAL_ACTIONS_TABLE = self._build_legal_actions_table()
        self._LEGAL_OUTCOMES_TABLE = self._build_legal_outcomes_table()
        self._RESTART_MAP = torch.tensor([
            RestartType.OPEN_PLAY,           # IN_PLAY
            RestartType.KICK_OFF,            # KICK_OFF
            RestartType.THROW_IN,            # THROW_IN
            RestartType.CORNER,              # CORNER_KICK
            RestartType.GOAL_KICK,           # GOAL_KICK
            RestartType.FREE_KICK_DIRECT,    # FREE_KICK_DIRECT
            RestartType.FREE_KICK_INDIRECT,  # FREE_KICK_INDIRECT
            RestartType.PENALTY,             # PENALTY
            RestartType.OPEN_PLAY,           # HALFTIME
            RestartType.OPEN_PLAY,           # FULLTIME
        ], dtype=torch.long)

        # Precompute row-major cell centers [600, 2] matching statsbombapi_v2/schemas.py (row * 30 + col)
        gy = torch.arange(GRID_Y, dtype=torch.float32).repeat_interleave(GRID_X)
        gx = torch.arange(GRID_X, dtype=torch.float32).repeat(GRID_Y)
        self._cell_centers = torch.stack([
            (gx + 0.5) * (PITCH_LENGTH / GRID_X),
            (gy + 0.5) * (PITCH_WIDTH / GRID_Y),
        ], dim=-1)
        self._device_cache: Dict[torch.device, Dict[str, Any]] = {}

    def _get_dev_cache(self, dev: torch.device) -> Dict[str, Any]:
        """Return cached device-resident constant tensors to avoid per-step H2D copies and allocations."""
        cache = self._device_cache.get(dev)
        if cache is None:
            centers = self._cell_centers.to(dev)
            home_slots = torch.zeros((1, 22), dtype=torch.bool, device=dev)
            home_slots[:, :11] = True
            outfield_mask = torch.ones((1, 22), dtype=torch.bool, device=dev)
            outfield_mask[:, 0] = False
            outfield_mask[:, 11] = False
            cache = {
                "legal_actions": self._LEGAL_ACTIONS_TABLE.to(dev),
                "legal_outcomes": self._LEGAL_OUTCOMES_TABLE.to(dev),
                "restart_map": self._RESTART_MAP.to(dev),
                "cell_centers": centers,
                "ko_xy": torch.tensor([60.0, 40.0], dtype=torch.float32, device=dev),
                "home_slots": home_slots,
                "away_slots": ~home_slots,
                "outfield_mask": outfield_mask,
                "own_half_home": (centers[:, 0] <= 60.0).unsqueeze(0),
                "own_half_away": (centers[:, 0] >= 60.0).unsqueeze(0),
            }
            self._device_cache[dev] = cache
        return cache

    @staticmethod
    def _build_legal_actions_table() -> BoolTensor:
        table = torch.zeros((len(BallState), NUM_ACTIONS), dtype=torch.bool)
        # IN_PLAY
        for act in (
            ActionType.PASS, ActionType.CARRY, ActionType.DRIBBLE, ActionType.SHOT,
            ActionType.CLEARANCE, ActionType.INTERCEPTION, ActionType.TACKLE_DUEL,
            ActionType.BALL_RECOVERY, ActionType.SAVE, ActionType.BLOCK,
            ActionType.PRESSURE, ActionType.FOUL_COMMITTED, ActionType.MISCONTROL,
            ActionType.DISPOSSESSED, ActionType.OFFSIDE, ActionType.CARD,
        ):
            table[BallState.IN_PLAY, act] = True

        # Fixed restarts per docs/revamp/02 §3.5 and 03 §2.4
        table[BallState.KICK_OFF, ActionType.PASS] = True
        table[BallState.THROW_IN, ActionType.PASS] = True
        table[BallState.THROW_IN, ActionType.SUBSTITUTION] = True
        table[BallState.GOAL_KICK, ActionType.PASS] = True
        table[BallState.GOAL_KICK, ActionType.SUBSTITUTION] = True
        # Corner deliveries/in-swingers are modeled as PASS(restart_type=Corner, cross=True)
        table[BallState.CORNER_KICK, ActionType.PASS] = True
        table[BallState.CORNER_KICK, ActionType.SUBSTITUTION] = True
        table[BallState.FREE_KICK_DIRECT, ActionType.PASS] = True
        table[BallState.FREE_KICK_DIRECT, ActionType.SHOT] = True
        table[BallState.FREE_KICK_DIRECT, ActionType.SUBSTITUTION] = True
        table[BallState.FREE_KICK_INDIRECT, ActionType.PASS] = True
        table[BallState.FREE_KICK_INDIRECT, ActionType.SUBSTITUTION] = True
        table[BallState.PENALTY, ActionType.SHOT] = True
        table[BallState.HALFTIME, ActionType.PERIOD_BOUNDARY] = True
        table[BallState.FULLTIME, ActionType.PERIOD_BOUNDARY] = True
        return table

    @staticmethod
    def _build_legal_outcomes_table() -> BoolTensor:
        table = torch.zeros((NUM_ACTIONS, NUM_OUTCOMES), dtype=torch.bool)
        for out in (EventOutcome.COMPLETE, EventOutcome.INCOMPLETE, EventOutcome.OUT_TOUCHLINE,
                    EventOutcome.OUT_BYLINE, EventOutcome.OFFSIDE):
            table[ActionType.PASS, out] = True
        for out in (EventOutcome.COMPLETE, EventOutcome.INCOMPLETE, EventOutcome.FOUL_CONCEDED):
            table[ActionType.CARRY, out] = True
            table[ActionType.DRIBBLE, out] = True
        for out in (EventOutcome.GOAL, EventOutcome.SAVED_HELD, EventOutcome.SAVED_PARRY,
                    EventOutcome.BLOCKED, EventOutcome.OFF_TARGET, EventOutcome.POST):
            table[ActionType.SHOT, out] = True
        for out in (EventOutcome.COMPLETE, EventOutcome.OUT_TOUCHLINE, EventOutcome.OUT_BYLINE):
            table[ActionType.CLEARANCE, out] = True
        for out in (EventOutcome.COMPLETE, EventOutcome.OUT_TOUCHLINE, EventOutcome.OUT_BYLINE):
            table[ActionType.INTERCEPTION, out] = True
        for out in (EventOutcome.WON, EventOutcome.LOST, EventOutcome.FOUL_CONCEDED):
            table[ActionType.TACKLE_DUEL, out] = True
        table[ActionType.BALL_RECOVERY, EventOutcome.COMPLETE] = True
        for out in (EventOutcome.SAVED_HELD, EventOutcome.SAVED_PARRY, EventOutcome.OUT_BYLINE):
            table[ActionType.SAVE, out] = True
        for out in (EventOutcome.COMPLETE, EventOutcome.OUT_BYLINE, EventOutcome.OUT_TOUCHLINE):
            table[ActionType.BLOCK, out] = True
        table[ActionType.PRESSURE, EventOutcome.NEUTRAL] = True
        for out in (EventOutcome.FOUL_CONCEDED, EventOutcome.YELLOW_CARD,
                    EventOutcome.SECOND_YELLOW, EventOutcome.RED_CARD, EventOutcome.PENALTY_WON):
            table[ActionType.FOUL_COMMITTED, out] = True
        for out in (EventOutcome.LOST, EventOutcome.OUT_TOUCHLINE, EventOutcome.OUT_BYLINE):
            table[ActionType.MISCONTROL, out] = True
            table[ActionType.DISPOSSESSED, out] = True
        table[ActionType.OFFSIDE, EventOutcome.OFFSIDE] = True
        for out in (EventOutcome.YELLOW_CARD, EventOutcome.SECOND_YELLOW, EventOutcome.RED_CARD):
            table[ActionType.CARD, out] = True
        table[ActionType.SUBSTITUTION, EventOutcome.SUB_EXECUTED] = True
        table[ActionType.PERIOD_BOUNDARY, EventOutcome.PERIOD_END] = True
        return table

    def init_state(
        self,
        batch_size: int,
        first_half_kickoff_team: int = 1,
        extra_time_enabled: bool = False,
        device: torch.device | str = "cpu",
        generator: Optional[torch.Generator] = None,
    ) -> BatchedFSMState:
        """Create initial match states at 00:00 ready for 1H KICK_OFF."""
        dev = torch.device(device)
        # Realistic stoppage draws: 1H ~1-4 min, 2H ~2-6 min
        s1_sec = torch.randint(60, 240, (batch_size,), generator=generator, device=dev)
        s2_sec = torch.randint(120, 360, (batch_size,), generator=generator, device=dev)
        ball_xy = torch.tensor([60.0, 40.0], device=dev, dtype=torch.float32).unsqueeze(0).expand(batch_size, 2).clone()
        return BatchedFSMState(
            period=torch.full((batch_size,), Period.FIRST_HALF, dtype=torch.long, device=dev),
            clock_ms=torch.zeros(batch_size, dtype=torch.long, device=dev),
            period_elapsed_ms=torch.zeros(batch_size, dtype=torch.long, device=dev),
            stoppage_1h_ms=s1_sec * 1000,
            stoppage_2h_ms=s2_sec * 1000,
            ball_state=torch.full((batch_size,), BallState.KICK_OFF, dtype=torch.long, device=dev),
            poss_team=torch.full((batch_size,), first_half_kickoff_team, dtype=torch.long, device=dev),
            poss_phase=torch.full((batch_size,), PossPhase.SET_PIECE_ATTACK, dtype=torch.long, device=dev),
            poss_phase_id=torch.ones(batch_size, dtype=torch.long, device=dev),
            ball_xy=ball_xy,
            ball_z=torch.zeros(batch_size, dtype=torch.float32, device=dev),
            home_score=torch.zeros(batch_size, dtype=torch.long, device=dev),
            away_score=torch.zeros(batch_size, dtype=torch.long, device=dev),
            active_mask=torch.ones((batch_size, 22), dtype=torch.bool, device=dev),
            yellow_cards=torch.zeros((batch_size, 22), dtype=torch.bool, device=dev),
            red_cards=torch.zeros((batch_size, 22), dtype=torch.bool, device=dev),
            subs_used=torch.zeros((batch_size, 2), dtype=torch.long, device=dev),
            sub_windows_used=torch.zeros((batch_size, 2), dtype=torch.long, device=dev),
            first_half_kickoff_team=torch.full((batch_size,), first_half_kickoff_team, dtype=torch.long, device=dev),
            last_actor_slot=torch.zeros(batch_size, dtype=torch.long, device=dev),
            last_action=torch.full((batch_size,), ActionType.PERIOD_BOUNDARY, dtype=torch.long, device=dev),
            extra_time_enabled=torch.full((batch_size,), extra_time_enabled, dtype=torch.bool, device=dev),
            finished=torch.zeros(batch_size, dtype=torch.bool, device=dev),
        )

    def _period_duration_ms(self, state: BatchedFSMState) -> LongTensor:
        base_45 = 45 * 60 * 1000
        base_15 = 15 * 60 * 1000
        dur = torch.full_like(state.period_elapsed_ms, base_45)
        dur = torch.where(state.period == Period.FIRST_HALF, base_45 + state.stoppage_1h_ms, dur)
        dur = torch.where(state.period == Period.SECOND_HALF, base_45 + state.stoppage_2h_ms, dur)
        dur = torch.where((state.period == Period.ET1) | (state.period == Period.ET2), base_15 + 60_000, dur)
        return dur

    def compute_masks(
        self,
        state: BatchedFSMState,
        tactical_actor_mask: Optional[BoolTensor] = None,
        tactical_action_mask: Optional[BoolTensor] = None,
        tactical_target_mask: Optional[BoolTensor] = None,
        tactical_spatial_prior: Optional[FloatTensor] = None,
    ) -> FSMMasks:
        """Compute hard legality masks over actors, actions, spatial cells, body parts, and targets."""
        B = state.batch_size
        dev = state.device
        dc = self._get_dev_cache(dev)
        legal_table = dc["legal_actions"]
        centers = dc["cell_centers"]  # [600, 2]

        # Check period expiration (end half/match once clock >= threshold and ball not mid-penalty)
        period_limit = self._period_duration_ms(state)
        is_dead_or_neutral = (state.ball_state != BallState.PENALTY)
        force_boundary = (
            (~state.finished)
            & ((state.ball_state == BallState.HALFTIME)
               | (state.ball_state == BallState.FULLTIME)
               | ((state.period_elapsed_ms >= period_limit) & is_dead_or_neutral))
        )

        # 1. Action mask
        action_mask = legal_table[state.ball_state].clone()
        # Allow PERIOD_BOUNDARY strictly when period boundary reached
        boundary_only = torch.zeros_like(action_mask)
        boundary_only[:, ActionType.PERIOD_BOUNDARY] = True
        action_mask = torch.where(force_boundary.unsqueeze(-1), boundary_only, action_mask)

        # Distance-to-target-goal constraint for open-play SHOT (42 yards max unless late & trailing)
        target_goal_x = torch.where(state.poss_team == 1, PITCH_LENGTH, 0.0)
        goal_xy = torch.stack([target_goal_x, torch.full((B,), 40.0, device=dev)], dim=-1)
        dist_to_goal = torch.linalg.norm(state.ball_xy - goal_xy, dim=-1)
        is_trailing = torch.where(
            state.poss_team == 1,
            state.home_score < state.away_score,
            state.away_score < state.home_score,
        )
        minute = state.clock_ms.float() / 60_000.0
        allow_long_shot = (dist_to_goal <= self.shot_max_distance_yards) | ((minute >= 88.0) & is_trailing)
        in_play_mask = state.ball_state == BallState.IN_PLAY
        action_mask[in_play_mask, ActionType.SHOT] &= allow_long_shot[in_play_mask]

        # SAVE only allowed immediately after an on-target SHOT
        can_save = (state.last_action == ActionType.SHOT) & in_play_mask
        action_mask[:, ActionType.SAVE] &= can_save

        # Enforce FIFA 5-substitution cap per team on dead-ball restarts
        poss_subs = torch.where(
            state.poss_team == 1,
            state.subs_used[:, 0],
            torch.where(state.poss_team == 2, state.subs_used[:, 1], state.subs_used.min(dim=-1).values),
        )
        action_mask[:, ActionType.SUBSTITUTION] &= (poss_subs < 5) & (minute >= 30.0)

        if tactical_action_mask is not None:
            action_mask &= tactical_action_mask.to(dev)
        # Ensure at least one legal action per batch item (branchless, no host sync)
        no_action = ~action_mask.any(dim=-1)
        action_mask[:, ActionType.PASS] = action_mask[:, ActionType.PASS] | no_action

        # 2. Actor mask [B, 22] and [B, 23]
        actor_mask = state.active_mask.clone()
        # Fixed restarts restrict actor to the possessing team
        is_fixed_restart = (
            (state.ball_state == BallState.KICK_OFF)
            | (state.ball_state == BallState.THROW_IN)
            | (state.ball_state == BallState.CORNER_KICK)
            | (state.ball_state == BallState.GOAL_KICK)
            | (state.ball_state == BallState.FREE_KICK_DIRECT)
            | (state.ball_state == BallState.FREE_KICK_INDIRECT)
            | (state.ball_state == BallState.PENALTY)
        )
        home_slots = dc["home_slots"].expand(B, 22)
        away_slots = dc["away_slots"].expand(B, 22)
        poss_team_mask = torch.where(
            (state.poss_team == 1).unsqueeze(-1),
            home_slots,
            torch.where((state.poss_team == 2).unsqueeze(-1), away_slots, True),
        )
        actor_mask = torch.where(is_fixed_restart.unsqueeze(-1), actor_mask & poss_team_mask, actor_mask)
        # Kick-off, corners, and penalties use outfielders (not GK) when available
        outfield_mask = dc["outfield_mask"].expand(B, 22)
        outfield_restart = (
            (state.ball_state == BallState.KICK_OFF)
            | (state.ball_state == BallState.CORNER_KICK)
            | (state.ball_state == BallState.PENALTY)
        )
        actor_mask = torch.where(outfield_restart.unsqueeze(-1), actor_mask & outfield_mask, actor_mask)

        if tactical_actor_mask is not None:
            # Apply tactical filter safely without zeroing out every active player
            merged = actor_mask & tactical_actor_mask.to(dev)
            has_any = merged.any(dim=-1, keepdim=True)
            actor_mask = torch.where(has_any, merged, actor_mask)

        # Build 23-slot mask: slot 0 = SYSTEM only for forced boundaries
        actor_mask_23 = torch.zeros((B, NUM_ACTOR_SLOTS), dtype=torch.bool, device=dev)
        actor_mask_23[:, 1:] = actor_mask
        actor_mask_23[force_boundary, 0] = True
        actor_mask_23[force_boundary, 1:] = False

        # 3. Target mask [B, 11] (possessing team's active slots)
        target_mask = torch.where(
            (state.poss_team == 1).unsqueeze(-1),
            state.active_mask[:, :11],
            state.active_mask[:, 11:],
        )
        if tactical_target_mask is not None:
            merged_t = target_mask & tactical_target_mask.to(dev)
            target_mask = torch.where(merged_t.any(dim=-1, keepdim=True), merged_t, target_mask)

        # 4. Spatial start/end cell masks [B, 602]
        start_cell_mask = torch.zeros((B, NUM_CELL_TOKENS), dtype=torch.bool, device=dev)
        end_cell_mask = torch.zeros((B, NUM_CELL_TOKENS), dtype=torch.bool, device=dev)

        # Open play start: within continuity radius of ball_xy
        dist_from_ball = torch.linalg.norm(centers.unsqueeze(0) - state.ball_xy.unsqueeze(1), dim=-1)  # [B, 600]
        near_ball = dist_from_ball <= self.in_play_continuity_radius
        # Ensure nearest cell is always True
        nearest_cell = xy_to_cell(state.ball_xy).clamp(0, NUM_PITCH_CELLS - 1)
        near_ball.scatter_(1, nearest_cell.unsqueeze(-1), True)
        start_cell_mask[:, :NUM_PITCH_CELLS] = near_ball
        start_cell_mask[force_boundary, :] = False
        start_cell_mask[force_boundary, CELL_SYSTEM] = True

        # Fixed restart exact start clamps (branchless, no host sync)
        fixed_start = is_fixed_restart & (~force_boundary)
        exact_mask = torch.zeros_like(start_cell_mask)
        exact_mask.scatter_(1, nearest_cell.unsqueeze(-1), True)
        start_cell_mask = torch.where(fixed_start.unsqueeze(-1), exact_mask, start_cell_mask)

        # End cell mask: all in-pitch cells + out-of-bounds token for open play;
        # Kick-off restricted to possessing team's own half (02 §3.5)
        end_cell_mask[:, :NUM_PITCH_CELLS] = True
        end_cell_mask[:, CELL_OUT_OF_BOUNDS] = state.ball_state == BallState.IN_PLAY
        is_ko = (state.ball_state == BallState.KICK_OFF) & (~force_boundary)
        own_half = torch.where(
            (state.poss_team == 1).unsqueeze(-1),
            dc["own_half_home"].expand(B, -1),
            dc["own_half_away"].expand(B, -1),
        )
        ko_end = torch.zeros_like(end_cell_mask)
        ko_end[:, :NUM_PITCH_CELLS] = own_half
        end_cell_mask = torch.where(is_ko.unsqueeze(-1), ko_end, end_cell_mask)
        end_cell_mask[force_boundary, :] = False
        end_cell_mask[force_boundary, CELL_SYSTEM] = True

        # 5. Body part mask [B, 5] (KeeperHands only for GK slots in own penalty area)
        body_part_mask = torch.ones((B, NUM_BODY_PARTS), dtype=torch.bool, device=dev)
        in_own_box = torch.where(
            state.poss_team == 1,
            (state.ball_xy[:, 0] <= 18.0) & (state.ball_xy[:, 1] >= 18.0) & (state.ball_xy[:, 1] <= 62.0),
            (state.ball_xy[:, 0] >= 102.0) & (state.ball_xy[:, 1] >= 18.0) & (state.ball_xy[:, 1] <= 62.0),
        )
        body_part_mask[:, BodyPart.KEEPER_HANDS] = (state.ball_state == BallState.IN_PLAY) & in_own_box

        restart_type_forced = dc["restart_map"][state.ball_state]
        spatial_prior = torch.zeros((B, NUM_ACTIONS + 11), dtype=torch.float32, device=dev)
        if tactical_spatial_prior is not None:
            tp = tactical_spatial_prior.to(dev)
            n_act_in = max(0, tp.shape[-1] - 11)
            n_act_copy = min(NUM_ACTIONS, n_act_in)
            if n_act_copy > 0:
                spatial_prior[:, :n_act_copy] = tp[:, :n_act_copy]
            if tp.shape[-1] >= 11:
                spatial_prior[:, NUM_ACTIONS:] = tp[:, -11:]

        return FSMMasks(
            actor_mask=actor_mask,
            actor_mask_23=actor_mask_23,
            action_mask=action_mask,
            target_mask=target_mask,
            spatial_log_prior=spatial_prior,
            start_cell_mask=start_cell_mask,
            end_cell_mask=end_cell_mask,
            body_part_mask=body_part_mask,
            restart_type_forced=restart_type_forced,
            force_period_boundary=force_boundary,
        )

    def outcome_mask_for_action(self, action_type: LongTensor) -> BoolTensor:
        """Return legal outcome mask [B, NUM_OUTCOMES] given sampled action_type [B]."""
        table = self._get_dev_cache(action_type.device)["legal_outcomes"]
        return table[action_type.clamp(0, NUM_ACTIONS - 1)]

    def step(
        self,
        state: BatchedFSMState,
        dt_seconds: FloatTensor,
        actor_slot: LongTensor,
        action_type: LongTensor,
        start_xy: FloatTensor,
        end_xy: FloatTensor,
        end_z: FloatTensor,
        outcome: LongTensor,
    ) -> BatchedFSMState:
        """Advance BatchedFSMState deterministically from a committed event."""
        nxt = state.clone()
        B = state.batch_size
        dev = state.device
        dc = self._get_dev_cache(dev)

        # 1. Advance monotonic clock
        dt_ms = torch.clamp((dt_seconds * 1000.0).round().long(), min=0)
        not_done = ~nxt.finished
        nxt.clock_ms = torch.where(not_done, nxt.clock_ms + dt_ms, nxt.clock_ms)
        nxt.period_elapsed_ms = torch.where(not_done, nxt.period_elapsed_ms + dt_ms, nxt.period_elapsed_ms)

        nxt.last_actor_slot = actor_slot
        nxt.last_action = action_type

        # Determine actor team (0=system, 1=home, 2=away)
        actor_team = torch.where(
            actor_slot == 0,
            0,
            torch.where(actor_slot <= 11, 1, 2),
        )
        other_team = torch.where(actor_team == 1, 2, 1)

        # 2. Cards & Red-Card removals (branchless, no host sync)
        is_yellow = (outcome == EventOutcome.YELLOW_CARD) & (actor_slot > 0)
        is_red = ((outcome == EventOutcome.RED_CARD) | (outcome == EventOutcome.SECOND_YELLOW)) & (actor_slot > 0)
        safe_card_idx = (actor_slot - 1).clamp(0, 21)
        b_arange = torch.arange(B, device=dev)
        was_yellow = nxt.yellow_cards[b_arange, safe_card_idx]
        nxt.yellow_cards[b_arange, safe_card_idx] = was_yellow | is_yellow
        make_red = is_red | (is_yellow & was_yellow)
        nxt.red_cards[b_arange, safe_card_idx] = nxt.red_cards[b_arange, safe_card_idx] | make_red
        nxt.active_mask[b_arange, safe_card_idx] = nxt.active_mask[b_arange, safe_card_idx] & (~make_red)

        # 3. Goals -> score update + KICK_OFF by conceding team at (60, 40)
        is_goal = (action_type == ActionType.SHOT) & (outcome == EventOutcome.GOAL) & not_done
        nxt.home_score = nxt.home_score + (is_goal & (actor_team == 1)).long()
        nxt.away_score = nxt.away_score + (is_goal & (actor_team == 2)).long()

        # 4. Clip end_xy for in-bounds ball updates and handle restarts
        clamped_end = torch.stack([
            end_xy[:, 0].clamp(0.0, PITCH_LENGTH),
            end_xy[:, 1].clamp(0.0, PITCH_WIDTH),
        ], dim=-1)
        nxt.ball_xy = torch.where(not_done.unsqueeze(-1), clamped_end, nxt.ball_xy)
        nxt.ball_z = torch.where(not_done, end_z.clamp(0.0, 10.0), nxt.ball_z)

        # Default: transition restarts back to IN_PLAY
        new_ball_state = torch.full((B,), BallState.IN_PLAY, dtype=torch.long, device=dev)
        new_poss_team = torch.where(actor_team > 0, actor_team, nxt.poss_team)

        # Goal -> Kick-Off by conceding team
        ko_xy = dc["ko_xy"].unsqueeze(0).expand(B, 2)
        new_ball_state = torch.where(is_goal, BallState.KICK_OFF, new_ball_state)
        new_poss_team = torch.where(is_goal, other_team, new_poss_team)
        nxt.ball_xy = torch.where(is_goal.unsqueeze(-1), ko_xy, nxt.ball_xy)
        nxt.ball_z = torch.where(is_goal, 0.0, nxt.ball_z)

        # Touchline out -> THROW_IN to opposite team at nearest touchline
        is_touch = (outcome == EventOutcome.OUT_TOUCHLINE) & not_done
        throw_y = torch.where(clamped_end[:, 1] >= 40.0, PITCH_WIDTH, 0.0)
        throw_xy = torch.stack([clamped_end[:, 0].clamp(1.0, PITCH_LENGTH - 1.0), throw_y], dim=-1)
        new_ball_state = torch.where(is_touch, BallState.THROW_IN, new_ball_state)
        new_poss_team = torch.where(is_touch, other_team, new_poss_team)
        nxt.ball_xy = torch.where(is_touch.unsqueeze(-1), throw_xy, nxt.ball_xy)

        # Byline / Off-target shot -> CORNER_KICK or GOAL_KICK
        is_byline_or_miss = (
            (outcome == EventOutcome.OUT_BYLINE)
            | ((action_type == ActionType.SHOT) & ((outcome == EventOutcome.OFF_TARGET) | (outcome == EventOutcome.POST)))
        ) & not_done
        was_defensive_touch = (
            (action_type == ActionType.CLEARANCE)
            | (action_type == ActionType.SAVE)
            | (action_type == ActionType.BLOCK)
            | (action_type == ActionType.INTERCEPTION)
        )
        corner_mask = is_byline_or_miss & was_defensive_touch
        gk_mask = is_byline_or_miss & (~was_defensive_touch)

        att_team_c = torch.where(was_defensive_touch, other_team, actor_team)
        c_x = torch.where(att_team_c == 1, PITCH_LENGTH, 0.0)
        c_y = torch.where(clamped_end[:, 1] >= 40.0, PITCH_WIDTH, 0.0)
        corner_xy = torch.stack([c_x, c_y], dim=-1)
        new_ball_state = torch.where(corner_mask, BallState.CORNER_KICK, new_ball_state)
        new_poss_team = torch.where(corner_mask, att_team_c, new_poss_team)
        nxt.ball_xy = torch.where(corner_mask.unsqueeze(-1), corner_xy, nxt.ball_xy)

        gk_x = torch.where(other_team == 1, 6.0, PITCH_LENGTH - 6.0)
        gk_xy = torch.stack([gk_x, torch.full_like(gk_x, 40.0)], dim=-1)
        new_ball_state = torch.where(gk_mask, BallState.GOAL_KICK, new_ball_state)
        new_poss_team = torch.where(gk_mask, other_team, new_poss_team)
        nxt.ball_xy = torch.where(gk_mask.unsqueeze(-1), gk_xy, nxt.ball_xy)

        # Offside -> Indirect Free Kick for defending team
        is_offside = ((outcome == EventOutcome.OFFSIDE) | (action_type == ActionType.OFFSIDE)) & not_done
        new_ball_state = torch.where(is_offside, BallState.FREE_KICK_INDIRECT, new_ball_state)
        new_poss_team = torch.where(is_offside, other_team, new_poss_team)

        # Foul committed -> Penalty strictly when defending team commits FOUL_COMMITTED inside own penalty box
        is_foul = ((action_type == ActionType.FOUL_COMMITTED) | (outcome == EventOutcome.FOUL_CONCEDED)) & not_done
        in_home_box = (start_xy[:, 0] <= 16.5) & (start_xy[:, 1] >= 20.0) & (start_xy[:, 1] <= 60.0)
        in_away_box = (start_xy[:, 0] >= 103.5) & (start_xy[:, 1] >= 20.0) & (start_xy[:, 1] <= 60.0)
        defending_foul = (action_type == ActionType.FOUL_COMMITTED) & (actor_team != state.poss_team)
        foul_in_own_box = torch.where(actor_team == 1, in_home_box, in_away_box) & defending_foul
        pen_mask = is_foul & foul_in_own_box
        fk_mask = is_foul & (~foul_in_own_box)

        pen_x = torch.where(other_team == 1, 108.0, 12.0)
        pen_xy = torch.stack([pen_x, torch.full_like(pen_x, 40.0)], dim=-1)
        new_ball_state = torch.where(pen_mask, BallState.PENALTY, new_ball_state)
        new_poss_team = torch.where(pen_mask, other_team, new_poss_team)
        nxt.ball_xy = torch.where(pen_mask.unsqueeze(-1), pen_xy, nxt.ball_xy)

        new_ball_state = torch.where(fk_mask, BallState.FREE_KICK_DIRECT, new_ball_state)
        new_poss_team = torch.where(fk_mask, other_team, new_poss_team)
        nxt.ball_xy = torch.where(fk_mask.unsqueeze(-1), start_xy.clamp(0.0, PITCH_LENGTH), nxt.ball_xy)

        # Incomplete pass / lost duel / saved shot held by GK -> possession flips
        is_turnover = (
            ((action_type == ActionType.PASS) & (outcome == EventOutcome.INCOMPLETE))
            | ((action_type == ActionType.SHOT) & (outcome == EventOutcome.SAVED_HELD))
            | ((action_type == ActionType.TACKLE_DUEL) & (outcome == EventOutcome.LOST))
            | (action_type == ActionType.MISCONTROL)
            | (action_type == ActionType.DISPOSSESSED)
        ) & not_done
        new_poss_team = torch.where(is_turnover, other_team, new_poss_team)

        # Substitutions & pure cards preserve dead-ball restart state and ball position
        is_sub = (action_type == ActionType.SUBSTITUTION) & not_done
        nxt.subs_used[:, 0] = nxt.subs_used[:, 0] + (is_sub & (actor_team == 1)).long()
        nxt.subs_used[:, 1] = nxt.subs_used[:, 1] + (is_sub & (actor_team == 2)).long()
        preserve_restart = ((action_type == ActionType.SUBSTITUTION) | (action_type == ActionType.CARD)) & not_done
        new_ball_state = torch.where(preserve_restart, state.ball_state, new_ball_state)
        new_poss_team = torch.where(preserve_restart, state.poss_team, new_poss_team)
        nxt.ball_xy = torch.where(preserve_restart.unsqueeze(-1), state.ball_xy, nxt.ball_xy)
        nxt.ball_z = torch.where(preserve_restart, state.ball_z, nxt.ball_z)

        # 5. Period boundary transitions (1H -> 2H Kick-Off -> FT), branchless without host sync
        is_boundary = (action_type == ActionType.PERIOD_BOUNDARY) & not_done
        end_1h = is_boundary & (nxt.period == Period.FIRST_HALF)
        end_2h = is_boundary & (nxt.period == Period.SECOND_HALF)
        go_et = end_2h & nxt.extra_time_enabled & (nxt.home_score == nxt.away_score)
        go_ft = end_2h & (~go_et)
        end_et1 = is_boundary & (nxt.period == Period.ET1)
        end_et2 = is_boundary & (nxt.period == Period.ET2)

        # Apply 1H -> 2H, 2H -> ET1, ET1 -> ET2, 2H/ET2 -> FT
        nxt.period = torch.where(end_1h, Period.SECOND_HALF, nxt.period)
        nxt.period = torch.where(go_et, Period.ET1, nxt.period)
        nxt.period = torch.where(end_et1, Period.ET2, nxt.period)
        nxt.period = torch.where(go_ft | end_et2, Period.FULLTIME, nxt.period)

        reset_elapsed = end_1h | go_et | end_et1
        nxt.period_elapsed_ms = torch.where(reset_elapsed, 0, nxt.period_elapsed_ms)

        second_half_ko = torch.where(nxt.first_half_kickoff_team == 1, 2, 1)
        new_ball_state = torch.where(reset_elapsed, BallState.KICK_OFF, new_ball_state)
        new_ball_state = torch.where(go_ft | end_et2, BallState.FULLTIME, new_ball_state)
        new_poss_team = torch.where(end_1h | end_et1, second_half_ko, new_poss_team)
        new_poss_team = torch.where(go_et, nxt.first_half_kickoff_team, new_poss_team)
        nxt.ball_xy = torch.where(reset_elapsed.unsqueeze(-1), ko_xy, nxt.ball_xy)
        nxt.finished = nxt.finished | go_ft | end_et2

        # 6. Update possession phase & increment possession_phase_id on turnovers/restarts
        poss_changed = (new_poss_team != state.poss_team) | (new_ball_state != BallState.IN_PLAY)
        nxt.poss_phase_id = torch.where(not_done & poss_changed, nxt.poss_phase_id + 1, nxt.poss_phase_id)
        nxt.ball_state = torch.where(not_done, new_ball_state, nxt.ball_state)
        nxt.poss_team = torch.where(not_done, new_poss_team, nxt.poss_team)

        # Compute tactical PossPhase based on ball_x relative to possessing team & restart state
        x_att = torch.where(nxt.poss_team == 1, nxt.ball_xy[:, 0], PITCH_LENGTH - nxt.ball_xy[:, 0])
        open_phase = torch.where(
            x_att < 40.0,
            PossPhase.OPEN_BUILDUP,
            torch.where(x_att < 80.0, PossPhase.OPEN_PROGRESSION, PossPhase.OPEN_FINAL_THIRD),
        )
        phase = torch.where(
            nxt.ball_state == BallState.IN_PLAY,
            open_phase,
            torch.where(
                (nxt.ball_state == BallState.HALFTIME) | (nxt.ball_state == BallState.FULLTIME),
                PossPhase.DEAD_BALL,
                PossPhase.SET_PIECE_ATTACK,
            ),
        )
        nxt.poss_phase = torch.where(not_done, phase, nxt.poss_phase)

        return nxt
