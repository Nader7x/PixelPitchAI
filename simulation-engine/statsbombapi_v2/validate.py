"""
Automated Data Quality Gates (`validate.py`) for Pillar 1 Gold Events, Player Profiles,
and Train/Val/Test Split Isolation.
"""

from __future__ import annotations

import math
from typing import Any, Dict, List, Optional, Sequence, Tuple

import numpy as np
from pydantic import BaseModel, Field

from statsbombapi_v2.schemas import (
    ActionType,
    EventGoldV2,
    EventOutcome,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PlayerProfileV2,
)


class ValidationReport(BaseModel):
    passed: bool
    checks_run: int = 0
    errors: List[str] = Field(default_factory=list)
    warnings: List[str] = Field(default_factory=list)
    metrics: Dict[str, Any] = Field(default_factory=dict)


def validate_gold_events(
    events: Sequence[EventGoldV2],
    expected_final_scores: Optional[Dict[int, Tuple[int, int]]] = None,
) -> ValidationReport:
    """
    Validate spatial bounds, monotonic clocks, 22-slot conservation, red-card exclusion,
    derived analytics bounds, and cumulative scoreline accuracy.
    """
    errors: List[str] = []
    warnings: List[str] = []
    checks = 0

    by_match: Dict[int, List[EventGoldV2]] = {}
    for ev in events:
        by_match.setdefault(ev.match_id, []).append(ev)

    xg_v2_vals: List[float] = []
    xg_sb_vals: List[float] = []

    for mid, mevs in by_match.items():
        mevs_sorted = sorted(mevs, key=lambda e: e.event_seq_idx)
        prev_ts = -1
        prev_seq = -1
        calc_home, calc_away = 0, 0

        for ev in mevs_sorted:
            # 1. Sequence & timestamp monotonicity
            checks += 1
            if ev.event_seq_idx <= prev_seq:
                errors.append(f"match {mid}: non-monotonic event_seq_idx {ev.event_seq_idx} after {prev_seq}")
            if ev.timestamp_ms < prev_ts:
                errors.append(f"match {mid} seq {ev.event_seq_idx}: timestamp_ms {ev.timestamp_ms} < prev {prev_ts}")
            prev_seq = ev.event_seq_idx
            prev_ts = ev.timestamp_ms

            # 2. Spatial bounds & NaN check
            checks += 1
            for val_name, val in (
                ("x_canon", ev.x_canon),
                ("y_canon", ev.y_canon),
                ("end_x_canon", ev.end_x_canon),
                ("end_y_canon", ev.end_y_canon),
                ("end_z_m", ev.end_z_m),
            ):
                if math.isnan(val) or math.isinf(val):
                    errors.append(f"match {mid} seq {ev.event_seq_idx}: non-finite {val_name}")
            if not (-2.0 <= ev.x_canon <= PITCH_LENGTH_YD + 2.0 and -2.0 <= ev.y_canon <= PITCH_WIDTH_YD + 2.0):
                errors.append(f"match {mid} seq {ev.event_seq_idx}: out-of-bounds start ({ev.x_canon}, {ev.y_canon})")
            if not (0.0 <= ev.end_z_m <= 5.0):
                errors.append(f"match {mid} seq {ev.event_seq_idx}: out-of-bounds end_z_m={ev.end_z_m}")

            # 3. 22-slot conservation & red-card actor mask check
            checks += 1
            if len(ev.active_slots_player_ids) != 22 or len(ev.active_slots_roles) != 22:
                errors.append(f"match {mid} seq {ev.event_seq_idx}: active_slots vector length != 22")
            if 1 <= ev.actor_slot <= 22 and ev.action_type not in (ActionType.CARD, ActionType.SUBSTITUTION):
                if (ev.active_slots_sent_off >> (ev.actor_slot - 1)) & 1:
                    errors.append(
                        f"match {mid} seq {ev.event_seq_idx}: sent-off actor_slot={ev.actor_slot} acted on ball"
                    )

            # 4. Analytics bounds
            checks += 1
            if not (0.0 <= ev.xg_v2_pre_shot <= 1.0 and 0.0 <= ev.psxg_v2 <= 1.0 and 0.0 <= ev.xpass_prob <= 1.0):
                errors.append(f"match {mid} seq {ev.event_seq_idx}: invalid xG/PSxG/xPass probability range")

            if ev.action_type == ActionType.SHOT:
                xg_v2_vals.append(ev.xg_v2_pre_shot)
                if ev.xg_statsbomb is not None:
                    xg_sb_vals.append(ev.xg_statsbomb)

            # Verify pre-event score matches running tally
            if ev.score_home != calc_home or ev.score_away != calc_away:
                errors.append(
                    f"match {mid} seq {ev.event_seq_idx}: pre-score ({ev.score_home},{ev.score_away}) != ({calc_home},{calc_away})"
                )
            if ev.outcome == EventOutcome.GOAL:
                if ev.is_home_actor:
                    calc_home += 1
                else:
                    calc_away += 1
            elif ev.outcome == EventOutcome.OWN_GOAL:
                if ev.is_home_actor:
                    calc_away += 1
                else:
                    calc_home += 1

        if expected_final_scores and mid in expected_final_scores:
            checks += 1
            exp_h, exp_a = expected_final_scores[mid]
            if (calc_home, calc_away) != (exp_h, exp_a):
                errors.append(f"match {mid}: final score ({calc_home},{calc_away}) != expected ({exp_h},{exp_a})")

    metrics: Dict[str, Any] = {
        "matches_checked": len(by_match),
        "events_checked": len(events),
        "mean_xg_v2": float(np.mean(xg_v2_vals)) if xg_v2_vals else 0.0,
        "mean_xg_sb": float(np.mean(xg_sb_vals)) if xg_sb_vals else None,
    }
    return ValidationReport(passed=len(errors) == 0, checks_run=checks, errors=errors, warnings=warnings, metrics=metrics)


def validate_player_profiles(profiles: Sequence[PlayerProfileV2]) -> ValidationReport:
    """Validate Empirical-Bayes attribute bounds, 22-role vectors, unit-norm style_64d, and 24x16 heatmaps."""
    errors: List[str] = []
    checks = 0
    for prof in profiles:
        checks += 1
        if len(prof.attributes_24d) != 24 or any(not (0.0 <= v <= 1.0) for v in prof.attributes_24d):
            errors.append(f"player {prof.player_id}: invalid attributes_24d")
        if len(prof.sigma_24d) != 24 or any(v <= 0.0 or math.isnan(v) for v in prof.sigma_24d):
            errors.append(f"player {prof.player_id}: invalid sigma_24d")
        if len(prof.gk_attributes_8d) != 8 or any(not (0.0 <= v <= 1.0) for v in prof.gk_attributes_8d):
            errors.append(f"player {prof.player_id}: invalid gk_attributes_8d")
        if len(prof.role_suitability_scores) != 22 or any(not (0.0 <= v <= 1.0) for v in prof.role_suitability_scores):
            errors.append(f"player {prof.player_id}: role_suitability_scores must have 22 values in [0,1]")

        style_norm = float(np.linalg.norm(prof.latent_style_64d))
        if abs(style_norm - 1.0) > 1e-2:
            errors.append(f"player {prof.player_id}: latent_style_64d not unit norm ({style_norm:.4f})")

        for hm_name, hm in (("touch_heatmap_24x16", prof.touch_heatmap_24x16), ("pressing_zone_heatmap_24x16", prof.pressing_zone_heatmap_24x16)):
            s = float(sum(hm))
            if len(hm) != 384 or abs(s - 1.0) > 1e-2:
                errors.append(f"player {prof.player_id}: {hm_name} len={len(hm)} sum={s:.4f}")

    return ValidationReport(
        passed=len(errors) == 0,
        checks_run=checks,
        errors=errors,
        metrics={"profiles_checked": len(profiles)},
    )


def validate_splits(splits: Dict[str, List[int]]) -> ValidationReport:
    """Enforce zero train/val/test/ood match-ID leakage."""
    errors: List[str] = []
    seen: Dict[int, str] = {}
    for split_name, mids in splits.items():
        for mid in mids:
            if mid in seen:
                errors.append(f"Match {mid} leaked across splits '{seen[mid]}' and '{split_name}'")
            seen[mid] = split_name
    return ValidationReport(
        passed=len(errors) == 0,
        checks_run=len(seen),
        errors=errors,
        metrics={k: len(v) for k, v in splits.items()},
    )


def run_all_gates(
    events: Sequence[EventGoldV2],
    profiles: Sequence[PlayerProfileV2],
    splits: Optional[Dict[str, List[int]]] = None,
    expected_final_scores: Optional[Dict[int, Tuple[int, int]]] = None,
) -> ValidationReport:
    """Execute all Pillar 1 data quality gates and aggregate results."""
    r_ev = validate_gold_events(events, expected_final_scores=expected_final_scores)
    r_pr = validate_player_profiles(profiles)
    all_errors = list(r_ev.errors) + list(r_pr.errors)
    total_checks = r_ev.checks_run + r_pr.checks_run
    metrics = {**r_ev.metrics, **r_pr.metrics}

    if splits is not None:
        r_sp = validate_splits(splits)
        all_errors.extend(r_sp.errors)
        total_checks += r_sp.checks_run
        metrics["splits"] = r_sp.metrics

    return ValidationReport(
        passed=len(all_errors) == 0,
        checks_run=total_checks,
        errors=all_errors,
        metrics=metrics,
    )
