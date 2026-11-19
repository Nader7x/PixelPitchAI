"""
Historical Player Capability & Role Profiler (`player_profiles_v2`) and
Team-Match Tactical Manifold (`tactical_manifold_v2.pkl`).

Implements Pillar 1 §4:
- Minutes & positional spell tracking with anti-leakage `as_of_date` filtering
- 540-day exponential recency decay + Empirical-Bayes shrinkage (`m0 = 900` min)
- Precision-weighted fusion with 2022-2026 scraped scouting/physical feeds
- 24 core attributes (`attributes_24d`), epistemic uncertainty (`sigma_24d`),
  8 GK/specialist attributes (`gk_attributes_8d`), `finishing_log_odds`,
  22 canonical `PlayerRole` suitability scores, 24x16 touch/press heatmaps,
  and L2-normalized `latent_style_64d` (+ `attr24_to_style64` linear adapter).
- Team-half `TeamMatchTactics` extractor + `TacticalManifoldV2` (Ledoit-Wolf Mahalanobis + k-NN).
"""

from __future__ import annotations

import math
import pickle
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import numpy as np
from sklearn.covariance import LedoitWolf
from sklearn.decomposition import TruncatedSVD
from sklearn.linear_model import Ridge
from sklearn.neighbors import NearestNeighbors

from statsbombapi_v2.schemas import (
    ActionType,
    BodyPart,
    EventGoldV2,
    EventOutcome,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PlayerProfileV2,
    PlayerRole,
    PossPhase,
    RestartType,
    TeamMatchTactics,
    XT_GRID_COLS,
    XT_GRID_ROWS,
)

# Canonical positional priors (mu, tau) across 5 macro groups for Empirical Bayes shrinkage
MACRO_POSITION_PRIORS: Dict[str, Tuple[np.ndarray, np.ndarray]] = {
    "GK": (
        np.array(
            [
                0.55, 0.25, 0.45, 0.10, 0.05, 0.55,
                0.05, 0.40, 0.15, 0.70,
                0.02, 0.02, 0.50, 0.02, 0.35,
                0.10, 0.35, 0.35, 0.55, 0.20, 0.25,
                0.65, 0.25, 0.20,
            ],
            dtype=np.float64,
        ),
        np.full(24, 0.14, dtype=np.float64),
    ),
    "DEF": (
        np.array(
            [
                0.65, 0.48, 0.50, 0.25, 0.35, 0.55,
                0.28, 0.52, 0.45, 0.62,
                0.18, 0.15, 0.48, 0.42, 0.42,
                0.48, 0.62, 0.65, 0.62, 0.45, 0.55,
                0.62, 0.55, 0.35,
            ],
            dtype=np.float64,
        ),
        np.full(24, 0.16, dtype=np.float64),
    ),
    "MID": (
        np.array(
            [
                0.68, 0.65, 0.55, 0.58, 0.45, 0.62,
                0.48, 0.56, 0.58, 0.64,
                0.35, 0.48, 0.50, 0.30, 0.52,
                0.62, 0.55, 0.55, 0.45, 0.48, 0.52,
                0.65, 0.58, 0.55,
            ],
            dtype=np.float64,
        ),
        np.full(24, 0.16, dtype=np.float64),
    ),
    "FWD": (
        np.array(
            [
                0.55, 0.42, 0.32, 0.52, 0.55, 0.54,
                0.65, 0.55, 0.62, 0.52,
                0.72, 0.58, 0.54, 0.55, 0.55,
                0.55, 0.38, 0.32, 0.44, 0.42, 0.64,
                0.58, 0.66, 0.48,
            ],
            dtype=np.float64,
        ),
        np.full(24, 0.17, dtype=np.float64),
    ),
}

# Canonical ideal attribute/spatial archetypes over the 22 PlayerRole enums (0..21)
# Columns: [x_att_norm, y_abs_center_norm, prog_pass, dribble, box_shot, press_work, tackle_int, aerial]
ROLE_PROTOTYPES_8D: Dict[PlayerRole, np.ndarray] = {
    PlayerRole.SWEEPER_KEEPER: np.array([0.16, 0.12, 0.45, 0.10, 0.02, 0.25, 0.50, 0.55]),
    PlayerRole.SHOT_STOPPER_GK: np.array([0.10, 0.10, 0.25, 0.05, 0.01, 0.10, 0.30, 0.55]),
    PlayerRole.BALL_PLAYING_CB: np.array([0.34, 0.25, 0.65, 0.32, 0.12, 0.45, 0.68, 0.70]),
    PlayerRole.STOPPER_CB: np.array([0.30, 0.22, 0.35, 0.18, 0.10, 0.52, 0.76, 0.78]),
    PlayerRole.WIDE_CENTER_BACK: np.array([0.38, 0.52, 0.55, 0.42, 0.14, 0.55, 0.66, 0.65]),
    PlayerRole.INVERTED_FULLBACK: np.array([0.50, 0.45, 0.64, 0.45, 0.22, 0.60, 0.60, 0.45]),
    PlayerRole.OVERLAPPING_FULLBACK: np.array([0.55, 0.80, 0.58, 0.55, 0.22, 0.58, 0.58, 0.45]),
    PlayerRole.DEFENSIVE_FULLBACK: np.array([0.40, 0.72, 0.42, 0.32, 0.12, 0.55, 0.68, 0.55]),
    PlayerRole.COMPLETE_WING_BACK: np.array([0.60, 0.84, 0.62, 0.62, 0.30, 0.62, 0.55, 0.44]),
    PlayerRole.DEEP_LYING_PLAYMAKER: np.array([0.48, 0.22, 0.82, 0.42, 0.24, 0.52, 0.60, 0.42]),
    PlayerRole.DESTROYER_DM: np.array([0.45, 0.26, 0.48, 0.32, 0.18, 0.74, 0.78, 0.60]),
    PlayerRole.BOX_TO_BOX_MIDFIELDER: np.array([0.56, 0.35, 0.64, 0.52, 0.42, 0.72, 0.62, 0.50]),
    PlayerRole.MEZZALA: np.array([0.62, 0.48, 0.68, 0.62, 0.48, 0.62, 0.48, 0.40]),
    PlayerRole.CARRILERO: np.array([0.54, 0.54, 0.60, 0.46, 0.32, 0.68, 0.60, 0.46]),
    PlayerRole.ADVANCED_PLAYMAKER: np.array([0.68, 0.28, 0.78, 0.66, 0.52, 0.48, 0.34, 0.30]),
    PlayerRole.INVERTED_WINGER: np.array([0.74, 0.66, 0.56, 0.78, 0.64, 0.50, 0.32, 0.30]),
    PlayerRole.TOUCHLINE_WINGER: np.array([0.72, 0.86, 0.54, 0.76, 0.46, 0.50, 0.32, 0.30]),
    PlayerRole.INSIDE_FORWARD: np.array([0.80, 0.48, 0.44, 0.70, 0.78, 0.52, 0.28, 0.42]),
    PlayerRole.FALSE_9: np.array([0.72, 0.26, 0.66, 0.68, 0.64, 0.54, 0.30, 0.36]),
    PlayerRole.PRESSING_FORWARD: np.array([0.78, 0.32, 0.40, 0.52, 0.68, 0.82, 0.46, 0.50]),
    PlayerRole.TARGET_FORWARD: np.array([0.82, 0.22, 0.34, 0.36, 0.74, 0.44, 0.28, 0.84]),
    PlayerRole.POACHER: np.array([0.86, 0.20, 0.26, 0.42, 0.88, 0.42, 0.22, 0.58]),
}

ROLE_TO_MACRO_POS: Dict[PlayerRole, str] = {
    PlayerRole.SWEEPER_KEEPER: "GK",
    PlayerRole.SHOT_STOPPER_GK: "GK",
    PlayerRole.BALL_PLAYING_CB: "DEF",
    PlayerRole.STOPPER_CB: "DEF",
    PlayerRole.WIDE_CENTER_BACK: "DEF",
    PlayerRole.INVERTED_FULLBACK: "DEF",
    PlayerRole.OVERLAPPING_FULLBACK: "DEF",
    PlayerRole.DEFENSIVE_FULLBACK: "DEF",
    PlayerRole.COMPLETE_WING_BACK: "DEF",
    PlayerRole.DEEP_LYING_PLAYMAKER: "MID",
    PlayerRole.DESTROYER_DM: "MID",
    PlayerRole.BOX_TO_BOX_MIDFIELDER: "MID",
    PlayerRole.MEZZALA: "MID",
    PlayerRole.CARRILERO: "MID",
    PlayerRole.ADVANCED_PLAYMAKER: "MID",
    PlayerRole.INVERTED_WINGER: "FWD",
    PlayerRole.TOUCHLINE_WINGER: "FWD",
    PlayerRole.INSIDE_FORWARD: "FWD",
    PlayerRole.FALSE_9: "FWD",
    PlayerRole.PRESSING_FORWARD: "FWD",
    PlayerRole.TARGET_FORWARD: "FWD",
    PlayerRole.POACHER: "FWD",
}

MACRO_POS_TO_CODE: Dict[str, str] = {
    "GK": "GK",
    "DEF": "CB",
    "MID": "CM",
    "FWD": "ST",
}


def _parse_iso_date(date_str: Optional[str]) -> Optional[datetime]:
    if not date_str:
        return None
    for fmt in ("%Y-%m-%d", "%Y-%m-%dT%H:%M:%S", "%Y/%m/%d"):
        try:
            return datetime.strptime(str(date_str)[:19], fmt)
        except ValueError:
            continue
    return None


def _clip01(x: float) -> float:
    return float(min(max(x, 0.0), 1.0))


class HistoricalPlayerProfiler:
    """
    Computes Empirical-Bayes player capability profiles (`attributes_24d`, `sigma_24d`,
    `gk_attributes_8d`, `finishing_log_odds`, `role_suitability_scores`, 24x16 heatmaps,
    and `latent_style_64d`) from Gold event sequences + optional 2022-2026 scraped feeds.
    """

    def __init__(
        self,
        shrinkage_m0_minutes: float = 900.0,
        decay_half_life_days: float = 540.0,
    ) -> None:
        self.m0 = float(shrinkage_m0_minutes)
        self.half_life_days = float(decay_half_life_days)
        self._attr_to_style_weights: Optional[np.ndarray] = None
        self._attr_to_style_bias: Optional[np.ndarray] = None

    @staticmethod
    def _deterministic_style_basis(feat_vec: np.ndarray) -> np.ndarray:
        """
        Deterministic orthogonal random-feature + structured projection fallback into R^64
        when `TruncatedSVD` has fewer than 64 sample players. Always L2-normalized.
        """
        rng = np.random.default_rng(202601)
        proj = rng.standard_normal((len(feat_vec), 64)) / math.sqrt(len(feat_vec))
        z = feat_vec @ proj
        norm = float(np.linalg.norm(z))
        return (z / norm) if norm > 1e-8 else np.eye(1, 64)[0]

    def fit_style_projection(self, feature_matrix: np.ndarray, attr24_matrix: np.ndarray) -> np.ndarray:
        """
        Fit L2-normalized 64d latent style embeddings via TruncatedSVD (or orthogonal fallback)
        plus a Ridge regression adapter `attr24_to_style64` for synthetic PlayerAttributeOverride.
        """
        n_samples, n_feats = feature_matrix.shape
        if n_samples >= 64 and n_feats >= 64:
            svd = TruncatedSVD(n_components=64, random_state=42)
            styles = svd.fit_transform(feature_matrix)
        else:
            rng = np.random.default_rng(202601)
            proj = rng.standard_normal((n_feats, 64)) / math.sqrt(max(n_feats, 1))
            styles = feature_matrix @ proj

        norms = np.linalg.norm(styles, axis=1, keepdims=True)
        norms = np.where(norms > 1e-8, norms, 1.0)
        styles = styles / norms

        if n_samples >= 4:
            ridge = Ridge(alpha=1.0)
            ridge.fit(attr24_matrix, styles)
            self._attr_to_style_weights = ridge.coef_.T.astype(np.float64)  # [24, 64]
            self._attr_to_style_bias = ridge.intercept_.astype(np.float64)  # [64]
        return styles

    def predict_style_from_attributes(self, attributes_24d: Sequence[float]) -> List[float]:
        """Adapter equivalent to `attr24_to_style64.onnx` for unseen/custom players."""
        a = np.asarray(attributes_24d, dtype=np.float64)
        if self._attr_to_style_weights is not None and self._attr_to_style_bias is not None:
            z = a @ self._attr_to_style_weights + self._attr_to_style_bias
        else:
            z = self._deterministic_style_basis(a)
        norm = float(np.linalg.norm(z))
        return ((z / norm) if norm > 1e-8 else np.eye(1, 64)[0]).tolist()

    def build_profiles(
        self,
        events: Sequence[EventGoldV2],
        match_dates: Optional[Dict[int, str]] = None,
        match_minutes_override: Optional[Dict[Tuple[int, int], float]] = None,
        player_names: Optional[Dict[int, str]] = None,
        player_teams: Optional[Dict[int, int]] = None,
        scraped_records: Optional[Sequence[Dict[str, Any]]] = None,
        season_label: str = "career_latest",
        as_of_date: Optional[str] = None,
    ) -> List[PlayerProfileV2]:
        """
        Aggregate per-90 rates, Empirical-Bayes attributes, 24x16 zone heatmaps,
        and 22-role suitability scores across events (respecting `as_of_date` anti-leakage).
        """
        as_of_dt = _parse_iso_date(as_of_date)
        match_dates = match_dates or {}
        player_names = player_names or {}
        player_teams = player_teams or {}

        # Filter events strictly before `as_of_date` if provided
        filtered_events: List[EventGoldV2] = []
        match_weights: Dict[int, float] = {}
        match_max_min: Dict[int, float] = defaultdict(lambda: 90.0)
        player_match_span: Dict[Tuple[int, int], List[float]] = {}
        player_match_roles: Dict[Tuple[int, int], List[int]] = defaultdict(list)

        for ev in events:
            m_dt = _parse_iso_date(match_dates.get(ev.match_id))
            if as_of_dt is not None and m_dt is not None and m_dt >= as_of_dt:
                continue
            filtered_events.append(ev)
            if ev.match_id not in match_weights:
                if as_of_dt is not None and m_dt is not None:
                    age_days = max(0.0, (as_of_dt - m_dt).days)
                    match_weights[ev.match_id] = math.exp(-math.log(2.0) * age_days / self.half_life_days)
                else:
                    match_weights[ev.match_id] = 1.0
            match_max_min[ev.match_id] = max(match_max_min[ev.match_id], float(ev.minute))

            if ev.actor_player_id > 0:
                key = (ev.actor_player_id, ev.match_id)
                if key not in player_match_span:
                    player_match_span[key] = [float(ev.minute), float(ev.minute)]
                else:
                    player_match_span[key][0] = min(player_match_span[key][0], float(ev.minute))
                    player_match_span[key][1] = max(player_match_span[key][1], float(ev.minute))
                if 1 <= ev.actor_slot <= 22:
                    player_match_roles[key].append(ev.active_slots_roles[ev.actor_slot - 1])

        # Accumulate per-player stats
        stats: Dict[int, Dict[str, Any]] = defaultdict(
            lambda: {
                "weighted_minutes": 0.0,
                "raw_minutes": 0.0,
                "role_counts": defaultdict(float),
                "pass_short_att": 0.0,
                "pass_short_cmp": 0.0,
                "pass_short_xpass": 0.0,
                "pass_prog": 0.0,
                "pass_switch": 0.0,
                "pass_through": 0.0,
                "pass_cross": 0.0,
                "pass_press_att": 0.0,
                "pass_press_cmp": 0.0,
                "pass_press_xpass": 0.0,
                "dribble_att": 0.0,
                "dribble_cmp": 0.0,
                "carry_prog_yd": 0.0,
                "touches": 0.0,
                "touch_errors": 0.0,
                "shots_box": 0.0,
                "shots_long": 0.0,
                "goals": 0.0,
                "xg_sum": 0.0,
                "psxg_sum": 0.0,
                "headed_xg": 0.0,
                "left_touches": 0.0,
                "right_touches": 0.0,
                "pressures": 0.0,
                "tackles_att": 0.0,
                "tackles_won": 0.0,
                "interceptions_rec": 0.0,
                "aerial_att": 0.0,
                "aerial_won": 0.0,
                "fouls": 0.0,
                "def_actions": 0.0,
                "fast_carries": 0.0,
                "late_actions": 0.0,
                "early_actions": 0.0,
                "set_piece_xt": 0.0,
                "gk_saves": 0.0,
                "gk_saves_held": 0.0,
                "gk_sweeper": 0.0,
                "gk_passes": 0.0,
                "gk_short_passes": 0.0,
                "gk_long_att": 0.0,
                "gk_long_cmp": 0.0,
                "gk_psxg_prevented": 0.0,
                "touch_grid": np.zeros((XT_GRID_COLS, XT_GRID_ROWS), dtype=np.float64),
                "press_grid": np.zeros((XT_GRID_COLS, XT_GRID_ROWS), dtype=np.float64),
                "x_att_sum": 0.0,
                "y_canon_sum": 0.0,
                "pass_lengths": [],
                "pass_fwd": 0.0,
                "pass_total": 0.0,
                "carry_count": 0.0,
                "carry_len_sum": 0.0,
                "shot_central": 0.0,
                "shot_first_time": 0.0,
                "shot_dist_sum": 0.0,
            }
        )

        # Compute weighted minutes per player
        for (pid, mid), (m_start, m_end) in player_match_span.items():
            w = match_weights[mid]
            if match_minutes_override and (pid, mid) in match_minutes_override:
                mins = float(match_minutes_override[(pid, mid)])
            else:
                # If started early (<15 min) assume starter until last touch or 90 min
                span = max(15.0, (match_max_min[mid] if m_start <= 10.0 and m_end >= 60.0 else (m_end - m_start + 5.0)))
                mins = min(match_max_min[mid], span)
            stats[pid]["raw_minutes"] += mins
            stats[pid]["weighted_minutes"] += mins * w
            for r in player_match_roles[(pid, mid)]:
                stats[pid]["role_counts"][r] += w

        # Accumulate event statistics
        for ev in filtered_events:
            pid = ev.actor_player_id
            if pid <= 0:
                continue
            w = match_weights[ev.match_id]
            s = stats[pid]
            s["touches"] += w
            s["x_att_sum"] += ev.x_att * w
            s["y_canon_sum"] += ev.y_canon * w
            if ev.body_part == BodyPart.LEFT_FOOT:
                s["left_touches"] += w
            elif ev.body_part == BodyPart.RIGHT_FOOT:
                s["right_touches"] += w

            cx = min(max(int(ev.x_att // 5.0), 0), XT_GRID_COLS - 1)
            cy = min(max(int(ev.y_att // 5.0), 0), XT_GRID_ROWS - 1)
            s["touch_grid"][cx, cy] += w

            if 15 <= ev.minute <= 45:
                s["early_actions"] += w
            elif ev.minute >= 70:
                s["late_actions"] += w

            if ev.action_type == ActionType.PASS:
                s["pass_total"] += w
                s["pass_lengths"].append(ev.distance_yd)
                if ev.end_x_att > ev.x_att + 3.0:
                    s["pass_fwd"] += w
                if ev.distance_yd < 15.0:
                    s["pass_short_att"] += w
                    s["pass_short_xpass"] += ev.xpass_prob * w
                    if ev.outcome == EventOutcome.COMPLETE:
                        s["pass_short_cmp"] += w
                if ev.end_x_att - ev.x_att >= 10.0 and ev.outcome == EventOutcome.COMPLETE:
                    s["pass_prog"] += w
                if ev.pass_switch:
                    s["pass_switch"] += w
                if ev.through_ball:
                    s["pass_through"] += w
                if ev.cross or ev.cutback:
                    s["pass_cross"] += w
                if ev.under_pressure:
                    s["pass_press_att"] += w
                    s["pass_press_xpass"] += ev.xpass_prob * w
                    if ev.outcome == EventOutcome.COMPLETE:
                        s["pass_press_cmp"] += w
                if ev.restart_type in (RestartType.CORNER, RestartType.FREE_KICK_INDIRECT):
                    s["set_piece_xt"] += max(0.0, ev.xt_delta) * w
                if ev.actor_slot in (1, 12):
                    s["gk_passes"] += w
                    if ev.distance_yd < 20.0:
                        s["gk_short_passes"] += w
                    else:
                        s["gk_long_att"] += w
                        if ev.outcome == EventOutcome.COMPLETE:
                            s["gk_long_cmp"] += w

            elif ev.action_type == ActionType.CARRY:
                s["carry_count"] += w
                s["carry_len_sum"] += ev.distance_yd * w
                prog = max(0.0, ev.end_x_att - ev.x_att)
                s["carry_prog_yd"] += prog * w
                if ev.distance_yd >= 15.0:
                    s["fast_carries"] += w

            elif ev.action_type == ActionType.DRIBBLE:
                s["dribble_att"] += w
                if ev.outcome == EventOutcome.COMPLETE:
                    s["dribble_cmp"] += w

            elif ev.action_type in (ActionType.MISCONTROL, ActionType.DISPOSSESSED):
                s["touch_errors"] += w

            elif ev.action_type == ActionType.SHOT:
                if ev.x_att >= 102.0:
                    s["shots_box"] += w
                else:
                    s["shots_long"] += w
                xg_val = ev.xg_v2_pre_shot if ev.xg_v2_pre_shot > 0 else (ev.xg_statsbomb or 0.08)
                s["xg_sum"] += xg_val * w
                s["psxg_sum"] += ev.psxg_v2 * w
                if ev.outcome == EventOutcome.GOAL:
                    s["goals"] += w
                if ev.body_part == BodyPart.HEAD:
                    s["headed_xg"] += xg_val * w
                if 28.0 <= ev.y_att <= 52.0:
                    s["shot_central"] += w
                if ev.first_time:
                    s["shot_first_time"] += w
                s["shot_dist_sum"] += math.hypot(PITCH_LENGTH_YD - ev.x_att, 40.0 - ev.y_att) * w

            elif ev.action_type == ActionType.PRESSURE:
                s["pressures"] += w
                s["def_actions"] += w
                s["press_grid"][cx, cy] += w

            elif ev.action_type == ActionType.TACKLE_DUEL:
                s["tackles_att"] += w
                s["def_actions"] += w
                s["press_grid"][cx, cy] += w
                if ev.outcome == EventOutcome.WON:
                    s["tackles_won"] += w
                if ev.aerial_won:
                    s["aerial_att"] += w
                    s["aerial_won"] += w

            elif ev.action_type in (ActionType.INTERCEPTION, ActionType.BALL_RECOVERY):
                s["interceptions_rec"] += w
                s["def_actions"] += w
                s["press_grid"][cx, cy] += w
                if ev.actor_slot in (1, 12) and ev.x_att > 18.0:
                    s["gk_sweeper"] += w

            elif ev.action_type == ActionType.FOUL_COMMITTED:
                s["fouls"] += w
                s["def_actions"] += w

            elif ev.action_type == ActionType.SAVE:
                s["gk_saves"] += w
                if ev.outcome == EventOutcome.SAVED_HELD:
                    s["gk_saves_held"] += w
                s["gk_psxg_prevented"] += 0.25 * w

        # Optional scraped enrichment lookup indexed by player_id
        scraped_by_pid: Dict[int, Dict[str, Any]] = {}
        if scraped_records:
            for rec in scraped_records:
                if rec.get("player_id") is not None:
                    scraped_by_pid[int(rec["player_id"])] = rec

        # Build unprojected profiles first so TruncatedSVD can fit latent_style_64d across pool
        raw_profiles: List[Dict[str, Any]] = []
        style_input_rows: List[np.ndarray] = []
        attr24_rows: List[np.ndarray] = []

        all_pids = sorted(set(stats.keys()) | set(scraped_by_pid.keys()))
        for pid in all_pids:
            s = stats[pid]
            m_p = max(0.0, float(s["weighted_minutes"]))
            ninety = max(m_p / 90.0, 1e-3) if m_p > 0 else 1.0

            # Determine dominant macro position from observed slot roles
            if s["role_counts"]:
                top_role = PlayerRole(max(s["role_counts"], key=lambda r: s["role_counts"][r]))
            else:
                top_role = PlayerRole.BOX_TO_BOX_MIDFIELDER
            macro_pos = ROLE_TO_MACRO_POS.get(top_role, "MID")
            mu_prior, tau_prior = MACRO_POSITION_PRIORS[macro_pos]

            # Raw normalized [0,1] estimates before Empirical-Bayes shrinkage
            touches = max(s["touches"], 1.0)
            short_att = max(s["pass_short_att"], 1.0)
            press_att = max(s["pass_press_att"], 1.0)
            drib_att = max(s["dribble_att"], 1.0)
            tkl_att = max(s["tackles_att"], 1.0)
            aer_att = max(s["aerial_att"], 1.0)
            shots_tot = max(s["shots_box"] + s["shots_long"], 1.0)

            short_res = (s["pass_short_cmp"] - s["pass_short_xpass"]) / short_att
            press_res = (s["pass_press_cmp"] - s["pass_press_xpass"]) / press_att
            fin_delta = (s["goals"] - s["xg_sum"]) / max(shots_tot, 5.0)
            fin_log_odds = float(np.clip((s["goals"] - s["xg_sum"]) / max(shots_tot + 10.0, 10.0) * 1.5, -0.45, 0.45))

            raw_24 = np.array(
                [
                    _clip01(0.65 + short_res * 1.5),
                    _clip01((s["pass_prog"] / ninety) / 10.0),
                    _clip01((s["pass_switch"] / ninety) / 3.0),
                    _clip01((s["pass_through"] / ninety) / 1.5),
                    _clip01((s["pass_cross"] / ninety) / 5.0),
                    _clip01(0.60 + press_res * 1.5),
                    _clip01((s["dribble_att"] / ninety) / 5.0),
                    _clip01(s["dribble_cmp"] / drib_att if s["dribble_att"] > 0 else mu_prior[7]),
                    _clip01((s["carry_prog_yd"] / ninety) / 220.0),
                    _clip01(1.0 - (s["touch_errors"] / touches) * 5.0),
                    _clip01((s["shots_box"] / ninety) / 3.2),
                    _clip01((s["shots_long"] / ninety) / 1.8),
                    _clip01(0.50 + fin_delta * 1.2),
                    _clip01((s["headed_xg"] / ninety) / 0.25),
                    _clip01((min(s["left_touches"], s["right_touches"]) / max(s["left_touches"] + s["right_touches"], 1.0)) * 2.0),
                    _clip01((s["pressures"] / ninety) / 24.0),
                    _clip01(s["tackles_won"] / tkl_att if s["tackles_att"] > 0 else mu_prior[16]),
                    _clip01((s["interceptions_rec"] / ninety) / 10.0),
                    _clip01(s["aerial_won"] / aer_att if s["aerial_att"] > 0 else mu_prior[18]),
                    _clip01((s["fouls"] / max(s["def_actions"], 1.0)) * 2.5),
                    _clip01((s["fast_carries"] / ninety) / 4.0),
                    _clip01(0.55 + 0.35 * ((s["late_actions"] + 1.0) / max(s["early_actions"] + 1.0, 1.0) - 0.5)),
                    _clip01((s["fast_carries"] / ninety) / 3.5),
                    _clip01((s["set_piece_xt"] / ninety) / 0.12),
                ],
                dtype=np.float64,
            )

            # Empirical-Bayes shrinkage toward positional peer priors (m0 = 900 min)
            weight_emp = m_p / (m_p + self.m0) if m_p > 0 else 0.0
            attr_24 = weight_emp * raw_24 + (1.0 - weight_emp) * mu_prior
            sigma_24 = tau_prior / math.sqrt(1.0 + m_p / self.m0)
            provenance = 1 if m_p > 0 else 0

            # Optional precision-weighted fusion with 2022-2026 scraped scouting/physical record
            scr = scraped_by_pid.get(pid)
            if scr is not None:
                provenance |= 2
                scr_mins = float(scr.get("minutes_played", 900.0))
                scr_w = scr_mins / (scr_mins + self.m0)
                if m_p == 0:
                    # Scraped-only post-2022 player: 1.15x epistemic uncertainty floor
                    sigma_24 = sigma_24 * 1.15
                if scr.get("prog_passes_per90") is not None:
                    attr_24[1] = _clip01(0.5 * attr_24[1] + 0.5 * _clip01(float(scr["prog_passes_per90"]) / 10.0))
                if scr.get("sprint_recovery_prior") is not None:
                    attr_24[22] = _clip01(0.5 * attr_24[22] + 0.5 * float(scr["sprint_recovery_prior"]))
                if scr.get("stamina_prior") is not None:
                    attr_24[21] = _clip01(0.5 * attr_24[21] + 0.5 * float(scr["stamina_prior"]))
                m_p = max(m_p, scr_mins * scr_w)

            # GK & spatial specialist 8d
            mean_x_att_norm = _clip01((s["x_att_sum"] / touches) / PITCH_LENGTH_YD) if s["touches"] > 0 else 0.5
            mean_y_canon_norm = _clip01((s["y_canon_sum"] / touches) / PITCH_WIDTH_YD) if s["touches"] > 0 else 0.5
            if macro_pos == "GK":
                gk_saves = max(s["gk_saves"], 1.0)
                gk_p = max(s["gk_passes"], 1.0)
                gk_long = max(s["gk_long_att"], 1.0)
                gk_8 = np.array(
                    [
                        _clip01(0.50 + s["gk_psxg_prevented"] / max(ninety * 2.0, 1.0)),
                        _clip01(s["gk_saves_held"] / gk_saves if s["gk_saves"] > 0 else 0.55),
                        _clip01((s["gk_sweeper"] / ninety) / 2.5),
                        0.55,
                        _clip01(s["gk_short_passes"] / gk_p),
                        _clip01(s["gk_long_cmp"] / gk_long if s["gk_long_att"] > 0 else 0.55),
                        mean_y_canon_norm,
                        mean_x_att_norm,
                    ],
                    dtype=np.float64,
                )
            else:
                gk_8 = np.array([0.5, 0.5, 0.5, 0.5, 0.5, 0.5, mean_y_canon_norm, mean_x_att_norm], dtype=np.float64)

            # Normalized 24x16 touch & pressing heatmaps
            t_grid = s["touch_grid"]
            t_sum = float(t_grid.sum())
            touch_hm = (t_grid / t_sum).ravel() if t_sum > 0 else np.full(384, 1.0 / 384.0)

            p_grid = s["press_grid"]
            p_sum = float(p_grid.sum())
            press_hm = (p_grid / p_sum).ravel() if p_sum > 0 else np.full(384, 1.0 / 384.0)

            # Behavioral summary vectors (pass 16d, carry 8d, shot 8d)
            plen = np.array(s["pass_lengths"], dtype=np.float64) if s["pass_lengths"] else np.array([15.0])
            pass_16 = np.zeros(16, dtype=np.float64)
            pass_16[0] = _clip01(float(np.mean(plen)) / 40.0)
            pass_16[1] = _clip01(float(np.percentile(plen, 25)) / 40.0)
            pass_16[2] = _clip01(float(np.percentile(plen, 75)) / 40.0)
            pass_16[3] = _clip01(s["pass_fwd"] / max(s["pass_total"], 1.0))
            pass_16[4:9] = attr_24[1:6]

            carry_8 = np.zeros(8, dtype=np.float64)
            carry_8[0] = _clip01((s["carry_count"] / touches))
            carry_8[1] = _clip01((s["carry_len_sum"] / max(s["carry_count"], 1.0)) / 25.0)
            carry_8[2:4] = attr_24[7:9]

            shot_8 = np.zeros(8, dtype=np.float64)
            shot_8[0] = _clip01((s["shots_box"] + s["shots_long"]) / touches * 5.0)
            shot_8[1] = _clip01(s["shot_central"] / shots_tot)
            shot_8[2] = _clip01((s["shot_dist_sum"] / shots_tot) / 35.0)
            shot_8[3] = _clip01(s["shot_first_time"] / shots_tot)
            shot_8[4:6] = attr_24[10:12]

            # 22-role suitability scores via Gaussian RBF over 8D behavioral prototype
            y_center_dist_norm = abs(mean_y_canon_norm - 0.5) * 2.0
            player_8d = np.array(
                [
                    mean_x_att_norm,
                    y_center_dist_norm,
                    attr_24[1],
                    attr_24[6],
                    attr_24[10],
                    attr_24[15],
                    0.5 * (attr_24[16] + attr_24[17]),
                    attr_24[18],
                ],
                dtype=np.float64,
            )
            role_scores = np.zeros(22, dtype=np.float64)
            for role_enum, proto in ROLE_PROTOTYPES_8D.items():
                d2 = float(np.sum((player_8d - proto) ** 2))
                role_scores[int(role_enum)] = math.exp(-d2 / 0.25)
            # Boost observed role slightly
            role_scores[int(top_role)] = min(1.0, role_scores[int(top_role)] + 0.10)
            ranked_roles = [PlayerRole(int(i)) for i in np.argsort(-role_scores)[:3]]

            pref_foot = "Right"
            if s["left_touches"] > s["right_touches"] * 1.35:
                pref_foot = "Left"
            elif min(s["left_touches"], s["right_touches"]) > 0.42 * max(s["left_touches"] + s["right_touches"], 1.0):
                pref_foot = "Both"
            if scr and scr.get("preferred_foot"):
                pref_foot = str(scr["preferred_foot"])

            combined_feat = np.concatenate([touch_hm, press_hm, pass_16, carry_8, shot_8, attr_24, gk_8])
            style_input_rows.append(combined_feat)
            attr24_rows.append(attr_24)
            raw_profiles.append(
                {
                    "player_id": int(pid),
                    "season_id": season_label,
                    "player_name": player_names.get(pid, scr.get("player_name", "") if scr else ""),
                    "team_id": player_teams.get(pid),
                    "as_of_date": as_of_date,
                    "minutes_played": float(m_p),
                    "primary_position": MACRO_POS_TO_CODE.get(macro_pos, "CM"),
                    "preferred_foot": pref_foot,
                    "natural_roles": ranked_roles,
                    "role_suitability_scores": role_scores.tolist(),
                    "attributes_24d": attr_24.tolist(),
                    "sigma_24d": sigma_24.tolist(),
                    "gk_attributes_8d": gk_8.tolist(),
                    "finishing_log_odds": fin_log_odds,
                    "touch_heatmap_24x16": touch_hm.tolist(),
                    "pressing_zone_heatmap_24x16": press_hm.tolist(),
                    "pass_vector_summary_16d": pass_16.tolist(),
                    "carry_tendency_8d": carry_8.tolist(),
                    "shot_tendency_8d": shot_8.tolist(),
                    "source_provenance_flags": provenance,
                }
            )

        if not raw_profiles:
            return []

        styles = self.fit_style_projection(np.stack(style_input_rows), np.stack(attr24_rows))
        profiles: List[PlayerProfileV2] = []
        for idx, rp in enumerate(raw_profiles):
            rp["latent_style_64d"] = styles[idx].tolist()
            profiles.append(PlayerProfileV2(**rp))
        return profiles


def extract_team_match_tactics(
    events: Sequence[EventGoldV2],
    match_id: int,
    team_id: int,
    half: int = 1,
    squad_elo: float = 1500.0,
    opponent_elo: float = 1500.0,
) -> TeamMatchTactics:
    """Compute observed 10-slider `TacticalInstructions` vector from historical team-half events."""
    half_events = [
        ev
        for ev in events
        if ev.match_id == match_id
        and ((half == 1 and ev.minute < 45) or (half == 2 and ev.minute >= 45))
    ]
    team_evs = [ev for ev in half_events if ev.actor_team_id == team_id]
    opp_evs = [ev for ev in half_events if ev.actor_team_id != team_id and ev.actor_team_id > 0]

    def_actions = [
        ev
        for ev in team_evs
        if ev.action_type
        in (ActionType.PRESSURE, ActionType.TACKLE_DUEL, ActionType.INTERCEPTION, ActionType.CLEARANCE)
        and ev.actor_slot not in (1, 12)
    ]
    def_xs = [ev.x_att for ev in def_actions] or [42.0]
    # Defensive line height normalized (25 yd -> 0.1, 65 yd -> 0.9)
    line_height = _clip01((float(np.percentile(def_xs, 20)) - 20.0) / 45.0)

    # PPDA: opponent passes in opp own 60% / team defensive actions in opp 60%
    opp_buildup_passes = sum(1 for ev in opp_evs if ev.action_type == ActionType.PASS and ev.x_att <= 72.0)
    team_high_def = max(1, sum(1 for ev in def_actions if ev.x_att >= 48.0))
    ppda = opp_buildup_passes / team_high_def
    pressing_intensity = _clip01(1.0 - (ppda - 5.0) / 20.0)

    passes = [ev for ev in team_evs if ev.action_type == ActionType.PASS]
    dts = [max(0.4, ev.dt_sec) for ev in passes] or [2.5]
    tempo = _clip01((1.0 / float(np.median(dts))) / 1.0)

    pass_dists = [ev.distance_yd for ev in passes] or [15.0]
    prog_ratios = [max(0.0, ev.end_x_att - ev.x_att) / max(ev.distance_yd, 1.0) for ev in passes] or [0.4]
    long_share = sum(1 for d in pass_dists if d > 30.0) / max(len(pass_dists), 1)
    directness = _clip01(0.6 * float(np.mean(prog_ratios)) + 0.4 * long_share * 2.0)

    ys = [ev.y_canon for ev in team_evs if ev.x_att >= 40.0] or [40.0, 42.0]
    width = _clip01(float(np.std(ys)) / 28.0)

    def_ys = [ev.y_canon for ev in def_actions] or [40.0]
    spread = math.hypot(float(np.std(def_xs)), float(np.std(def_ys)))
    compactness = _clip01(1.0 - spread / 35.0)

    fast_trans = sum(1 for ev in team_evs if ev.poss_phase == PossPhase.TRANSITION_FAST)
    cp_trans = sum(1 for ev in team_evs if ev.poss_phase == PossPhase.TRANSITION_COUNTERPRESS)
    counter_press = _clip01(cp_trans / max(len(def_actions) * 0.25, 1.0))
    counter_urg = _clip01(fast_trans / max(len(team_evs) * 0.20, 1.0))

    xpasses = [ev.xpass_prob for ev in passes if ev.xpass_prob > 0]
    risk = _clip01(1.0 - float(np.mean(xpasses))) if xpasses else 0.45

    return TeamMatchTactics(
        match_id=match_id,
        team_id=team_id,
        half=half,
        squad_elo=squad_elo,
        opponent_elo=opponent_elo,
        defensive_line_height=line_height,
        pressing_intensity=pressing_intensity,
        build_up_tempo=tempo,
        directness=directness,
        attacking_width=width,
        defensive_compactness=compactness,
        counter_press_vs_regroup=counter_press,
        counter_attack_urgency=counter_urg,
        possession_risk_appetite=risk,
        game_state_adaptivity=0.55,
    )


class TacticalManifoldV2:
    """
    Empirical tactical manifold (`tactical_manifold_v2.pkl`) supporting Tier-1 OOD
    Mahalanobis + k-NN scoring (Module B) and Stage 2 IPW propensity weighting (Module C).
    """

    def __init__(self) -> None:
        self.mean_vec: np.ndarray = np.full(10, 0.5, dtype=np.float64)
        self.cov_matrix: np.ndarray = np.eye(10, dtype=np.float64) * 0.05
        self.precision_matrix: np.ndarray = np.eye(10, dtype=np.float64) * 20.0
        self.knn_index: Optional[NearestNeighbors] = None
        self.fitted_vectors: Optional[np.ndarray] = None
        self.propensity_model: Optional[Ridge] = None
        self.mahalanobis_p95: float = 4.2

    def fit(self, tactics_list: Sequence[TeamMatchTactics]) -> "TacticalManifoldV2":
        if not tactics_list:
            return self
        X = np.array([t.as_vector_10d() for t in tactics_list], dtype=np.float64)
        self.fitted_vectors = X
        self.mean_vec = np.mean(X, axis=0)
        if len(X) >= 5:
            lw = LedoitWolf().fit(X)
            self.cov_matrix = lw.covariance_.astype(np.float64)
            self.precision_matrix = np.linalg.pinv(self.cov_matrix + np.eye(10) * 1e-4)
        else:
            self.cov_matrix = np.eye(10, dtype=np.float64) * 0.05
            self.precision_matrix = np.eye(10, dtype=np.float64) * 20.0

        k = min(5, len(X))
        self.knn_index = NearestNeighbors(n_neighbors=k, metric="euclidean").fit(X)
        dists = [self.mahalanobis_distance(row) for row in X]
        self.mahalanobis_p95 = max(float(np.percentile(dists, 95)), 2.5)

        elo_X = np.array([[t.squad_elo, t.opponent_elo, t.squad_elo - t.opponent_elo] for t in tactics_list], dtype=np.float64)
        self.propensity_model = Ridge(alpha=10.0).fit(elo_X, X)
        return self

    def mahalanobis_distance(self, tactic_vec: Sequence[float]) -> float:
        x = np.asarray(tactic_vec, dtype=np.float64) - self.mean_vec
        d2 = float(x.T @ self.precision_matrix @ x)
        return math.sqrt(max(0.0, d2))

    def score_ood(self, tactic_vec: Sequence[float]) -> Dict[str, float]:
        """Return normalized [0, 1+] OOD score compatible with Module B GREEN/AMBER/RED gates."""
        mahal = self.mahalanobis_distance(tactic_vec)
        knn_dist = 0.0
        if self.knn_index is not None:
            dists, _ = self.knn_index.kneighbors(np.asarray(tactic_vec, dtype=np.float64)[None, :])
            knn_dist = float(np.mean(dists[0]))
        ood_score = min(1.0, 0.70 * (mahal / max(self.mahalanobis_p95 * 1.6, 1e-3)) + 0.30 * (knn_dist / 0.65))
        return {
            "ood_score": float(ood_score),
            "mahalanobis_distance": float(mahal),
            "knn_distance": float(knn_dist),
        }

    def save(self, path: Path) -> Path:
        """Serialize manifold dictionary (`mean_10d`, `cov_10d`, plus k-NN & IPW propensity state)."""
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "mean_10d": self.mean_vec,
            "cov_10d": self.cov_matrix,
            "precision_matrix": self.precision_matrix,
            "mahalanobis_p95": self.mahalanobis_p95,
            "fitted_vectors": self.fitted_vectors,
            "propensity_model": self.propensity_model,
        }
        with open(path, "wb") as f:
            pickle.dump(payload, f)
        return path

    @classmethod
    def load(cls, path: Path) -> "TacticalManifoldV2":
        with open(path, "rb") as f:
            obj = pickle.load(f)
        if isinstance(obj, cls):
            return obj
        inst = cls()
        inst.mean_vec = np.asarray(obj["mean_10d"], dtype=np.float64)
        inst.cov_matrix = np.asarray(obj["cov_10d"], dtype=np.float64)
        inst.precision_matrix = np.asarray(obj["precision_matrix"], dtype=np.float64)
        inst.mahalanobis_p95 = float(obj.get("mahalanobis_p95", 4.2))
        inst.fitted_vectors = obj.get("fitted_vectors")
        inst.propensity_model = obj.get("propensity_model")
        if inst.fitted_vectors is not None and len(inst.fitted_vectors) > 0:
            inst.knn_index = NearestNeighbors(n_neighbors=min(5, len(inst.fitted_vectors)), metric="euclidean").fit(
                inst.fitted_vectors
            )
        return inst


def export_profiles_parquet(profiles: Sequence[PlayerProfileV2], output_path: Path) -> Path:
    """
    Export `player_profiles_v2.parquet` compatible with Module B's `PlayerProfileRegistry.from_parquet`
    (falls back to gzipped JSONL when PyArrow/DuckDB are not installed).
    """
    import pandas as pd

    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    rows = []
    for prof in profiles:
        d = prof.model_dump()
        d["display_name"] = prof.player_name or f"Player {prof.player_id}"
        d["natural_roles"] = [int(r) for r in prof.natural_roles]
        rows.append(d)
    df = pd.DataFrame(rows)

    try:
        import pyarrow  # noqa: F401
        pq_path = output_path.with_suffix(".parquet")
        df.to_parquet(pq_path, compression="zstd", index=False)
        return pq_path
    except Exception:
        try:
            import duckdb  # type: ignore
            pq_path = output_path.with_suffix(".parquet")
            con = duckdb.connect(":memory:")
            con.register("profiles_df", df)
            con.execute(f"COPY profiles_df TO '{pq_path.as_posix()}' (FORMAT PARQUET, COMPRESSION ZSTD)")
            con.close()
            return pq_path
        except Exception:
            jsonl_path = output_path.with_suffix(".jsonl.gz")
            df.to_json(jsonl_path, orient="records", lines=True, compression="gzip")
            return jsonl_path
