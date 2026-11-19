"""Comprehensive tests for PixelPitchAI v2 Modules A & B (api.v2.tactical)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
import torch

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from api.v2.tactical import (  # noqa: E402
    ATTRIBUTE_NAMES_24D,
    FORMATION_PRESETS,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    ROLE_ORDER,
    ROLE_PRIOR_VECTORS,
    ROLE_TO_ID,
    BenchPlayerConfig,
    CoachHydrator,
    DiagnosticAttributionCalculator,
    ExperimentVariant,
    FormationConfig,
    LineupSlotConfig,
    MatchConfigurationV2,
    OODDetector,
    PhaseSlotAnchor,
    PlayerAttributeOverride,
    PlayerCapabilityRecord,
    PlayerProfileRegistry,
    PlayerRole,
    ScriptedSubstitutionRule,
    SimulatedEventRecord,
    SubstitutionPolicy,
    TacticalInstructions,
    TacticalPhase,
    TacticalStateEngine,
    TeamMatchConfig,
    apply_live_tactical_override,
    apply_variant_to_config,
    assess_experiment_ood,
    build_default_team_config,
    load_from_models_dir,
    resolve_formation_anchors,
)


def _sample_match_config(
    match_id: int = 101,
    home_style: str = "tiki-taka",
    away_style: str = "low block counter",
) -> MatchConfigurationV2:
    home = build_default_team_config("Home FC", base_player_id=100, team_id=1, formation_name="4-3-3", coaching_style=home_style)
    away = build_default_team_config("Away FC", base_player_id=200, team_id=2, formation_name="4-4-2", coaching_style=away_style)
    return MatchConfigurationV2(match_id=match_id, home=home, away=away, random_seed=42)


def test_roles_priors_and_dual_phase_formations():
    assert len(ROLE_ORDER) == 22
    assert set(ROLE_TO_ID.values()) == set(range(22))
    for role in ROLE_ORDER:
        assert len(ROLE_PRIOR_VECTORS[role]) == 16
        assert all(0.0 <= x <= 1.0 for x in ROLE_PRIOR_VECTORS[role])

    assert len(FORMATION_PRESETS) >= 11
    cfg = _sample_match_config()
    roles = [s.tactical_role for s in cfg.home.ordered_xi()]
    resolved = resolve_formation_anchors(cfg.home.formation, roles)
    assert set(resolved.keys()) == set(TacticalPhase)
    for phase in TacticalPhase:
        assert len(resolved[phase]) == 11

    # InvertedFullback (slot 4, RB) tucks inward & forward in IN_POSSESSION vs OUT_OF_POSSESSION
    rb_in_poss = resolved[TacticalPhase.IN_POSSESSION][4]
    rb_oop = resolved[TacticalPhase.OUT_OF_POSSESSION][4]
    assert rb_in_poss.anchor_x > rb_oop.anchor_x
    assert rb_in_poss.anchor_y < rb_oop.anchor_y

    # False9 (slot 9) drops deeper in IN_POSSESSION compared to Poacher
    roles_poacher = list(roles)
    roles_poacher[9] = PlayerRole.POACHER
    resolved_poacher = resolve_formation_anchors(cfg.home.formation, roles_poacher)
    assert resolved[TacticalPhase.IN_POSSESSION][9].anchor_x < resolved_poacher[TacticalPhase.IN_POSSESSION][9].anchor_x


def test_scripted_custom_bench_sub_validation_and_variants():
    # Valid custom bench player with player_id=None referenced via in_bench_index
    team = build_default_team_config("Custom XI", base_player_id=500)
    custom_sub = BenchPlayerConfig(
        player_id=None,
        inline_profile_override=PlayerAttributeOverride(
            display_name="Scouted Winger",
            attribute_deltas={"dribble_success_skill": 0.15},
        ),
        shirt_number=27,
        natural_positions=["RW"],
        default_role=PlayerRole.TOUCHLINE_WINGER,
    )
    team.bench.append(custom_sub)
    custom_bench_idx = len(team.bench) - 1
    team.substitution_policy = SubstitutionPolicy(
        mode="scripted",
        scripted_rules=[
            ScriptedSubstitutionRule(
                trigger_minute=60,
                out_slot_index=10,
                in_bench_index=custom_bench_idx,
                new_role=PlayerRole.TOUCHLINE_WINGER,
                tactical_patch={"attacking_width": 0.85},
            )
        ],
    )
    validated = TeamMatchConfig.model_validate(team.model_dump())
    assert validated.bench[custom_bench_idx].player_id is None

    # Out-of-bounds bench index raises ValueError
    with pytest.raises(ValueError):
        TeamMatchConfig(
            **{
                **team.model_dump(),
                "substitution_policy": {
                    "mode": "scripted",
                    "scripted_rules": [{"trigger_minute": 60, "out_slot_index": 10, "in_bench_index": 11}],
                },
            }
        )

    # Slot 0 non-GK rejected
    with pytest.raises(ValueError):
        LineupSlotConfig(
            slot_index=0,
            player_id=10,
            shirt_number=1,
            base_position="GK",
            tactical_role=PlayerRole.POACHER,
        )

    # Variant application does not mutate baseline
    base_cfg = _sample_match_config()
    orig_line = base_cfg.home.tactical_instructions.defensive_line_height
    variant = ExperimentVariant(
        variant_id="high_line",
        label="High Line",
        tactical_overrides={"defensive_line_height": 0.90},
        role_overrides={9: PlayerRole.PRESSING_FORWARD},
    )
    mod_cfg = apply_variant_to_config(base_cfg, variant, lock_game_state_adaptivity=0.0)
    assert base_cfg.home.tactical_instructions.defensive_line_height == orig_line
    assert mod_cfg.home.tactical_instructions.defensive_line_height == pytest.approx(0.90)
    assert mod_cfg.home.starting_xi[9].tactical_role == PlayerRole.PRESSING_FORWARD
    assert mod_cfg.away.tactical_instructions.game_state_adaptivity == 0.0

    # CoachHydrator
    form, tac = CoachHydrator.hydrate_tactics("4-2-3-1", "Gegenpressing")
    assert form.base_name == "4-2-3-1"
    assert tac.pressing_intensity > 0.80


def test_state_engine_tensor_shapes_and_epistemic_crn():
    engine = TacticalStateEngine()
    cfg = _sample_match_config()
    bsz = 4
    init_res = engine.init_batch(cfg, batch_size=bsz, seeds=[11, 22, 33, 44], sample_epistemic=True)

    assert init_res.player_cond.shape == (bsz, 22, 128)
    assert init_res.memory_tokens.shape == (bsz, 28, 768)
    assert init_res.global_state.shape == (bsz, 64)
    assert init_res.actor_mask.shape == (bsz, 22)
    assert init_res.spatial.positions.shape == (bsz, 22, 2)
    assert init_res.spatial.pitch_control_grid.shape == (bsz, 24, 16)
    assert init_res.spatial.freeze_frame_360.shape == (bsz, 22, 7)
    assert init_res.spatial.target_feasibility.shape == (bsz, 11)
    assert init_res.spatial.spatial_log_prior.shape == (bsz, 18 + 11)
    assert not torch.isnan(init_res.memory_tokens).any()
    assert not torch.isnan(init_res.global_state).any()

    # Paired CRN reproducibility with identical seeds
    init_res_2 = engine.init_batch(cfg, batch_size=bsz, seeds=[11, 22, 33, 44], sample_epistemic=True)
    assert torch.allclose(init_res.state.base_attr_24d, init_res_2.state.base_attr_24d)


def test_fatigue_halftime_cards_subs_and_live_override():
    engine = TacticalStateEngine()
    cfg = _sample_match_config()
    # Add scripted custom sub at minute 50
    cfg.home.bench.append(
        BenchPlayerConfig(
            player_id=None,
            inline_profile_override=PlayerAttributeOverride(
                display_name="Impact Sub",
                attribute_deltas={"sprint_recovery_capacity": 0.15},
            ),
            shirt_number=29,
            natural_positions=["LW"],
            default_role=PlayerRole.INSIDE_FORWARD,
        )
    )
    custom_b_idx = len(cfg.home.bench) - 1
    cfg.home.substitution_policy = SubstitutionPolicy(
        mode="hybrid",
        scripted_rules=[
            ScriptedSubstitutionRule(
                trigger_minute=50,
                out_slot_index=8,
                in_bench_index=custom_b_idx,
                new_role=PlayerRole.INSIDE_FORWARD,
                tactical_patch={"directness": 0.72},
            )
        ],
    )

    res = engine.init_batch(cfg, batch_size=2)
    state = res.state
    init_stamina = state.stamina.clone()

    # Advance 45 minutes in 1H (several steps) -> stamina drops
    for _ in range(15):
        res = engine.step(
            state=state,
            dt_sec=torch.tensor([180.0, 180.0]),
            ball_xy=torch.tensor([[75.0, 40.0], [50.0, 30.0]]),
            possessing_team=torch.tensor([0, 1]),
            actor_slot_idx=torch.tensor([5, 16]),
        )
    assert (state.stamina < init_stamina).all()
    pre_ht_stamina = state.stamina.clone()

    # Enter 2nd half (period=3) -> halftime recovery (+0.16)
    res = engine.step(
        state=state,
        dt_sec=torch.tensor([5.0, 5.0]),
        ball_xy=torch.tensor([[60.0, 40.0], [60.0, 40.0]]),
        possessing_team=torch.tensor([1, 0]),
        period=torch.tensor([3, 3]),
    )
    assert (state.stamina > pre_ht_stamina).all()

    # Advance past minute 50 with dead ball -> scripted custom bench sub executes
    res = engine.step(
        state=state,
        dt_sec=torch.tensor([360.0, 360.0]),
        ball_xy=torch.tensor([[60.0, 40.0], [60.0, 40.0]]),
        possessing_team=torch.tensor([0, 0]),
        is_dead_ball=torch.tensor([True, True]),
    )
    assert len(res.executed_subs) == 2
    assert int(state.subs_used[0, 0].item()) == 1
    assert state.team_tactics_10d[0, 0, 3].item() == pytest.approx(0.72)

    # Yellow card then second yellow -> red card compaction on slot 14 (Away slot)
    pc_before_red = float(res.spatial.home_voronoi_control[0].item())
    res = engine.step(
        state=state,
        dt_sec=torch.tensor([4.0, 4.0]),
        ball_xy=torch.tensor([[70.0, 45.0], [70.0, 45.0]]),
        possessing_team=torch.tensor([0, 0]),
        yellow_card_slots=torch.tensor([14, -1]),
    )
    assert bool(state.yellow_cards[0, 14].item())
    assert bool(state.active_mask[0, 14].item())

    # Second yellow -> Red
    res = engine.step(
        state=state,
        dt_sec=torch.tensor([4.0, 4.0]),
        ball_xy=torch.tensor([[72.0, 44.0], [72.0, 44.0]]),
        possessing_team=torch.tensor([0, 0]),
        yellow_card_slots=torch.tensor([14, -1]),
    )
    assert bool(state.red_cards[0, 14].item())
    assert not bool(state.active_mask[0, 14].item())
    assert not bool(res.actor_mask[0, 14].item())
    # Away has 10 men -> Home Voronoi pitch control rises
    assert float(res.spatial.home_voronoi_control[0].item()) >= pc_before_red - 0.02

    # Scoreline elasticity: trailing late pushes Home line height up
    res = engine.step(
        state=state,
        dt_sec=torch.tensor([1800.0, 1800.0]),
        ball_xy=torch.tensor([[60.0, 40.0], [60.0, 40.0]]),
        possessing_team=torch.tensor([0, 0]),
        score=torch.tensor([[0, 1], [1, 0]]),
    )
    assert state.effective_tactics_10d[0, 0, 0] > state.team_tactics_10d[0, 0, 0]  # trailing boosts line height
    assert state.effective_tactics_10d[1, 0, 0] < state.team_tactics_10d[1, 0, 0]  # leading-1 drops line height

    # Live override mid-match
    sub_ev = engine.apply_live_override(
        state=state,
        team_side="home",
        tactical_patch={"pressing_intensity": 0.92},
        new_formation=FormationConfig(base_name="3-5-2"),
        sub_request=(5, 0),
        batch_idx=0,
    )
    assert sub_ev is not None
    assert state.team_tactics_10d[0, 0, 1].item() == pytest.approx(0.92)


def test_spatial_solver_bounds_offside_and_pitch_control():
    engine = TacticalStateEngine()
    cfg = _sample_match_config()
    res = engine.init_batch(cfg, batch_size=2)
    state = res.state

    # Positions strictly inside pitch bounds and Pitch Control in [0, 1]
    pos = res.spatial.positions
    assert (pos[..., 0] >= 0.0).all() and (pos[..., 0] <= PITCH_LENGTH_YD).all()
    assert (pos[..., 1] >= 0.0).all() and (pos[..., 1] <= PITCH_WIDTH_YD).all()
    assert (res.spatial.pitch_control_grid >= 0.0).all() and (res.spatial.pitch_control_grid <= 1.0).all()

    # Place Home striker (slot 10) past Away 2nd-last defender to test offside flag & throw-in exemption
    state.positions[:, :, 0] = 60.0
    state.positions[:, 11:22, 0] = 85.0
    state.positions[:, 11, 0] = 115.0  # Away GK
    state.positions[:, 10, 0] = 98.0   # Home slot 10 ahead of Away 2nd-last defender (85.0)
    state.positions[:, 9, 0] = 75.0    # Passer behind

    feas_normal, offside_normal, _, _ = engine.spatial_solver.compute_pass_targets_and_priors(
        positions=state.positions,
        velocities=state.velocities,
        active_mask=state.active_mask,
        local_pc=res.spatial.local_pitch_control,
        possessing_team=torch.tensor([0, 0]),
        actor_slot_idx=torch.tensor([9, 9]),
        ball_xy=torch.tensor([[75.0, 40.0], [75.0, 40.0]]),
        ball_pressure=torch.tensor([0.2, 0.2]),
        poss_tactics_10d=state.effective_tactics_10d[:, 0, :],
        restart_offside_exempt=torch.tensor([False, False]),
    )
    assert bool(offside_normal[0, 10].item())

    _, offside_throwin, _, _ = engine.spatial_solver.compute_pass_targets_and_priors(
        positions=state.positions,
        velocities=state.velocities,
        active_mask=state.active_mask,
        local_pc=res.spatial.local_pitch_control,
        possessing_team=torch.tensor([0, 0]),
        actor_slot_idx=torch.tensor([9, 9]),
        ball_xy=torch.tensor([[75.0, 40.0], [75.0, 40.0]]),
        ball_pressure=torch.tensor([0.2, 0.2]),
        poss_tactics_10d=state.effective_tactics_10d[:, 0, :],
        restart_offside_exempt=torch.tensor([True, True]),
    )
    assert not bool(offside_throwin[0, 10].item())
    assert feas_normal[0, 9] == 0.0  # Self-pass masked


def test_ood_detector_tiers_and_guardrails():
    detector = OODDetector()
    registry = PlayerProfileRegistry()

    # 1. Realistic baseline -> GREEN
    green_cfg = _sample_match_config()
    green_rep = detector.assess_match(green_cfg, registry)
    assert green_rep.confidence_tier == "GREEN"
    assert not green_rep.execution_blocked
    assert green_rep.allow_causal_claims

    # 2. Extreme inverse decoupling: Ultra-high line (0.96) + zero pressing (0.04) -> TACTICAL_COMBO_OOD
    tactic_ood_cfg = _sample_match_config()
    tactic_ood_cfg.home.tactical_instructions = TacticalInstructions(
        defensive_line_height=0.96,
        pressing_intensity=0.04,
        counter_press_vs_regroup=0.05,
        directness=0.90,
    )
    tactic_rep = detector.assess_match(tactic_ood_cfg, registry)
    assert tactic_rep.confidence_tier in ("AMBER", "RED")
    assert any(c.check_name == "TACTICAL_COMBO_OOD" for c in tactic_rep.triggered_checks)
    assert tactic_rep.projected_nearest_feasible is not None

    # 3. Goalkeeper profile placed at InvertedWinger + saturated superhuman override -> RED & blocked
    red_cfg = _sample_match_config()
    gk_pid = 9001
    gk_attrs = [0.50] * 24
    gk_attrs[ATTRIBUTE_NAMES_24D.index("dribble_takeon_volume")] = 0.08
    gk_attrs[ATTRIBUTE_NAMES_24D.index("shot_volume_box")] = 0.05
    registry.register(
        PlayerCapabilityRecord(
            player_id=gk_pid,
            display_name="Pure ShotStopper",
            attributes_24d=gk_attrs,
            gk_attributes_8d=[0.85, 0.80, 0.25, 0.70, 0.30, 0.65, 0.50, 0.05],
            latent_style_64d=[0.05] * 64,
        )
    )
    red_cfg.home.starting_xi[8] = LineupSlotConfig(
        slot_index=8,
        player_id=gk_pid,
        shirt_number=99,
        base_position="LW",
        tactical_role=PlayerRole.INVERTED_WINGER,
    )
    red_cfg.home.starting_xi[9] = LineupSlotConfig(
        slot_index=9,
        inline_profile_override=PlayerAttributeOverride(
            display_name="Superhuman Bot",
            attributes_24d={k: 0.99 for k in ATTRIBUTE_NAMES_24D},
        ),
        shirt_number=9,
        base_position="ST",
        tactical_role=PlayerRole.POACHER,
    )
    red_rep = detector.assess_match(red_cfg, registry)
    assert red_rep.confidence_tier == "RED"
    assert red_rep.execution_blocked
    assert not red_rep.allow_causal_claims
    check_names = {c.check_name for c in red_rep.triggered_checks}
    assert "ROLE_MISMATCH_OOD" in check_names
    assert "SYNTHETIC_EXTRAPOLATION" in check_names

    # 4. Anchor packing + high runtime clamp triggers SPATIAL_COLLISION_DRIFT
    packed_cfg = _sample_match_config()
    packed_cfg.home.formation = FormationConfig(
        base_name="CUSTOM",
        phase_anchors={
            TacticalPhase.IN_POSSESSION: [
                PhaseSlotAnchor(slot_index=i, anchor_x=0.90 if i > 0 else 0.10, anchor_y=0.30 + 0.04 * i)
                for i in range(11)
            ]
        },
    )
    drift_rep = detector.assess_match(packed_cfg, registry, runtime_clamp_fraction=0.30)
    assert any(c.check_name == "SPATIAL_COLLISION_DRIFT" for c in drift_rep.triggered_checks)


def test_diagnostics_waterfall_turnovers_and_spatial_redundancy():
    calc = DiagnosticAttributionCalculator()

    baseline_matches = []
    variant_matches = []
    for seed in range(1, 31):
        # Baseline: Home concedes after pressured midfield turnover & creates 1 modest shot
        base_evs = [
            SimulatedEventRecord(
                match_seed=seed, event_index=0, timestamp_sec=100.0, team_side=0, actor_slot=6,
                action_type="pass", outcome="Incomplete", x_canon=52.0, y_canon=38.0,
                end_x_canon=68.0, end_y_canon=36.0, under_pressure=True, xt_delta=-0.02, vaep_value=-0.03,
            ),
            SimulatedEventRecord(
                match_seed=seed, event_index=1, timestamp_sec=109.0, team_side=1, actor_slot=20,
                action_type="shot", outcome="Saved" if seed % 4 else "Goal", x_canon=18.0, y_canon=40.0,
                end_x_canon=0.0, end_y_canon=39.0, xg=0.22, psxg=0.30, defenders_in_shot_cone=1,
            ),
            SimulatedEventRecord(
                match_seed=seed, event_index=2, timestamp_sec=300.0, team_side=0, actor_slot=10,
                action_type="shot", outcome="Saved", x_canon=102.0, y_canon=42.0,
                end_x_canon=120.0, end_y_canon=40.0, xg=0.14, psxg=0.18, defenders_in_shot_cone=2,
            ),
        ]
        # Variant: Home slot 6 completes progressive pass into Zone 14 -> slot 10 big chance
        var_evs = [
            SimulatedEventRecord(
                match_seed=seed, event_index=0, timestamp_sec=100.0, team_side=0, actor_slot=6,
                action_type="pass", outcome="Complete", x_canon=52.0, y_canon=38.0,
                end_x_canon=88.0, end_y_canon=40.0, under_pressure=True, through_ball=True,
                xt_delta=0.12, vaep_value=0.15,
            ),
            SimulatedEventRecord(
                match_seed=seed, event_index=1, timestamp_sec=104.0, team_side=0, actor_slot=10,
                action_type="shot", outcome="Goal" if seed % 2 == 0 else "Saved", x_canon=109.0, y_canon=40.0,
                end_x_canon=120.0, end_y_canon=38.5, xg=0.44, psxg=0.55, defenders_in_shot_cone=0,
            ),
        ]
        baseline_matches.append(base_evs)
        variant_matches.append(var_evs)

    report = calc.compare_arms(baseline_matches, variant_matches, target_team_side="home", n_boot=300)
    assert report.n_matches == 30
    assert report.xg_diff_delta.mean_delta > 0.45
    assert report.xg_diff_delta.ci95_low > 0.35
    assert report.turnover_causes_p90["forced_under_press"] < 0.0
    assert report.turnover_xg_cost_15s_p90 < 0.0
    assert report.shot_quality_histogram["gte_0.35"] == 30
    assert report.assist_pattern_counts["through_ball"] == 30
    assert len(report.zone_progression_15) == 15
    assert len(report.role_contributions) == 11
    assert len(report.representative_match_seeds) == 3

    # Exact additive waterfall check
    wf = report.waterfall
    reconstructed = (
        wf.build_up_retention
        + wf.final_third_entry_quality
        + wf.shot_conversion_quality
        - wf.transition_vulnerability_allowed
        + wf.set_piece_delta
    )
    assert reconstructed == pytest.approx(wf.total_xg_diff_delta, abs=1e-3)

    # Spatial redundancy Q4 check: overlapping slots flag zone congestion & negative synergy
    overlap_events = [
        [
            SimulatedEventRecord(
                match_seed=1, event_index=k, timestamp_sec=float(k), team_side=0,
                actor_slot=7 if k % 2 == 0 else 9, action_type="carry", outcome="Complete",
                x_canon=82.0, y_canon=22.0, end_x_canon=86.0, end_y_canon=24.0,
            )
            for k in range(40)
        ]
    ]
    red_rep = calc.compute_spatial_redundancy(
        overlap_events, slot_a=7, slot_b=9, solo_a_xg_diff_delta=0.18, solo_b_xg_diff_delta=0.16, coplay_xg_diff_delta=0.20
    )
    assert red_rep.spatial_overlap_score > 0.78
    assert red_rep.zone_congestion_flag
    assert red_rep.synergy_xg_diff < 0.0


def test_batch_b128_monte_carlo_scaling_and_slot_mapping():
    """Verify vectorized B=128 parallel step stability and 1-based slot conversion."""
    engine = TacticalStateEngine()
    cfg = _sample_match_config()
    bsz = 128
    res = engine.init_batch(cfg, batch_size=bsz, sample_epistemic=False)
    assert res.player_cond.shape == (bsz, 22, 128)
    assert res.memory_tokens.shape == (bsz, 28, 768)
    assert res.global_state.shape == (bsz, 64)

    wire_slots = torch.tensor([0, 1, 11, 12, 22])
    mapped = engine.slot_1_22_to_idx(wire_slots)
    assert mapped.tolist() == [-1, 0, 10, 11, 21]

    res2 = engine.step(
        state=res.state,
        dt_sec=torch.full((bsz,), 2.5),
        ball_xy=torch.tensor([[84.0, 32.0]]).expand(bsz, 2),
        ball_end_xy=torch.tensor([[102.0, 38.0]]).expand(bsz, 2),
        possessing_team=torch.zeros((bsz,), dtype=torch.int64),
        actor_slot_idx=torch.full((bsz,), 9, dtype=torch.int64),
    )
    assert res2.spatial.positions.shape == (bsz, 22, 2)
    assert res2.spatial.freeze_frame_360.shape == (bsz, 22, 7)
    assert not torch.isnan(res2.memory_tokens).any()


def test_fifa_sub_window_limit_and_direct_red_card():
    """Verify max 3 stoppage windows limit reactive subs and direct red updates masks."""
    engine = TacticalStateEngine()
    cfg = _sample_match_config()
    cfg.home.substitution_policy = SubstitutionPolicy(
        mode="rule_based",
        max_substitutions=5,
        max_windows=3,
        stamina_sub_threshold=0.78,
    )
    res = engine.init_batch(cfg, batch_size=1)
    state = res.state
    state.stamina[0, 1:11] = 0.70
    state.clock_seconds[0] = 3600.0  # Minute 60

    # Simulate 4 separate stoppage windows (>10s apart): only 3 subs should execute
    for w in range(4):
        step_res = engine.step(
            state=state,
            dt_sec=torch.tensor([30.0]),
            ball_xy=torch.tensor([[60.0, 40.0]]),
            possessing_team=torch.tensor([0]),
            is_dead_ball=torch.tensor([True]),
        )
        assert len(step_res.executed_subs) <= 2  # at most 1 per team per window
    assert int(state.sub_windows_used[0, 0].item()) == 3
    assert int(state.subs_used[0, 0].item()) == 3

    # Direct red card on Home slot 3
    step_red = engine.step(
        state=state,
        dt_sec=torch.tensor([2.0]),
        ball_xy=torch.tensor([[45.0, 40.0]]),
        possessing_team=torch.tensor([1]),
        red_card_slots=torch.tensor([3]),
    )
    assert bool(state.red_cards[0, 3].item())
    assert not bool(step_red.actor_mask[0, 3].item())


def test_module_e_bridge_helpers_ood_and_live_override():
    """Verify assess_experiment_ood and apply_live_tactical_override match Module E bridge."""
    ood = assess_experiment_ood(
        {"match_id": 42, "home_team": {"name": "Home"}, "away_team": {"name": "Away"}},
        interventions=[
            {
                "arm_id": "decoupled",
                "patches": [
                    {"target": "home_team.tactics.defensive_line_height", "value": 0.96},
                    {"target": "home_team.tactics.pressing_intensity", "value": 0.04},
                ],
            }
        ],
    )
    assert ood["confidence_tier"] in ("AMBER", "RED")
    assert "decoupled" in ood["per_arm"]
    assert len(ood["ood_reasons"]) >= 1

    engine = TacticalStateEngine()
    res = engine.init_batch(_sample_match_config(), batch_size=1)
    updated = apply_live_tactical_override(
        (engine, res.state),
        {
            "team_side": "home",
            "tactical_instructions_patch": {"directness": 0.88},
            "formation_base_name": "4-2-3-1",
            "substitution": {"out_slot_index": 7, "in_bench_index": 0},
        },
    )
    assert updated["home"]["directness"] == pytest.approx(0.88)
    assert updated["sub_executed"] is not None


def test_checkpoint_save_and_load_from_models_dir_roundtrip(tmp_path, monkeypatch):
    """Verify auto-loading player_profiles_v2.parquet, tactical_manifold_v2.pkl, and spatial_predictor.pt."""
    from api.v2.tactical import OffBallSpatialSolver, TacticalManifoldModel

    # 1. Save player_profiles_v2.parquet
    reg = PlayerProfileRegistry(auto_load=False)
    reg.register(
        PlayerCapabilityRecord(
            player_id=777,
            display_name="Pedri Test",
            attributes_24d=[0.88] * 24,
            sigma_24d=[0.03] * 24,
            gk_attributes_8d=[0.50] * 8,
            finishing_log_odds=0.14,
            latent_style_64d=[0.125] * 64,
            minutes_played=2700.0,
            role_suitability_scores=[0.75] * 22,
        )
    )
    pq_file = reg.save_parquet(tmp_path / "player_profiles_v2.parquet")
    assert pq_file.is_file()

    # 2. Save tactical_manifold_v2.pkl
    manifold = TacticalManifoldModel.default_empirical_prior()
    pkl_file = manifold.save_pickle(tmp_path / "tactical_manifold_v2.pkl")
    assert pkl_file.is_file()

    # 3. Save spatial_predictor.pt with non-zero head bias to verify weight restoration
    solver = OffBallSpatialSolver(auto_load=False)
    with torch.no_grad():
        solver.predictor.head.bias.fill_(0.25)
    sp_file = solver.save_checkpoint(tmp_path / "spatial_predictor.pt")
    assert sp_file.is_file()

    # 4. Load via load_from_models_dir(tmp_path)
    bundle = load_from_models_dir(tmp_path, device="cpu")
    assert bundle["weights_loaded"] is True
    assert set(bundle["loaded_files"]) == {
        "player_profiles_v2.parquet",
        "tactical_manifold_v2.pkl",
        "spatial_predictor.pt",
    }
    loaded_prof = bundle["registry"].get(777)
    assert loaded_prof is not None
    assert loaded_prof.display_name == "Pedri Test"
    assert loaded_prof.finishing_log_odds == pytest.approx(0.14)
    assert torch.allclose(
        bundle["spatial_solver"].predictor.head.bias,
        torch.full((4,), 0.25),
    )

    # 5. Verify MODEL_V2_DIR environment variable auto-discovery on default constructors
    monkeypatch.setenv("MODEL_V2_DIR", str(tmp_path))
    auto_engine = TacticalStateEngine()
    auto_ood = OODDetector()
    assert auto_engine.weights_loaded is True
    assert auto_engine.spatial_solver.weights_loaded is True
    assert auto_engine.registry.get(777) is not None
    assert auto_ood.loaded_from_path is not None



