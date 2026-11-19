"""
Comprehensive unit & integration tests for Pillar 1 (`statsbombapi_v2`).
"""

from __future__ import annotations

import asyncio
import json
import math
import os
import sys
from pathlib import Path

import numpy as np
import pytest

ENGINE_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if ENGINE_ROOT not in sys.path:
    sys.path.insert(0, ENGINE_ROOT)

from statsbombapi_v2 import (
    ActionType,
    BodyPart,
    EntityResolver,
    EventGoldV2,
    EventOutcome,
    ExpectedThreatModel,
    FBrefAdapter,
    HistoricalPlayerProfiler,
    LakehouseEngine,
    MatchPeriod,
    PLAYER_ROLE_NAMES,
    PassHeight,
    PlayerIdCrosswalk,
    PlayerProfileV2,
    PlayerRole,
    PoliteScraperSession,
    PossPhase,
    PostShotXGModel,
    PreShotXGModel,
    RestartType,
    ScraperBlockedError,
    SofaScoreFotMobAdapter,
    TacticalManifoldV2,
    TransfermarktEAFCAdapter,
    UnderstatAdapter,
    VAEPModel,
    XPassModel,
    assign_match_splits,
    compute_freeze_frame_geometry,
    enrich_events_with_labels,
    export_packed_shards,
    export_profiles_parquet,
    extract_team_match_tactics,
    ingest_open_data_dir,
    ingest_statsbomb_match,
    ingest_wyscout_match,
    normalize_player_name,
    pack_modifier_bitmask,
    pack_sequences_16k,
    quantize_dt_bucket,
    quantize_end_z,
    quantize_pitch_cell,
    rescale_opta,
    rescale_skillcorner,
    rescale_wyscout,
    run_all_gates,
    to_attacking_relative,
    to_canonical_global,
    validate_gold_events,
    validate_splits,
)


def _make_sample_sb_match() -> tuple[dict, list[dict], list[dict]]:
    """Build a realistic StatsBomb match payload with Starting XI, passes, receipts, carry, 3D shot, sub, red card, and 360."""
    match_meta = {
        "match_id": 9001,
        "match_date": "2024-03-10",
        "home_team": {"home_team_id": 10, "home_team_name": "HomeFC"},
        "away_team": {"away_team_id": 20, "away_team_name": "AwayFC"},
        "home_score": 1,
        "away_score": 0,
    }
    home_lineup = [
        {"player": {"id": 100 + i, "name": f"HomeP{i}"}, "position": {"id": pos_id}, "jersey_number": i + 1}
        for i, pos_id in enumerate([1, 2, 3, 5, 6, 10, 13, 15, 17, 21, 23])
    ]
    away_lineup = [
        {"player": {"id": 200 + i, "name": f"AwayP{i}"}, "position": {"id": pos_id}, "jersey_number": i + 1}
        for i, pos_id in enumerate([1, 2, 3, 5, 6, 10, 13, 15, 17, 21, 23])
    ]

    raw_events = [
        {
            "id": "ev-xi-h",
            "index": 1,
            "period": 1,
            "timestamp": "00:00:00.000",
            "minute": 0,
            "second": 0,
            "type": {"id": 35, "name": "Starting XI"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "tactics": {"formation": 433, "lineup": home_lineup},
        },
        {
            "id": "ev-xi-a",
            "index": 2,
            "period": 1,
            "timestamp": "00:00:00.000",
            "minute": 0,
            "second": 0,
            "type": {"id": 35, "name": "Starting XI"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 20, "name": "AwayFC"},
            "tactics": {"formation": 4231, "lineup": away_lineup},
        },
        {
            "id": "ev-pass-1",
            "index": 3,
            "period": 1,
            "timestamp": "00:00:01.000",
            "minute": 0,
            "second": 1,
            "type": {"id": 30, "name": "Pass"},
            "play_pattern": {"id": 9, "name": "From Kick Off"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 110, "name": "HomeP10"},
            "position": {"id": 23, "name": "Striker"},
            "location": [60.0, 40.0],
            "under_pressure": True,
            "related_events": ["ev-rec-1"],
            "pass": {
                "recipient": {"id": 105, "name": "HomeP5"},
                "length": 14.0,
                "angle": 3.0,
                "height": {"id": 1, "name": "Ground Pass"},
                "end_location": [46.0, 41.0],
                "body_part": {"id": 40, "name": "Right Foot"},
                "type": {"id": 65, "name": "Kick Off"},
            },
        },
        {
            "id": "ev-rec-1",
            "index": 4,
            "period": 1,
            "timestamp": "00:00:02.000",
            "minute": 0,
            "second": 2,
            "type": {"id": 42, "name": "Ball Receipt*"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 105, "name": "HomeP5"},
            "position": {"id": 10, "name": "Center Defensive Midfield"},
            "location": [46.0, 41.0],
        },
        # Micro carry (<0.5 yd & <0.4s) right before pass -> should fold into one_touch
        {
            "id": "ev-micro-carry",
            "index": 5,
            "period": 1,
            "timestamp": "00:00:02.100",
            "minute": 0,
            "second": 2,
            "duration": 0.2,
            "type": {"id": 43, "name": "Carry"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 105, "name": "HomeP5"},
            "location": [46.0, 41.0],
            "carry": {"end_location": [46.2, 41.1]},
        },
        {
            "id": "ev-pass-2",
            "index": 6,
            "period": 1,
            "timestamp": "00:00:02.300",
            "minute": 0,
            "second": 2,
            "type": {"id": 30, "name": "Pass"},
            "play_pattern": {"id": 1, "name": "Regular Play"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 105, "name": "HomeP5"},
            "location": [46.2, 41.1],
            "under_pressure": True,
            "related_events": ["ev-rec-2"],
            "pass": {
                "recipient": {"id": 108, "name": "HomeP8"},
                "height": {"id": 3, "name": "High Pass"},
                "end_location": [92.0, 68.0],
                "body_part": {"id": 40, "name": "Right Foot"},
                "switch": True,
                "through_ball": True,
            },
        },
        {
            "id": "ev-rec-2",
            "index": 7,
            "period": 1,
            "timestamp": "00:00:04.500",
            "minute": 0,
            "second": 4,
            "type": {"id": 42, "name": "Ball Receipt*"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 108, "name": "HomeP8"},
            "location": [92.0, 68.0],
        },
        # Real carry with carry_end_location preserved
        {
            "id": "ev-carry-real",
            "index": 8,
            "period": 1,
            "timestamp": "00:00:05.000",
            "minute": 0,
            "second": 5,
            "duration": 2.0,
            "type": {"id": 43, "name": "Carry"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 108, "name": "HomeP8"},
            "location": [92.0, 68.0],
            "under_pressure": True,
            "carry": {"end_location": [104.0, 44.0]},
        },
        # Shot with 3D end_location + shot_statsbomb_xg + shot_freeze_frame -> Goal
        {
            "id": "ev-shot-goal",
            "index": 9,
            "period": 1,
            "timestamp": "00:00:07.500",
            "minute": 0,
            "second": 7,
            "type": {"id": 16, "name": "Shot"},
            "play_pattern": {"id": 1, "name": "Regular Play"},
            "possession": 1,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 108, "name": "HomeP8"},
            "location": [104.0, 44.0],
            "under_pressure": False,
            "shot": {
                "statsbomb_xg": 0.34,
                "end_location": [120.0, 42.8, 1.85],
                "outcome": {"id": 97, "name": "Goal"},
                "body_part": {"id": 38, "name": "Left Foot"},
                "type": {"id": 87, "name": "Open Play"},
                "first_time": True,
                "one_on_one": False,
                "freeze_frame": [
                    {"location": [117.5, 40.0], "player": {"id": 200}, "position": {"id": 1, "name": "Goalkeeper"}, "teammate": False},
                    {"location": [110.0, 41.5], "player": {"id": 202}, "position": {"id": 3, "name": "Right Center Back"}, "teammate": False},
                    {"location": [101.0, 32.0], "player": {"id": 110}, "position": {"id": 23, "name": "Striker"}, "teammate": True},
                ],
            },
        },
        # Away team foul -> Red Card (tests 180-deg flip & sent-off slot bitmask)
        {
            "id": "ev-away-foul-red",
            "index": 10,
            "period": 2,
            "timestamp": "00:05:00.000",
            "minute": 50,
            "second": 0,
            "type": {"id": 22, "name": "Foul Committed"},
            "possession": 2,
            "possession_team": {"id": 20, "name": "AwayFC"},
            "team": {"id": 20, "name": "AwayFC"},
            "player": {"id": 202, "name": "AwayP2"},
            "location": [25.0, 30.0],
            "foul_committed": {"card": {"id": 7, "name": "Red Card"}},
        },
        # Substitution for Home team (replaces HomeP8 -> 199)
        {
            "id": "ev-sub-home",
            "index": 11,
            "period": 2,
            "timestamp": "00:15:00.000",
            "minute": 60,
            "second": 0,
            "type": {"id": 19, "name": "Substitution"},
            "possession": 3,
            "possession_team": {"id": 10, "name": "HomeFC"},
            "team": {"id": 10, "name": "HomeFC"},
            "player": {"id": 108, "name": "HomeP8"},
            "location": [60.0, 0.0],
            "substitution": {"replacement": {"id": 199, "name": "HomeSub99"}},
        },
    ]

    frames_360 = [
        {
            "event_uuid": "ev-pass-2",
            "visible_area": [30.0, 10.0, 110.0, 10.0, 110.0, 75.0, 30.0, 75.0],
            "freeze_frame": [
                {"teammate": True, "actor": True, "keeper": False, "location": [46.2, 41.1]},
                {"teammate": True, "actor": False, "keeper": False, "location": [92.0, 68.0]},
                {"teammate": False, "actor": False, "keeper": False, "location": [65.0, 52.0]},
                {"teammate": False, "actor": False, "keeper": False, "location": [78.0, 56.0]},
                {"teammate": False, "actor": False, "keeper": True, "location": [116.0, 40.0]},
            ],
        }
    ]
    return match_meta, raw_events, frames_360


# --- 1. Schemas & Coordinates ---

def test_schemas_coordinates_and_quantizers():
    # Canonical global flip symmetry
    xc, yc = to_canonical_global(102.5, 41.4, is_home=False)
    assert math.isclose(xc, 17.5) and math.isclose(yc, 38.6)
    xa, ya = to_attacking_relative(xc, yc, is_home=False)
    assert math.isclose(xa, 102.5) and math.isclose(ya, 41.4)

    # Provider rescalings
    assert rescale_wyscout(50.0, 50.0) == (60.0, 40.0)
    assert rescale_opta(50.0, 100.0) == (60.0, 0.0)  # Opta y=100 is StatsBomb y=0
    assert rescale_skillcorner(0.0, 0.0) == (60.0, 40.0)

    # Discretizers & roles
    assert len(PlayerRole) == 22
    assert len(PLAYER_ROLE_NAMES) == 22
    assert list(range(22)) == [int(r) for r in PlayerRole]
    assert len(PossPhase) == 8

    cid, dx, dy = quantize_pitch_cell(60.0, 40.0)
    assert 0 <= cid < 600
    assert -2.0 <= dx <= 2.0 and -2.0 <= dy <= 2.0
    assert quantize_pitch_cell(125.0, 40.0)[0] == 600  # OFF_PITCH
    assert quantize_pitch_cell(None, 40.0)[0] == 601   # NONE

    dtb, dtr = quantize_dt_bucket(4.2)
    assert 0 <= dtb < 64
    assert isinstance(dtr, float)
    assert 0 <= quantize_end_z(1.85) < 32
    assert pack_modifier_bitmask(under_pressure=True, through_ball=True) == 3

    prof = PlayerProfileV2(
        player_id=10,
        season_id="2024/2025",
        role_suitability_scores=[0.5] * 22,
        attributes_24d=[0.6] * 24,
        sigma_24d=[0.1] * 24,
        gk_attributes_8d=[0.5] * 8,
        latent_style_64d=[1.0 / 8.0] * 64,
    )
    assert len(prof.player_capability_vec_32d) == 32
    assert len(prof.active_gk_attributes_6d) == 6


# --- 2. ETL, Folding, Slot Tracking, 360 Geometry & Open-Data Dir ---

def test_etl_statsbomb_folding_slots_and_360_geometry(tmp_path: Path):
    meta, raw_ev, frames = _make_sample_sb_match()
    events = ingest_statsbomb_match(meta, raw_ev, frames_360=frames)

    # Starting XI (2), Ball Receipt* (2), and micro-carry (1) folded -> 6 structured events
    assert len(events) == 6
    assert [e.action_type for e in events] == [
        ActionType.PASS,
        ActionType.PASS,
        ActionType.CARRY,
        ActionType.SHOT,
        ActionType.FOUL_COMMITTED,
        ActionType.SUBSTITUTION,
    ]

    # Folded receipt attached target_player_id & target_slot onto Pass 1
    p1 = events[0]
    assert p1.target_player_id == 105
    assert 1 <= p1.target_slot <= 11
    assert p1.restart_type == RestartType.KICK_OFF

    # Micro-carry folded into one_touch + first_time on Pass 2 + 360 corridor features computed
    p2 = events[1]
    assert p2.one_touch and p2.first_time
    assert p2.pass_switch and p2.through_ball and p2.under_pressure
    assert p2.has_360_frame is True
    assert p2.ff_receiver_voronoi_area > 0.0
    assert p2.ff_actor_packing_bypassed >= 2
    assert p2.ff_min_defender_dist_to_pass_ray < 30.0

    # Real carry preserved end location
    c_real = events[2]
    assert c_real.end_x_canon == 104.0 and c_real.end_y_canon == 44.0
    assert c_real.under_pressure is True

    # Shot preserves 3D z, xG, and cone occlusion (<1.0 because GK + CB sit in shot cone)
    shot = events[3]
    assert shot.xg_statsbomb == pytest.approx(0.34)
    assert shot.end_z_m == pytest.approx(1.85)
    assert shot.end_z_bin >= 0
    assert shot.has_shot_freeze_frame is True
    assert shot.ff_n_defenders_in_shot_triangle >= 1
    assert 0.0 < shot.ff_open_goal_fraction < 1.0
    assert shot.score_home == 0 and shot.score_away == 0  # pre-shot score

    # Away foul occurs AFTER goal -> pre-score is (1, 0), canonical coords rotated 180 deg, Red Card updates sent-off mask for subsequent events
    foul = events[4]
    assert foul.score_home == 1 and foul.score_away == 0
    assert foul.x_canon == pytest.approx(95.0) and foul.y_canon == pytest.approx(50.0)
    assert foul.outcome == EventOutcome.RED_CARD
    assert foul.active_slots_sent_off == 0  # pre-card state prior to foul

    # Substitution swaps incoming player 199 in-place into outgoing player 108's slot and carries Red Card bitmask forward
    sub = events[5]
    assert sub.active_slots_sent_off > 0
    assert 199 in sub.active_slots_player_ids
    assert 108 not in sub.active_slots_player_ids

    # Lakehouse SQL engine round-trip
    engine = LakehouseEngine()
    engine.register_events(events)
    res = engine.query(
        f"SELECT COUNT(*) AS n, SUM(CASE WHEN outcome = {int(EventOutcome.GOAL)} THEN 1 ELSE 0 END) AS goals FROM events_gold_v2"
    )
    assert int(res["n"].iloc[0]) == 6
    assert int(res["goals"].iloc[0]) == 1
    exported = engine.export_table("events_gold_v2", tmp_path / "events_gold_v2")
    assert exported.exists()
    engine.close()


def test_etl_wyscout_and_open_data_dir(tmp_path: Path):
    wy_events = [
        {"id": 1, "matchPeriod": "1H", "eventSec": 1.0, "teamId": 1, "playerId": 11, "eventName": "Pass", "subEventName": "Smart pass", "positions": [{"x": 40, "y": 50}, {"x": 55, "y": 50}], "tags": [{"id": 1801}, {"id": 1901}]},
        {"id": 2, "matchPeriod": "1H", "eventSec": 4.0, "teamId": 1, "playerId": 11, "eventName": "Shot", "subEventName": "Shot", "positions": [{"x": 85, "y": 50}], "tags": [{"id": 101}]},
    ]
    events = ingest_wyscout_match({"wyId": 77, "home_team_id": 1, "away_team_id": 2}, wy_events)
    # Implicit carry synthesized because player 11 moved from x=55% (66 yd) to x=85% (102 yd)
    assert [e.action_type for e in events] == [ActionType.PASS, ActionType.CARRY, ActionType.SHOT]
    assert events[0].through_ball and events[0].counter_attack
    assert events[2].outcome == EventOutcome.GOAL
    assert all(not e.has_360_frame for e in events)

    # Local hudl/open-data directory loader
    meta, raw_ev, frames = _make_sample_sb_match()
    od_root = tmp_path / "open-data" / "data"
    (od_root / "matches" / "11").mkdir(parents=True)
    (od_root / "events").mkdir(parents=True)
    (od_root / "three-sixty").mkdir(parents=True)
    (od_root / "competitions.json").write_text(json.dumps([{"competition_id": 11, "season_id": 90}]), encoding="utf-8")
    (od_root / "matches" / "11" / "90.json").write_text(json.dumps([meta]), encoding="utf-8")
    (od_root / "events" / "9001.json").write_text(json.dumps(raw_ev), encoding="utf-8")
    (od_root / "three-sixty" / "9001.json").write_text(json.dumps(frames), encoding="utf-8")
    loaded = ingest_open_data_dir(tmp_path / "open-data")
    assert len(loaded) == 6


# --- 3. Scrapers & Entity Resolution ---

def test_scrapers_and_entity_resolver(tmp_path: Path):
    # Cache & CAPTCHA compliance
    session = PoliteScraperSession(cache_dir=tmp_path / "cache", min_interval_sec=0.0)
    session.put_cached("k1", "https://example.org/test", "cached_ok")
    assert asyncio.run(session.fetch_text("https://example.org/test", "example.org", cache_key="k1")) == "cached_ok"
    with pytest.raises(ScraperBlockedError):
        PoliteScraperSession.check_response_allowed(200, "<html>Please verify you are human (CAPTCHA)</html>", "example.org")
    with pytest.raises(ScraperBlockedError):
        PoliteScraperSession.check_response_allowed(429, "Too many requests", "example.org")

    # FBref HTML parser
    fbref_html = """
    <table id="stats_standard">
      <tr><th>Player</th><th>Squad</th><th>Pos</th><th>Born</th><th>Min</th><th>PrgP</th><th>PrgC</th><th>npxG</th><th>xAG</th><th>Cmp%</th></tr>
      <tr><td>Lamine Yamal</td><td>Barcelona</td><td>FW</td><td>2007</td><td>1800</td><td>95</td><td>140</td><td>7.2</td><td>8.5</td><td>81.5</td></tr>
      <tr><td>Pedri</td><td>Barcelona</td><td>MF</td><td>2002</td><td>1620</td><td>160</td><td>80</td><td>2.4</td><td>6.0</td><td>89.2</td></tr>
    </table>
    """
    fb_recs = FBrefAdapter.parse_scouting_html(fbref_html)
    assert len(fb_recs) == 2
    assert fb_recs[0]["prog_carries_per90"] == pytest.approx(140.0 / 20.0)

    # Understat parser
    understat_html = """<script>var shotsData = JSON.parse('{"h":[{"player_id":"2097","player":"Lamine Yamal","h_team":"Barcelona","minute":"67","xG":"0.18","result":"Goal","X":"0.88","Y":"0.35","shotType":"LeftFoot","situation":"OpenPlay","lastAction":"Throughball"}],"a":[]}');</script>"""
    us_shots = UnderstatAdapter.parse_match_shots(understat_html, match_id=555)
    assert len(us_shots) == 1
    assert us_shots[0]["x_att"] == pytest.approx(0.88 * 120.0)
    assert us_shots[0]["is_goal"] is True

    # SofaScore / FotMob / Transfermarkt adapters
    hm = SofaScoreFotMobAdapter.heatmap_points_to_24x16([(80.0, 20.0), (82.0, 22.0)])
    assert len(hm) == 384 and math.isclose(sum(hm), 1.0)
    scraped_evs = SofaScoreFotMobAdapter.parse_match_events(
        {
            "home_lineup": [{"id": 1001}],
            "away_lineup": [{"id": 2001}],
            "events": [{"player_id": 1001, "is_home": True, "minute": 12, "type": "shot", "x": 90, "y": 50, "is_goal": True, "xg": 0.42}],
        },
        match_id=888,
        home_team_id=10,
        away_team_id=20,
    )
    assert len(scraped_evs) == 1 and scraped_evs[0].provider_source == "scraped_event"

    tm_recs = TransfermarktEAFCAdapter.parse_roster_records(
        [{"player_name": "Kylian Mbappé", "team_name": "Real Madrid", "birth_year": 1998, "preferred_foot": "Right", "weak_foot_stars": 4, "sprint_speed": 97, "acceleration": 97, "stamina": 88, "transfermarkt_id": 342229}]
    )
    assert tm_recs[0]["sprint_recovery_prior"] > 0.95

    # Entity Resolution (Unicode accent folding + Hungarian bipartite match + net-new ID)
    seed_cw = [
        PlayerIdCrosswalk(
            pixelpitch_player_id=12,
            statsbomb_id=3009,
            full_name_norm=normalize_player_name("Kylian Mbappé"),
            birth_year=1998,
            current_team_name="Real Madrid",
        )
    ]
    resolver = EntityResolver(existing_crosswalk=seed_cw)
    resolved = resolver.link_external_records(
        [
            {"player_name": "Kylian Mbappe", "team_name": "Real Madrid", "birth_year": 1998, "understat_id": 3423},
            {"player_name": "Lamine Yamal", "team_name": "Barcelona", "birth_year": 2007, "understat_id": 11527},
        ],
        source_id_field="understat_id",
    )
    assert len(resolved) == 2
    by_id = {c.pixelpitch_player_id: c for c in resolved}
    assert by_id[12].understat_id == 3423
    assert by_id[100_000].understat_id == 11527


# --- 4. Bellman xT, xG, PSxG, xPass, VAEP ---

def _build_synthetic_corpus(n_matches: int = 8) -> list[EventGoldV2]:
    events: list[EventGoldV2] = []
    for m_idx in range(n_matches):
        meta, raw_ev, frames = _make_sample_sb_match()
        meta["match_id"] = 9000 + m_idx
        events.extend(ingest_statsbomb_match(meta, raw_ev, frames_360=frames))
    return events


def test_labels_xt_xg_psxg_xpass_vaep(tmp_path: Path):
    events = _build_synthetic_corpus(n_matches=8)

    xt = ExpectedThreatModel().fit(events, max_iters=50)
    assert xt.grid.shape == (24, 16)
    assert xt.bellman_residual < 1e-3
    # Box center threat must exceed own-box threat
    assert xt.evaluate_point(110.0, 40.0) > xt.evaluate_point(20.0, 40.0)
    xt_path = xt.save_npy(tmp_path / "xt_grid_24x16.npy")
    assert ExpectedThreatModel.load_npy(xt_path).grid.shape == (24, 16)

    xg_m = PreShotXGModel().fit(events)
    psxg_m = PostShotXGModel(pre_xg_model=xg_m).fit(events)
    xp_m = XPassModel().fit(events)
    vaep_m = VAEPModel().fit(events)
    enrich_events_with_labels(events, xt_model=xt, xg_model=xg_m, psxg_model=psxg_m, xpass_model=xp_m, vaep_model=vaep_m)

    shots = [e for e in events if e.action_type == ActionType.SHOT]
    passes = [e for e in events if e.action_type == ActionType.PASS]
    assert all(0.0 < s.xg_v2_pre_shot < 1.0 for s in shots)
    assert all(0.0 < s.psxg_v2 <= 1.0 for s in shots if s.outcome == EventOutcome.GOAL)
    assert all(0.0 < p.xpass_prob < 1.0 for p in passes)
    assert any(abs(e.vaep_value) > 0.0 for e in events)


# --- 5. Historical Player Profiler & Tactical Manifold ---

def test_player_profiler_and_tactical_manifold(tmp_path: Path):
    events = _build_synthetic_corpus(n_matches=6)
    enrich_events_with_labels(events)
    match_dates = {9000 + i: f"2024-0{i + 1}-15" for i in range(6)}

    profiler = HistoricalPlayerProfiler(shrinkage_m0_minutes=900.0, decay_half_life_days=540.0)
    # Anti-leakage cutoff excludes 2024-06-15 match
    profiles = profiler.build_profiles(
        events,
        match_dates=match_dates,
        scraped_records=[{"player_id": 108, "prog_passes_per90": 7.5, "sprint_recovery_prior": 0.90, "minutes_played": 1800.0}],
        as_of_date="2024-06-01",
    )
    assert len(profiles) > 0
    p108 = next(p for p in profiles if p.player_id == 108)
    assert len(p108.attributes_24d) == 24
    assert len(p108.sigma_24d) == 24
    assert len(p108.gk_attributes_8d) == 8
    assert len(p108.role_suitability_scores) == 22
    assert math.isclose(float(np.linalg.norm(p108.latent_style_64d)), 1.0, abs_tol=1e-2)
    assert p108.source_provenance_flags == 3  # both event history (1) + scraped (2)

    # Synthetic player style adapter
    synth_style = profiler.predict_style_from_attributes([0.7] * 24)
    assert len(synth_style) == 64 and math.isclose(float(np.linalg.norm(synth_style)), 1.0, abs_tol=1e-2)

    # Team tactics & TacticalManifoldV2
    tactics_list = [
        extract_team_match_tactics(events, match_id=9000 + i, team_id=10, half=1)
        for i in range(6)
    ]
    manifold = TacticalManifoldV2().fit(tactics_list)
    in_dist = manifold.score_ood(tactics_list[0].as_vector_10d())
    out_dist = manifold.score_ood([0.99, 0.99, 0.99, 0.99, 0.01, 0.01, 0.99, 0.99, 0.99, 0.01])
    assert out_dist["mahalanobis_distance"] > in_dist["mahalanobis_distance"]
    m_path = manifold.save(tmp_path / "tactical_manifold_v2.pkl")
    loaded_m = TacticalManifoldV2.load(m_path)
    assert math.isclose(loaded_m.mahalanobis_distance(tactics_list[0].as_vector_10d()), in_dist["mahalanobis_distance"])

    # Cross-module compatibility with Module B TacticalManifoldModel.from_pickle
    from api.v2.tactical.ood_and_diagnostics import TacticalManifoldModel

    mod_b_manifold = TacticalManifoldModel.from_pickle(m_path)
    assert mod_b_manifold.mahalanobis_distance([0.99] * 10) > mod_b_manifold.mahalanobis_distance(tactics_list[0].as_vector_10d())


# --- 6. Sharding & Automated Data Quality Gates ---

def test_sharding_and_validation_gates(tmp_path: Path):
    events = _build_synthetic_corpus(n_matches=10)
    enrich_events_with_labels(events)
    match_ids = sorted({e.match_id for e in events})
    meta = {mid: {"competition_id": 99 if mid == 9009 else 11} for mid in match_ids}
    splits = assign_match_splits(match_ids, match_metadata=meta, ood_competition_ids=[99])
    assert 9009 in splits["ood_eval"]
    assert validate_splits(splits).passed

    by_match = {mid: [e for e in events if e.match_id == mid] for mid in match_ids}
    blocks = pack_sequences_16k(by_match, max_seq_len=20)
    assert len(blocks) >= 2
    assert all(b.length <= 20 for b in blocks)
    assert all(b.cu_seqlens[0] == 0 and b.cu_seqlens[-1] == b.length for b in blocks)

    manifest = export_packed_shards(events, tmp_path / "shards", splits=splits, max_seq_len=16384)
    assert (tmp_path / "shards" / "manifest.json").exists()
    assert manifest["splits"]["ood_eval"]["num_matches"] == 1

    profiles = HistoricalPlayerProfiler().build_profiles(events)
    report = run_all_gates(events, profiles, splits=splits, expected_final_scores={9000 + i: (1, 0) for i in range(10)})
    assert report.passed, f"Validation failed: {report.errors}"

    # Verify validator catches sent-off player acting & split leakage
    bad_ev = events[0].model_copy(deep=True)
    bad_ev.active_slots_sent_off = 1 << (bad_ev.actor_slot - 1)
    assert not validate_gold_events([bad_ev]).passed
    assert not validate_splits({"train": [9000], "test_eval": [9000]}).passed

    # Export profiles & verify Module B PlayerProfileRegistry compatibility if Parquet written
    prof_path = export_profiles_parquet(profiles, tmp_path / "player_profiles_v2")
    assert prof_path.exists()
    if prof_path.suffix == ".parquet":
        from api.v2.tactical.state_engine import PlayerProfileRegistry

        reg = PlayerProfileRegistry.from_parquet(str(prof_path))
        assert reg.get(profiles[0].player_id) is not None


def test_labels_fit_calibrated_xgboost_and_fsm_enum_parity():
    """Verify real XGBoost teacher fitting on mixed outcomes and 1:1 enum parity with Module C FSM."""
    from api.v2.simulator.fsm import (
        ActionType as FsmAction,
        BodyPart as FsmBody,
        EventOutcome as FsmOutcome,
        PassHeight as FsmHeight,
        PossPhase as FsmPhase,
        RestartType as FsmRestart,
    )

    for a in ActionType:
        assert int(a) == int(FsmAction[a.name])
    for p in PossPhase:
        assert int(p) == int(FsmPhase[p.name])
    for r in RestartType:
        assert int(r) == int(FsmRestart[r.name])
    for h in PassHeight:
        assert int(h) == int(FsmHeight[h.name])
    for b in BodyPart:
        assert int(b) == int(FsmBody[b.name])
    for name in ("COMPLETE", "INCOMPLETE", "OUT_TOUCHLINE", "OUT_BYLINE", "OFFSIDE", "GOAL", "SAVED_HELD", "SAVED_PARRY", "BLOCKED", "OFF_TARGET", "POST", "WON", "LOST", "YELLOW_CARD", "SECOND_YELLOW", "RED_CARD", "FOUL_CONCEDED", "PENALTY_WON", "SUB_EXECUTED", "PERIOD_END"):
        assert int(EventOutcome[name]) == int(FsmOutcome[name])

    # Build 36 matches with mixed shot & pass outcomes so all XGBoost models (>=25 on-target shots) fit genuinely
    mixed_events: list[EventGoldV2] = []
    for m_idx in range(36):
        meta, raw_ev, frames = _make_sample_sb_match()
        meta["match_id"] = 7000 + m_idx
        if m_idx % 2 == 1:
            # Turn shot into saved/off-target and pass into incomplete
            raw_ev[5]["pass"]["outcome"] = {"id": 9, "name": "Incomplete"}
            raw_ev[8]["shot"]["outcome"] = {"id": 100, "name": "Saved"} if m_idx % 4 == 1 else {"id": 98, "name": "Off T"}
            raw_ev[8]["location"] = [94.0, 22.0]
            raw_ev[8]["shot"]["end_location"] = [120.0, 40.5, 1.2] if m_idx % 4 == 1 else [120.0, 52.0, 3.2]
        mixed_events.extend(ingest_statsbomb_match(meta, raw_ev, frames_360=frames))

    xt = ExpectedThreatModel().fit(mixed_events, max_iters=30)
    xg_m = PreShotXGModel().fit(mixed_events)
    psxg_m = PostShotXGModel(pre_xg_model=xg_m).fit(mixed_events)
    xp_m = XPassModel().fit(mixed_events)
    vaep_m = VAEPModel().fit(mixed_events)
    assert xg_m._model is not None and psxg_m._model is not None and xp_m._model is not None

    enrich_events_with_labels(mixed_events, xt_model=xt, xg_model=xg_m, psxg_model=psxg_m, xpass_model=xp_m, vaep_model=vaep_m)
    off_target_shots = [e for e in mixed_events if e.action_type == ActionType.SHOT and e.outcome == EventOutcome.OFF_TARGET]
    assert len(off_target_shots) > 0 and all(s.psxg_v2 == 0.0 for s in off_target_shots)


def test_build_v2_lakehouse_end_to_end_local_fallback(tmp_path: Path):
    """Verify scripts/build_v2_lakehouse.py runs end-to-end and produces all 7 models_v2 artifacts."""
    import json
    import torch
    from scripts.build_v2_lakehouse import build_v2_lakehouse

    out_dir = tmp_path / "models_v2"
    manifest = build_v2_lakehouse(out_dir=out_dir, max_matches=3)
    assert manifest["validation_passed"] is True, f"Gates failed: {manifest.get('validation_errors')}"
    assert manifest["match_count"] >= 3
    assert manifest["event_count"] > 100
    assert manifest["frames_360_count"] > 0

    expected_files = [
        "events_gold_v2.parquet",
        "player_id_crosswalk.parquet",
        "player_profiles_v2.parquet",
        "tactical_manifold_v2.pkl",
        "xt_grid_24x16.npy",
        "packed_shards_v2.pt",
        "lakehouse_manifest.json",
    ]
    for fname in expected_files:
        assert (out_dir / fname).exists(), f"Missing artifact: {fname}"

    bundle = torch.load(out_dir / "packed_shards_v2.pt", map_location="cpu", weights_only=False)
    assert bundle["discrete_tokens"].ndim == 2 and bundle["discrete_tokens"].shape[1] == 14
    assert bundle["continuous_features"].ndim == 2 and bundle["continuous_features"].shape[1] == 10
    assert bundle["spatial_360_features"].ndim == 3 and bundle["spatial_360_features"].shape[1:] == (22, 18)
    assert bundle["spatial_360_targets"].shape == (bundle["spatial_360_features"].shape[0], 22, 2)
    assert len(bundle["event_sequences"]) >= 3
    loaded_manifest = json.loads((out_dir / "lakehouse_manifest.json").read_text(encoding="utf-8"))
    assert loaded_manifest["validation_passed"] is True

