"""Tests for Module E: Presentation v2 schemas, commentary, compute router, storage, and SSE/REST routes."""

from __future__ import annotations

import asyncio
import json
import sys
from pathlib import Path
from typing import Any, AsyncIterator, Dict, List

import httpx
import pytest

SIM_ENGINE_ROOT = Path(__file__).resolve().parent.parent
if str(SIM_ENGINE_ROOT) not in sys.path:
    sys.path.insert(0, str(SIM_ENGINE_ROOT))

from api.main import app  # noqa: E402
from api.v2 import get_v2_router  # noqa: E402
from api.v2.presentation.commentary import (  # noqa: E402
    CommentaryFactVerifier,
    CommentaryOrchestrator,
    FlashApiCommentaryProvider,
    LocalSidecarCommentaryProvider,
    Tier1TemplateNarrator,
    compute_salience_score,
    pitch_zone_name,
)
from api.v2.presentation.compute_router import (  # noqa: E402
    ComputeBackendRouter,
    SpatialTrackStorageService,
)
from api.v2.presentation.routes_v2 import (  # noqa: E402
    EngineV2Bridge,
    PresentationServiceV2,
    get_presentation_service_v2,
)
from api.v2.presentation.schemas_v2 import (  # noqa: E402
    CommentaryConfigV2,
    FrameKind,
    MatchStreamFrameV2,
    NotificationSseEventV2,
    PitchSpatialFrameV2,
    PlayerSpatialStateV2,
    StructuredEventV2,
    TelemetrySnapshotV2,
)


def _parse_sse_events(raw_body: str) -> List[Dict[str, Any]]:
    """Parse SSE text/event-stream payload into structured event dicts."""
    events: List[Dict[str, Any]] = []
    current: Dict[str, Any] = {}
    data_lines: List[str] = []

    for raw_line in raw_body.splitlines():
        line = raw_line.rstrip("\r")
        if not line:
            if data_lines or current:
                raw_data = "\n".join(data_lines)
                try:
                    parsed_data = json.loads(raw_data) if raw_data else None
                except json.JSONDecodeError:
                    parsed_data = raw_data
                events.append(
                    {
                        "id": current.get("id"),
                        "event": current.get("event", "message"),
                        "data": parsed_data,
                    }
                )
                current = {}
                data_lines = []
            continue
        if line.startswith(":"):
            continue
        if ":" in line:
            field, _, value = line.partition(":")
            value = value.lstrip(" ")
            if field == "data":
                data_lines.append(value)
            else:
                current[field] = value

    if data_lines or current:
        raw_data = "\n".join(data_lines)
        events.append(
            {
                "id": current.get("id"),
                "event": current.get("event", "message"),
                "data": json.loads(raw_data) if raw_data else None,
            }
        )
    return events


def _sample_goal_event(event_index: int = 10) -> StructuredEventV2:
    return StructuredEventV2(
        event_index=event_index,
        possession_phase_id=4,
        possessing_team_side=1,
        actor_slot=9,
        player_id=101,
        player_name="Erling Haaland",
        team_name="Manchester City",
        action_type="shot",
        sub_type="open_play",
        outcome="Goal",
        start_x=108.0,
        start_y=40.0,
        end_x=120.0,
        end_y=38.5,
        end_z=1.2,
        home_score=1,
        away_score=0,
        xg=0.42,
        post_shot_xg=0.81,
        xt_delta=0.25,
        vaep_value=0.89,
    )


def _sample_spatial_frame(ts_ms: int = 200) -> PitchSpatialFrameV2:
    players = [
        PlayerSpatialStateV2(
            slot=i,
            player_id=100 + i,
            x=float(10 + i * 4),
            y=float(20 + (i % 5) * 10),
            vx=1.5,
            vy=-0.4,
            stamina=0.92,
            has_ball=(i == 9),
        )
        for i in range(1, 23)
    ]
    return PitchSpatialFrameV2(
        sim_timestamp_ms=ts_ms,
        ball_x=46.0,
        ball_y=40.0,
        ball_z=0.1,
        players=players,
        home_voronoi_control=0.58,
    )


def test_spatial_frame_compact_wire_encoding() -> None:
    frame = _sample_spatial_frame(ts_ms=400)
    wire = frame.to_compact_wire()
    assert wire["t"] == 400
    assert wire["b"] == [46.0, 40.0, 0.1]
    assert len(wire["p"]) == 22
    assert wire["hc"] == 0.58
    # Slot 9 carries ball bit = 1
    assert wire["p"][8][0] == 9
    assert wire["p"][8][6] == 1


def test_pitch_zone_and_salience_scoring() -> None:
    assert pitch_zone_name(108.0, 40.0, language="en") == "the penalty area"
    assert pitch_zone_name(90.0, 40.0, language="en") == "Zone 14"
    assert pitch_zone_name(108.0, 40.0, language="es") == "el área penal"

    goal = _sample_goal_event()
    assert compute_salience_score(goal) == 1.0

    routine_pass = StructuredEventV2(
        event_index=1,
        player_name="Rodri",
        team_name="Manchester City",
        action_type="pass",
        outcome="Complete",
        xpass=0.91,
        xt_delta=0.01,
        vaep_value=0.02,
    )
    assert compute_salience_score(routine_pass) < 0.35


def test_tier1_template_narrator_languages_and_verifier() -> None:
    narrator = Tier1TemplateNarrator()
    goal = _sample_goal_event()
    cue_en = narrator.narrate_event(goal, minute=34, language="en")
    cue_es = narrator.narrate_event(goal, minute=34, language="es")
    cue_fr = narrator.narrate_event(goal, minute=34, language="fr")
    assert "GOAL" in cue_en.headline and "1-0" in cue_en.headline
    assert "GOL" in cue_es.headline
    assert "BUT" in cue_fr.headline
    assert cue_en.latency_ms < 50

    # Fact verifier passes grounded commentary and catches score/outcome/player hallucinations
    ok, reason = CommentaryFactVerifier.verify(
        "Erling Haaland scores for Manchester City to make it 1-0!",
        home_score=1,
        away_score=0,
        trigger_event=goal,
        allowed_names=["Erling Haaland", "Manchester City"],
    )
    assert ok is True and reason is None

    bad_score, r1 = CommentaryFactVerifier.verify("It is now 2-1!", home_score=1, away_score=0)
    assert not bad_score and r1 and r1.startswith("score_mismatch")

    saved_shot = goal.model_copy(update={"outcome": "Saved", "home_score": 0})
    bad_goal, r2 = CommentaryFactVerifier.verify(
        "Goal! Haaland finds the net!",
        home_score=0,
        away_score=0,
        trigger_event=saved_shot,
    )
    assert not bad_goal and r2 and r2.startswith("outcome_mismatch")

    bad_player, r3 = CommentaryFactVerifier.verify(
        "Lionel Messi blasts it wide for Manchester City.",
        home_score=1,
        away_score=0,
        trigger_event=goal,
        allowed_names=["Erling Haaland", "Manchester City"],
    )
    assert not bad_player and r3 == "unknown_entity:Lionel Messi"


def test_commentary_orchestrator_ab_routing_verifier_and_timeout_fallback() -> None:
    async def _local_gen(payload: Dict[str, Any], timeout_ms: int):
        del payload, timeout_ms
        return ("34' Haaland strikes!", "Erling Haaland finishes cleanly from the penalty area to make it 1-0.")

    async def _slow_flash(payload: Dict[str, Any], timeout_ms: int):
        del payload, timeout_ms
        await asyncio.sleep(0.3)
        return ("Too late", "Should time out")

    orchestrator = CommentaryOrchestrator(
        local_provider=LocalSidecarCommentaryProvider(transport_fn=_local_gen),
        flash_provider=FlashApiCommentaryProvider(transport_fn=_slow_flash),
    )

    ev_even = _sample_goal_event(event_index=2)
    ev_odd = _sample_goal_event(event_index=3)

    async def _bad_score_local(payload: Dict[str, Any], timeout_ms: int):
        del payload, timeout_ms
        return ("34' Shock scoreline!", "Score is now 4-3!")

    async def _run() -> None:
        cfg = CommentaryConfigV2(provider="ab_split", salience_threshold=0.65, timeout_ms=50)
        cue_local = await orchestrator.narrate_event(
            ev_even, 34, "Manchester City", "Arsenal", [ev_even], ["Erling Haaland"], cfg
        )
        assert cue_local.provider_id == "local_3b"
        assert cue_local.verifier_passed is True
        assert cue_local.fallback_used is False

        cue_flash_timeout = await orchestrator.narrate_event(
            ev_odd, 35, "Manchester City", "Arsenal", [ev_odd], ["Erling Haaland"], cfg
        )
        assert cue_flash_timeout.provider_id == "flash_api"
        assert cue_flash_timeout.fallback_used is True
        assert cue_flash_timeout.tier == "template"

        bad_orch = CommentaryOrchestrator(
            local_provider=LocalSidecarCommentaryProvider(transport_fn=_bad_score_local),
        )
        rejected_cue = await bad_orch.narrate_event(
            ev_even,
            34,
            "Manchester City",
            "Arsenal",
            [ev_even],
            ["Erling Haaland"],
            CommentaryConfigV2(provider="local_3b", salience_threshold=0.65, timeout_ms=50),
        )
        assert rejected_cue.verifier_passed is False
        assert rejected_cue.fallback_used is True
        assert bad_orch.get_ab_telemetry()["local_3b"]["verifier_rejections"] == 1

    asyncio.run(_run())
    telemetry = orchestrator.get_ab_telemetry()
    assert telemetry["local_3b"]["successes"] == 1
    assert telemetry["flash_api"]["fallbacks"] == 1


def test_compute_backend_router_tiers() -> None:
    router_remote = ComputeBackendRouter(remote_gpu_url="https://gpu.example.com", cuda_available=False, onnx_available=True)
    assert set(router_remote.available_tiers()) == {"local_cpu_onnx", "remote_gpu"}
    d_mc = router_remote.select_tier("monte_carlo", n_simulations=150)
    assert d_mc.tier == "remote_gpu"

    d_fallback = ComputeBackendRouter(remote_gpu_url="", cuda_available=False, onnx_available=True).select_tier(
        "monte_carlo", n_simulations=10, requested="local_cuda"
    )
    assert d_fallback.tier == "local_cpu_onnx"
    assert d_fallback.fallback_reason == "local_cuda_unavailable"


def test_spatial_track_storage_roundtrip(tmp_path: Path) -> None:
    frames = [_sample_spatial_frame(0), _sample_spatial_frame(200), _sample_spatial_frame(400)]

    async def _roundtrip() -> None:
        svc_mem = SpatialTrackStorageService(backend="memory")
        art = await svc_mem.save_track("match-mem-1", frames)
        assert art.frame_count == 3
        assert art.codec in ("zstd", "zlib")
        assert art.compressed_bytes < art.raw_bytes
        loaded = await svc_mem.load_track(art.object_key, codec=art.codec)
        assert len(loaded) == 3
        assert loaded[1]["t"] == 200

        svc_fs = SpatialTrackStorageService(backend="local_fs", local_dir=str(tmp_path / "tracks"))
        art_fs = await svc_fs.save_track("match-fs-1", frames)
        loaded_fs = await svc_fs.load_track(art_fs.object_key, codec=art_fs.codec)
        assert loaded_fs[2]["t"] == 400

    asyncio.run(_roundtrip())

    r2_svc = SpatialTrackStorageService(
        backend="r2",
        r2_endpoint="https://example.r2.cloudflarestorage.com",
        r2_bucket="tracks-bucket",
        r2_access_key="AKIA_TEST",
        r2_secret_key="SECRET_TEST",
    )
    signed_url, signed_headers = r2_svc._s3_v4_headers("PUT", "tracks/v2/m1.zlib", b"abc")
    assert signed_url == "https://example.r2.cloudflarestorage.com/tracks-bucket/tracks/v2/m1.zlib"
    assert signed_headers["Authorization"].startswith("AWS4-HMAC-SHA256 Credential=AKIA_TEST/")


def _build_mock_presentation_service(tmp_path: Path) -> PresentationServiceV2:
    async def _mock_stream(req, session, routing) -> AsyncIterator[MatchStreamFrameV2]:
        del req, routing
        yield MatchStreamFrameV2(
            match_id=session.match_id,
            sequence_no=1,
            match_clock_seconds=45,
            period=1,
            kind=FrameKind.SPATIAL,
            spatial=_sample_spatial_frame(45000),
        )
        yield MatchStreamFrameV2(
            match_id=session.match_id,
            sequence_no=2,
            match_clock_seconds=90,
            period=1,
            kind=FrameKind.EVENT,
            event=_sample_goal_event(event_index=10),
        )
        yield MatchStreamFrameV2(
            match_id=session.match_id,
            sequence_no=3,
            match_clock_seconds=95,
            period=1,
            kind=FrameKind.TELEMETRY,
            telemetry=TelemetrySnapshotV2(home_xg=0.42, away_xg=0.0, home_shots=1, home_shots_on_target=1),
        )

    async def _mock_mc(req, routing, progress_cb) -> Dict[str, Any]:
        del routing
        await progress_cb(req.n_simulations, req.n_simulations, "aggregating")
        return {
            "experiment_id": req.experiment_id,
            "n_simulations": req.n_simulations,
            "arms": [
                {"arm_id": "baseline", "expected_points": 1.62, "ood_guardrail": {"confidence_tier": "GREEN"}},
                {"arm_id": req.interventions[0].arm_id, "expected_points": 1.88, "ood_guardrail": {"confidence_tier": "GREEN"}},
            ],
        }

    async def _mock_ood(req) -> Dict[str, Any]:
        if req.interventions and req.interventions[0].arm_id == "ood_blocked":
            return {"in_distribution": False, "warning_level": "extrapolation_risk", "ood_reasons": ["mahalanobis_d2 > 99th"]}
        return {"in_distribution": True, "warning_level": "ok", "ood_reasons": []}

    bridge = EngineV2Bridge(
        stream_override=_mock_stream,
        monte_carlo_override=_mock_mc,
        ood_check_override=_mock_ood,
    )
    return PresentationServiceV2(
        compute_router=ComputeBackendRouter(remote_gpu_url="", cuda_available=False, onnx_available=True),
        storage=SpatialTrackStorageService(backend="local_fs", local_dir=str(tmp_path / "tracks")),
        bridge=bridge,
    )


def test_app_mounts_v2_routes_and_auth(tmp_path: Path) -> None:
    v2_paths = {r.path for r in app.routes if hasattr(r, "path") and r.path.startswith("/v2")}
    assert {
        "/v2/health",
        "/v2/simulate/stream",
        "/v2/matches/{match_id}/stream",
        "/v2/matches/{match_id}/tactics",
        "/v2/experiments/monte-carlo",
        "/v2/experiments/{experiment_id}",
        "/v2/experiments/{experiment_id}/stream",
        "/v2/notifications/stream",
        "/v2/commentary/narrate",
    }.issubset(v2_paths)
    assert get_v2_router() is not None

    service = _build_mock_presentation_service(tmp_path)
    app.dependency_overrides[get_presentation_service_v2] = lambda: service

    async def _run() -> None:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://testserver") as client:
            health = await client.get("/v2/health")
            assert health.status_code == 200
            body = health.json()
            assert body["model_architecture"] == "PixelPitch-EventTF-112M"
            assert body["spatial_storage_backend"] == "local_fs"

            # Unauthenticated request to protected endpoint must return 401
            unauth = await client.post("/v2/commentary/narrate", json={"match_id": "m1", "home_team": "A", "away_team": "B"})
            assert unauth.status_code == 401

    try:
        asyncio.run(_run())
    finally:
        app.dependency_overrides.clear()


def test_sse_match_streaming_compact_resume_tactics_and_notifications(tmp_path: Path) -> None:
    service = _build_mock_presentation_service(tmp_path)
    app.dependency_overrides[get_presentation_service_v2] = lambda: service
    headers = {"X-API-Key": "football_simulation_test_key"}

    async def _run() -> None:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://testserver") as client:
            payload = {
                "stream_mode": "compact",
                "compute_target": "auto",
                "commentary": {"enabled": True, "provider": "template_only", "salience_threshold": 0.65},
                "match_config": {
                    "match_id": "match-sse-1",
                    "home_team": {"team_name": "Manchester City", "tactics": {"pressing_intensity": 0.7}},
                    "away_team": {"team_name": "Arsenal", "tactics": {"pressing_intensity": 0.6}},
                },
            }
            resp = await client.post("/v2/simulate/stream", json=payload, headers=headers)
            assert resp.status_code == 200
            assert resp.headers.get("x-accel-buffering") == "no"
            events = _parse_sse_events(resp.text)
            kinds = [e["event"] for e in events]
            assert kinds[0] == "match_lifecycle"
            assert "spatial_frame" in kinds
            assert "match_event" in kinds
            assert "commentary" in kinds
            assert "telemetry" in kinds
            assert kinds[-1] == "match_lifecycle"

            spatial_evt = next(e for e in events if e["event"] == "spatial_frame")
            assert "spatial_compact" in spatial_evt["data"]
            assert len(spatial_evt["data"]["spatial_compact"]["p"]) == 22

            end_evt = events[-1]
            assert end_evt["data"]["lifecycle"]["status"] == "truncated_max_events"
            assert end_evt["data"]["lifecycle"]["spatial_track_object_key"] == "tracks/v2/match-sse-1." + (
                end_evt["data"]["lifecycle"]["spatial_track_codec"]
            )

            # Reconnect with Last-Event-ID on GET /v2/matches/{id}/stream
            resume_after = int(events[2]["id"])
            resumed_resp = await client.get(
                "/v2/matches/match-sse-1/stream",
                headers={**headers, "Last-Event-ID": str(resume_after)},
            )
            assert resumed_resp.status_code == 200
            resumed_events = _parse_sse_events(resumed_resp.text)
            assert len(resumed_events) == len(events) - 3
            assert all(int(e["id"]) > resume_after for e in resumed_events)

            # Apply live tactical override on tracked session
            tactics_resp = await client.post(
                "/v2/matches/match-sse-1/tactics",
                json={
                    "team_side": "home",
                    "tactical_instructions_patch": {"pressing_intensity": 0.92},
                    "formation_base_name": "3-2-4-1",
                },
                headers=headers,
            )
            assert tactics_resp.status_code == 200
            tactics_body = tactics_resp.json()
            assert tactics_body["applied"] is True
            assert tactics_body["active_tactics"]["home"]["pressing_intensity"] == 0.92
            assert tactics_body["active_tactics"]["home"]["formation_base_name"] == "3-2-4-1"

            # Notifications SSE stream replays emitted simulation_started + match_completed events
            notif_resp = await client.get("/v2/notifications/stream?max_events=2", headers=headers)
            assert notif_resp.status_code == 200
            notif_events = _parse_sse_events(notif_resp.text)
            assert [e["event"] for e in notif_events] == ["simulation_started", "match_completed"]

    try:
        asyncio.run(_run())
    finally:
        app.dependency_overrides.clear()


def test_monte_carlo_sync_async_ood_gate_and_commentary_endpoint(tmp_path: Path) -> None:
    service = _build_mock_presentation_service(tmp_path)
    app.dependency_overrides[get_presentation_service_v2] = lambda: service
    headers = {"X-API-Key": "football_simulation_test_key"}

    async def _run() -> None:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://testserver") as client:
            # 1. OOD Guardrail blocks extrapolation unless allow_ood_extrapolation=True
            ood_req = {
                "experiment_id": "exp-ood-1",
                "execution_mode": "sync",
                "n_simulations": 10,
                "baseline_config": {"match_id": "base"},
                "interventions": [{"arm_id": "ood_blocked", "label": "Extreme Out-of-Distribution"}],
            }
            blocked = await client.post("/v2/experiments/monte-carlo", json=ood_req, headers=headers)
            assert blocked.status_code == 422

            # 2. Sync execution with Tier-2B narrative briefing
            sync_req = {
                "experiment_id": "exp-sync-1",
                "execution_mode": "sync",
                "n_simulations": 12,
                "baseline_config": {"match_id": "base"},
                "interventions": [{"arm_id": "high_press", "label": "High Press"}],
                "diagnostics_options": {"generate_llm_briefing": True, "commentary_provider": "template_only"},
            }
            sync_resp = await client.post("/v2/experiments/monte-carlo", json=sync_req, headers=headers)
            assert sync_resp.status_code == 200
            sync_data = sync_resp.json()
            assert sync_data["status"] == "completed"
            assert sync_data["narrative_briefing"]["tier"] == "tactical_insight"

            # 3. Async 202 + SSE progress stream
            async_req = {
                "experiment_id": "exp-async-1",
                "execution_mode": "async",
                "n_simulations": 80,
                "baseline_config": {"match_id": "base"},
                "interventions": [{"arm_id": "inverted_fb", "label": "Inverted Fullbacks"}],
                "diagnostics_options": {"generate_llm_briefing": True, "commentary_provider": "template_only"},
            }
            queued = await client.post("/v2/experiments/monte-carlo", json=async_req, headers=headers)
            assert queued.status_code == 202
            assert queued.json()["stream_url"] == "/v2/experiments/exp-async-1/stream"

            sse_exp = await client.get("/v2/experiments/exp-async-1/stream", headers=headers)
            assert sse_exp.status_code == 200
            exp_events = _parse_sse_events(sse_exp.text)
            assert exp_events[-1]["event"] == "experiment_completed"
            assert exp_events[-1]["data"]["status"] == "completed"

            status_resp = await client.get("/v2/experiments/exp-async-1", headers=headers)
            assert status_resp.status_code == 200
            assert status_resp.json()["status"] == "completed"
            missing_resp = await client.get("/v2/experiments/does-not-exist", headers=headers)
            assert missing_resp.status_code == 404

            # 4. On-demand replay narration endpoint (events & experiment diagnostics)
            narrate_resp = await client.post(
                "/v2/commentary/narrate",
                json={
                    "match_id": "hist-1",
                    "home_team": "Manchester City",
                    "away_team": "Arsenal",
                    "home_score": 1,
                    "away_score": 0,
                    "roster_names": ["Erling Haaland"],
                    "events": [_sample_goal_event().model_dump(mode="json")],
                    "commentary": {"enabled": True, "provider": "template_only", "language": "es"},
                },
                headers=headers,
            )
            assert narrate_resp.status_code == 200
            assert "GOL" in narrate_resp.json()["cues"][0]["headline"]

    try:
        asyncio.run(_run())
    finally:
        app.dependency_overrides.clear()


def test_end_to_end_real_v2_engine_routes(tmp_path: Path) -> None:
    """Verify real unmocked api.v2.tactical + api.v2.simulator execution through FastAPI /v2 routes."""
    from api.v2.tactical import MatchConfigurationV2, build_default_team_config

    match_cfg = MatchConfigurationV2(
        match_id=9001,
        home=build_default_team_config("Home FC", base_player_id=1000, team_id=1, coaching_style="high_press"),
        away=build_default_team_config("Away FC", base_player_id=2000, team_id=2, coaching_style="balanced"),
        random_seed=77,
    )
    real_service = PresentationServiceV2(
        storage=SpatialTrackStorageService(backend="local_fs", local_dir=str(tmp_path / "e2e_tracks"))
    )
    app.dependency_overrides[get_presentation_service_v2] = lambda: real_service
    headers = {"X-API-Key": "football_simulation_test_key"}

    async def _run() -> None:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://testserver") as client:
            health = await client.get("/v2/health")
            assert health.status_code == 200
            h = health.json()
            assert h["status"] == "healthy"
            assert h["tactical_engine_loaded"] is True
            assert h["event_transformer_loaded"] is True
            assert h["outcome_models_loaded"] is True
            assert h["player_profiler_loaded"] is True

            # Real /v2/simulate/stream SSE execution
            stream_req = {
                "stream_mode": "compact",
                "max_events": 25,
                "commentary": {"enabled": True, "provider": "template_only"},
                "match_config": match_cfg.model_dump(mode="json"),
            }
            sse_resp = await client.post("/v2/simulate/stream", json=stream_req, headers=headers)
            assert sse_resp.status_code == 200
            events = _parse_sse_events(sse_resp.text)
            assert events[0]["event"] == "match_lifecycle"
            assert events[0]["data"]["lifecycle"]["status"] == "match_start"
            assert any(e["event"] == "match_event" for e in events)
            assert any(e["event"] == "spatial_frame" for e in events)
            assert any(e["event"] == "telemetry" for e in events)
            assert events[-1]["event"] == "match_lifecycle"
            assert events[-1]["data"]["lifecycle"]["status"] == "truncated_max_events"
            assert events[-1]["data"]["lifecycle"]["spatial_track_object_key"] is not None

            # Verify no duplicate match_start frames
            match_starts = [
                e for e in events
                if e["event"] == "match_lifecycle" and e["data"]["lifecycle"]["status"] == "match_start"
            ]
            assert len(match_starts) == 1

            # Real /v2/experiments/monte-carlo paired-CRN + OOD check execution
            mc_req = {
                "experiment_id": "exp-e2e-real-1",
                "execution_mode": "sync",
                "n_simulations": 4,
                "baseline_config": match_cfg.model_dump(mode="json"),
                "interventions": [
                    {
                        "arm_id": "press_plus",
                        "label": "Higher Pressing",
                        "patches": [{"target": "home.tactical_instructions.pressing_intensity", "value": 0.85}],
                    }
                ],
                "diagnostics_options": {"generate_llm_briefing": True, "commentary_provider": "template_only"},
            }
            mc_resp = await client.post("/v2/experiments/monte-carlo", json=mc_req, headers=headers)
            assert mc_resp.status_code == 200
            mc_data = mc_resp.json()
            assert mc_data["status"] == "completed"
            assert mc_data["ood_report"]["in_distribution"] is True
            assert len(mc_data["arms"]) == 2
            assert "delta_vs_baseline" in mc_data["arms"][1]
            assert mc_data["narrative_briefing"]["tier"] == "tactical_insight"

    try:
        asyncio.run(_run())
    finally:
        app.dependency_overrides.clear()


def test_checkpoint_autoload_and_health_metrics(tmp_path: Path) -> None:
    """Verify EngineV2Bridge & /v2/health auto-discover trained checkpoints and calibration metrics from models_v2."""
    models_dir = tmp_path / "models_v2"
    models_dir.mkdir(parents=True, exist_ok=True)
    expected_files = (
        "pixelpitch_event_tf_112m.pt",
        "module_d_suite.pt",
        "spatial_predictor.pt",
        "xt_grid_24x16.npy",
        "player_profiles_v2.parquet",
        "tactical_manifold_v2.pkl",
        "training_metrics.json",
    )
    for fname in expected_files:
        if fname != "training_metrics.json":
            (models_dir / fname).write_bytes(b"checkpoint-placeholder")

    metrics_payload = {
        "model_architecture": "PixelPitch-EventTF-112M",
        "module_d_xg_ece": 0.0084,
        "module_d_xpass_ece": 0.0091,
        "stage2_val_loss": 1.142,
    }
    (models_dir / "training_metrics.json").write_text(json.dumps(metrics_payload), encoding="utf-8")

    service = PresentationServiceV2(
        storage=SpatialTrackStorageService(backend="memory"),
        models_dir=models_dir,
    )
    app.dependency_overrides[get_presentation_service_v2] = lambda: service

    async def _run() -> None:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://testserver") as client:
            resp = await client.get("/v2/health")
            assert resp.status_code == 200
            body = resp.json()
            assert body["weights_loaded"] is True
            assert set(expected_files).issubset(set(body["checkpoint_filenames"]))
            assert all(body["checkpoints_present"][k] is True for k in expected_files)
            assert body["training_metrics"]["module_d_xg_ece"] == 0.0084

    try:
        asyncio.run(_run())
    finally:
        app.dependency_overrides.clear()
