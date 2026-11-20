"""Phase 3 Counterfactual & Realism Validation Suite for PixelPitchAI v2.

Runs 8 paired-CRN simulation arms using HybridRolloutEngine.from_models_dir,
computes paired deltas, 95% confidence intervals, baseline realism metrics,
and 6 counterfactual monotonicity checks, writing results incrementally to JSON.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
import time
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import torch

# Ensure simulation-engine root is on sys.path when invoked as a script
_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from api.v2.simulator.rollout_engine import HybridRolloutEngine
from api.v2.tactical.config_schema import (
    MatchConfigurationV2,
    PlayerAttributeOverride,
    TeamMatchConfig,
    build_default_team_config,
)


def _base_team(team_name: str, base_player_id: int, team_id: int) -> TeamMatchConfig:
    """Create balanced 4-3-3 team config with game_state_adaptivity=0.0."""
    cfg = build_default_team_config(
        team_name=team_name,
        base_player_id=base_player_id,
        team_id=team_id,
        formation_name="4-3-3",
        coaching_style="balanced",
    )
    cfg.tactical_instructions.defensive_line_height = 0.50
    cfg.tactical_instructions.pressing_intensity = 0.50
    cfg.tactical_instructions.directness = 0.45
    cfg.tactical_instructions.build_up_tempo = 0.50
    cfg.tactical_instructions.counter_press_vs_regroup = 0.50
    cfg.tactical_instructions.game_state_adaptivity = 0.0
    return cfg


def _make_match_config(
    match_id: int,
    home_tactics: Optional[Dict[str, float]] = None,
    home_slot_overrides: Optional[Dict[int, PlayerAttributeOverride]] = None,
) -> MatchConfigurationV2:
    home = _base_team("Baseline Home", base_player_id=1000, team_id=1)
    away = _base_team("Baseline Away", base_player_id=2000, team_id=2)

    if home_tactics:
        for k, v in home_tactics.items():
            setattr(home.tactical_instructions, k, float(v))

    if home_slot_overrides:
        for slot_idx, override in home_slot_overrides.items():
            home.starting_xi[slot_idx].inline_profile_override = override

    return MatchConfigurationV2(match_id=match_id, home=home, away=away)


def _paired_stats(var_arr: List[float], base_arr: List[float]) -> Dict[str, float]:
    """Compute mean_var, mean_base, mean_delta, and 95% CI over paired samples."""
    n = len(var_arr)
    deltas = [v - b for v, b in zip(var_arr, base_arr)]
    mean_v = sum(var_arr) / max(1, n)
    mean_b = sum(base_arr) / max(1, n)
    mean_d = sum(deltas) / max(1, n)
    if n > 1:
        var_d = sum((d - mean_d) ** 2 for d in deltas) / (n - 1)
        se_d = math.sqrt(var_d / n)
    else:
        se_d = 0.0
    ci95 = 1.96 * se_d
    return {
        "var_mean": round(mean_v, 4),
        "base_mean": round(mean_b, 4),
        "delta_mean": round(mean_d, 4),
        "ci95_low": round(mean_d - ci95, 4),
        "ci95_high": round(mean_d + ci95, 4),
    }


def _extract_per_match_arrays(raw: Dict[str, Any]) -> Dict[str, List[float]]:
    """Convert raw CPU tensors from _run_batched_steps into per-match float lists."""
    h_score = raw["home_score"].float()
    a_score = raw["away_score"].float()
    h_xg = raw["home_xg"].float()
    a_xg = raw["away_xg"].float()
    h_shots = raw["home_shots"].float()
    a_shots = raw["away_shots"].float()
    h_passes = raw["home_passes"].float()
    a_passes = raw["away_passes"].float()
    h_passes_ok = raw["home_passes_ok"].float()
    a_passes_ok = raw["away_passes_ok"].float()
    h_build = raw["home_build_passes"].float()
    a_build = raw["away_build_passes"].float()
    h_def = raw["home_def_act"].float()
    a_def = raw["away_def_act"].float()
    h_f3 = raw["home_f3_touches"].float()
    a_f3 = raw["away_f3_touches"].float()
    h_ht = raw["home_high_turnovers"].float()
    a_ht = raw["away_high_turnovers"].float()
    h_sp_xg = raw["home_set_piece_xg"].float()
    a_sp_xg = raw["away_set_piece_xg"].float()
    h_g75 = raw["home_goals_75_plus"].float()
    a_g75 = raw["away_goals_75_plus"].float()

    home_ppda = a_build / h_def.clamp(min=1.0)
    away_ppda = h_build / a_def.clamp(min=1.0)
    home_field_tilt = h_f3 / (h_f3 + a_f3).clamp(min=1.0)
    home_conv = h_score / h_xg.clamp(min=0.01)
    away_conv = a_score / a_xg.clamp(min=0.01)
    home_pass_pct = h_passes_ok / h_passes.clamp(min=1.0)
    away_pass_pct = a_passes_ok / a_passes.clamp(min=1.0)
    home_passes_per_shot = h_passes / h_shots.clamp(min=1.0)

    return {
        "home_goals": h_score.tolist(),
        "away_goals": a_score.tolist(),
        "total_goals": (h_score + a_score).tolist(),
        "home_xg": h_xg.tolist(),
        "away_xg": a_xg.tolist(),
        "home_shots": h_shots.tolist(),
        "away_shots": a_shots.tolist(),
        "home_passes": h_passes.tolist(),
        "away_passes": a_passes.tolist(),
        "home_passes_ok": h_passes_ok.tolist(),
        "away_passes_ok": a_passes_ok.tolist(),
        "home_build_passes": h_build.tolist(),
        "away_build_passes": a_build.tolist(),
        "home_def_act": h_def.tolist(),
        "away_def_act": a_def.tolist(),
        "home_f3_touches": h_f3.tolist(),
        "away_f3_touches": a_f3.tolist(),
        "home_high_turnovers": h_ht.tolist(),
        "away_high_turnovers": a_ht.tolist(),
        "home_set_piece_xg": h_sp_xg.tolist(),
        "away_set_piece_xg": a_sp_xg.tolist(),
        "home_goals_75_plus": h_g75.tolist(),
        "away_goals_75_plus": a_g75.tolist(),
        "home_ppda": home_ppda.tolist(),
        "away_ppda": away_ppda.tolist(),
        "home_field_tilt": home_field_tilt.tolist(),
        "home_conversion": home_conv.tolist(),
        "away_conversion": away_conv.tolist(),
        "home_pass_pct": home_pass_pct.tolist(),
        "away_pass_pct": away_pass_pct.tolist(),
        "home_passes_per_shot": home_passes_per_shot.tolist(),
    }


def _check_nan_oob(raw: Dict[str, Any]) -> Dict[str, Any]:
    """Verify zero NaNs/Infs and non-negative counts across all output tensors."""
    nan_keys: List[str] = []
    oob_keys: List[str] = []
    for k, v in raw.items():
        if isinstance(v, torch.Tensor):
            if v.is_floating_point() and not bool(torch.isfinite(v).all().item()):
                nan_keys.append(k)
            if bool((v < 0).any().item()):
                oob_keys.append(k)
    return {
        "nan_count": len(nan_keys),
        "oob_count": len(oob_keys),
        "nan_keys": nan_keys,
        "oob_keys": oob_keys,
        "clean": len(nan_keys) == 0 and len(oob_keys) == 0,
    }


def _compute_baseline_realism(raw: Dict[str, Any]) -> Dict[str, Any]:
    """Compute aggregate realism metrics on the baseline arm."""
    h_score = raw["home_score"].float()
    a_score = raw["away_score"].float()
    n = max(1, int(h_score.numel()))

    h_wins = float((h_score > a_score).sum().item()) / n
    draws = float((h_score == a_score).sum().item()) / n
    a_wins = float((h_score < a_score).sum().item()) / n

    tot_goals = float((h_score + a_score).sum().item())
    tot_xg = float((raw["home_xg"] + raw["away_xg"]).sum().item())
    tot_shots = float((raw["home_shots"] + raw["away_shots"]).sum().item())
    tot_passes = float((raw["home_passes"] + raw["away_passes"]).sum().item())
    tot_passes_ok = float((raw["home_passes_ok"] + raw["away_passes_ok"]).sum().item())
    tot_sp_xg = float((raw["home_set_piece_xg"] + raw["away_set_piece_xg"]).sum().item())
    tot_g75 = float((raw["home_goals_75_plus"] + raw["away_goals_75_plus"]).sum().item())

    h_build = float(raw["home_build_passes"].sum().item())
    a_build = float(raw["away_build_passes"].sum().item())
    h_def = float(raw["home_def_act"].sum().item())
    a_def = float(raw["away_def_act"].sum().item())

    return {
        "matches": n,
        "mean_goals_per_match": round(tot_goals / n, 4),
        "home_win_rate": round(h_wins, 4),
        "draw_rate": round(draws, 4),
        "away_win_rate": round(a_wins, 4),
        "mean_home_xg": round(float(raw["home_xg"].mean().item()), 4),
        "mean_away_xg": round(float(raw["away_xg"].mean().item()), 4),
        "xg_per_shot": round(tot_xg / max(1.0, tot_shots), 4),
        "pass_completion_pct": round(100.0 * tot_passes_ok / max(1.0, tot_passes), 2),
        "home_ppda": round(a_build / max(1.0, h_def), 4),
        "away_ppda": round(h_build / max(1.0, a_def), 4),
        "mean_home_high_turnovers": round(float(raw["home_high_turnovers"].float().mean().item()), 4),
        "mean_away_high_turnovers": round(float(raw["away_high_turnovers"].float().mean().item()), 4),
        "set_piece_xg_share": round(tot_sp_xg / max(1e-6, tot_xg), 4),
        "goals_75_plus_share": round(tot_g75 / max(1.0, tot_goals), 4),
        "integrity": _check_nan_oob(raw),
    }


def _write_json(path: Path, payload: Dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="PixelPitchAI v2 Counterfactual & Realism Validator")
    parser.add_argument("--models-dir", type=str, default="./models_v2")
    parser.add_argument("--device", type=str, default="cuda" if torch.cuda.is_available() else "cpu")
    parser.add_argument("--batch-size", type=int, default=16)
    parser.add_argument("--max-events", type=int, default=4200)
    parser.add_argument("--base-seed", type=int, default=42000)
    parser.add_argument("--arms", type=str, default="", help="Optional comma-separated subset of arm names to run")
    parser.add_argument("--output", type=str, default="./phase3_counterfactual_results.json")
    args = parser.parse_args()

    out_path = Path(args.output)
    engine = HybridRolloutEngine.from_models_dir(models_dir=args.models_dir, device=args.device)

    arms_spec: List[Tuple[str, MatchConfigurationV2]] = [
        ("baseline", _make_match_config(1)),
        (
            "high_press",
            _make_match_config(
                2,
                home_tactics={"pressing_intensity": 0.88, "counter_press_vs_regroup": 0.88},
            ),
        ),
        (
            "high_line",
            _make_match_config(
                3,
                home_tactics={"defensive_line_height": 0.86, "pressing_intensity": 0.75},
            ),
        ),
        (
            "low_block",
            _make_match_config(
                4,
                home_tactics={
                    "defensive_line_height": 0.22,
                    "pressing_intensity": 0.28,
                    "defensive_compactness": 0.82,
                },
            ),
        ),
        (
            "elite_finisher",
            _make_match_config(
                5,
                home_slot_overrides={
                    8: PlayerAttributeOverride(display_name="Elite Finisher LW", finishing_log_odds=0.35),
                    9: PlayerAttributeOverride(display_name="Elite Finisher ST", finishing_log_odds=0.35),
                    10: PlayerAttributeOverride(display_name="Elite Finisher RW", finishing_log_odds=0.35),
                },
            ),
        ),
        (
            "elite_gk",
            _make_match_config(
                6,
                home_slot_overrides={
                    0: PlayerAttributeOverride(
                        display_name="Elite GK",
                        gk_attributes_8d={"gk_shot_stopping_psxg_prevented": 0.95},
                    ),
                },
            ),
        ),
        (
            "direct_vertical",
            _make_match_config(
                7,
                home_tactics={
                    "directness": 0.86,
                    "build_up_tempo": 0.82,
                    "counter_attack_urgency": 0.86,
                },
            ),
        ),
        (
            "elite_midfield_pass",
            _make_match_config(
                8,
                home_slot_overrides={
                    5: PlayerAttributeOverride(
                        display_name="Press-Resistant Mid LCM",
                        attributes_24d={"short_pass_accuracy": 0.95, "press_resistance": 0.95},
                    ),
                    6: PlayerAttributeOverride(
                        display_name="Press-Resistant Mid DM",
                        attributes_24d={"short_pass_accuracy": 0.95, "press_resistance": 0.95},
                    ),
                    7: PlayerAttributeOverride(
                        display_name="Press-Resistant Mid RCM",
                        attributes_24d={"short_pass_accuracy": 0.95, "press_resistance": 0.95},
                    ),
                },
            ),
        ),
    ]

    results: Dict[str, Any] = {
        "config": {
            "models_dir": args.models_dir,
            "device": str(engine.device),
            "weights_loaded": bool(engine.weights_loaded),
            "loaded_artifacts": engine.loaded_artifacts,
            "batch_size": args.batch_size,
            "max_events": args.max_events,
            "base_seed": args.base_seed,
        },
        "arms": {},
        "baseline_realism": {},
        "counterfactual_checks": {},
    }

    arm_arrays: Dict[str, Dict[str, List[float]]] = {}

    if args.arms.strip():
        selected = {a.strip() for a in args.arms.split(",") if a.strip()}
        arms_spec = [(n, c) for (n, c) in arms_spec if n in selected]

    with torch.inference_mode():
        for name, cfg in arms_spec:
            t0 = time.perf_counter()
            raw = engine._run_batched_steps(
                batch_size=args.batch_size,
                max_events=args.max_events,
                seed=args.base_seed,
                match_config=cfg,
                collect_events=False,
            )
            elapsed_s = round(time.perf_counter() - t0, 2)
            arrays = _extract_per_match_arrays(raw)
            arm_arrays[name] = arrays

            summary = {
                k: round(sum(vals) / max(1, len(vals)), 4)
                for k, vals in arrays.items()
            }
            results["arms"][name] = {
                "elapsed_s": elapsed_s,
                "integrity": _check_nan_oob(raw),
                "means": summary,
            }
            if name == "baseline":
                results["baseline_realism"] = _compute_baseline_realism(raw)

            _write_json(out_path, results)
            print(f"[arm={name}] done in {elapsed_s}s | mean_goals={summary['total_goals']:.3f}", flush=True)

    base = arm_arrays.get("baseline")

    # 1. High Press vs Baseline
    if base is not None and "high_press" in arm_arrays:
        hp = arm_arrays["high_press"]
        hp_ht = _paired_stats(hp["home_high_turnovers"], base["home_high_turnovers"])
        hp_ppda = _paired_stats(hp["home_ppda"], base["home_ppda"])
        results["counterfactual_checks"]["high_press_vs_baseline"] = {
            "home_high_turnovers": hp_ht,
            "home_ppda": hp_ppda,
            "passed": (hp_ht["delta_mean"] > 0.0) and (hp_ppda["delta_mean"] < 0.0),
        }

    # 2. High Line vs Low Block
    if "high_line" in arm_arrays and "low_block" in arm_arrays:
        hl = arm_arrays["high_line"]
        lb = arm_arrays["low_block"]
        hl_tilt = _paired_stats(hl["home_field_tilt"], lb["home_field_tilt"])
        hl_ht = _paired_stats(hl["home_high_turnovers"], lb["home_high_turnovers"])
        hl_axg = _paired_stats(hl["away_xg"], lb["away_xg"])
        hl_aht = _paired_stats(hl["away_high_turnovers"], lb["away_high_turnovers"])
        hl_af3 = _paired_stats(hl["away_f3_touches"], lb["away_f3_touches"])
        results["counterfactual_checks"]["high_line_vs_low_block"] = {
            "home_field_tilt": hl_tilt,
            "home_high_turnovers": hl_ht,
            "away_xg": hl_axg,
            "away_high_turnovers": hl_aht,
            "away_f3_touches": hl_af3,
            "passed": (hl_axg["delta_mean"] > 0.0) and ((hl_aht["delta_mean"] > 0.0) or (hl_af3["delta_mean"] > 0.0)),
        }

    # 3. Elite Finisher vs Baseline
    if base is not None and "elite_finisher" in arm_arrays:
        ef = arm_arrays["elite_finisher"]
        ef_conv = _paired_stats(ef["home_conversion"], base["home_conversion"])
        ef_goals = _paired_stats(ef["home_goals"], base["home_goals"])
        ef_shots = _paired_stats(ef["home_shots"], base["home_shots"])
        ef_shot_drift = abs(ef_shots["var_mean"] - ef_shots["base_mean"]) / max(1.0, ef_shots["base_mean"])
        results["counterfactual_checks"]["elite_finisher_vs_baseline"] = {
            "home_conversion": ef_conv,
            "home_goals": ef_goals,
            "home_shots": ef_shots,
            "home_shots_rel_drift": round(ef_shot_drift, 4),
            "passed": (ef_conv["delta_mean"] > 0.0) and (ef_goals["delta_mean"] > 0.0) and (ef_shot_drift < 0.15),
        }

    # 4. Elite GK vs Baseline
    if base is not None and "elite_gk" in arm_arrays:
        egk = arm_arrays["elite_gk"]
        egk_conv = _paired_stats(egk["away_conversion"], base["away_conversion"])
        egk_goals = _paired_stats(egk["away_goals"], base["away_goals"])
        egk_shots = _paired_stats(egk["away_shots"], base["away_shots"])
        egk_shot_drift = abs(egk_shots["var_mean"] - egk_shots["base_mean"]) / max(1.0, egk_shots["base_mean"])
        results["counterfactual_checks"]["elite_gk_vs_baseline"] = {
            "away_conversion": egk_conv,
            "away_goals": egk_goals,
            "away_shots": egk_shots,
            "away_shots_rel_drift": round(egk_shot_drift, 4),
            "passed": (egk_conv["delta_mean"] < 0.0) and (egk_goals["delta_mean"] < 0.0) and (egk_shot_drift < 0.15),
        }

    # 5. Direct Vertical vs Baseline
    if base is not None and "direct_vertical" in arm_arrays:
        dv = arm_arrays["direct_vertical"]
        dv_passes = _paired_stats(dv["home_passes"], base["home_passes"])
        dv_pps = _paired_stats(dv["home_passes_per_shot"], base["home_passes_per_shot"])
        results["counterfactual_checks"]["direct_vertical_vs_baseline"] = {
            "home_passes": dv_passes,
            "home_passes_per_shot": dv_pps,
            "passed": (dv_passes["delta_mean"] < 0.0) or (dv_pps["delta_mean"] < 0.0),
        }

    # 6. Elite Midfield Pass vs Baseline
    if base is not None and "elite_midfield_pass" in arm_arrays:
        emp = arm_arrays["elite_midfield_pass"]
        emp_pct = _paired_stats(emp["home_pass_pct"], base["home_pass_pct"])
        results["counterfactual_checks"]["elite_midfield_pass_vs_baseline"] = {
            "home_pass_pct": emp_pct,
            "passed": emp_pct["delta_mean"] > 0.0,
        }

    _write_json(out_path, results)
    print(f"All {len(arms_spec)} selected arms complete. Results written to {out_path}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
