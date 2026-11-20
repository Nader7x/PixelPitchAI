"""Unit tests for V2-only graceful boot (absent V1 weights) and loop/FSM optimizations."""

from __future__ import annotations

import logging
from pathlib import Path
from unittest.mock import patch

import torch
from fastapi.testclient import TestClient

from api.v2.simulator.fsm import (
    ActionType,
    BallState,
    EventOutcome,
    FootballFSM,
    Period,
)
from api.v2.simulator.rollout_engine import HybridRolloutEngine, _coerce_match_config
from api.v2.tactical.spatial_solver import OffBallSpatialSolver


def test_v2_only_boot_without_v1_weights(tmp_path: Path, caplog):
    """ModelResources, UltraOptimizedModelResources, and FastAPI app boot cleanly when V1 weights are missing."""
    missing_gpt2 = str(tmp_path / "missing-gpt2")
    missing_xgb = str(tmp_path / "missing_xgb.json")

    with caplog.at_level(logging.WARNING):
        with (
            patch("api.services.simulation_service.MODEL_PATH", missing_gpt2),
            patch("api.services.simulation_service.XGB_MODEL_PATH", missing_xgb),
        ):
            from api.services.simulation_service import ModelResources

            res_v1 = ModelResources()
            assert res_v1.model is None
            assert res_v1.tokenizer is None
            assert res_v1.xgboost_model is None
            assert res_v1.is_healthy() == (False, False)

        with (
            patch("api.services.optimized_simulation_service.MODEL_PATH", missing_gpt2),
            patch("api.services.optimized_simulation_service.ONNX_MODEL_PATH", str(tmp_path / "missing-onnx")),
            patch("api.services.optimized_simulation_service.XGB_MODEL_PATH", missing_xgb),
        ):
            from api.services.optimized_simulation_service import UltraOptimizedModelResources

            res_opt = UltraOptimizedModelResources()
            assert res_opt.model is None
            assert res_opt.tokenizer is None
            assert res_opt.xgboost_model is None
            assert res_opt.is_healthy() == (False, False)

            from api.main import app

            with (
                patch("api.main.start_grpc_server", return_value=None),
                patch("api.main.stop_grpc_server", return_value=None),
            ):
                with TestClient(app) as client:
                    resp = client.get("/v2/health")
                    assert resp.status_code == 200
                    data = resp.json()
                    assert data["status"] in ("healthy", "degraded")

    assert any("V2-only mode" in rec.message for rec in caplog.records)


def test_branchless_fsm_cards_and_period_transitions():
    """Branchless FootballFSM.step preserves yellow->red escalation and period boundaries."""
    fsm = FootballFSM()
    state = fsm.init_state(batch_size=2, extra_time_enabled=False, device=torch.device("cpu"))

    dt = torch.tensor([2.0, 2.0])
    actor_slot = torch.tensor([5, 14], dtype=torch.long)
    action_type = torch.tensor([ActionType.FOUL_COMMITTED, ActionType.PASS], dtype=torch.long)
    start_xy = torch.tensor([[50.0, 40.0], [60.0, 40.0]])
    end_xy = torch.tensor([[50.0, 40.0], [70.0, 40.0]])
    end_z = torch.tensor([0.0, 0.0])
    outcome = torch.tensor([EventOutcome.YELLOW_CARD, EventOutcome.COMPLETE], dtype=torch.long)

    # First yellow card on batch 0, slot 5 (index 4)
    state = fsm.step(state, dt, actor_slot, action_type, start_xy, end_xy, end_z, outcome)
    assert bool(state.yellow_cards[0, 4])
    assert not bool(state.red_cards[0, 4])
    assert bool(state.active_mask[0, 4])

    # Second yellow card on batch 0, slot 5 -> escalates to red + inactive
    state = fsm.step(state, dt, actor_slot, action_type, start_xy, end_xy, end_z, outcome)
    assert bool(state.red_cards[0, 4])
    assert not bool(state.active_mask[0, 4])

    # Period boundary: 1H -> 2H -> FT
    b_act = torch.tensor([ActionType.PERIOD_BOUNDARY, ActionType.PERIOD_BOUNDARY], dtype=torch.long)
    b_out = torch.tensor([EventOutcome.PERIOD_END, EventOutcome.PERIOD_END], dtype=torch.long)
    b_slot = torch.tensor([0, 0], dtype=torch.long)
    state = fsm.step(state, dt, b_slot, b_act, start_xy, end_xy, end_z, b_out)
    assert (state.period == Period.SECOND_HALF).all()
    assert (state.ball_state == BallState.KICK_OFF).all()

    state = fsm.step(state, dt, b_slot, b_act, start_xy, end_xy, end_z, b_out)
    assert (state.period == Period.FULLTIME).all()
    assert state.finished.all()


def test_spatial_solver_and_rollout_determinism():
    """Optimized SpatialSolver & HybridRolloutEngine remain deterministic and emit valid frames."""
    solver = OffBallSpatialSolver(auto_load=False)
    assert solver.is_home_22.shape == (1, 22)
    assert solver.eye_22.shape == (1, 22, 22)

    engine = HybridRolloutEngine.from_models_dir(device="cpu")
    cfg = _coerce_match_config({})
    res_a = engine._run_batched_steps(batch_size=1, max_events=25, seed=123, match_config=cfg, collect_events=True)
    res_b = engine._run_batched_steps(batch_size=1, max_events=25, seed=123, match_config=cfg, collect_events=True)

    assert len(res_a["frames"]) == 25
    assert torch.equal(res_a["home_passes"], res_b["home_passes"])
    assert torch.allclose(res_a["home_xg"], res_b["home_xg"])
    f0 = res_a["frames"][0]["step"]
    assert len(f0.spatial_positions) == 22
    assert len(f0.spatial_positions[0]) == 6
