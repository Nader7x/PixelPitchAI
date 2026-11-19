"""
Derived Analytics & Labels Pipeline:
- 24x16 Expected Threat (`xT`) Bellman Value Iteration Solver + Bilinear Interpolation
- Calibrated Pre-Shot `xG`, Post-Shot `PSxG`, `xPass`, and 10-Action Horizon `VAEP` calculators.

Uses `xgboost` + `scikit-learn` (and optional `catboost`/`lightgbm` if installed) with
physically calibrated logistic priors so inference and small-batch unit tests behave consistently.
"""

from __future__ import annotations

import math
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence, Tuple

import numpy as np
import xgboost as xgb
from sklearn.linear_model import LogisticRegression

from statsbombapi_v2.schemas import (
    ActionType,
    BodyPart,
    EventGoldV2,
    EventOutcome,
    GOAL_CENTER_Y,
    GOAL_MOUTH_Y_MAX,
    GOAL_MOUTH_Y_MIN,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PassHeight,
    RestartType,
    XT_GRID_COLS,
    XT_GRID_ROWS,
    YD_TO_M_X,
    YD_TO_M_Y,
)


def _sigmoid(z: np.ndarray | float) -> np.ndarray | float:
    return 1.0 / (1.0 + np.exp(-np.clip(z, -20.0, 20.0)))


def _event_to_xt_cell(x_att: float, y_att: float) -> Tuple[int, int]:
    cell_w = PITCH_LENGTH_YD / XT_GRID_COLS  # 5.0 yd
    cell_h = PITCH_WIDTH_YD / XT_GRID_ROWS   # 5.0 yd
    cx = min(max(int(x_att // cell_w), 0), XT_GRID_COLS - 1)
    cy = min(max(int(y_att // cell_h), 0), XT_GRID_ROWS - 1)
    return cx, cy


class ExpectedThreatModel:
    """
    24 x 16 canonical Expected Threat (`xT`) grid solved via Bellman value iteration
    and evaluated with continuous bilinear interpolation.
    """

    def __init__(self, grid: Optional[np.ndarray] = None) -> None:
        if grid is not None:
            arr = np.asarray(grid, dtype=np.float64)
            if arr.shape != (XT_GRID_COLS, XT_GRID_ROWS):
                raise ValueError(f"Expected xT grid shape {(XT_GRID_COLS, XT_GRID_ROWS)}, got {arr.shape}")
            self.grid = arr
        else:
            self.grid = self.default_prior_grid()
        self.bellman_residual: float = 0.0

    @staticmethod
    def default_prior_grid() -> np.ndarray:
        """Smooth monotonic pitch threat prior rising sharply inside the attacking penalty box."""
        cols = (np.arange(XT_GRID_COLS) + 0.5) * (PITCH_LENGTH_YD / XT_GRID_COLS)
        rows = (np.arange(XT_GRID_ROWS) + 0.5) * (PITCH_WIDTH_YD / XT_GRID_ROWS)
        gx, gy = np.meshgrid(cols, rows, indexing="ij")
        dx_m = np.maximum(PITCH_LENGTH_YD - gx, 1.0) * YD_TO_M_X
        dy_m = np.abs(gy - GOAL_CENTER_Y) * YD_TO_M_Y
        dist_m = np.hypot(dx_m, dy_m)
        # Smooth exponential decay from goal mouth (~0.30 inside 6-yd box -> ~0.004 in own half)
        prior = 0.34 * np.exp(-dist_m / 13.5) + 0.002 * (gx / PITCH_LENGTH_YD)
        return np.clip(prior, 0.001, 0.65)

    def fit(
        self,
        events: Sequence[EventGoldV2],
        max_iters: int = 100,
        tol: float = 1e-6,
        laplace_alpha: float = 1.0,
        prior_weight: float = 5.0,
    ) -> "ExpectedThreatModel":
        """Solve 24x16 Bellman xT fixed-point equation over open-play move & shot events."""
        n_cells = XT_GRID_COLS * XT_GRID_ROWS
        move_counts = np.zeros(n_cells, dtype=np.float64)
        shoot_counts = np.zeros(n_cells, dtype=np.float64)
        goal_counts = np.zeros(n_cells, dtype=np.float64)
        trans_counts = np.zeros((n_cells, n_cells), dtype=np.float64)

        for ev in events:
            if ev.action_type not in (ActionType.PASS, ActionType.CARRY, ActionType.SHOT):
                continue
            cx, cy = _event_to_xt_cell(ev.x_att, ev.y_att)
            z = cx * XT_GRID_ROWS + cy
            if ev.action_type == ActionType.SHOT:
                shoot_counts[z] += 1.0
                if ev.outcome == EventOutcome.GOAL:
                    goal_counts[z] += 1.0
                elif ev.xg_statsbomb is not None:
                    goal_counts[z] += float(ev.xg_statsbomb) * 0.5
            else:
                move_counts[z] += 1.0
                if ev.outcome == EventOutcome.COMPLETE:
                    ex, ey = _event_to_xt_cell(ev.end_x_att, ev.end_y_att)
                    zp = ex * XT_GRID_ROWS + ey
                    trans_counts[z, zp] += 1.0

        prior_flat = self.default_prior_grid().ravel()
        total_actions = move_counts + shoot_counts + prior_weight
        s_z = (shoot_counts + prior_weight * 0.05) / total_actions
        m_z = (move_counts + prior_weight * 0.95) / total_actions
        g_z = (goal_counts + prior_weight * prior_flat) / (shoot_counts + prior_weight)

        # Laplace-smoothed transition matrix T_{z -> z'}
        row_sums = trans_counts.sum(axis=1, keepdims=True) + laplace_alpha * n_cells
        T = (trans_counts + laplace_alpha) / row_sums

        xt = prior_flat.copy()
        residual = 1.0
        for _ in range(max_iters):
            xt_next = s_z * g_z + m_z * (T @ xt)
            residual = float(np.max(np.abs(xt_next - xt)))
            xt = xt_next
            if residual < tol:
                break

        self.grid = xt.reshape((XT_GRID_COLS, XT_GRID_ROWS))
        self.bellman_residual = residual
        return self

    def evaluate_point(self, x_att: float, y_att: float) -> float:
        """Continuous bilinear interpolation at attacking-relative (x_att, y_att)."""
        return float(self.evaluate_points(np.array([x_att]), np.array([y_att]))[0])

    def evaluate_points(self, x_att: np.ndarray, y_att: np.ndarray) -> np.ndarray:
        cell_w = PITCH_LENGTH_YD / XT_GRID_COLS  # 5.0
        cell_h = PITCH_WIDTH_YD / XT_GRID_ROWS   # 5.0

        # Normalized coordinates in cell-center index space [0, cols-1] x [0, rows-1]
        fx = np.clip(np.asarray(x_att, dtype=np.float64) / cell_w - 0.5, 0.0, XT_GRID_COLS - 1.0 - 1e-9)
        fy = np.clip(np.asarray(y_att, dtype=np.float64) / cell_h - 0.5, 0.0, XT_GRID_ROWS - 1.0 - 1e-9)

        x0 = np.floor(fx).astype(np.int64)
        y0 = np.floor(fy).astype(np.int64)
        x1 = np.minimum(x0 + 1, XT_GRID_COLS - 1)
        y1 = np.minimum(y0 + 1, XT_GRID_ROWS - 1)

        wx = fx - x0
        wy = fy - y0

        v00 = self.grid[x0, y0]
        v10 = self.grid[x1, y0]
        v01 = self.grid[x0, y1]
        v11 = self.grid[x1, y1]

        return (
            (1.0 - wx) * (1.0 - wy) * v00
            + wx * (1.0 - wy) * v10
            + (1.0 - wx) * wy * v01
            + wx * wy * v11
        )

    def save_npy(self, path: Path) -> Path:
        path.parent.mkdir(parents=True, exist_ok=True)
        np.save(path, self.grid.astype(np.float32))
        return path

    @classmethod
    def load_npy(cls, path: Path) -> "ExpectedThreatModel":
        return cls(grid=np.load(path))

    def apply_to_events(self, events: Sequence[EventGoldV2]) -> None:
        if not events:
            return
        xs = np.array([ev.x_att for ev in events], dtype=np.float64)
        ys = np.array([ev.y_att for ev in events], dtype=np.float64)
        exs = np.array([ev.end_x_att for ev in events], dtype=np.float64)
        eys = np.array([ev.end_y_att for ev in events], dtype=np.float64)

        xt_s = self.evaluate_points(xs, ys)
        xt_e = self.evaluate_points(exs, eys)

        for idx, ev in enumerate(events):
            ev.xt_start = float(xt_s[idx])
            if ev.action_type in (ActionType.PASS, ActionType.CARRY):
                if ev.outcome == EventOutcome.COMPLETE:
                    ev.xt_end = float(xt_e[idx])
                    ev.xt_delta = float(xt_e[idx] - xt_s[idx])
                else:
                    ev.xt_end = 0.0
                    ev.xt_delta = float(-0.5 * xt_s[idx])
            elif ev.action_type == ActionType.SHOT:
                ev.xt_end = float(xt_s[idx])
                ev.xt_delta = 0.0
            else:
                ev.xt_end = float(xt_s[idx])
                ev.xt_delta = 0.0


# --- Geometric Feature Extractors & Calibrated Outcome Models (xG, PSxG, xPass, VAEP) ---

def extract_shot_features(ev: EventGoldV2) -> np.ndarray:
    """14-dim geometric + 360 freeze-frame feature vector for Pre-Shot xG."""
    dx_m = max(PITCH_LENGTH_YD - ev.x_att, 0.2) * YD_TO_M_X
    dy_m = (ev.y_att - GOAL_CENTER_Y) * YD_TO_M_Y
    dist_m = math.hypot(dx_m, dy_m)

    # Visible goal mouth opening angle in radians
    y_post_top = (GOAL_MOUTH_Y_MIN - ev.y_att) * YD_TO_M_Y
    y_post_bot = (GOAL_MOUTH_Y_MAX - ev.y_att) * YD_TO_M_Y
    ang1 = math.atan2(y_post_top, dx_m)
    ang2 = math.atan2(y_post_bot, dx_m)
    goal_angle_rad = abs(ang2 - ang1)

    return np.array(
        [
            dist_m,
            goal_angle_rad,
            1.0 if ev.body_part == BodyPart.HEAD else 0.0,
            1.0 if ev.body_part == BodyPart.LEFT_FOOT else 0.0,
            1.0 if ev.restart_type == RestartType.PENALTY else 0.0,
            1.0 if ev.restart_type in (RestartType.FREE_KICK_DIRECT, RestartType.FREE_KICK_INDIRECT) else 0.0,
            1.0 if ev.first_time else 0.0,
            1.0 if ev.one_on_one else 0.0,
            1.0 if ev.under_pressure else 0.0,
            1.0 if ev.counter_attack else 0.0,
            float(ev.ff_n_defenders_in_shot_triangle),
            float(ev.ff_open_goal_fraction),
            float(ev.ff_gk_dist_goal),
            float(ev.ff_closest_defender_dist),
        ],
        dtype=np.float32,
    )


def extract_psxg_features(ev: EventGoldV2) -> Tuple[np.ndarray, bool]:
    """19-dim Post-Shot xG vector adding placement (end_y, end_z) & GK reach."""
    base = extract_shot_features(ev)
    is_on_target = (
        ev.outcome in (EventOutcome.GOAL, EventOutcome.SAVED_HELD, EventOutcome.SAVED_PARRY)
        and (GOAL_MOUTH_Y_MIN <= ev.end_y_att <= GOAL_MOUTH_Y_MAX)
        and (0.0 <= ev.end_z_m <= 2.48)
    )
    dist_nearest_post = min(abs(ev.end_y_att - GOAL_MOUTH_Y_MIN), abs(ev.end_y_att - GOAL_MOUTH_Y_MAX)) * YD_TO_M_Y
    gk_reach_dist = math.hypot((ev.end_y_att - GOAL_CENTER_Y) * YD_TO_M_Y, max(0.0, ev.end_z_m - 0.8))
    ext = np.array(
        [
            float(ev.end_y_att),
            float(ev.end_z_m),
            float(dist_nearest_post),
            1.0 if is_on_target else 0.0,
            float(gk_reach_dist),
        ],
        dtype=np.float32,
    )
    return np.concatenate([base, ext]), is_on_target


def extract_pass_features(ev: EventGoldV2) -> np.ndarray:
    """16-dim spatial + modifier + 360 passing-corridor feature vector for xPass."""
    dx = ev.end_x_att - ev.x_att
    dy = ev.end_y_att - ev.y_att
    length = math.hypot(dx, dy)
    return np.array(
        [
            float(ev.x_att),
            float(ev.y_att),
            float(ev.end_x_att),
            float(ev.end_y_att),
            float(length),
            float(dx),
            float(abs(dy)),
            float(int(ev.pass_height)),
            1.0 if ev.restart_type != RestartType.OPEN_PLAY else 0.0,
            1.0 if ev.under_pressure else 0.0,
            1.0 if ev.through_ball else 0.0,
            1.0 if ev.pass_switch else 0.0,
            1.0 if ev.cross else 0.0,
            float(ev.ff_min_defender_dist_to_pass_ray),
            float(ev.ff_n_defenders_in_pass_corridor),
            float(ev.ff_receiver_voronoi_area),
        ],
        dtype=np.float32,
    )


class PreShotXGModel:
    """Calibrated Pre-Shot xG predictor (XGBoost + Platt calibration or physical baseline)."""

    def __init__(self) -> None:
        self._model: Optional[xgb.XGBClassifier] = None
        self._calibrator: Optional[LogisticRegression] = None

    @staticmethod
    def _physics_prior(feat: np.ndarray) -> float:
        dist_m, angle_rad, is_head, _, is_pen, is_fk, first_time, one_v_one, under_p, counter, n_tri, open_frac, gk_dist, close_def = feat
        if is_pen > 0.5:
            return 0.78
        logit = (
            -1.45
            - 0.135 * dist_m
            + 2.45 * angle_rad
            - 0.42 * is_head
            - 0.35 * is_fk
            + 0.15 * first_time
            + 0.65 * one_v_one
            - 0.25 * under_p
            + 0.20 * counter
            - 0.28 * min(n_tri, 4.0)
            + 0.85 * (open_frac - 0.5)
            + 0.05 * min(close_def, 8.0)
        )
        return float(min(max(_sigmoid(logit), 0.005), 0.96))

    def fit(self, events: Sequence[EventGoldV2]) -> "PreShotXGModel":
        shots = [ev for ev in events if ev.action_type == ActionType.SHOT and ev.outcome != EventOutcome.OWN_GOAL]
        if len(shots) < 25:
            return self
        X = np.stack([extract_shot_features(s) for s in shots])
        y = np.array([1 if s.outcome == EventOutcome.GOAL else 0 for s in shots], dtype=np.int32)
        if len(np.unique(y)) < 2:
            return self
        clf = xgb.XGBClassifier(
            n_estimators=80,
            max_depth=4,
            learning_rate=0.06,
            subsample=0.85,
            colsample_bytree=0.85,
            eval_metric="logloss",
            random_state=42,
        )
        clf.fit(X, y)
        raw_probs = clf.predict_proba(X)[:, 1:2]
        cal = LogisticRegression()
        cal.fit(raw_probs, y)
        self._model = clf
        self._calibrator = cal
        return self

    def predict(self, ev: EventGoldV2) -> float:
        if ev.action_type != ActionType.SHOT:
            return 0.0
        feat = extract_shot_features(ev)
        if self._model is not None and self._calibrator is not None:
            p_raw = self._model.predict_proba(feat[None, :])[:, 1:2]
            return float(np.clip(self._calibrator.predict_proba(p_raw)[0, 1], 0.002, 0.98))
        return self._physics_prior(feat)


class PostShotXGModel:
    """Calibrated Post-Shot xG (PSxG) conditioned on goalmouth (end_y, end_z) placement."""

    def __init__(self, pre_xg_model: Optional[PreShotXGModel] = None) -> None:
        self.pre_xg = pre_xg_model or PreShotXGModel()
        self._model: Optional[xgb.XGBClassifier] = None

    def fit(self, events: Sequence[EventGoldV2]) -> "PostShotXGModel":
        feats, labels = [], []
        for ev in events:
            if ev.action_type != ActionType.SHOT or ev.outcome == EventOutcome.OWN_GOAL:
                continue
            f, on_target = extract_psxg_features(ev)
            if on_target:
                feats.append(f)
                labels.append(1 if ev.outcome == EventOutcome.GOAL else 0)
        if len(feats) < 25 or len(set(labels)) < 2:
            return self
        X = np.stack(feats)
        y = np.array(labels, dtype=np.int32)
        clf = xgb.XGBClassifier(n_estimators=60, max_depth=3, learning_rate=0.06, eval_metric="logloss", random_state=42)
        clf.fit(X, y)
        self._model = clf
        return self

    def predict(self, ev: EventGoldV2, pre_xg: Optional[float] = None) -> float:
        if ev.action_type != ActionType.SHOT:
            return 0.0
        if ev.outcome in (EventOutcome.OFF_TARGET, EventOutcome.POST, EventOutcome.BLOCKED):
            return 0.0
        feat, on_target = extract_psxg_features(ev)
        if not on_target and ev.outcome != EventOutcome.GOAL:
            return 0.0
        if self._model is not None:
            return float(np.clip(self._model.predict_proba(feat[None, :])[0, 1], 0.01, 0.99))
        base_xg = pre_xg if pre_xg is not None else self.pre_xg.predict(ev)
        # Placement bonus toward top/side corners & away from GK center
        dist_post_m = float(feat[16])
        end_z = float(feat[15])
        corner_bonus = max(0.0, (1.5 - dist_post_m)) * 0.45 + min(end_z, 2.2) * 0.25
        logit = math.log(max(base_xg, 1e-4) / max(1.0 - base_xg, 1e-4)) + 0.35 + corner_bonus
        return float(np.clip(_sigmoid(logit), 0.01, 0.99))


class XPassModel:
    """Calibrated xPass completion probability + progressive pass value estimator."""

    def __init__(self) -> None:
        self._model: Optional[xgb.XGBClassifier] = None

    @staticmethod
    def _physics_prior(feat: np.ndarray) -> float:
        length = float(feat[4])
        dx = float(feat[5])
        pass_height = float(feat[7])
        under_p = float(feat[9])
        through = float(feat[10])
        cross = float(feat[12])
        ray_dist = float(feat[13])
        n_corr = float(feat[14])

        logit = (
            2.45
            - 0.042 * length
            - 0.020 * max(0.0, dx)
            - 0.35 * max(0.0, pass_height - 1.0)
            - 0.55 * under_p
            - 0.85 * through
            - 0.95 * cross
            + 0.14 * min(ray_dist, 8.0)
            - 0.35 * min(n_corr, 4.0)
        )
        return float(np.clip(_sigmoid(logit), 0.05, 0.99))

    def fit(self, events: Sequence[EventGoldV2]) -> "XPassModel":
        passes = [ev for ev in events if ev.action_type == ActionType.PASS]
        if len(passes) < 50:
            return self
        X = np.stack([extract_pass_features(p) for p in passes])
        y = np.array([1 if p.outcome == EventOutcome.COMPLETE else 0 for p in passes], dtype=np.int32)
        if len(np.unique(y)) < 2:
            return self
        clf = xgb.XGBClassifier(n_estimators=80, max_depth=4, learning_rate=0.07, eval_metric="logloss", random_state=42)
        clf.fit(X, y)
        self._model = clf
        return self

    def predict(self, ev: EventGoldV2) -> Tuple[float, float]:
        if ev.action_type != ActionType.PASS:
            return 0.0, 0.0
        feat = extract_pass_features(ev)
        if self._model is not None:
            prob = float(np.clip(self._model.predict_proba(feat[None, :])[0, 1], 0.02, 0.995))
        else:
            prob = self._physics_prior(feat)
        # Progressive pass value: difficulty-weighted positive threat added
        prog_val = float((1.0 - prob) * max(0.0, ev.xt_delta)) if ev.outcome == EventOutcome.COMPLETE else float(-0.25 * prob * max(0.0, ev.xt_start))
        return prob, prog_val

    def apply_to_events(self, events: Sequence[EventGoldV2], chunk_size: int = 50_000) -> None:
        """Vectorized batched pass prediction (avoids 800k+ single-row XGBoost calls)."""
        passes = [ev for ev in events if ev.action_type == ActionType.PASS]
        if not passes:
            return
        if self._model is None:
            for ev in passes:
                prob, pval = self.predict(ev)
                ev.xpass_prob = prob
                ev.xpass_progressive_value = pval
            return
        for s_i in range(0, len(passes), chunk_size):
            batch = passes[s_i : s_i + chunk_size]
            X = np.stack([extract_pass_features(ev) for ev in batch])
            probs = np.clip(self._model.predict_proba(X)[:, 1], 0.02, 0.995)
            for ev, prob_f in zip(batch, probs.tolist()):
                ev.xpass_prob = float(prob_f)
                ev.xpass_progressive_value = (
                    float((1.0 - prob_f) * max(0.0, ev.xt_delta))
                    if ev.outcome == EventOutcome.COMPLETE
                    else float(-0.25 * prob_f * max(0.0, ev.xt_start))
                )


class VAEPModel:
    """
    10-action same-half VAEP (Valuing Actions by Estimating Probabilities) calculator.
    Computes `P_score(s_t)` and `P_concede(s_t)` before/after each action.
    """

    def __init__(self) -> None:
        self._score_model: Optional[xgb.XGBClassifier] = None
        self._concede_model: Optional[xgb.XGBClassifier] = None

    @staticmethod
    def _state_features(ev: EventGoldV2) -> np.ndarray:
        score_diff = (ev.score_home - ev.score_away) if ev.is_home_actor else (ev.score_away - ev.score_home)
        time_rem = max(0.0, 90.0 - float(ev.minute)) / 90.0
        return np.array(
            [
                float(ev.end_x_att / PITCH_LENGTH_YD),
                float(abs(ev.end_y_att - GOAL_CENTER_Y) / (PITCH_WIDTH_YD / 2.0)),
                float(ev.xt_end),
                float(ev.xt_delta),
                float(ev.xg_v2_pre_shot),
                float(ev.xpass_prob),
                1.0 if ev.action_type == ActionType.SHOT else 0.0,
                1.0 if ev.outcome in (EventOutcome.COMPLETE, EventOutcome.WON, EventOutcome.GOAL) else 0.0,
                1.0 if ev.outcome in (EventOutcome.INCOMPLETE, EventOutcome.LOST) else 0.0,
                float(score_diff),
                float(time_rem),
                float(int(ev.poss_phase)),
            ],
            dtype=np.float32,
        )

    @staticmethod
    def _build_vaep_targets(events: Sequence[EventGoldV2], horizon: int = 10) -> Tuple[np.ndarray, np.ndarray]:
        n = len(events)
        y_score = np.zeros(n, dtype=np.int32)
        y_concede = np.zeros(n, dtype=np.int32)
        for i in range(n):
            team_i = events[i].possession_team_id
            period_i = events[i].period
            for j in range(i, min(n, i + horizon)):
                if events[j].period != period_i:
                    break
                if events[j].outcome == EventOutcome.GOAL:
                    if events[j].actor_team_id == team_i:
                        y_score[i] = 1
                    else:
                        y_concede[i] = 1
                    break
        return y_score, y_concede

    @staticmethod
    def _heuristic_probs(ev: EventGoldV2) -> Tuple[float, float]:
        """Smooth calibrated baseline when VAEP GBDTs are not explicitly fitted on a multi-match corpus."""
        # Scoring probability rises monotonically with end_x_att and shot xG; drops on turnovers
        if ev.outcome == EventOutcome.GOAL:
            return 1.0, 0.0
        success = ev.outcome in (EventOutcome.COMPLETE, EventOutcome.WON, EventOutcome.RETAIN, EventOutcome.SAVED_HELD)
        if ev.action_type == ActionType.SHOT:
            p_score = float(min(0.95, 0.03 + 0.65 * ev.xg_v2_pre_shot))
            p_concede = 0.012
        elif success:
            p_score = float(np.clip(0.008 + 0.42 * ev.xt_end + 0.015 * (ev.end_x_att / PITCH_LENGTH_YD), 0.003, 0.35))
            p_concede = float(np.clip(0.025 - 0.015 * (ev.end_x_att / PITCH_LENGTH_YD), 0.004, 0.06))
        else:
            # Turnover gives opponent scoring likelihood proportional to how deep in own half it occurred
            p_score = 0.004
            p_concede = float(np.clip(0.010 + 0.14 * max(0.0, 1.0 - ev.x_att / PITCH_LENGTH_YD) ** 2, 0.005, 0.20))
        return p_score, p_concede

    def fit(self, events: Sequence[EventGoldV2], horizon: int = 10) -> "VAEPModel":
        if len(events) < 100:
            return self
        X = np.stack([self._state_features(ev) for ev in events])
        y_s, y_c = self._build_vaep_targets(events, horizon=horizon)
        if len(np.unique(y_s)) == 2:
            self._score_model = xgb.XGBClassifier(n_estimators=60, max_depth=3, learning_rate=0.06, eval_metric="logloss", random_state=42)
            self._score_model.fit(X, y_s)
        if len(np.unique(y_c)) == 2:
            self._concede_model = xgb.XGBClassifier(n_estimators=60, max_depth=3, learning_rate=0.06, eval_metric="logloss", random_state=42)
            self._concede_model.fit(X, y_c)
        return self

    def apply_to_events(self, events: Sequence[EventGoldV2]) -> None:
        if not events:
            return
        prev_p_score, prev_p_concede = 0.01, 0.01
        prev_team = events[0].possession_team_id

        if self._score_model is not None and self._concede_model is not None:
            chunk_size = 50_000
            for s_i in range(0, len(events), chunk_size):
                batch = events[s_i : s_i + chunk_size]
                X = np.stack([self._state_features(ev) for ev in batch])
                p_scores = np.clip(self._score_model.predict_proba(X)[:, 1], 0.001, 0.99).tolist()
                p_concedes = np.clip(self._concede_model.predict_proba(X)[:, 1], 0.001, 0.99).tolist()
                for j, (ev, p_score_after, p_concede_after) in enumerate(zip(batch, p_scores, p_concedes)):
                    if (s_i + j) > 0 and ev.possession_team_id != prev_team:
                        p_score_before, p_concede_before = prev_p_concede, prev_p_score
                    else:
                        p_score_before, p_concede_before = prev_p_score, prev_p_concede
                    ev.vaep_p_score_before = float(p_score_before)
                    ev.vaep_p_score_after = float(p_score_after)
                    ev.vaep_p_concede_before = float(p_concede_before)
                    ev.vaep_p_concede_after = float(p_concede_after)
                    ev.vaep_value = float((p_score_after - p_score_before) - (p_concede_after - p_concede_before))
                    prev_p_score, prev_p_concede = p_score_after, p_concede_after
                    prev_team = ev.possession_team_id
            return

        for idx, ev in enumerate(events):
            if idx > 0 and ev.possession_team_id != prev_team:
                p_score_before, p_concede_before = prev_p_concede, prev_p_score
            else:
                p_score_before, p_concede_before = prev_p_score, prev_p_concede
            p_score_after, p_concede_after = self._heuristic_probs(ev)
            ev.vaep_p_score_before = float(p_score_before)
            ev.vaep_p_score_after = float(p_score_after)
            ev.vaep_p_concede_before = float(p_concede_before)
            ev.vaep_p_concede_after = float(p_concede_after)
            ev.vaep_value = float((p_score_after - p_score_before) - (p_concede_after - p_concede_before))
            prev_p_score, prev_p_concede = p_score_after, p_concede_after
            prev_team = ev.possession_team_id


def enrich_events_with_labels(
    events: Sequence[EventGoldV2],
    xt_model: Optional[ExpectedThreatModel] = None,
    xg_model: Optional[PreShotXGModel] = None,
    psxg_model: Optional[PostShotXGModel] = None,
    xpass_model: Optional[XPassModel] = None,
    vaep_model: Optional[VAEPModel] = None,
) -> Sequence[EventGoldV2]:
    """Attach calibrated xT, xG, PSxG, xPass, and VAEP analytics labels onto Gold events."""
    xt = xt_model or ExpectedThreatModel()
    xg = xg_model or PreShotXGModel()
    psxg = psxg_model or PostShotXGModel(pre_xg_model=xg)
    xp = xpass_model or XPassModel()
    vaep = vaep_model or VAEPModel()

    xt.apply_to_events(events)
    for ev in events:
        if ev.action_type == ActionType.SHOT:
            ev.xg_v2_pre_shot = xg.predict(ev)
            ev.psxg_v2 = psxg.predict(ev, pre_xg=ev.xg_v2_pre_shot)
    xp.apply_to_events(events)
    vaep.apply_to_events(events)
    return events
