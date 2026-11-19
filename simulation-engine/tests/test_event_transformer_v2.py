"""Comprehensive tests for Modules C & D (api/v2/simulator) and Module B integration."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
import torch

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from api.v2.simulator import (
    ActionType,
    BallState,
    BellmanXTGrid,
    BodyPart,
    EventOutcome,
    EventTransformerConfig,
    FootballFSM,
    HybridRolloutEngine,
    ModuleDOutcomeSuite,
    PassHeight,
    Period,
    PixelPitchEventTF,
    ResidualCalibratedMLP,
    RestartType,
    apply_bayesian_log_odds,
    build_default_freeze_frame_360,
    compute_ipw_weights,
    compute_multitask_pretrain_loss,
    distill_mlp_from_teacher,
    expected_calibration_error,
    export_transformer_to_onnx,
)
from api.v2.simulator.fsm import NUM_ACTIONS, NUM_MODIFIER_FLAGS, xy_to_cell


# ---------------------------------------------------------------------------
# 1. Model Sizing & Architecture Tests (Base-112M & Scaling Tiers)
# ---------------------------------------------------------------------------

def test_base_112m_parameter_count_and_architecture():
    cfg = EventTransformerConfig.base_112m()
    assert cfg.d_model == 768
    assert cfg.n_layers == 12
    assert cfg.n_heads == 12
    assert cfg.n_kv_heads == 4  # 3:1 GQA
    assert cfg.max_seq_len == 16384
    assert cfg.rope_theta == 500_000.0

    model = PixelPitchEventTF(cfg)
    counts = model.count_parameters()
    total_m = counts["total"] / 1e6
    assert 108.0 <= total_m <= 116.0, f"Expected ~112M Base params, got {total_m:.2f}M"
    # Verify < 3% parameters sit in input embeddings (vs 31% in legacy GPT-2 BPE)
    assert counts["event_embedder"] / counts["total"] < 0.03

    # Verify scaling tier boundaries (78M .. 155M)
    small_m = PixelPitchEventTF(EventTransformerConfig.small_78m()).count_parameters()["total"] / 1e6
    med_m = PixelPitchEventTF(EventTransformerConfig.medium_128m()).count_parameters()["total"] / 1e6
    large_m = PixelPitchEventTF(EventTransformerConfig.large_154m()).count_parameters()["total"] / 1e6
    assert 75.0 <= small_m <= 85.0
    assert 124.0 <= med_m <= 134.0
    assert 148.0 <= large_m <= 160.0

    # Verify Base-112M forward conditioning & single-step KV decode shapes
    B = 2
    player_cond = torch.randn(B, 22, 128)
    s_t = torch.randn(B, 64)
    team_tac = torch.randn(B, 2, 18)
    mem, c_t = model.encode_memory_and_state(player_cond, s_t, team_tac)
    assert mem.shape == (B, 28, 768)
    assert c_t.shape == (B, 512)

    fsm = FootballFSM()
    state = fsm.init_state(B)
    masks = fsm.compute_masks(state)
    prev_ev = HybridRolloutEngine._initial_prev_event(B, torch.device("cpu"))
    sampled, kvs = model.step_decode(
        prev_event=prev_ev,
        player_cond=player_cond,
        s_t=s_t,
        fsm_masks=masks,
        poss_team=state.poss_team,
        team_tactics=team_tac,
        outcome_mask_fn=fsm.outcome_mask_for_action,
    )
    assert len(kvs) == 12
    # GQA KV cache shape: [B, n_kv_heads=4, seq=1, head_dim=64]
    assert kvs[0][0].shape == (B, 4, 1, 64)
    assert sampled["action_type"].shape == (B,)
    assert torch.all(sampled["action_type"] == ActionType.PASS)  # KICK_OFF hard mask


# ---------------------------------------------------------------------------
# 2. FootballFSM Laws of the Game & Hard Legality Mask Tests
# ---------------------------------------------------------------------------

def test_fsm_restart_masks_and_state_transitions():
    fsm = FootballFSM()
    B = 4
    state = fsm.init_state(batch_size=B, first_half_kickoff_team=1)

    # Kick-Off: only PASS legal, start at (60,40), own half target, Home outfielder only
    masks = fsm.compute_masks(state)
    assert torch.all(masks.action_mask[:, ActionType.PASS])
    assert not masks.action_mask[:, ActionType.SHOT].any()
    assert not masks.actor_mask[:, 0].any()       # Home GK masked on kick-off
    assert not masks.actor_mask[:, 11:].any()     # Away masked on Home kick-off
    assert torch.all(masks.restart_type_forced == RestartType.KICK_OFF)

    # Corner Kick: Home (120, 0) & Away (0, 80) -> only PASS (+ SUBSTITUTION) legal, SHOT/CARRY masked
    state.ball_state[:] = BallState.CORNER_KICK
    state.poss_team = torch.tensor([1, 2, 1, 2])
    state.ball_xy = torch.tensor([[120.0, 0.0], [0.0, 80.0], [120.0, 80.0], [0.0, 0.0]])
    c_masks = fsm.compute_masks(state)
    assert torch.all(c_masks.action_mask[:, ActionType.PASS])
    assert not c_masks.action_mask[:, ActionType.SHOT].any()
    assert not c_masks.action_mask[:, ActionType.CARRY].any()
    assert torch.all(c_masks.restart_type_forced == RestartType.CORNER)

    # Penalty -> only SHOT legal
    state.ball_state[:] = BallState.PENALTY
    p_masks = fsm.compute_masks(state)
    assert torch.all(p_masks.action_mask[:, ActionType.SHOT])
    assert not p_masks.action_mask[:, ActionType.PASS].any()

    # Open Play Shot Distance Guard (>42yd blocked unless minute>=88 & trailing)
    state.ball_state[:] = BallState.IN_PLAY
    state.poss_team[:] = 1
    state.ball_xy = torch.tensor([[20.0, 40.0], [105.0, 40.0], [20.0, 40.0], [105.0, 40.0]])
    state.home_score = torch.tensor([0, 0, 0, 0])
    state.away_score = torch.tensor([0, 0, 1, 0])
    state.clock_ms = torch.tensor([1_000_000, 1_000_000, 5_340_000, 1_000_000])  # idx 2 is 89' trailing
    op_masks = fsm.compute_masks(state)
    assert not bool(op_masks.action_mask[0, ActionType.SHOT])  # 100 yds away at 16' -> masked
    assert bool(op_masks.action_mask[1, ActionType.SHOT])      # 15 yds away -> allowed
    assert bool(op_masks.action_mask[2, ActionType.SHOT])      # 89' trailing Hail Mary -> allowed

    # Goal -> score increments + KICK_OFF by conceding team at (60, 40)
    nxt = fsm.step(
        state=state,
        dt_seconds=torch.tensor([1.5] * B),
        actor_slot=torch.tensor([10, 10, 20, 10]),
        action_type=torch.full((B,), ActionType.SHOT),
        start_xy=state.ball_xy,
        end_xy=torch.tensor([[120.0, 40.0], [120.0, 40.0], [0.0, 40.0], [120.0, 40.0]]),
        end_z=torch.full((B,), 1.2),
        outcome=torch.tensor([EventOutcome.GOAL, EventOutcome.OFF_TARGET, EventOutcome.GOAL, EventOutcome.SAVED_HELD]),
    )
    assert nxt.home_score.tolist() == [1, 0, 0, 0]
    assert nxt.away_score.tolist() == [0, 0, 2, 0]
    assert nxt.ball_state[0] == BallState.KICK_OFF and nxt.poss_team[0] == 2
    assert nxt.ball_state[1] == BallState.GOAL_KICK and nxt.poss_team[1] == 2
    assert nxt.ball_state[2] == BallState.KICK_OFF and nxt.poss_team[2] == 1
    assert nxt.ball_state[3] == BallState.IN_PLAY and nxt.poss_team[3] == 2  # Saved held flips possession

    # Second Yellow -> Permanent Red Card slot removal
    nxt2 = fsm.step(
        state=nxt,
        dt_seconds=torch.ones(B),
        actor_slot=torch.full((B,), 5),
        action_type=torch.full((B,), ActionType.FOUL_COMMITTED),
        start_xy=torch.tensor([[50.0, 30.0]] * B),
        end_xy=torch.tensor([[50.0, 30.0]] * B),
        end_z=torch.zeros(B),
        outcome=torch.full((B,), EventOutcome.YELLOW_CARD),
    )
    assert bool(nxt2.yellow_cards[0, 4]) and bool(nxt2.active_mask[0, 4])
    nxt3 = fsm.step(
        state=nxt2,
        dt_seconds=torch.ones(B),
        actor_slot=torch.full((B,), 5),
        action_type=torch.full((B,), ActionType.FOUL_COMMITTED),
        start_xy=torch.tensor([[50.0, 30.0]] * B),
        end_xy=torch.tensor([[50.0, 30.0]] * B),
        end_z=torch.zeros(B),
        outcome=torch.full((B,), EventOutcome.YELLOW_CARD),
    )
    assert bool(nxt3.red_cards[0, 4]) and not bool(nxt3.active_mask[0, 4])
    m_after_red = fsm.compute_masks(nxt3)
    assert not bool(m_after_red.actor_mask[0, 4])

    # 1H Expiration -> automatic 2H Kick-Off by opposite team (team 2)
    nxt3.period[:] = Period.FIRST_HALF
    nxt3.ball_state[:] = BallState.IN_PLAY
    nxt3.period_elapsed_ms[:] = 50 * 60 * 1000  # Past 45 + stoppage
    b_masks = fsm.compute_masks(nxt3)
    assert torch.all(b_masks.force_period_boundary)
    assert torch.all(b_masks.actor_mask_23[:, 0])
    assert not b_masks.actor_mask_23[:, 1:].any()
    nxt4 = fsm.step(
        state=nxt3,
        dt_seconds=torch.zeros(B),
        actor_slot=torch.zeros(B, dtype=torch.long),
        action_type=torch.full((B,), ActionType.PERIOD_BOUNDARY),
        start_xy=torch.tensor([[60.0, 40.0]] * B),
        end_xy=torch.tensor([[60.0, 40.0]] * B),
        end_z=torch.zeros(B),
        outcome=torch.full((B,), EventOutcome.PERIOD_END),
    )
    assert torch.all(nxt4.period == Period.SECOND_HALF)
    assert torch.all(nxt4.ball_state == BallState.KICK_OFF)
    assert torch.all(nxt4.poss_team == 2)  # Opposite of 1H team 1


# ---------------------------------------------------------------------------
# 3. Module D Calibrated Outcome Models & Bayesian Log-Odds Physics Tests
# ---------------------------------------------------------------------------

def test_module_d_xg_xpass_xt_turnover_and_distillation():
    suite = ModuleDOutcomeSuite()

    # D4 Bellman xT monotonicity: central penalty box >> own defensive corner
    xt_pts = torch.tensor([[10.0, 10.0], [60.0, 40.0], [112.0, 40.0]])
    xt_vals = suite.xt_grid.bilinear_query(xt_pts, torch.tensor([1, 1, 1]))
    assert xt_vals[2] > xt_vals[1] > xt_vals[0]
    xt_delta = suite.xt_grid.compute_xt_delta(
        torch.tensor([[50.0, 40.0]]), torch.tensor([[105.0, 40.0]]), torch.tensor([1])
    )
    assert float(xt_delta.item()) > 0.05

    # Synthetic 360 freeze frames: [open close shot, pressured far shot with defenders in cone]
    pos = torch.tensor([
        [[6.0, 40.0], *[[40.0, 20.0 + i * 3] for i in range(9)], [111.0, 40.0],
         [118.0, 40.0], *[[70.0, 20.0 + i * 3] for i in range(10)]],
        [[6.0, 40.0], *[[40.0, 20.0 + i * 3] for i in range(9)], [93.0, 15.0],
         [118.0, 40.0], [100.0, 22.0], [104.0, 28.0], [108.0, 32.0], *[[80.0, 20.0 + i * 3] for i in range(7)]],
    ], dtype=torch.float32)
    vel = torch.zeros_like(pos)
    ff = build_default_freeze_frame_360(pos, vel, torch.tensor([1, 1]), torch.tensor([11, 11]))

    shot_out = suite.evaluate_shot(
        freeze_frame=ff,
        start_xy=torch.tensor([[111.0, 40.0], [93.0, 15.0]]),
        end_xy=torch.tensor([[120.0, 37.0], [120.0, 45.5]]),  # #0 on target corner, #1 off target
        end_z=torch.tensor([1.0, 3.2]),
        poss_team=torch.tensor([1, 1]),
        body_part=torch.tensor([BodyPart.RIGHT_FOOT, BodyPart.LEFT_FOOT]),
        restart_type=torch.tensor([RestartType.OPEN_PLAY, RestartType.OPEN_PLAY]),
        flags=torch.tensor([[0, 0, 0, 0, 0, 0, 0, 0], [1, 0, 0, 0, 0, 0, 0, 0]], dtype=torch.float32),
    )
    assert float(shot_out["xg"][0].detach()) > float(shot_out["xg"][1].detach()) * 3.0
    assert float(shot_out["psxg"][0].detach()) > 0.15
    assert float(shot_out["psxg"][1].detach()) == 0.0  # Off target shot -> 0.0 PSxG

    # Bayesian log-odds counterfactual monotonicity
    base_p = torch.tensor([0.20, 0.20, 0.20])
    p_mod = apply_bayesian_log_odds(
        base_p,
        actor_residual=torch.tensor([0.0, 0.45, 0.0]),
        opp_residual=torch.tensor([0.0, 0.0, 0.45]),
        actor_stamina=torch.tensor([0.90, 0.90, 0.90]),
    )
    assert p_mod[1] > p_mod[0] > p_mod[2]

    # D3 xPass: short open pass >> long pressured pass through defenders
    pass_out = suite.evaluate_pass(
        freeze_frame=ff,
        start_xy=torch.tensor([[45.0, 40.0], [45.0, 40.0]]),
        end_xy=torch.tensor([[53.0, 42.0], [108.0, 32.0]]),
        poss_team=torch.tensor([1, 1]),
        pass_height=torch.tensor([PassHeight.GROUND, PassHeight.HIGH]),
        restart_type=torch.tensor([RestartType.OPEN_PLAY, RestartType.OPEN_PLAY]),
        flags=torch.tensor([[0, 0, 0, 0, 0, 0, 0, 0], [1, 1, 0, 0, 0, 0, 0, 0]], dtype=torch.float32),
    )
    assert float(pass_out["xpass"][0]) > 0.85
    assert float(pass_out["xpass"][0]) > float(pass_out["xpass"][1]) + 0.15

    # D6 Pressure Turnover: high pressing + low stamina increases risk
    turn_low = suite.evaluate_pressure_turnover(
        ff[:1], torch.tensor([[60.0, 40.0]]), torch.tensor([1]),
        pressing_intensity=torch.tensor([0.20]), carrier_stamina=torch.tensor([0.95]),
    )
    turn_high = suite.evaluate_pressure_turnover(
        ff[:1], torch.tensor([[60.0, 40.0]]), torch.tensor([1]),
        pressing_intensity=torch.tensor([0.95]), carrier_stamina=torch.tensor([0.25]),
    )
    assert float(turn_high["turnover_risk"][0]) > float(turn_low["turnover_risk"][0])

    # Tier-2 MLP Distillation from synthetic teacher probabilities + ECE check
    torch.manual_seed(0)
    feats = torch.randn(256, 12)
    teacher_p = torch.sigmoid(0.8 * feats[:, 0] - 0.5 * feats[:, 1]).unsqueeze(-1)
    student = ResidualCalibratedMLP(in_dim=12, out_dim=1, hidden_dim=64)
    loss = distill_mlp_from_teacher(student, feats, teacher_p, steps=120, lr=2e-2)
    with torch.no_grad():
        pred_p = torch.sigmoid(student(feats).squeeze(-1))
        synthetic_y = torch.bernoulli(teacher_p.squeeze(-1))
        ece = expected_calibration_error(pred_p, synthetic_y, n_bins=10)
    assert loss < 0.65
    assert ece < 0.12


# ---------------------------------------------------------------------------
# 4. Multi-Hundred-Step Rollout & Module B Integration Tests
# ---------------------------------------------------------------------------

def test_multi_hundred_step_rollout_standalone_and_tactical_engine():
    engine = HybridRolloutEngine(model=PixelPitchEventTF(EventTransformerConfig.tiny_test()))
    fsm = engine.fsm

    # A. Standalone 320-step rollout + live tactical override
    overrides = [{"team_side": "home", "tactical_instructions_patch": {"pressing_intensity": 0.92}}]
    frames = list(engine.simulate_match_stream(max_events=320, seed=101, tactical_override_queue=overrides))
    assert len(frames) >= 150  # Plays until match_end or 320 steps
    assert len(overrides) == 0  # Override consumed cleanly

    prev_clock = -1
    for fr in frames:
        assert fr.clock_ms >= prev_clock
        prev_clock = fr.clock_ms
        ev = fr.event
        assert 0.0 <= ev["start_x"] <= 120.0 and 0.0 <= ev["start_y"] <= 80.0
        assert 0.0 <= ev["end_x"] <= 120.0 and 0.0 <= ev["end_y"] <= 80.0
        assert 0 <= ev["actor_slot"] <= 22
        assert 0.0 <= ev["xg"] <= 1.0 and 0.0 <= ev["xpass"] <= 1.0
        assert len(fr.spatial_positions) == 22
        for px, py, _, _, stam, _ in fr.spatial_positions:
            assert 0.0 <= px <= 120.0 and 0.0 <= py <= 80.0
            assert 0.0 <= stam <= 1.0

    # B. End-to-End Integration with Module A/B (MatchConfigurationV2 + TacticalStateEngine)
    from api.v2.tactical.config_schema import (
        FormationConfig,
        MatchConfigurationV2,
        TacticalInstructions,
        TeamMatchConfig,
        build_default_team_config,
    )

    home_cfg = build_default_team_config(team_id=2, team_name="Barcelona", formation_name="4-3-3")
    away_cfg = build_default_team_config(team_id=7, team_name="Real Madrid", formation_name="4-2-3-1")
    match_cfg = MatchConfigurationV2(match_id=999, home=home_cfg, away=away_cfg, random_seed=777)

    tac_frames = list(engine.simulate_match_stream(match_config=match_cfg, max_events=80, seed=777))
    assert len(tac_frames) == 80
    assert any(f.event["player_id"] > 0 for f in tac_frames)
    assert all(0.0 < f.home_voronoi_control < 1.0 for f in tac_frames)


# ---------------------------------------------------------------------------
# 5. Paired CRN Monte Carlo Batch Test
# ---------------------------------------------------------------------------

def test_paired_crn_monte_carlo_batch():
    engine = HybridRolloutEngine(model=PixelPitchEventTF(EventTransformerConfig.tiny_test()))

    # Identical baseline vs clone under paired CRN must yield zero delta
    res_same = engine.run_monte_carlo_batch(
        n_simulations=12,
        max_events=60,
        base_seed=12345,
        use_paired_crn=True,
        baseline_params={"finishing_residuals": torch.zeros(1, 22)},
        variants={"clone": {"finishing_residuals": torch.zeros(1, 22)}},
    )
    assert res_same.baseline.n_simulations == 12
    assert abs(res_same.deltas_vs_baseline["clone"].home_xg_delta_mean) < 1e-6
    assert abs(res_same.deltas_vs_baseline["clone"].home_win_prob_delta) < 1e-6
    assert len(res_same.baseline.representative_seeds) == 1

    # Counterfactual intervention with boosted finishing residuals
    boost = torch.zeros(1, 22)
    boost[:, 1:11] = 0.55
    res_cf = engine.run_monte_carlo_batch(
        n_simulations=16,
        max_events=80,
        base_seed=54321,
        use_paired_crn=True,
        baseline_params={"finishing_residuals": torch.zeros(1, 22)},
        variants={"elite_finishing": {"finishing_residuals": boost}},
    )
    delta = res_cf.deltas_vs_baseline["elite_finishing"]
    assert len(delta.home_xg_delta_ci95) == 2
    assert 0.0 <= delta.p_value_xg_diff <= 1.0


# ---------------------------------------------------------------------------
# 6. Colab Training Loss, IPW & ONNX Export Smoke Tests
# ---------------------------------------------------------------------------

def test_training_loss_ipw_and_onnx_export(tmp_path: Path):
    cfg = EventTransformerConfig.tiny_test()
    model = PixelPitchEventTF(cfg)
    B, L = 2, 6

    ev = {
        "dt_bucket": torch.randint(0, 16, (B, L)),
        "period": torch.ones((B, L), dtype=torch.long),
        "poss_phase": torch.zeros((B, L), dtype=torch.long),
        "actor_slot": torch.randint(1, 23, (B, L)),
        "action_type": torch.randint(0, 6, (B, L)),
        "restart_type": torch.zeros((B, L), dtype=torch.long),
        "pass_height": torch.ones((B, L), dtype=torch.long),
        "body_part": torch.zeros((B, L), dtype=torch.long),
        "start_cell": torch.randint(0, 600, (B, L)),
        "end_cell": torch.randint(0, 600, (B, L)),
        "end_z_bin": torch.zeros((B, L), dtype=torch.long),
        "target_slot": torch.randint(1, 23, (B, L)),
        "outcome": torch.zeros((B, L), dtype=torch.long),
        "flags": torch.zeros((B, L, NUM_MODIFIER_FLAGS)),
        "continuous": torch.zeros((B, L, 10)),
    }
    player_cond = torch.randn(B, 22, cfg.player_cond_dim)
    s_t = torch.randn(B, cfg.state_dim)
    team_tac = torch.randn(B, 2, cfg.team_tactics_dim)

    # Test Stage 2 CFG conditioning dropout masks
    drop_p = torch.tensor([True, False])
    drop_t = torch.tensor([False, True])
    out = model.forward_sequence(ev, ev, player_cond, s_t, team_tac, drop_player_mask=drop_p, drop_tactic_mask=drop_t)
    loss = compute_multitask_pretrain_loss(out, ev)
    assert torch.isfinite(loss) and loss.item() > 0.0
    loss.backward()

    # IPW weights
    ipw = compute_ipw_weights(torch.tensor([0.05, 0.50]), torch.tensor([0.30, 0.30]), clip_max=3.0)
    assert torch.allclose(ipw, torch.tensor([3.0, 0.6]))

    # ONNX export + ORT inference check
    onnx_file = export_transformer_to_onnx(model, tmp_path / "event_tf_tiny.onnx", seq_len=4)
    assert onnx_file.exists() and onnx_file.stat().st_size > 0
    import onnxruntime as ort
    sess = ort.InferenceSession(str(onnx_file), providers=["CPUExecutionProvider"])
    ort_out = sess.run(
        None,
        {
            "action_type": ev["action_type"][:, :4].numpy(),
            "actor_slot": ev["actor_slot"][:, :4].numpy(),
            "start_cell": ev["start_cell"][:, :4].numpy(),
            "end_cell": ev["end_cell"][:, :4].numpy(),
            "player_cond": player_cond.numpy(),
            "s_t": s_t.numpy(),
        },
    )
    assert ort_out[0].shape == (B, 4, cfg.d_model)


def test_module_e_bridge_entrypoints_and_readiness():
    import asyncio
    from types import SimpleNamespace
    from api.v2.presentation.routes_v2 import ActiveMatchSession, EngineV2Bridge
    from api.v2.presentation.schemas_v2 import (
        FrameKind,
        MonteCarloArmSpec,
        MonteCarloExperimentRequestV2,
        MonteCarloPatch,
        SimulateMatchV2Request,
    )

    bridge = EngineV2Bridge()
    probes = bridge.probe_readiness()
    assert probes["event_transformer_loaded"] is True
    assert probes["outcome_models_loaded"] is True
    assert probes["tactical_engine_loaded"] is True

    async def _run():
        req = SimulateMatchV2Request(
            max_events=20,
            match_config={"match_id": 77, "home_team": {"team_name": "Barcelona"}, "away_team": {"team_name": "Real Madrid"}},
        )
        session = ActiveMatchSession(match_id="77", request=req, execution_tier="local_cpu_onnx")
        routing = SimpleNamespace(tier="local_cpu_onnx", device="cpu")
        frames = [f async for f in bridge.stream_match(req, session, routing)]
        assert any(f.kind == FrameKind.EVENT for f in frames)
        assert any(f.kind == FrameKind.SPATIAL for f in frames)
        assert any(f.kind == FrameKind.TELEMETRY for f in frames)

        mc_req = MonteCarloExperimentRequestV2(
            experiment_id="exp_bridge_1",
            n_simulations=6,
            use_paired_crn=True,
            baseline_config={"match_id": 77, "home": {"team_name": "Barcelona"}, "away": {"team_name": "Real Madrid"}},
            interventions=[
                MonteCarloArmSpec(
                    arm_id="high_press",
                    label="High Press",
                    patches=[MonteCarloPatch(target="home.tactical_instructions.pressing_intensity", value=0.92)],
                )
            ],
        )
        # Keep unit test runtime fast by overriding max_events dynamically
        mc_req_fast = SimpleNamespace(**mc_req.model_dump(), max_events=25)
        seen_progress = []

        async def _cb(done, total, stage):
            seen_progress.append((done, total, stage))

        mc_out = await bridge.run_monte_carlo(mc_req_fast, routing, _cb)
        assert mc_out["experiment_id"] == "exp_bridge_1"
        assert len(mc_out["arms"]) == 2
        assert "delta_vs_baseline" in mc_out["arms"][1]
        assert seen_progress[-1] == (6, 6, "aggregating")

    asyncio.run(_run())


def test_from_models_dir_and_save_roundtrip(tmp_path: Path):
    from api.v2.simulator import inspect_models_dir

    mdir = tmp_path / "models_v2_test"
    eng = HybridRolloutEngine(model=PixelPitchEventTF(EventTransformerConfig.tiny_test()))
    eng.calibrated_temperature = 0.89
    eng.save_models_dir(mdir, training_metrics={"stage1_loss": 1.23, "ece": 0.009})

    loaded = HybridRolloutEngine.from_models_dir(mdir, device="cpu")
    assert loaded.weights_loaded is True
    assert abs(loaded.calibrated_temperature - 0.89) < 1e-5
    assert "pixelpitch_event_tf_112m.pt" in loaded.loaded_artifacts
    assert "module_d_suite.pt" in loaded.loaded_artifacts
    assert "xt_grid_24x16.npy" in loaded.loaded_artifacts
    assert loaded.training_metrics.get("ece") == 0.009

    info = inspect_models_dir(mdir)
    assert info["weights_loaded"] is True
    frames = list(loaded.simulate_match_stream(max_events=15, seed=99))
    assert len(frames) == 15
