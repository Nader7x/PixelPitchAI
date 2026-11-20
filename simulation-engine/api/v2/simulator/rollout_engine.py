"""Module C & D Hybrid Rollout Engine, Colab Training Loss/IPW, and ONNX Export Helpers.

Combines:
- FootballFSM (Approach B laws of the game, clock, restarts, hard masks)
- PixelPitchEventTF (Approach A custom event transformer)
- ModuleDOutcomeSuite (Calibrated GPU-resident xG, PSxG, xPass, xT, VAEP, Turnover)
- Optional Module B TacticalStateEngine / OffBallSpatialSolver integration with standalone fallback.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Callable, Dict, Generator, List, Optional, Sequence, Tuple

import math
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch import FloatTensor, LongTensor, Tensor

from api.v2.simulator.event_transformer import (
    EventTransformerConfig,
    PixelPitchEventTF,
    StaticBackboneCUDAGraphRunner,
)
from api.v2.simulator.fsm import (
    ActionType,
    BallState,
    BatchedFSMState,
    BodyPart,
    EventOutcome,
    FootballFSM,
    NUM_ACTIONS,
    NUM_MODIFIER_FLAGS,
    PITCH_LENGTH,
    PITCH_WIDTH,
    PassHeight,
    Period,
    PossPhase,
    RestartType,
    xy_to_cell,
)
from api.v2.simulator.outcome_models import ModuleDOutcomeSuite


# ---------------------------------------------------------------------------
# Enum String Maps (Matching StructuredEventV2 & simulation.proto V2)
# ---------------------------------------------------------------------------

_ACTION_NAMES = {a.value: a.name.lower() for a in ActionType}
_OUTCOME_NAMES = {
    EventOutcome.COMPLETE.value: "Complete",
    EventOutcome.INCOMPLETE.value: "Incomplete",
    EventOutcome.OUT_TOUCHLINE.value: "OutTouchline",
    EventOutcome.OUT_BYLINE.value: "OutByline",
    EventOutcome.OFFSIDE.value: "Offside",
    EventOutcome.GOAL.value: "Goal",
    EventOutcome.SAVED_HELD.value: "Saved",
    EventOutcome.SAVED_PARRY.value: "SavedParry",
    EventOutcome.BLOCKED.value: "Blocked",
    EventOutcome.OFF_TARGET.value: "OffTarget",
    EventOutcome.POST.value: "Post",
    EventOutcome.WON.value: "Won",
    EventOutcome.LOST.value: "Lost",
    EventOutcome.YELLOW_CARD.value: "YellowCard",
    EventOutcome.SECOND_YELLOW.value: "SecondYellow",
    EventOutcome.RED_CARD.value: "RedCard",
    EventOutcome.FOUL_CONCEDED.value: "FoulConceded",
    EventOutcome.PENALTY_WON.value: "PenaltyWon",
    EventOutcome.SUB_EXECUTED.value: "Substitution",
    EventOutcome.PERIOD_END.value: "PeriodEnd",
    EventOutcome.NEUTRAL.value: "Neutral",
}
_BODY_PART_NAMES = {
    BodyPart.RIGHT_FOOT.value: "RightFoot",
    BodyPart.LEFT_FOOT.value: "LeftFoot",
    BodyPart.HEAD.value: "Head",
    BodyPart.KEEPER_HANDS.value: "KeeperHands",
    BodyPart.OTHER.value: "Other",
}
_PASS_HEIGHT_NAMES = {
    PassHeight.NA.value: "",
    PassHeight.GROUND.value: "Ground",
    PassHeight.LOW.value: "Low",
    PassHeight.HIGH.value: "High",
}
_RESTART_SUBTYPE = {
    RestartType.OPEN_PLAY.value: "",
    RestartType.KICK_OFF.value: "KickOff",
    RestartType.THROW_IN.value: "ThrowIn",
    RestartType.CORNER.value: "Corner",
    RestartType.FREE_KICK_DIRECT.value: "FreeKick",
    RestartType.FREE_KICK_INDIRECT.value: "FreeKickIndirect",
    RestartType.GOAL_KICK.value: "GoalKick",
    RestartType.PENALTY.value: "Penalty",
}


# ---------------------------------------------------------------------------
# Lightweight Default Tactical / Spatial Fallback (when Module B absent in tests)
# ---------------------------------------------------------------------------

def _default_22_positions(batch_size: int, device: torch.device) -> FloatTensor:
    """Canonical 4-3-3 baseline coordinates [B, 22, 2] for Home (0..10) & Away (11..21)."""
    home = torch.tensor([
        [6.0, 40.0],
        [24.0, 14.0], [22.0, 32.0], [22.0, 48.0], [24.0, 66.0],
        [44.0, 24.0], [40.0, 40.0], [44.0, 56.0],
        [72.0, 14.0], [76.0, 40.0], [72.0, 66.0],
    ], dtype=torch.float32, device=device)
    away = torch.stack([PITCH_LENGTH - home[:, 0], PITCH_WIDTH - home[:, 1]], dim=-1)
    both = torch.cat([home, away], dim=0).unsqueeze(0).expand(batch_size, 22, 2).clone()
    return both


def build_default_freeze_frame_360(
    positions: FloatTensor,        # [B, 22, 2]
    velocities: FloatTensor,       # [B, 22, 2]
    poss_team: LongTensor,         # [B] 1=home, 2=away
    actor_slot: Optional[LongTensor] = None,
) -> FloatTensor:
    """Assemble Module D's exact [B, 22, 7] synthetic 360 freeze-frame struct."""
    B = positions.shape[0]
    dev = positions.device
    slots = torch.arange(1, 23, device=dev).unsqueeze(0).expand(B, 22)
    is_home = slots <= 11
    is_teammate = torch.where((poss_team == 1).unsqueeze(-1), is_home, ~is_home).float()
    is_gk = ((slots == 1) | (slots == 12)).float()
    if actor_slot is not None:
        is_actor = (slots == actor_slot.unsqueeze(-1)).float()
    else:
        is_actor = torch.zeros_like(is_gk)
    return torch.cat([
        positions,
        velocities,
        is_teammate.unsqueeze(-1),
        is_actor.unsqueeze(-1),
        is_gk.unsqueeze(-1),
    ], dim=-1)


@dataclass
class MatchStepFrame:
    """Single emitted event step ready for Module E MatchStreamFrameV2 & Monte Carlo aggregation."""

    event_index: int
    period: int
    clock_ms: int
    match_clock_seconds: int
    event: Dict[str, Any]
    spatial_positions: List[List[float]]  # 22 x [x, y, vx, vy, stamina, is_sent_off]
    ball_xyz: Tuple[float, float, float]
    home_voronoi_control: float
    telemetry: Dict[str, Any]
    lifecycle_event: Optional[str] = None


@dataclass
class MonteCarloArmStats:
    arm_id: str
    n_simulations: int
    home_win_prob: float
    draw_prob: float
    away_win_prob: float
    expected_points_home: float
    home_goals_mean: float
    away_goals_mean: float
    home_xg_mean: float
    away_xg_mean: float
    home_xg_ci95: Tuple[float, float]
    away_xg_ci95: Tuple[float, float]
    home_xt_mean: float
    away_xt_mean: float
    events_per_match_mean: float
    per_seed_home_xg: List[float] = field(default_factory=list)
    per_seed_away_xg: List[float] = field(default_factory=list)
    per_seed_home_goals: List[int] = field(default_factory=list)
    per_seed_away_goals: List[int] = field(default_factory=list)
    representative_seeds: List[Dict[str, Any]] = field(default_factory=list)

    @property
    def expected_points(self) -> float:
        return self.expected_points_home


@dataclass
class MonteCarloDeltaStats:
    arm_id: str
    home_win_prob_delta: float
    expected_points_delta: float
    home_xg_delta_mean: float
    home_xg_delta_ci95: Tuple[float, float]
    away_xg_delta_mean: float
    away_xg_delta_ci95: Tuple[float, float]
    p_value_xg_diff: float


@dataclass
class MonteCarloBatchResult:
    n_simulations: int
    use_paired_crn: bool
    baseline: MonteCarloArmStats
    variants: Dict[str, MonteCarloArmStats]
    deltas_vs_baseline: Dict[str, MonteCarloDeltaStats]


# ---------------------------------------------------------------------------
# Hybrid Rollout Engine
# ---------------------------------------------------------------------------

class HybridRolloutEngine:
    """Orchestrates Modules B, C (FSM + EventTF), and D for streaming and Monte Carlo batches."""

    def __init__(
        self,
        model: Optional[PixelPitchEventTF] = None,
        outcome_suite: Optional[ModuleDOutcomeSuite] = None,
        fsm: Optional[FootballFSM] = None,
        tactical_engine: Optional[Any] = None,
        device: torch.device | str = "cpu",
        models_dir: Optional[str | Path] = None,
    ):
        self.device = torch.device(device)
        self.fsm = fsm or FootballFSM()
        self.weights_loaded: bool = False
        self.loaded_artifacts: List[str] = []
        self.training_metrics: Dict[str, Any] = {}
        self.calibrated_temperature: float = 0.95
        self._cuda_graph_runners: Dict[Tuple[int, int], StaticBackboneCUDAGraphRunner] = {}

        if model is None and outcome_suite is None and models_dir is not None and Path(models_dir).exists():
            loaded = self._load_artifacts_into(Path(models_dir), self.device)
            self.model = loaded["model"]
            self.outcome_suite = loaded["outcome_suite"]
            self.tactical_engine = loaded.get("tactical_engine")
            self.weights_loaded = loaded["weights_loaded"]
            self.loaded_artifacts = loaded["loaded_artifacts"]
            self.training_metrics = loaded["training_metrics"]
            self.calibrated_temperature = loaded["calibrated_temperature"]
        else:
            self.model = (model or PixelPitchEventTF(EventTransformerConfig.tiny_test())).to(self.device).eval()
            self.outcome_suite = (outcome_suite or ModuleDOutcomeSuite()).to(self.device).eval()
            self.tactical_engine = tactical_engine.to(self.device).eval() if tactical_engine is not None else None
        self.model.requires_grad_(False)
        self.outcome_suite.requires_grad_(False)
        if self.tactical_engine is not None:
            self.tactical_engine.requires_grad_(False)

    @staticmethod
    def _load_artifacts_into(mdir: Path, dev: torch.device) -> Dict[str, Any]:
        import json
        import numpy as np

        loaded_files: List[str] = []
        metrics: Dict[str, Any] = {}
        cal_temp: float = 0.95

        # 1. Load PixelPitchEventTF checkpoint (pixelpitch_event_tf_112m.pt)
        tf_path = mdir / "pixelpitch_event_tf_112m.pt"
        tf_loaded = False
        if tf_path.exists():
            ckpt = torch.load(tf_path, map_location=dev, weights_only=False)
            if isinstance(ckpt, dict) and "state_dict" in ckpt:
                cfg_dict = ckpt.get("config")
                cfg = EventTransformerConfig(**cfg_dict) if isinstance(cfg_dict, dict) else EventTransformerConfig.base_112m()
                model = PixelPitchEventTF(cfg).to(dev)
                model.load_state_dict(ckpt["state_dict"], strict=False)
                if "temperature" in ckpt:
                    cal_temp = float(ckpt["temperature"])
            elif isinstance(ckpt, dict):
                # Infer config from norm_final.weight shape
                nf = ckpt.get("norm_final.weight")
                d_mod = int(nf.shape[0]) if nf is not None else 768
                cfg = EventTransformerConfig.base_112m() if d_mod == 768 else EventTransformerConfig.tiny_test()
                model = PixelPitchEventTF(cfg).to(dev)
                model.load_state_dict(ckpt, strict=False)
            else:
                model = PixelPitchEventTF(EventTransformerConfig.base_112m()).to(dev)
            model.eval()
            tf_loaded = True
            loaded_files.append(tf_path.name)
        else:
            model = PixelPitchEventTF(EventTransformerConfig.tiny_test()).to(dev).eval()

        # 2. Load Module D Outcome Suite (module_d_suite.pt + xt_grid_24x16.npy)
        outcome_suite = ModuleDOutcomeSuite().to(dev)
        mod_d_path = mdir / "module_d_suite.pt"
        mod_d_loaded = False
        if mod_d_path.exists():
            d_ckpt = torch.load(mod_d_path, map_location=dev, weights_only=False)
            sd = d_ckpt.get("state_dict", d_ckpt) if isinstance(d_ckpt, dict) else d_ckpt
            outcome_suite.load_state_dict(sd, strict=False)
            mod_d_loaded = True
            loaded_files.append(mod_d_path.name)

        xt_path = mdir / "xt_grid_24x16.npy"
        if xt_path.exists():
            xt_arr = np.load(xt_path)
            outcome_suite.xt_grid.xt_grid.copy_(torch.as_tensor(xt_arr, dtype=torch.float32, device=dev))
            loaded_files.append(xt_path.name)
        outcome_suite.eval()

        # 3. Load TacticalStateEngine & SpatialPredictor + PlayerProfiles + TacticalManifold
        tac_engine = None
        try:
            from api.v2.tactical.state_engine import PlayerProfileRegistry, TacticalStateEngine

            prof_path = mdir / "player_profiles_v2.parquet"
            reg = None
            if prof_path.exists():
                try:
                    reg = PlayerProfileRegistry.from_parquet(str(prof_path))
                    loaded_files.append(prof_path.name)
                except Exception:
                    reg = PlayerProfileRegistry()

            tac_engine = TacticalStateEngine(profile_registry=reg, num_actions=NUM_ACTIONS, d_model=model.config.d_model).to(dev)
            sp_path = mdir / "spatial_predictor.pt"
            if sp_path.exists():
                sp_ckpt = torch.load(sp_path, map_location=dev, weights_only=False)
                sp_sd = sp_ckpt.get("state_dict", sp_ckpt) if isinstance(sp_ckpt, dict) else sp_ckpt
                tac_engine.spatial_solver.spatial_predictor.load_state_dict(sp_sd, strict=False)
                loaded_files.append(sp_path.name)
            tac_engine.eval()
        except Exception:
            tac_engine = None

        for extra_name in ("events_gold_v2.parquet", "player_id_crosswalk.parquet", "tactical_manifold_v2.pkl", "pixelpitch_event_tf_int8.onnx"):
            if (mdir / extra_name).exists():
                loaded_files.append(extra_name)

        cal_path = mdir / "calibration_temperature.json"
        if cal_path.exists():
            try:
                cal_data = json.loads(cal_path.read_text(encoding="utf-8"))
                cal_temp = float(cal_data.get("temperature", cal_temp))
                loaded_files.append(cal_path.name)
            except Exception:
                pass

        metrics_path = mdir / "training_metrics.json"
        if metrics_path.exists():
            try:
                metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
                loaded_files.append(metrics_path.name)
            except Exception:
                pass

        return {
            "model": model,
            "outcome_suite": outcome_suite,
            "tactical_engine": tac_engine,
            "weights_loaded": bool(tf_loaded and mod_d_loaded),
            "loaded_artifacts": sorted(set(loaded_files)),
            "training_metrics": metrics,
            "calibrated_temperature": cal_temp,
        }

    @classmethod
    def from_models_dir(
        cls,
        models_dir: str | Path = "./models_v2",
        device: torch.device | str = "cpu",
    ) -> "HybridRolloutEngine":
        """Instantiate HybridRolloutEngine and load all trained v2 checkpoints from models_dir."""
        import os

        resolved_dir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
        dev = torch.device(device)
        engine = cls.__new__(cls)
        engine.device = dev
        engine.fsm = FootballFSM()
        engine._cuda_graph_runners = {}
        if resolved_dir.exists():
            loaded = cls._load_artifacts_into(resolved_dir, dev)
            engine.model = loaded["model"]
            engine.outcome_suite = loaded["outcome_suite"]
            engine.tactical_engine = loaded["tactical_engine"]
            engine.weights_loaded = loaded["weights_loaded"]
            engine.loaded_artifacts = loaded["loaded_artifacts"]
            engine.training_metrics = loaded["training_metrics"]
            engine.calibrated_temperature = loaded["calibrated_temperature"]
        else:
            engine.model = PixelPitchEventTF(EventTransformerConfig.tiny_test()).to(dev).eval()
            engine.outcome_suite = ModuleDOutcomeSuite().to(dev).eval()
            engine.tactical_engine = None
            engine.weights_loaded = False
            engine.loaded_artifacts = []
            engine.training_metrics = {}
            engine.calibrated_temperature = 0.95
        engine.model.requires_grad_(False)
        engine.outcome_suite.requires_grad_(False)
        if engine.tactical_engine is not None:
            engine.tactical_engine.requires_grad_(False)
        return engine

    def save_models_dir(
        self,
        models_dir: str | Path = "./models_v2",
        training_metrics: Optional[Dict[str, Any]] = None,
    ) -> Path:
        """Persist trained EventTF, Module D suite, SpatialPredictor, xT grid, and calibration metadata."""
        import json
        from dataclasses import asdict
        import numpy as np

        out = Path(models_dir)
        out.mkdir(parents=True, exist_ok=True)
        torch.save(
            {
                "config": asdict(self.model.config),
                "state_dict": self.model.state_dict(),
                "temperature": self.calibrated_temperature,
            },
            out / "pixelpitch_event_tf_112m.pt",
        )
        torch.save({"state_dict": self.outcome_suite.state_dict()}, out / "module_d_suite.pt")
        np.save(out / "xt_grid_24x16.npy", self.outcome_suite.xt_grid.xt_grid.detach().cpu().numpy())
        if self.tactical_engine is not None and hasattr(self.tactical_engine, "spatial_solver"):
            torch.save(
                {"state_dict": self.tactical_engine.spatial_solver.spatial_predictor.state_dict()},
                out / "spatial_predictor.pt",
            )
        (out / "calibration_temperature.json").write_text(
            json.dumps({"temperature": self.calibrated_temperature}, indent=2),
            encoding="utf-8",
        )
        if training_metrics is not None:
            (out / "training_metrics.json").write_text(
                json.dumps(training_metrics, indent=2),
                encoding="utf-8",
            )
        return out

    @staticmethod
    def _initial_prev_event(batch_size: int, device: torch.device) -> Dict[str, Tensor]:
        return {
            "dt_bucket": torch.zeros(batch_size, dtype=torch.long, device=device),
            "period": torch.full((batch_size,), Period.FIRST_HALF, dtype=torch.long, device=device),
            "poss_phase": torch.zeros(batch_size, dtype=torch.long, device=device),
            "actor_slot": torch.zeros(batch_size, dtype=torch.long, device=device),
            "action_type": torch.full((batch_size,), ActionType.PERIOD_BOUNDARY, dtype=torch.long, device=device),
            "restart_type": torch.full((batch_size,), RestartType.KICK_OFF, dtype=torch.long, device=device),
            "pass_height": torch.zeros(batch_size, dtype=torch.long, device=device),
            "body_part": torch.zeros(batch_size, dtype=torch.long, device=device),
            "start_cell": xy_to_cell(torch.tensor([[60.0, 40.0]], device=device)).expand(batch_size),
            "end_cell": xy_to_cell(torch.tensor([[60.0, 40.0]], device=device)).expand(batch_size),
            "end_z_bin": torch.zeros(batch_size, dtype=torch.long, device=device),
            "target_slot": torch.zeros(batch_size, dtype=torch.long, device=device),
            "outcome": torch.full((batch_size,), EventOutcome.NEUTRAL, dtype=torch.long, device=device),
            "flags": torch.zeros((batch_size, NUM_MODIFIER_FLAGS), dtype=torch.float32, device=device),
            "continuous": torch.zeros((batch_size, 10), dtype=torch.float32, device=device),
        }

    def _build_state_vector_s_t(self, state: BatchedFSMState, stamina: FloatTensor) -> FloatTensor:
        """Assemble global match state s_t [B, 64] aligned 1:1 with Pillar 1 `_build_event_state_64d`."""
        B = state.batch_size
        dev = state.device
        s_t = torch.zeros((B, 64), dtype=torch.float32, device=dev)
        clock_sec = state.clock_ms.float() / 1000.0
        minute = clock_sec / 60.0
        second = torch.remainder(clock_sec, 60.0)
        is_home_poss = (state.poss_team == 1).float()
        bx = state.ball_xy[:, 0].clamp(0.0, PITCH_LENGTH)
        by = state.ball_xy[:, 1].clamp(0.0, PITCH_WIDTH)
        x_att = torch.where(state.poss_team == 1, bx, PITCH_LENGTH - bx)
        y_att = torch.where(state.poss_team == 1, by, PITCH_WIDTH - by)
        restart_t = self.fsm._RESTART_MAP.to(dev)[state.ball_state].float()

        s_t[:, 0] = state.period.float() / 5.0
        s_t[:, 1] = minute / 95.0
        s_t[:, 2] = second / 60.0
        s_t[:, 3] = (state.home_score - state.away_score).float().clamp(-5.0, 5.0) / 5.0
        s_t[:, 4] = state.home_score.float() / 5.0
        s_t[:, 5] = state.away_score.float() / 5.0
        s_t[:, 6] = is_home_poss
        s_t[:, 7] = bx / PITCH_LENGTH
        s_t[:, 8] = by / PITCH_WIDTH
        s_t[:, 9] = bx / PITCH_LENGTH
        s_t[:, 10] = by / PITCH_WIDTH
        s_t[:, 11] = x_att / PITCH_LENGTH
        s_t[:, 12] = y_att / PITCH_WIDTH
        s_t[:, 13] = state.poss_phase.float() / 7.0
        s_t[:, 14] = restart_t / 7.0
        return s_t

    @torch.no_grad()
    def _run_batched_steps(
        self,
        batch_size: int,
        max_events: int,
        seed: int,
        match_config: Optional[Any] = None,
        player_cond_init: Optional[FloatTensor] = None,
        team_tactics_init: Optional[FloatTensor] = None,
        finishing_residuals: Optional[FloatTensor] = None,  # [B, 22]
        gk_residuals: Optional[FloatTensor] = None,         # [B, 22]
        pass_residuals: Optional[FloatTensor] = None,       # [B, 22]
        press_residuals: Optional[FloatTensor] = None,      # [B, 22]
        pressing_intensity: Optional[FloatTensor] = None,   # [B, 2]
        temperature: float = 0.95,
        top_p: float = 0.95,
        collect_events: bool = False,
        tactical_override_queue: Optional[List[Dict[str, Any]]] = None,
        sample_epistemic: bool = False,
        on_step_frame: Optional[Callable[[MatchStepFrame], None]] = None,
    ) -> Dict[str, Any]:
        """Core vectorized rollout over B parallel matches with paired-CRN determinism."""
        dev = self.device
        cfg = self.model.config
        gen = torch.Generator(device=dev)
        gen.manual_seed(seed)

        extra_time = bool(getattr(getattr(match_config, "context", None), "extra_time_if_draw", False)) if match_config is not None else False
        state = self.fsm.init_state(batch_size=batch_size, extra_time_enabled=extra_time, device=dev, generator=gen)

        # Optional full Module B TacticalStateEngine integration
        tac_engine = self.tactical_engine
        tac_state = None
        if match_config is not None:
            if tac_engine is None:
                from api.v2.tactical.state_engine import TacticalStateEngine
                tac_engine = TacticalStateEngine(num_actions=NUM_ACTIONS, d_model=cfg.d_model).to(dev).eval()
            seed_list = [seed + i for i in range(batch_size)]
            init_res = tac_engine.init_batch(
                match_config,
                batch_size=batch_size,
                device=dev,
                sample_epistemic=sample_epistemic,
                seeds=seed_list,
            )
            tac_state = init_res.state
            positions = tac_state.positions.clone()
            velocities = tac_state.velocities.clone()
            stamina = tac_state.stamina.clone()
            press_int_2 = tac_state.effective_tactics_10d[:, :, 1].clone()
            if finishing_residuals is None:
                finishing_residuals = tac_state.finishing_log_odds
            if gk_residuals is None:
                gk_residuals = (tac_state.gk_attr_8d[..., 0] - 0.5) * 1.2
            if pass_residuals is None:
                pass_residuals = (tac_state.base_attr_24d[..., 0] - 0.5) * 1.0
            if press_residuals is None:
                press_residuals = (tac_state.base_attr_24d[..., 5] - 0.5) * 1.2
        else:
            positions = _default_22_positions(batch_size, dev)
            velocities = torch.zeros_like(positions)
            stamina = torch.ones((batch_size, 22), dtype=torch.float32, device=dev)
            press_int_2 = (
                pressing_intensity.to(dev).clone()
                if pressing_intensity is not None
                else torch.full((batch_size, 2), 0.55, dtype=torch.float32, device=dev)
            )

        if player_cond_init is not None:
            player_cond = player_cond_init.to(dev).clone()
        else:
            player_cond = torch.zeros((batch_size, 22, cfg.player_cond_dim), dtype=torch.float32, device=dev)

        if team_tactics_init is not None:
            team_tactics = team_tactics_init.to(dev).clone()
        else:
            team_tactics = torch.full((batch_size, 2, cfg.team_tactics_dim), 0.5, dtype=torch.float32, device=dev)

        # Accumulators across all B matches
        home_xg_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_xg_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_psxg_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_psxg_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_xt_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_xt_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_shots = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_shots = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_shots_sot = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_shots_sot = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_passes = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_passes = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_passes_ok = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_passes_ok = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_xpass_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_xpass_tot = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_poss_sec = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_poss_sec = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_build_passes = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_build_passes = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_def_act = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_def_act = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_f3_touches = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_f3_touches = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_high_turnovers = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_high_turnovers = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_set_piece_xg = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        away_set_piece_xg = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        home_goals_75_plus = torch.zeros(batch_size, dtype=torch.long, device=dev)
        away_goals_75_plus = torch.zeros(batch_size, dtype=torch.long, device=dev)
        home_last_ht_ms = torch.full((batch_size,), -60_000, dtype=torch.long, device=dev)
        away_last_ht_ms = torch.full((batch_size,), -60_000, dtype=torch.long, device=dev)
        event_counts = torch.zeros(batch_size, dtype=torch.long, device=dev)
        player_ids_cpu: Optional[List[int]] = (
            tac_state.player_ids[0].tolist() if (collect_events and batch_size == 1 and tac_state is not None) else None
        )

        prev_event = self._initial_prev_event(batch_size, dev)
        past_kvs: Optional[List[Tuple[Tensor, Tensor]]] = None
        use_cuda_graph = dev.type == "cuda" and batch_size <= 4
        max_cache_len = min(max(max_events, 512), int(self.model.config.max_seq_len)) if use_cuda_graph else 0
        cg_runner: Optional[StaticBackboneCUDAGraphRunner] = None
        emitted_frames: List[Dict[str, Any]] = []

        prev_dt_s = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        prev_actor_idx = torch.full((batch_size,), -1, dtype=torch.long, device=dev)
        prev_actor_pin: Optional[FloatTensor] = None
        prev_recv_idx = torch.full((batch_size,), -1, dtype=torch.long, device=dev)
        prev_recv_pin: Optional[FloatTensor] = None
        prev_yellow_idx = torch.full((batch_size,), -1, dtype=torch.long, device=dev)
        prev_red_idx = torch.full((batch_size,), -1, dtype=torch.long, device=dev)

        # Pre-allocated scratch constants/buffers to avoid per-step allocations
        _default_voronoi = torch.tensor(0.50, dtype=torch.float32, device=dev)
        _zero_b = torch.zeros(batch_size, dtype=torch.float32, device=dev)
        b_idx = torch.arange(batch_size, device=dev)
        cont_vec_buf = torch.empty((batch_size, 10), dtype=torch.float32, device=dev)
        team_tactics_buf = torch.zeros((batch_size, 2, cfg.team_tactics_dim), dtype=torch.float32, device=dev)
        finished_i = 0

        for step_idx in range(max_events):
            if collect_events and batch_size == 1:
                if finished_i:
                    break
            elif (step_idx & 15) == 15 and bool(state.finished.all().item()):
                break

            # Optional live tactical override injection (single-match stream mode)
            if tactical_override_queue:
                while tactical_override_queue:
                    ov = tactical_override_queue.pop(0)
                    side_idx = 0 if ov.get("team_side", "home") == "home" else 1
                    patch = ov.get("tactical_instructions_patch") or {}
                    if tac_engine is not None and tac_state is not None:
                        tac_engine.apply_live_override(
                            tac_state,
                            team_side=ov.get("team_side", "home"),
                            tactical_patch=patch,
                        )
                        press_int_2 = tac_state.effective_tactics_10d[:, :, 1].clone()
                    else:
                        if "pressing_intensity" in patch:
                            press_int_2[:, side_idx] = float(patch["pressing_intensity"])
                            team_tactics[:, side_idx, 1] = float(patch["pressing_intensity"])
                        if "defensive_line_height" in patch:
                            team_tactics[:, side_idx, 0] = float(patch["defensive_line_height"])

            not_done = ~state.finished
            home_voronoi_tensor = _default_voronoi

            if tac_engine is not None and tac_state is not None:
                is_dead_ball = state.ball_state != BallState.IN_PLAY
                tac_res = tac_engine.step(
                    state=tac_state,
                    dt_sec=prev_dt_s,
                    ball_xy=state.ball_xy,
                    possessing_team=torch.where(state.poss_team == 2, 1, 0),
                    period=state.period,
                    score=torch.stack([state.home_score, state.away_score], dim=-1),
                    actor_slot_idx=prev_actor_idx,
                    actor_pin_xy=prev_actor_pin,
                    receiver_slot_idx=prev_recv_idx,
                    receiver_pin_xy=prev_recv_pin,
                    yellow_card_slots=prev_yellow_idx,
                    red_card_slots=prev_red_idx,
                    is_dead_ball=is_dead_ball,
                    restart_offside_exempt=(state.ball_state == BallState.THROW_IN) | (state.ball_state == BallState.GOAL_KICK),
                )
                positions = tac_res.spatial.positions
                velocities = tac_res.spatial.velocities
                stamina = tac_state.stamina
                state.active_mask = tac_state.active_mask.clone()
                press_int_2 = tac_state.effective_tactics_10d[:, :, 1]
                freeze_frame = tac_res.spatial.freeze_frame_360
                player_cond = tac_res.player_cond
                s_t = self._build_state_vector_s_t(state, stamina)
                # Populate 18-dim Team Tactical & GK summary tokens [B, 2, 18] in-place
                team_tactics = team_tactics_buf
                team_tactics.zero_()
                team_tactics[:, :, :10] = tac_state.effective_tactics_10d
                team_tactics[:, 0, 10] = tac_state.red_cards[:, :11].float().sum(dim=-1) / 3.0
                team_tactics[:, 1, 10] = tac_state.red_cards[:, 11:].float().sum(dim=-1) / 3.0
                team_tactics[:, 0, 11] = tac_res.spatial.phase_ids[:, 0].float() / 3.0
                team_tactics[:, 1, 11] = tac_res.spatial.phase_ids[:, 1].float() / 3.0
                team_tactics[:, 0, 12:18] = tac_state.gk_attr_8d[:, 0, :6]
                team_tactics[:, 1, 12:18] = tac_state.gk_attr_8d[:, 11, :6]
                mem_override = None
                home_voronoi_tensor = tac_res.spatial.home_voronoi_control[0]
                if player_ids_cpu is not None and tac_res.executed_subs:
                    player_ids_cpu = tac_state.player_ids[0].tolist()
                fsm_masks = self.fsm.compute_masks(
                    state,
                    tactical_actor_mask=tac_res.actor_mask,
                    tactical_target_mask=tac_res.spatial.target_mask,
                    tactical_spatial_prior=tac_res.spatial.spatial_log_prior,
                )
            else:
                ball_center_pull = 0.12 * (state.ball_xy.unsqueeze(1) - positions)
                positions = torch.where(
                    not_done.view(-1, 1, 1),
                    (positions + ball_center_pull).clamp(1.0, 119.0),
                    positions,
                )
                positions[..., 1] = positions[..., 1].clamp(1.0, 79.0)
                freeze_frame = build_default_freeze_frame_360(positions, velocities, state.poss_team, state.last_actor_slot)
                fsm_masks = self.fsm.compute_masks(state)
                s_t = self._build_state_vector_s_t(state, stamina)
                mem_override = None

            # Prepare Module D outcome override closure for MicroDecoder step 6
            step_analytics: Dict[str, Tensor] = {}

            def _module_d_override(proposed: Dict[str, Tensor], raw_logits: Tensor) -> Tensor:
                actor_s = proposed["actor_slot"]
                slot_idx = (actor_s - 1).clamp(0, 21)
                def_gk_idx = torch.where(state.poss_team == 1, 11, 0)

                act_fin = finishing_residuals[b_idx, slot_idx] if finishing_residuals is not None else None
                opp_gk = gk_residuals[b_idx, def_gk_idx] if gk_residuals is not None else None
                act_pass = pass_residuals[b_idx, slot_idx] if pass_residuals is not None else None
                act_press = press_residuals[b_idx, slot_idx] if press_residuals is not None else None
                act_stam = stamina[b_idx, slot_idx]
                def_press_int = torch.where(state.poss_team == 1, press_int_2[:, 1], press_int_2[:, 0])
                score_diff = torch.where(
                    state.poss_team == 1,
                    (state.home_score - state.away_score).float(),
                    (state.away_score - state.home_score).float(),
                )

                cal_logits, analytics = self.outcome_suite.calibrate_and_override_outcomes(
                    proposed_event=proposed,
                    raw_outcome_logits=raw_logits,
                    freeze_frame=freeze_frame,
                    poss_team=state.poss_team,
                    pressing_intensity=def_press_int,
                    score_diff=score_diff,
                    actor_finishing_res=act_fin,
                    opp_gk_res=opp_gk,
                    actor_pass_res=act_pass,
                    actor_press_res=act_press,
                    actor_stamina=act_stam,
                )
                step_analytics.update(analytics)
                return cal_logits

            if use_cuda_graph and step_idx < max_cache_len:
                if cg_runner is None:
                    runners = getattr(self, "_cuda_graph_runners", None)
                    if runners is None:
                        runners = {}
                        self._cuda_graph_runners = runners
                    cg_key = (batch_size, max_cache_len)
                    cg_runner = runners.get(cg_key)
                    if cg_runner is None:
                        cg_runner = StaticBackboneCUDAGraphRunner(
                            model=self.model,
                            batch_size=batch_size,
                            max_cache_len=max_cache_len,
                            sample_prev_event=prev_event,
                            sample_player_cond=player_cond,
                            sample_s_t=s_t,
                            sample_team_tactics=team_tactics,
                        )
                        runners[cg_key] = cg_runner
                    else:
                        cg_runner.reset()

                h_t = cg_runner.run(
                    prev_event=prev_event,
                    player_cond=player_cond,
                    s_t=s_t,
                    team_tactics=team_tactics,
                    step_idx=step_idx,
                )
                sampled = self.model.micro_decoder.sample_step(
                    h_t=h_t,
                    fsm_masks=fsm_masks,
                    poss_team=state.poss_team,
                    outcome_override_fn=_module_d_override,
                    outcome_mask_fn=self.fsm.outcome_mask_for_action,
                    temperature=temperature,
                    top_p=top_p,
                    generator=gen,
                )
            else:
                sampled, past_kvs = self.model.step_decode(
                    prev_event=prev_event,
                    player_cond=player_cond,
                    s_t=s_t,
                    fsm_masks=fsm_masks,
                    poss_team=state.poss_team,
                    past_kvs=past_kvs,
                    pos_offset=step_idx,
                    team_tactics=team_tactics,
                    mem_override=mem_override,
                    outcome_override_fn=_module_d_override,
                    outcome_mask_fn=self.fsm.outcome_mask_for_action,
                    temperature=temperature,
                    top_p=top_p,
                    generator=gen,
                )

            # Advance FSM deterministically
            prev_period = state.period.clone()
            pre_poss_team = state.poss_team.clone()
            pre_poss_phase = state.poss_phase.clone()
            state = self.fsm.step(
                state=state,
                dt_seconds=sampled["dt_seconds"],
                actor_slot=sampled["actor_slot"],
                action_type=sampled["action_type"],
                start_xy=sampled["start_xy"],
                end_xy=sampled["end_xy"],
                end_z=sampled["end_z"],
                outcome=sampled["outcome"],
            )

            # Record step info for Module B spatial pinning & cards on next step
            prev_dt_s = sampled["dt_seconds"]
            prev_actor_idx = torch.where(sampled["actor_slot"] > 0, sampled["actor_slot"] - 1, -1)
            prev_actor_pin = sampled["start_xy"]
            prev_recv_idx = torch.where(
                (sampled["action_type"] == ActionType.PASS)
                & (sampled["outcome"] == EventOutcome.COMPLETE)
                & (sampled["target_slot"] > 0),
                sampled["target_slot"] - 1,
                -1,
            )
            prev_recv_pin = sampled["end_xy"]
            prev_yellow_idx = torch.where(
                sampled["outcome"] == EventOutcome.YELLOW_CARD,
                prev_actor_idx,
                -1,
            )
            prev_red_idx = torch.where(
                (sampled["outcome"] == EventOutcome.RED_CARD) | (sampled["outcome"] == EventOutcome.SECOND_YELLOW),
                prev_actor_idx,
                -1,
            )

            # Drain stamina when running standalone without TacticalStateEngine state
            dt_s = sampled["dt_seconds"]
            just_ht = (prev_period == Period.FIRST_HALF) & (state.period == Period.SECOND_HALF)
            if tac_engine is None or tac_state is None:
                drain_home = dt_s * (1.1e-4 + 1.3e-4 * press_int_2[:, 0])
                drain_away = dt_s * (1.1e-4 + 1.3e-4 * press_int_2[:, 1])
                stamina[:, :11] = (stamina[:, :11] - drain_home.unsqueeze(-1)).clamp(0.20, 1.0)
                stamina[:, 11:] = (stamina[:, 11:] - drain_away.unsqueeze(-1)).clamp(0.20, 1.0)
                stamina = torch.where(just_ht.unsqueeze(-1), (stamina + 0.16).clamp(max=1.0), stamina)

            # Update telemetry accumulators for active (not previously finished) matches
            act_slot = sampled["actor_slot"]
            is_home_act = (act_slot >= 1) & (act_slot <= 11) & not_done
            is_away_act = (act_slot >= 12) & (act_slot <= 22) & not_done
            event_counts += not_done.long()

            xg = step_analytics.get("xg", _zero_b)
            psxg = step_analytics.get("psxg", _zero_b)
            xpass = step_analytics.get("xpass", _zero_b)
            xt_d = step_analytics.get("xt_delta", _zero_b)
            vaep = step_analytics.get("vaep_value", _zero_b)
            turn_risk = step_analytics.get("turnover_risk", _zero_b)

            is_shot = sampled["action_type"] == ActionType.SHOT
            is_pass = sampled["action_type"] == ActionType.PASS
            is_complete = sampled["outcome"] == EventOutcome.COMPLETE
            is_sot = (
                (sampled["outcome"] == EventOutcome.GOAL)
                | (sampled["outcome"] == EventOutcome.SAVED_HELD)
                | (sampled["outcome"] == EventOutcome.SAVED_PARRY)
            )
            sx_t = sampled["start_xy"][:, 0]
            is_def_act = (
                (sampled["action_type"] == ActionType.INTERCEPTION)
                | (sampled["action_type"] == ActionType.TACKLE_DUEL)
                | (sampled["action_type"] == ActionType.FOUL_COMMITTED)
            )
            poss_flipped = (state.poss_team != pre_poss_team) & not_done

            home_xg_tot += torch.where(is_home_act & is_shot, xg, 0.0)
            away_xg_tot += torch.where(is_away_act & is_shot, xg, 0.0)
            home_psxg_tot += torch.where(is_home_act & is_shot, psxg, 0.0)
            away_psxg_tot += torch.where(is_away_act & is_shot, psxg, 0.0)
            home_xt_tot += torch.where(is_home_act & (xt_d > 0), xt_d, 0.0)
            away_xt_tot += torch.where(is_away_act & (xt_d > 0), xt_d, 0.0)
            home_shots += (is_home_act & is_shot).long()
            away_shots += (is_away_act & is_shot).long()
            home_shots_sot += (is_home_act & is_shot & is_sot).long()
            away_shots_sot += (is_away_act & is_shot & is_sot).long()
            home_passes += (is_home_act & is_pass).long()
            away_passes += (is_away_act & is_pass).long()
            home_passes_ok += (is_home_act & is_pass & is_complete).long()
            away_passes_ok += (is_away_act & is_pass & is_complete).long()
            home_xpass_tot += torch.where(is_home_act & is_pass, xpass, 0.0)
            away_xpass_tot += torch.where(is_away_act & is_pass, xpass, 0.0)
            home_poss_sec += torch.where((pre_poss_team == 1) & not_done, dt_s, 0.0)
            away_poss_sec += torch.where((pre_poss_team == 2) & not_done, dt_s, 0.0)
            home_build_passes += (is_home_act & is_pass & (sx_t < 72.0)).long()
            away_build_passes += (is_away_act & is_pass & (sx_t > 48.0)).long()
            home_def_act += (is_home_act & is_def_act & (sx_t > 48.0)).long()
            away_def_act += (is_away_act & is_def_act & (sx_t < 72.0)).long()
            home_f3_touches += (is_home_act & (sx_t >= 80.0)).long()
            away_f3_touches += (is_away_act & (sx_t <= 40.0)).long()
            is_open_regain = (
                (state.ball_state == BallState.IN_PLAY)
                & (
                    (sampled["action_type"] == ActionType.BALL_RECOVERY)
                    | (sampled["action_type"] == ActionType.INTERCEPTION)
                    | ((sampled["action_type"] == ActionType.TACKLE_DUEL) & (sampled["outcome"] == EventOutcome.WON))
                )
            )
            h_ht_hit = poss_flipped & is_open_regain & (state.poss_team == 1) & (sx_t >= 86.0) & ((state.clock_ms - home_last_ht_ms) >= 15_000)
            a_ht_hit = poss_flipped & is_open_regain & (state.poss_team == 2) & (sx_t <= 34.0) & ((state.clock_ms - away_last_ht_ms) >= 15_000)
            home_high_turnovers += h_ht_hit.long()
            away_high_turnovers += a_ht_hit.long()
            home_last_ht_ms = torch.where(h_ht_hit, state.clock_ms, home_last_ht_ms)
            away_last_ht_ms = torch.where(a_ht_hit, state.clock_ms, away_last_ht_ms)
            is_set_piece_shot = is_shot & (
                (sampled["restart_type"] != RestartType.OPEN_PLAY)
                | (pre_poss_phase == PossPhase.SET_PIECE_ATTACK)
                | (pre_poss_phase == PossPhase.SET_PIECE_DEFENSE)
            )
            home_set_piece_xg += torch.where(is_home_act & is_set_piece_shot, xg, 0.0)
            away_set_piece_xg += torch.where(is_away_act & is_set_piece_shot, xg, 0.0)
            is_goal = sampled["outcome"] == EventOutcome.GOAL
            is_late = state.clock_ms >= 4_500_000
            home_goals_75_plus += (is_home_act & is_shot & is_goal & is_late).long()
            away_goals_75_plus += (is_away_act & is_shot & is_goal & is_late).long()

            # Prepare next-step prev_event with XG_XT_HEAD continuous feedback (reusing pre-allocated buffer)
            cont_vec = cont_vec_buf
            cont_vec[:, 0] = dt_s / 60.0
            cont_vec[:, 1] = sampled["start_xy"][:, 0] / PITCH_LENGTH
            cont_vec[:, 2] = sampled["start_xy"][:, 1] / PITCH_WIDTH
            cont_vec[:, 3] = (sampled["end_xy"][:, 0] - sampled["start_xy"][:, 0]) / PITCH_LENGTH
            cont_vec[:, 4] = (sampled["end_xy"][:, 1] - sampled["start_xy"][:, 1]) / PITCH_WIDTH
            cont_vec[:, 5] = sampled["end_z"] / 4.0
            cont_vec[:, 6] = xg
            cont_vec[:, 7] = xpass
            cont_vec[:, 8] = xt_d
            cont_vec[:, 9] = vaep

            prev_event = {
                "dt_bucket": sampled["dt_bucket"],
                "period": state.period,
                "poss_phase": state.poss_phase,
                "actor_slot": sampled["actor_slot"],
                "action_type": sampled["action_type"],
                "restart_type": sampled["restart_type"],
                "pass_height": sampled["pass_height"],
                "body_part": sampled["body_part"],
                "start_cell": sampled["start_cell"],
                "end_cell": sampled["end_cell"],
                "end_z_bin": sampled["end_z_bin"],
                "target_slot": sampled["target_slot"],
                "outcome": sampled["outcome"],
                "flags": sampled["flags"].float(),
                "continuous": cont_vec,
            }

            if collect_events and batch_size == 1:
                # Bulk GPU->CPU transfer (3 packed transfers instead of 170+ individual .item() syncs)
                int_vals = torch.stack([
                    sampled["action_type"][0],
                    sampled["outcome"][0],
                    sampled["actor_slot"][0],
                    sampled["target_slot"][0],
                    sampled["body_part"][0],
                    sampled["pass_height"][0],
                    sampled["restart_type"][0],
                    state.poss_phase_id[0],
                    pre_poss_team[0],
                    state.home_score[0],
                    state.away_score[0],
                    home_shots[0],
                    away_shots[0],
                    home_shots_sot[0],
                    away_shots_sot[0],
                    home_passes[0],
                    away_passes[0],
                    home_passes_ok[0],
                    away_passes_ok[0],
                    home_build_passes[0],
                    away_build_passes[0],
                    home_def_act[0],
                    away_def_act[0],
                    home_f3_touches[0],
                    away_f3_touches[0],
                    home_high_turnovers[0],
                    away_high_turnovers[0],
                    state.period[0],
                    state.clock_ms[0],
                    just_ht[0].long(),
                    state.finished[0].long(),
                ]).tolist()
                (
                    act_i, out_i, slot_i, tgt_i, bp_i, ph_i, rs_i,
                    poss_phase_i, poss_team_i, h_score_i, a_score_i,
                    h_shots_i, a_shots_i, h_sot_i, a_sot_i, h_passes_i, a_passes_i,
                    h_passes_ok_i, a_passes_ok_i, h_build_i, a_build_i,
                    h_def_i, a_def_i, h_f3_i, a_f3_i, h_hturn_i, a_hturn_i,
                    period_i, clock_ms_i, just_ht_i, finished_i,
                ) = int_vals

                # Pack all float outputs into a single GPU->CPU transfer (2 packed syncs total per step)
                fl_len = int(sampled["flags"].shape[1])
                float_packed = torch.cat([
                    sampled["flags"][0].float(),
                    sampled["start_xy"][0].float(),
                    sampled["end_xy"][0].float(),
                    sampled["end_z"][0:1].float(),
                    state.ball_xy[0].float(),
                    state.ball_z[0:1].float(),
                    torch.stack([
                        xg[0].float(),
                        psxg[0].float(),
                        xpass[0].float(),
                        xt_d[0].float(),
                        vaep[0].float(),
                        turn_risk[0].float(),
                        home_xg_tot[0].float(),
                        away_xg_tot[0].float(),
                        home_psxg_tot[0].float(),
                        away_psxg_tot[0].float(),
                        home_xt_tot[0].float(),
                        away_xt_tot[0].float(),
                        home_xpass_tot[0].float(),
                        away_xpass_tot[0].float(),
                        home_poss_sec[0].float(),
                        away_poss_sec[0].float(),
                        home_voronoi_tensor.float(),
                    ]),
                    torch.cat([
                        positions[0].float(),
                        velocities[0].float(),
                        stamina[0].float().unsqueeze(-1),
                        (~state.active_mask[0]).float().unsqueeze(-1),
                    ], dim=-1).reshape(-1),
                ]).tolist()
                fl = float_packed[:fl_len]
                g_off = fl_len
                sx, sy, ex, ey, ez, bx, by, bz = float_packed[g_off:g_off + 8]
                a_off = g_off + 8
                (
                    xg_v, psxg_v, xpass_v, xt_v, vaep_v, turn_v,
                    h_xg_tot_v, a_xg_tot_v, h_psxg_tot_v, a_psxg_tot_v,
                    h_xt_tot_v, a_xt_tot_v, h_xpass_tot_v, a_xpass_tot_v,
                    h_poss_sec_v, a_poss_sec_v, home_voronoi_val,
                ) = float_packed[a_off:a_off + 17]
                s_off = a_off + 17
                sp_flat = float_packed[s_off:]
                spatial_packed = [sp_flat[i * 6:(i + 1) * 6] for i in range(22)]

                card_str = ""
                if out_i == EventOutcome.YELLOW_CARD:
                    card_str = "Yellow"
                elif out_i == EventOutcome.SECOND_YELLOW:
                    card_str = "SecondYellow"
                elif out_i == EventOutcome.RED_CARD:
                    card_str = "Red"

                sub_str = _RESTART_SUBTYPE.get(rs_i, "")
                if not sub_str and act_i == ActionType.PASS and fl[1] > 0.5:
                    sub_str = "ThroughBall"
                elif not sub_str and act_i == ActionType.PASS and fl[3] > 0.5:
                    sub_str = "Cross"

                ev_pid = int(player_ids_cpu[slot_i - 1]) if (player_ids_cpu is not None and 0 < slot_i <= 22) else slot_i
                ev_dict = {
                    "event_index": step_idx,
                    "possession_phase_id": poss_phase_i,
                    "possessing_team_side": poss_team_i,
                    "actor_slot": slot_i,
                    "recipient_slot": tgt_i,
                    "player_id": ev_pid,
                    "action_type": _ACTION_NAMES.get(act_i, "pass"),
                    "sub_type": sub_str,
                    "outcome": _OUTCOME_NAMES.get(out_i, "Complete"),
                    "start_x": round(sx, 2),
                    "start_y": round(sy, 2),
                    "end_x": round(ex, 2),
                    "end_y": round(ey, 2),
                    "end_z": round(ez, 2),
                    "under_pressure": bool(fl[0] > 0.5),
                    "body_part": _BODY_PART_NAMES.get(bp_i, "RightFoot"),
                    "pass_height": _PASS_HEIGHT_NAMES.get(ph_i, ""),
                    "card": card_str,
                    "home_score": h_score_i,
                    "away_score": a_score_i,
                    "xg": round(xg_v, 4),
                    "post_shot_xg": round(psxg_v, 4),
                    "xpass": round(xpass_v, 4),
                    "xt_delta": round(xt_v, 4),
                    "vaep_value": round(vaep_v, 4),
                    "turnover_risk": round(turn_v, 4),
                }
                hp = max(1, h_passes_i)
                ap = max(1, a_passes_i)
                tot_poss = h_poss_sec_v + a_poss_sec_v
                h_poss_pct = (100.0 * h_poss_sec_v / tot_poss) if tot_poss > 1e-3 else 50.0
                h_ppda = min(40.0, max(1.0, a_build_i / h_def_i)) if h_def_i > 0 else 10.0
                a_ppda = min(40.0, max(1.0, h_build_i / a_def_i)) if a_def_i > 0 else 10.0
                tot_f3 = h_f3_i + a_f3_i
                h_ftilt = (100.0 * h_f3_i / tot_f3) if tot_f3 > 0 else 50.0
                mom_idx = math.tanh(0.35 * ((h_xg_tot_v - a_xg_tot_v) + 0.5 * (h_xt_tot_v - a_xt_tot_v)))
                tel_dict = {
                    "home_score": h_score_i,
                    "away_score": a_score_i,
                    "home_xg": round(h_xg_tot_v, 3),
                    "away_xg": round(a_xg_tot_v, 3),
                    "home_psxg": round(h_psxg_tot_v, 3),
                    "away_psxg": round(a_psxg_tot_v, 3),
                    "home_xt": round(h_xt_tot_v, 3),
                    "away_xt": round(a_xt_tot_v, 3),
                    "home_possession_pct": round(h_poss_pct, 1),
                    "home_ppda": round(h_ppda, 2),
                    "away_ppda": round(a_ppda, 2),
                    "home_field_tilt_pct": round(h_ftilt, 1),
                    "momentum_index": round(mom_idx, 3),
                    "home_shots": h_shots_i,
                    "away_shots": a_shots_i,
                    "home_shots_on_target": h_sot_i,
                    "away_shots_on_target": a_sot_i,
                    "home_passes": h_passes_i,
                    "away_passes": a_passes_i,
                    "home_pass_accuracy": round(h_passes_ok_i / hp, 3),
                    "away_pass_accuracy": round(a_passes_ok_i / ap, 3),
                    "home_avg_xpass": round(h_xpass_tot_v / hp, 3),
                    "away_avg_xpass": round(a_xpass_tot_v / ap, 3),
                    "home_high_turnovers": h_hturn_i,
                    "away_high_turnovers": a_hturn_i,
                }
                pos_rows = [
                    [
                        round(row[0], 2),
                        round(row[1], 2),
                        round(row[2], 2),
                        round(row[3], 2),
                        round(row[4], 3),
                        bool(row[5] > 0.5),
                    ]
                    for row in spatial_packed
                ]
                lifecycle = None
                if just_ht_i:
                    lifecycle = "second_half_start"
                elif finished_i:
                    lifecycle = "match_end"

                step_frame = MatchStepFrame(
                    event_index=step_idx,
                    period=period_i,
                    clock_ms=clock_ms_i,
                    match_clock_seconds=clock_ms_i // 1000,
                    event=ev_dict,
                    spatial_positions=pos_rows,
                    ball_xyz=(round(bx, 2), round(by, 2), round(bz, 2)),
                    home_voronoi_control=round(home_voronoi_val, 3),
                    telemetry=tel_dict,
                    lifecycle_event=lifecycle,
                )
                if on_step_frame is not None:
                    on_step_frame(step_frame)
                else:
                    emitted_frames.append({"step": step_frame})

        return {
            "home_score": state.home_score.cpu(),
            "away_score": state.away_score.cpu(),
            "home_xg": home_xg_tot.cpu(),
            "away_xg": away_xg_tot.cpu(),
            "home_xt": home_xt_tot.cpu(),
            "away_xt": away_xt_tot.cpu(),
            "home_shots": home_shots.cpu(),
            "away_shots": away_shots.cpu(),
            "home_poss_sec": home_poss_sec.cpu(),
            "away_poss_sec": away_poss_sec.cpu(),
            "home_f3_touches": home_f3_touches.cpu(),
            "away_f3_touches": away_f3_touches.cpu(),
            "home_high_turnovers": home_high_turnovers.cpu(),
            "away_high_turnovers": away_high_turnovers.cpu(),
            "home_passes": home_passes.cpu(),
            "away_passes": away_passes.cpu(),
            "home_passes_ok": home_passes_ok.cpu(),
            "away_passes_ok": away_passes_ok.cpu(),
            "home_build_passes": home_build_passes.cpu(),
            "away_build_passes": away_build_passes.cpu(),
            "home_def_act": home_def_act.cpu(),
            "away_def_act": away_def_act.cpu(),
            "home_set_piece_xg": home_set_piece_xg.cpu(),
            "away_set_piece_xg": away_set_piece_xg.cpu(),
            "home_goals_75_plus": home_goals_75_plus.cpu(),
            "away_goals_75_plus": away_goals_75_plus.cpu(),
            "event_counts": event_counts.cpu(),
            "frames": emitted_frames,
        }

    def simulate_match_stream(
        self,
        match_config: Optional[Any] = None,
        max_events: int = 4200,
        seed: int = 42,
        player_cond: Optional[FloatTensor] = None,
        team_tactics: Optional[FloatTensor] = None,
        finishing_residuals: Optional[FloatTensor] = None,
        gk_residuals: Optional[FloatTensor] = None,
        pass_residuals: Optional[FloatTensor] = None,
        press_residuals: Optional[FloatTensor] = None,
        pressing_intensity: Optional[FloatTensor] = None,
        temperature: float = 0.95,
        top_p: float = 0.95,
        tactical_override_queue: Optional[List[Dict[str, Any]]] = None,
        on_step_frame: Optional[Callable[[MatchStepFrame], None]] = None,
    ) -> Generator[MatchStepFrame, None, None]:
        """Run single-match rollout and yield typed MatchStepFrame records for SSE/gRPC delivery."""
        out = self._run_batched_steps(
            batch_size=1,
            max_events=max_events,
            seed=seed,
            match_config=match_config,
            player_cond_init=player_cond,
            team_tactics_init=team_tactics,
            finishing_residuals=finishing_residuals,
            gk_residuals=gk_residuals,
            pass_residuals=pass_residuals,
            press_residuals=press_residuals,
            pressing_intensity=pressing_intensity,
            temperature=temperature,
            top_p=top_p,
            collect_events=True,
            tactical_override_queue=tactical_override_queue,
            on_step_frame=on_step_frame,
        )
        for item in out["frames"]:
            yield item["step"]

    @staticmethod
    def _mean_ci95(values: Tensor) -> Tuple[float, Tuple[float, float]]:
        v = values.float()
        n = max(1, v.numel())
        mean = float(v.mean().item())
        if n <= 1:
            return mean, (mean, mean)
        se = float(v.std(unbiased=True).item()) / math.sqrt(n)
        margin = 1.96 * se
        return mean, (mean - margin, mean + margin)

    def _summarize_arm(self, arm_id: str, raw: Dict[str, Tensor], base_seed: int) -> MonteCarloArmStats:
        hs = raw["home_score"].float()
        as_ = raw["away_score"].float()
        n = hs.numel()
        hw = float((hs > as_).float().mean().item())
        dr = float((hs == as_).float().mean().item())
        aw = float((hs < as_).float().mean().item())
        xpts = 3.0 * hw + 1.0 * dr

        hxg_m, hxg_ci = self._mean_ci95(raw["home_xg"])
        axg_m, axg_ci = self._mean_ci95(raw["away_xg"])
        hxt_m, _ = self._mean_ci95(raw["home_xt"])
        axt_m, _ = self._mean_ci95(raw["away_xt"])

        # Representative median-xG-diff seed
        xg_diff = raw["home_xg"] - raw["away_xg"]
        med_idx = int(torch.argsort(xg_diff)[n // 2].item())
        reps = [{
            "seed": base_seed + med_idx,
            "score": f"{int(hs[med_idx].item())}-{int(as_[med_idx].item())}",
            "tag": "median_outcome",
        }]

        return MonteCarloArmStats(
            arm_id=arm_id,
            n_simulations=n,
            home_win_prob=round(hw, 4),
            draw_prob=round(dr, 4),
            away_win_prob=round(aw, 4),
            expected_points_home=round(xpts, 4),
            home_goals_mean=round(float(hs.mean().item()), 4),
            away_goals_mean=round(float(as_.mean().item()), 4),
            home_xg_mean=round(hxg_m, 4),
            away_xg_mean=round(axg_m, 4),
            home_xg_ci95=(round(hxg_ci[0], 4), round(hxg_ci[1], 4)),
            away_xg_ci95=(round(axg_ci[0], 4), round(axg_ci[1], 4)),
            home_xt_mean=round(hxt_m, 4),
            away_xt_mean=round(axt_m, 4),
            events_per_match_mean=round(float(raw["event_counts"].float().mean().item()), 2),
            per_seed_home_xg=[round(float(x), 4) for x in raw["home_xg"].tolist()],
            per_seed_away_xg=[round(float(x), 4) for x in raw["away_xg"].tolist()],
            per_seed_home_goals=[int(x) for x in raw["home_score"].tolist()],
            per_seed_away_goals=[int(x) for x in raw["away_score"].tolist()],
            representative_seeds=reps,
        )

    def run_monte_carlo_batch(
        self,
        n_simulations: int = 32,
        max_events: int = 400,
        base_seed: int = 42000,
        use_paired_crn: bool = True,
        baseline_config: Optional[Any] = None,
        variant_configs: Optional[Dict[str, Any]] = None,
        baseline_params: Optional[Dict[str, Optional[Tensor]]] = None,
        variants: Optional[Dict[str, Dict[str, Optional[Tensor]]]] = None,
        temperature: float = 0.95,
        top_p: float = 0.95,
        sample_epistemic: bool = False,
    ) -> MonteCarloBatchResult:
        """Execute N paired-CRN GPU/CPU Monte Carlo rollouts across Baseline & Intervention arms."""
        bp = baseline_params or {}

        def _expand_param(t: Optional[Tensor], expected_shape_tail: Tuple[int, ...]) -> Optional[Tensor]:
            if t is None:
                return None
            if t.shape[0] == n_simulations:
                return t
            if t.shape[0] == 1:
                return t.expand(n_simulations, *expected_shape_tail).clone()
            return t.unsqueeze(0).expand(n_simulations, *expected_shape_tail).clone()

        base_raw = self._run_batched_steps(
            batch_size=n_simulations,
            max_events=max_events,
            seed=base_seed,
            match_config=baseline_config,
            player_cond_init=_expand_param(bp.get("player_cond"), (22, self.model.config.player_cond_dim)),
            team_tactics_init=_expand_param(bp.get("team_tactics"), (2, self.model.config.team_tactics_dim)),
            finishing_residuals=_expand_param(bp.get("finishing_residuals"), (22,)),
            gk_residuals=_expand_param(bp.get("gk_residuals"), (22,)),
            pass_residuals=_expand_param(bp.get("pass_residuals"), (22,)),
            press_residuals=_expand_param(bp.get("press_residuals"), (22,)),
            pressing_intensity=_expand_param(bp.get("pressing_intensity"), (2,)),
            temperature=temperature,
            top_p=top_p,
            collect_events=False,
            sample_epistemic=sample_epistemic,
        )
        base_stats = self._summarize_arm("baseline", base_raw, base_seed)

        variant_stats: Dict[str, MonteCarloArmStats] = {}
        delta_stats: Dict[str, MonteCarloDeltaStats] = {}
        all_variant_keys = list((variants or {}).keys()) or list((variant_configs or {}).keys())

        for idx, arm_id in enumerate(all_variant_keys):
            vp = (variants or {}).get(arm_id, {})
            v_cfg = (variant_configs or {}).get(arm_id, baseline_config)
            arm_seed = base_seed if use_paired_crn else (base_seed + (idx + 1) * 10_000)
            v_raw = self._run_batched_steps(
                batch_size=n_simulations,
                max_events=max_events,
                seed=arm_seed,
                match_config=v_cfg,
                player_cond_init=_expand_param(vp.get("player_cond", bp.get("player_cond")), (22, self.model.config.player_cond_dim)),
                team_tactics_init=_expand_param(vp.get("team_tactics", bp.get("team_tactics")), (2, self.model.config.team_tactics_dim)),
                finishing_residuals=_expand_param(vp.get("finishing_residuals", bp.get("finishing_residuals")), (22,)),
                gk_residuals=_expand_param(vp.get("gk_residuals", bp.get("gk_residuals")), (22,)),
                pass_residuals=_expand_param(vp.get("pass_residuals", bp.get("pass_residuals")), (22,)),
                press_residuals=_expand_param(vp.get("press_residuals", bp.get("press_residuals")), (22,)),
                pressing_intensity=_expand_param(vp.get("pressing_intensity", bp.get("pressing_intensity")), (2,)),
                temperature=temperature,
                top_p=top_p,
                collect_events=False,
                sample_epistemic=sample_epistemic,
            )
            v_st = self._summarize_arm(arm_id, v_raw, arm_seed)
            variant_stats[arm_id] = v_st

            d_hxg, d_hxg_ci = self._mean_ci95(v_raw["home_xg"] - base_raw["home_xg"])
            d_axg, d_axg_ci = self._mean_ci95(v_raw["away_xg"] - base_raw["away_xg"])
            xg_diff_delta = (v_raw["home_xg"] - v_raw["away_xg"]) - (base_raw["home_xg"] - base_raw["away_xg"])
            if n_simulations > 1 and float(xg_diff_delta.std().item()) > 1e-8:
                t_stat = float(xg_diff_delta.mean().item()) / (float(xg_diff_delta.std(unbiased=True).item()) / math.sqrt(n_simulations))
                # Normal tail approx for two-sided p-value without heavy runtime dependency
                p_val = float(math.erfc(abs(t_stat) / math.sqrt(2.0)))
            else:
                p_val = 1.0

            delta_stats[arm_id] = MonteCarloDeltaStats(
                arm_id=arm_id,
                home_win_prob_delta=round(v_st.home_win_prob - base_stats.home_win_prob, 4),
                expected_points_delta=round(v_st.expected_points_home - base_stats.expected_points_home, 4),
                home_xg_delta_mean=round(d_hxg, 4),
                home_xg_delta_ci95=(round(d_hxg_ci[0], 4), round(d_hxg_ci[1], 4)),
                away_xg_delta_mean=round(d_axg, 4),
                away_xg_delta_ci95=(round(d_axg_ci[0], 4), round(d_axg_ci[1], 4)),
                p_value_xg_diff=round(p_val, 6),
            )

        return MonteCarloBatchResult(
            n_simulations=n_simulations,
            use_paired_crn=use_paired_crn,
            baseline=base_stats,
            variants=variant_stats,
            deltas_vs_baseline=delta_stats,
        )


# ---------------------------------------------------------------------------
# Colab Training Objectives (Stage 1 & 2) & ONNX Export Helpers (Stage 4)
# ---------------------------------------------------------------------------

def compute_multitask_pretrain_loss(
    outputs: Dict[str, Tensor],
    targets: Dict[str, Tensor],
    sample_weights: Optional[FloatTensor] = None,
) -> Tensor:
    """Multi-task pre-training / Stage-2 debiasing objective (03 §4.1)."""
    def _ce(logits: Tensor, tgt: Tensor) -> Tensor:
        loss = F.cross_entropy(logits.reshape(-1, logits.shape[-1]), tgt.reshape(-1), reduction="none")
        if sample_weights is not None:
            if sample_weights.shape == tgt.shape:
                w = sample_weights.reshape(-1)
            elif sample_weights.dim() == 1 and tgt.dim() >= 2 and sample_weights.shape[0] == tgt.shape[0]:
                w = sample_weights.view(-1, *([1] * (tgt.dim() - 1))).expand_as(tgt).reshape(-1)
            else:
                w = sample_weights.reshape(-1)
            return (loss * w).sum() / w.sum().clamp(min=1e-6)
        return loss.mean()

    loss = (
        0.5 * _ce(outputs["dt_bucket_logits"], targets["dt_bucket"])
        + 1.0 * _ce(outputs["actor_logits"], targets["actor_slot"])
        + 1.5 * _ce(outputs["action_logits"], targets["action_type"])
        + 0.4 * _ce(outputs["pass_height_logits"], targets["pass_height"])
        + 0.4 * _ce(outputs["body_part_logits"], targets["body_part"])
        + 0.5 * F.binary_cross_entropy_with_logits(outputs["flags_logits"], targets["flags"].float())
        + 1.0 * _ce(outputs["start_cell_logits"], targets["start_cell"])
        + 1.2 * _ce(outputs["end_cell_logits"], targets["end_cell"])
        + 0.5 * _ce(outputs["end_z_logits"], targets["end_z_bin"])
        + 0.8 * _ce(outputs["target_logits"], targets["target_slot"])
        + 1.5 * _ce(outputs["outcome_logits"], targets["outcome"])
    )
    if "dt_res_target" in targets:
        loss = loss + 0.3 * F.smooth_l1_loss(outputs["dt_res"], targets["dt_res_target"])
    if "start_offset_target" in targets:
        loss = loss + 0.3 * F.smooth_l1_loss(outputs["start_offset"], targets["start_offset_target"])
    if "end_offset_target" in targets:
        loss = loss + 0.3 * F.smooth_l1_loss(outputs["end_offset"], targets["end_offset_target"])
    if "aux_analytics_target" in targets:
        loss = loss + 0.4 * F.mse_loss(outputs["aux_analytics"], targets["aux_analytics_target"])
    return loss


def compute_ipw_weights(
    propensity_scores: FloatTensor,
    marginal_probs: FloatTensor,
    clip_max: float = 3.0,
) -> FloatTensor:
    """Stabilized Inverse-Propensity Weights for Stage 2 counterfactual debiasing (03 §4.2)."""
    w = marginal_probs / propensity_scores.clamp(min=1e-4)
    return w.clamp(min=0.2, max=clip_max)


class _BackboneExportWrapper(nn.Module):
    """Stateless ONNX-exportable wrapper over PixelPitchEventTF backbone + micro-decoder h_proj."""

    def __init__(self, model: PixelPitchEventTF):
        super().__init__()
        self.model = model

    def forward(
        self,
        action_type: LongTensor,
        actor_slot: LongTensor,
        start_cell: LongTensor,
        end_cell: LongTensor,
        player_cond: FloatTensor,
        s_t: FloatTensor,
    ) -> FloatTensor:
        prev_ev = {
            "action_type": action_type,
            "actor_slot": actor_slot,
            "start_cell": start_cell,
            "end_cell": end_cell,
        }
        mem, c_t = self.model.encode_memory_and_state(player_cond, s_t)
        c_t_seq = c_t.unsqueeze(1).expand(-1, action_type.shape[1], -1)
        x = self.model.event_embedder(prev_ev)
        for layer in self.model.layers:
            x, _ = layer(x, mem=mem, c_t=c_t_seq, past_kv=None, pos_offset=0)
        return self.model.norm_final(x)


def export_transformer_to_onnx(
    model: PixelPitchEventTF,
    output_path: str | Path,
    seq_len: int = 8,
    opset_version: int = 18,
) -> Path:
    """Export PixelPitchEventTF backbone to ONNX for CPU/edge deployment (Stage 4)."""
    out = Path(output_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    model.eval()
    wrapper = _BackboneExportWrapper(model).eval()
    cfg = model.config
    dummy_action = torch.zeros((1, seq_len), dtype=torch.long)
    dummy_actor = torch.ones((1, seq_len), dtype=torch.long)
    dummy_cell = torch.full((1, seq_len), 300, dtype=torch.long)
    dummy_pcond = torch.zeros((1, 22, cfg.player_cond_dim), dtype=torch.float32)
    dummy_st = torch.zeros((1, cfg.state_dim), dtype=torch.float32)

    torch.onnx.export(
        wrapper,
        (dummy_action, dummy_actor, dummy_cell, dummy_cell, dummy_pcond, dummy_st),
        str(out),
        input_names=["action_type", "actor_slot", "start_cell", "end_cell", "player_cond", "s_t"],
        output_names=["hidden_states"],
        dynamic_axes={
            "action_type": {0: "batch", 1: "seq"},
            "actor_slot": {0: "batch", 1: "seq"},
            "start_cell": {0: "batch", 1: "seq"},
            "end_cell": {0: "batch", 1: "seq"},
            "player_cond": {0: "batch"},
            "s_t": {0: "batch"},
            "hidden_states": {0: "batch", 1: "seq"},
        },
        opset_version=opset_version,
        dynamo=False,
    )
    return out


def quantize_onnx_int8(onnx_path: str | Path, quantized_path: str | Path) -> Path:
    """Apply ONNX Runtime dynamic INT8 quantization if onnxruntime.quantization is available."""
    from onnxruntime.quantization import QuantType, quantize_dynamic

    src = Path(onnx_path)
    dst = Path(quantized_path)
    dst.parent.mkdir(parents=True, exist_ok=True)
    quantize_dynamic(model_input=str(src), model_output=str(dst), weight_type=QuantType.QInt8)
    return dst


# ---------------------------------------------------------------------------
# Module E Bridge Entrypoints (Consumed by api.v2.presentation.EngineV2Bridge)
# ---------------------------------------------------------------------------

RolloutEngine = HybridRolloutEngine
PixelPitchEventTransformer = PixelPitchEventTF
OutcomeModelsV2 = ModuleDOutcomeSuite

_DEFAULT_ENGINE_CACHE: Dict[str, HybridRolloutEngine] = {}


def inspect_models_dir(models_dir: Optional[str | Path] = None) -> Dict[str, Any]:
    """Return status of trained production checkpoints in models_v2/ for /v2/health."""
    import json
    import os

    mdir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
    expected = (
        "events_gold_v2.parquet",
        "player_profiles_v2.parquet",
        "player_id_crosswalk.parquet",
        "tactical_manifold_v2.pkl",
        "xt_grid_24x16.npy",
        "spatial_predictor.pt",
        "module_d_suite.pt",
        "pixelpitch_event_tf_112m.pt",
        "training_metrics.json",
    )
    present = [name for name in expected if (mdir / name).exists()]
    metrics: Dict[str, Any] = {}
    if (mdir / "training_metrics.json").exists():
        try:
            metrics = json.loads((mdir / "training_metrics.json").read_text(encoding="utf-8"))
        except Exception:
            pass
    weights_loaded = ("pixelpitch_event_tf_112m.pt" in present) and ("module_d_suite.pt" in present)
    return {
        "models_dir": str(mdir),
        "weights_loaded": weights_loaded,
        "checkpoint_files": present,
        "training_metrics": metrics,
    }


def _get_cached_engine(device_str: str, models_dir: Optional[str | Path] = None) -> HybridRolloutEngine:
    import os

    dev = "cuda" if ("cuda" in device_str and torch.cuda.is_available()) else "cpu"
    mdir = Path(models_dir or os.getenv("MODEL_V2_DIR", "./models_v2"))
    tf_mtime = int((mdir / "pixelpitch_event_tf_112m.pt").stat().st_mtime) if (mdir / "pixelpitch_event_tf_112m.pt").exists() else 0
    cache_key = f"{dev}:{mdir}:{tf_mtime}"
    if cache_key not in _DEFAULT_ENGINE_CACHE:
        if (mdir / "pixelpitch_event_tf_112m.pt").exists():
            _DEFAULT_ENGINE_CACHE[cache_key] = HybridRolloutEngine.from_models_dir(mdir, device=dev)
        else:
            _DEFAULT_ENGINE_CACHE[cache_key] = HybridRolloutEngine(
                model=PixelPitchEventTF(EventTransformerConfig.tiny_test()),
                device=dev,
            )
    return _DEFAULT_ENGINE_CACHE[cache_key]


def _coerce_match_config(raw: Dict[str, Any] | Any) -> Any:
    """Parse full MatchConfigurationV2 or hydrate a partial presentation payload via defaults."""
    from api.v2.tactical.config_schema import (
        MatchConfigurationV2,
        TacticalInstructions,
        build_default_team_config,
    )

    if isinstance(raw, MatchConfigurationV2):
        return raw
    data = dict(raw or {})
    try:
        return MatchConfigurationV2.model_validate(data)
    except Exception:
        pass

    home_raw = dict(data.get("home") or data.get("home_team") or {})
    away_raw = dict(data.get("away") or data.get("away_team") or {})
    mid_raw = data.get("match_id", 1)
    try:
        mid = max(1, int(mid_raw))
    except (TypeError, ValueError):
        mid = max(1, abs(hash(str(mid_raw))) % 1_000_000)

    def _hydrate_team(t_dict: Dict[str, Any], default_id: int, default_name: str, default_form: str):
        name = str(t_dict.get("team_name") or default_name)
        tid = int(t_dict.get("team_id") or default_id)
        form = t_dict.get("formation", {})
        form_name = form.get("base_name") if isinstance(form, dict) else str(form or default_form)
        tac_raw = t_dict.get("tactical_instructions") or t_dict.get("tactics") or {}
        team = build_default_team_config(team_id=tid, team_name=name, formation_name=form_name or default_form)
        if tac_raw:
            team = team.model_copy(
                update={"tactical_instructions": TacticalInstructions.model_validate(tac_raw)}
            )
        return team

    return MatchConfigurationV2(
        match_id=mid,
        home=_hydrate_team(home_raw, 1, "Home FC", "4-3-3"),
        away=_hydrate_team(away_raw, 2, "Away FC", "4-2-3-1"),
        random_seed=data.get("random_seed"),
    )


def _apply_dot_patches(cfg: Any, patches: Sequence[Any]) -> Any:
    """Apply MonteCarloPatch target/value mutations cleanly to MatchConfigurationV2."""
    dumped = cfg.model_dump(mode="python")
    for patch in patches:
        target = getattr(patch, "target", None) or (patch.get("target") if isinstance(patch, dict) else "")
        value = getattr(patch, "value", None) if not isinstance(patch, dict) else patch.get("value")
        parts = [p for p in str(target).split(".") if p]
        if not parts:
            continue
        cur = dumped
        for p in parts[:-1]:
            if p not in cur or not isinstance(cur[p], dict):
                cur[p] = {}
            cur = cur[p]
        cur[parts[-1]] = value
    from api.v2.tactical.config_schema import MatchConfigurationV2
    return MatchConfigurationV2.model_validate(dumped)


async def stream_match_v2(request: Any, session: Any, routing: Any):
    """Async generator bridging HybridRolloutEngine -> MatchStreamFrameV2 for SSE routes."""
    import asyncio
    from api.v2.presentation.schemas_v2 import (
        FrameKind,
        MatchLifecycleV2,
        MatchStreamFrameV2,
        PitchSpatialFrameV2,
        PlayerSpatialStateV2,
        StructuredEventV2,
        TelemetrySnapshotV2,
    )

    match_cfg = _coerce_match_config(getattr(request, "match_config", {}))
    engine = _get_cached_engine(getattr(routing, "device", "cpu"))
    max_events = int(getattr(request, "max_events", 400) or 400)
    seed = int(getattr(match_cfg, "random_seed", None) or 42)
    match_id_str = str(getattr(session, "match_id", match_cfg.match_id))
    home_name = match_cfg.home.team_name
    away_name = match_cfg.away.team_name

    yield MatchStreamFrameV2(
        match_id=match_id_str,
        sequence_no=1,
        match_clock_seconds=0,
        period=1,
        kind=FrameKind.LIFECYCLE,
        lifecycle=MatchLifecycleV2(
            status="match_start",
            message=f"Kick-off: {home_name} vs {away_name}",
            execution_tier=getattr(routing, "tier", "local_cpu_onnx"),
        ),
    )

    override_q: List[Dict[str, Any]] = []
    if hasattr(session, "override_queue"):
        while not session.override_queue.empty():
            override_q.append(session.override_queue.get_nowait())

    loop = asyncio.get_running_loop()
    step_queue: asyncio.Queue[Any] = asyncio.Queue()
    _SENTINEL = object()

    def _run_producer() -> None:
        try:
            for _ in engine.simulate_match_stream(
                match_config=match_cfg,
                max_events=max_events,
                seed=seed,
                tactical_override_queue=override_q,
                on_step_frame=lambda f: loop.call_soon_threadsafe(step_queue.put_nowait, f),
            ):
                pass
        except Exception as exc:
            loop.call_soon_threadsafe(step_queue.put_nowait, exc)
        finally:
            loop.call_soon_threadsafe(step_queue.put_nowait, _SENTINEL)

    producer_task = asyncio.create_task(asyncio.to_thread(_run_producer))

    seq = 1
    idx = -1
    last_step: Optional[MatchStepFrame] = None
    last_tel_idx = -1
    try:
        while True:
            item = await step_queue.get()
            if item is _SENTINEL:
                break
            if isinstance(item, Exception):
                raise item
            step: MatchStepFrame = item
            idx += 1
            last_step = step

            if hasattr(session, "override_queue"):
                while not session.override_queue.empty():
                    override_q.append(session.override_queue.get_nowait())

            ev = dict(step.event)
            side = int(ev.get("possessing_team_side", 0))
            ev["team_name"] = home_name if side == 1 else (away_name if side == 2 else "")
            ev.setdefault("player_name", f"Player {ev.get('actor_slot', 0)}")

            seq += 1
            yield MatchStreamFrameV2(
                match_id=match_id_str,
                sequence_no=seq,
                match_clock_seconds=step.match_clock_seconds,
                period=min(5, step.period),
                kind=FrameKind.EVENT,
                event=StructuredEventV2.model_validate(ev),
            )

            # Emit spatial keyframe every 2 events + telemetry snapshot every 5 events
            if idx % 2 == 0 or step.lifecycle_event is not None:
                seq += 1
                players = [
                    PlayerSpatialStateV2(
                        slot=s + 1,
                        player_id=s + 1,
                        x=row[0],
                        y=row[1],
                        vx=row[2],
                        vy=row[3],
                        stamina=row[4],
                        has_ball=(s + 1 == ev.get("actor_slot")),
                        is_sent_off=bool(row[5]),
                    )
                    for s, row in enumerate(step.spatial_positions)
                ]
                yield MatchStreamFrameV2(
                    match_id=match_id_str,
                    sequence_no=seq,
                    match_clock_seconds=step.match_clock_seconds,
                    period=min(5, step.period),
                    kind=FrameKind.SPATIAL,
                    spatial=PitchSpatialFrameV2(
                        sim_timestamp_ms=step.clock_ms,
                        ball_x=step.ball_xyz[0],
                        ball_y=step.ball_xyz[1],
                        ball_z=step.ball_xyz[2],
                        players=players,
                        home_voronoi_control=step.home_voronoi_control,
                    ),
                )

            if idx % 5 == 0 or step.lifecycle_event == "match_end":
                seq += 1
                last_tel_idx = idx
                yield MatchStreamFrameV2(
                    match_id=match_id_str,
                    sequence_no=seq,
                    match_clock_seconds=step.match_clock_seconds,
                    period=min(5, step.period),
                    kind=FrameKind.TELEMETRY,
                    telemetry=TelemetrySnapshotV2.model_validate(step.telemetry),
                )

            if step.lifecycle_event == "second_half_start":
                seq += 1
                yield MatchStreamFrameV2(
                    match_id=match_id_str,
                    sequence_no=seq,
                    match_clock_seconds=step.match_clock_seconds,
                    period=min(5, step.period),
                    kind=FrameKind.LIFECYCLE,
                    lifecycle=MatchLifecycleV2(
                        status="second_half_start",
                        message="Second half kick-off",
                        home_score=ev["home_score"],
                        away_score=ev["away_score"],
                        execution_tier=getattr(routing, "tier", "local_cpu_onnx"),
                    ),
                )
            elif step.lifecycle_event == "match_end":
                seq += 1
                yield MatchStreamFrameV2(
                    match_id=match_id_str,
                    sequence_no=seq,
                    match_clock_seconds=step.match_clock_seconds,
                    period=min(5, step.period),
                    kind=FrameKind.LIFECYCLE,
                    lifecycle=MatchLifecycleV2(
                        status="match_end",
                        message=f"Full time: {home_name} {ev['home_score']} - {ev['away_score']} {away_name}",
                        home_score=ev["home_score"],
                        away_score=ev["away_score"],
                        execution_tier=getattr(routing, "tier", "local_cpu_onnx"),
                    ),
                )

        if last_step is not None and last_tel_idx != idx:
            seq += 1
            yield MatchStreamFrameV2(
                match_id=match_id_str,
                sequence_no=seq,
                match_clock_seconds=last_step.match_clock_seconds,
                period=min(5, last_step.period),
                kind=FrameKind.TELEMETRY,
                telemetry=TelemetrySnapshotV2.model_validate(last_step.telemetry),
            )
    finally:
        await producer_task


async def run_monte_carlo_experiment(
    request: Any,
    routing: Any,
    progress_cb: Any = None,
) -> Dict[str, Any]:
    """Async entrypoint executing paired-CRN Monte Carlo counterfactual arms for Module E."""
    import asyncio
    import time

    t0 = time.perf_counter()
    base_cfg = _coerce_match_config(getattr(request, "baseline_config", {}))
    n_sims = int(getattr(request, "n_simulations", 50) or 50)
    use_crn = bool(getattr(request, "use_paired_crn", True))
    base_seed = int(getattr(request, "base_seed", 42000) or 42000)
    sample_epistemic = bool(getattr(request, "sample_player_epistemic_uncertainty", False))
    interventions = list(getattr(request, "interventions", []) or [])

    variant_cfgs: Dict[str, Any] = {}
    for arm in interventions:
        arm_id = getattr(arm, "arm_id", None) or arm.get("arm_id")
        patches = getattr(arm, "patches", None) if not isinstance(arm, dict) else arm.get("patches", [])
        variant_cfgs[str(arm_id)] = _apply_dot_patches(base_cfg, patches or [])

    engine = _get_cached_engine(getattr(routing, "device", "cpu"))
    if progress_cb is not None:
        await progress_cb(0, n_sims, "simulating")

    mc_res = await asyncio.to_thread(
        engine.run_monte_carlo_batch,
        n_simulations=n_sims,
        max_events=int(getattr(request, "max_events", 250) or 250),
        base_seed=base_seed,
        use_paired_crn=use_crn,
        baseline_config=base_cfg,
        variant_configs=variant_cfgs,
        sample_epistemic=sample_epistemic,
    )

    if progress_cb is not None:
        await progress_cb(n_sims, n_sims, "aggregating")

    def _arm_to_dict(arm: MonteCarloArmStats, delta: Optional[MonteCarloDeltaStats] = None) -> Dict[str, Any]:
        out: Dict[str, Any] = {
            "arm_id": arm.arm_id,
            "outcome_probs": {
                "home_win": arm.home_win_prob,
                "draw": arm.draw_prob,
                "away_win": arm.away_win_prob,
            },
            "expected_points": arm.expected_points_home,
            "metrics_summary": {
                "home_xg": {"mean": arm.home_xg_mean, "ci95": list(arm.home_xg_ci95)},
                "away_xg": {"mean": arm.away_xg_mean, "ci95": list(arm.away_xg_ci95)},
                "home_xt": {"mean": arm.home_xt_mean},
                "away_xt": {"mean": arm.away_xt_mean},
                "home_goals": {"mean": arm.home_goals_mean},
                "away_goals": {"mean": arm.away_goals_mean},
                "events_per_match": arm.events_per_match_mean,
            },
            "representative_match_seeds": arm.representative_seeds,
        }
        if delta is not None:
            out["delta_vs_baseline"] = {
                "home_win_prob_delta": delta.home_win_prob_delta,
                "expected_points_delta": delta.expected_points_delta,
                "home_xg_delta": {"mean": delta.home_xg_delta_mean, "ci95": list(delta.home_xg_delta_ci95)},
                "away_xg_delta": {"mean": delta.away_xg_delta_mean, "ci95": list(delta.away_xg_delta_ci95)},
                "p_value": delta.p_value_xg_diff,
            }
        return out

    arms_payload = [_arm_to_dict(mc_res.baseline)]
    for arm_id, arm_st in mc_res.variants.items():
        arms_payload.append(_arm_to_dict(arm_st, mc_res.deltas_vs_baseline.get(arm_id)))

    return {
        "experiment_id": getattr(request, "experiment_id", "exp_v2"),
        "n_simulations": n_sims,
        "use_paired_crn": use_crn,
        "execution_time_ms": int((time.perf_counter() - t0) * 1000),
        "arms": arms_payload,
    }
