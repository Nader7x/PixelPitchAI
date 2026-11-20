"""gRPC V2 Servicer implementing footex.simulation.v2.SimulationServiceV2.

Wires:
- api.v2.simulator.rollout_engine (via PresentationServiceV2 bridge)
- api.v2.presentation.commentary (CommentaryEngine)
- api.v2.presentation.compute_router (ComputeBackendRouter)
"""

from __future__ import annotations

import asyncio
import json
import logging
import os
import sys
from collections import deque
from typing import Any, AsyncIterator, Deque, Dict, List, Optional

import grpc

_PROTO_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "protos"))
if _PROTO_DIR not in sys.path:
    sys.path.insert(0, _PROTO_DIR)

import simulation_v2_pb2
import simulation_v2_pb2_grpc

from api.v2.presentation.commentary import CommentaryOrchestrator, compute_salience_score
from api.v2.presentation.compute_router import ComputeBackendRouter
from api.v2.presentation.routes_v2 import (
    ActiveMatchSession,
    PresentationServiceV2,
    get_presentation_service_v2,
)
from api.v2.presentation.schemas_v2 import (
    CommentaryConfigV2,
    FrameKind,
    LiveTacticalOverrideRequest as PydanticLiveTacticalOverrideRequest,
    MatchLifecycleV2,
    MatchStreamFrameV2,
    MonteCarloExperimentRequestV2,
    SimulateMatchV2Request as PydanticSimulateMatchV2Request,
    StructuredEventV2,
)

logger = logging.getLogger("simulation_grpc_v2")

_FRAME_KIND_MAP = {
    FrameKind.LIFECYCLE: simulation_v2_pb2.FrameKind.LIFECYCLE,
    FrameKind.EVENT: simulation_v2_pb2.FrameKind.EVENT,
    FrameKind.SPATIAL: simulation_v2_pb2.FrameKind.SPATIAL,
    FrameKind.TELEMETRY: simulation_v2_pb2.FrameKind.TELEMETRY,
    FrameKind.COMMENTARY: simulation_v2_pb2.FrameKind.COMMENTARY,
}


def _to_proto_frame(frame: MatchStreamFrameV2) -> simulation_v2_pb2.MatchStreamFrameV2:
    """Convert a Pydantic MatchStreamFrameV2 to its Protobuf representation."""
    pb = simulation_v2_pb2.MatchStreamFrameV2(
        match_id=str(frame.match_id),
        sequence_no=max(0, int(frame.sequence_no)),
        match_clock_seconds=max(0, int(frame.match_clock_seconds)),
        period=max(0, int(frame.period)),
        kind=_FRAME_KIND_MAP.get(frame.kind, simulation_v2_pb2.FrameKind.FRAME_KIND_UNSPECIFIED),
    )

    if frame.kind == FrameKind.EVENT and frame.event is not None:
        ev = frame.event
        pb.event.CopyFrom(
            simulation_v2_pb2.StructuredEventV2(
                event_index=max(0, int(ev.event_index)),
                possession_phase_id=max(0, int(ev.possession_phase_id)),
                possessing_team_side=max(0, int(ev.possessing_team_side)),
                actor_slot=max(0, int(ev.actor_slot)),
                recipient_slot=max(0, int(ev.recipient_slot)),
                player_id=max(0, int(ev.player_id)),
                player_name=ev.player_name or "",
                action_type=ev.action_type or "",
                sub_type=ev.sub_type or "",
                outcome=ev.outcome or "",
                start_x=float(ev.start_x),
                start_y=float(ev.start_y),
                end_x=float(ev.end_x),
                end_y=float(ev.end_y),
                end_z=float(ev.end_z),
                under_pressure=bool(ev.under_pressure),
                body_part=ev.body_part or "",
                pass_height=ev.pass_height or "",
                card=ev.card or "",
                home_score=max(0, int(ev.home_score)),
                away_score=max(0, int(ev.away_score)),
                xg=float(ev.xg),
                post_shot_xg=float(ev.post_shot_xg),
                xpass=float(ev.xpass),
                xt_delta=float(ev.xt_delta),
                vaep_value=float(ev.vaep_value),
                turnover_risk=float(ev.turnover_risk),
            )
        )
    elif frame.kind == FrameKind.SPATIAL and frame.spatial is not None:
        sp = frame.spatial
        pb.spatial.CopyFrom(
            simulation_v2_pb2.PitchSpatialFrameV2(
                sim_timestamp_ms=max(0, int(sp.sim_timestamp_ms)),
                ball_x=float(sp.ball_x),
                ball_y=float(sp.ball_y),
                ball_z=float(sp.ball_z),
                players=[
                    simulation_v2_pb2.PlayerSpatialStateV2(
                        slot=max(1, int(p.slot)),
                        player_id=max(0, int(p.player_id)),
                        x=float(p.x),
                        y=float(p.y),
                        vx=float(p.vx),
                        vy=float(p.vy),
                        facing_rad=float(p.facing_rad),
                        stamina=float(p.stamina),
                        has_ball=bool(p.has_ball),
                        is_sent_off=bool(p.is_sent_off),
                    )
                    for p in sp.players
                ],
                home_voronoi_control=float(sp.home_voronoi_control),
            )
        )
    elif frame.kind == FrameKind.TELEMETRY and frame.telemetry is not None:
        tl = frame.telemetry
        pb.telemetry.CopyFrom(
            simulation_v2_pb2.TelemetrySnapshotV2(
                home_xg=float(tl.home_xg),
                away_xg=float(tl.away_xg),
                home_psxg=float(tl.home_psxg),
                away_psxg=float(tl.away_psxg),
                home_xt=float(tl.home_xt),
                away_xt=float(tl.away_xt),
                home_possession_pct=float(tl.home_possession_pct),
                home_ppda=float(tl.home_ppda),
                away_ppda=float(tl.away_ppda),
                home_field_tilt_pct=float(tl.home_field_tilt_pct),
                momentum_index=float(tl.momentum_index),
                home_shots=max(0, int(tl.home_shots)),
                away_shots=max(0, int(tl.away_shots)),
                home_shots_on_target=max(0, int(tl.home_shots_on_target)),
                away_shots_on_target=max(0, int(tl.away_shots_on_target)),
                home_passes=max(0, int(tl.home_passes)),
                away_passes=max(0, int(tl.away_passes)),
                home_pass_accuracy=float(tl.home_pass_accuracy),
                away_pass_accuracy=float(tl.away_pass_accuracy),
                home_avg_xpass=float(tl.home_avg_xpass),
                away_avg_xpass=float(tl.away_avg_xpass),
                home_high_turnovers=max(0, int(tl.home_high_turnovers)),
                away_high_turnovers=max(0, int(tl.away_high_turnovers)),
            )
        )
    elif frame.kind == FrameKind.COMMENTARY and frame.commentary is not None:
        cm = frame.commentary
        pb.commentary.CopyFrom(
            simulation_v2_pb2.CommentaryCueV2(
                anchor_event_index=max(0, int(cm.anchor_event_index)),
                tier=cm.tier,
                headline=cm.headline,
                body=cm.body,
                salience_score=float(cm.salience_score),
                language=cm.language,
                provider_id=cm.provider_id,
                model_version=cm.model_version,
                latency_ms=max(0, int(cm.latency_ms)),
                verifier_passed=bool(cm.verifier_passed),
                fallback_used=bool(cm.fallback_used),
            )
        )
    elif frame.kind == FrameKind.LIFECYCLE and frame.lifecycle is not None:
        lc = frame.lifecycle
        pb.lifecycle.CopyFrom(
            simulation_v2_pb2.MatchLifecycleV2(
                status=lc.status,
                message=lc.message or "",
                home_score=max(0, int(lc.home_score)),
                away_score=max(0, int(lc.away_score)),
                execution_tier=lc.execution_tier or "",
                spatial_track_object_key=lc.spatial_track_object_key or "",
                spatial_track_codec=lc.spatial_track_codec or "",
            )
        )
    return pb


class SimulationServiceV2Servicer(simulation_v2_pb2_grpc.SimulationServiceV2Servicer):
    """Async gRPC servicer for footex.simulation.v2.SimulationServiceV2."""

    def __init__(
        self,
        presentation_service: Optional[PresentationServiceV2] = None,
        compute_router: Optional[ComputeBackendRouter] = None,
        commentary: Optional[CommentaryOrchestrator] = None,
    ) -> None:
        self._presentation = presentation_service
        self._compute_router = compute_router
        self._commentary = commentary

    @property
    def presentation(self) -> PresentationServiceV2:
        if self._presentation is None:
            self._presentation = get_presentation_service_v2()
        return self._presentation

    @property
    def compute_router(self) -> ComputeBackendRouter:
        return self._compute_router or self.presentation.compute_router

    @property
    def commentary(self) -> CommentaryOrchestrator:
        return self._commentary or self.presentation.commentary

    async def StreamMatchV2(
        self,
        request: simulation_v2_pb2.SimulateMatchV2Request,
        context: grpc.aio.ServicerContext,
    ) -> AsyncIterator[simulation_v2_pb2.MatchStreamFrameV2]:
        match_cfg: Dict[str, Any] = {}
        if request.match_config_json:
            try:
                match_cfg = json.loads(request.match_config_json)
            except json.JSONDecodeError as exc:
                await context.abort(grpc.StatusCode.INVALID_ARGUMENT, f"Invalid match_config_json: {exc}")
                return

        if request.match_id and "match_id" not in match_cfg:
            match_cfg["match_id"] = request.match_id

        sim_req = PydanticSimulateMatchV2Request(
            stream_mode=request.stream_mode or "broadcast",
            compute_target=request.compute_target or "auto",
            playback_speed=request.playback_speed if request.playback_speed > 0 else 1.0,
            spatial_hz=request.spatial_hz if request.spatial_hz > 0 else 5,
            max_events=request.max_events if request.max_events > 0 else 4200,
            commentary=CommentaryConfigV2(
                enabled=bool(request.commentary_enabled),
                provider=request.commentary_provider or "ab_split",
                language=request.commentary_language or "en",
                salience_threshold=(
                    request.commentary_salience_threshold
                    if request.commentary_salience_threshold > 0
                    else 0.65
                ),
                timeout_ms=request.commentary_timeout_ms if request.commentary_timeout_ms > 0 else 600,
            ),
            match_config=match_cfg,
        )

        match_id, home_name, away_name, roster_names = self.presentation._extract_match_meta(sim_req.match_config)
        routing = self.compute_router.select_tier("stream", requested=sim_req.compute_target)

        home_raw = sim_req.match_config.get("home_team") or sim_req.match_config.get("home") or {}
        away_raw = sim_req.match_config.get("away_team") or sim_req.match_config.get("away") or {}
        session = ActiveMatchSession(
            match_id=match_id,
            request=sim_req,
            execution_tier=routing.tier,
            active_tactics={
                "home": dict(
                    (home_raw.get("tactical_instructions") or home_raw.get("tactics") or {})
                    if isinstance(home_raw, dict)
                    else {}
                ),
                "away": dict(
                    (away_raw.get("tactical_instructions") or away_raw.get("tactics") or {})
                    if isinstance(away_raw, dict)
                    else {}
                ),
                "playback_speed": sim_req.playback_speed,
            },
        )
        self.presentation._active_matches[match_id] = session

        start_frame = MatchStreamFrameV2(
            match_id=match_id,
            sequence_no=session.next_seq(),
            match_clock_seconds=0,
            period=1,
            kind=FrameKind.LIFECYCLE,
            lifecycle=MatchLifecycleV2(
                status="match_start",
                message=f"{home_name} vs {away_name} live v2 simulation started",
                execution_tier=routing.tier,
            ),
        )
        yield _to_proto_frame(start_frame)

        recent_events: Deque[StructuredEventV2] = deque(maxlen=8)
        last_scores = (0, 0)
        latest_clock = 0
        latest_period = 1
        saw_match_end = False

        try:
            async for frame in self.presentation.bridge.stream_match(sim_req, session, routing):
                if context.cancelled():
                    break

                latest_clock = frame.match_clock_seconds
                latest_period = frame.period

                if frame.kind == FrameKind.EVENT and frame.event is not None:
                    recent_events.append(frame.event)
                    last_scores = (frame.event.home_score, frame.event.away_score)

                if frame.kind == FrameKind.LIFECYCLE and frame.lifecycle is not None:
                    if frame.lifecycle.status == "match_start":
                        continue
                    last_scores = (frame.lifecycle.home_score, frame.lifecycle.away_score)
                    if frame.lifecycle.status == "match_end":
                        saw_match_end = True

                numbered = frame.model_copy(update={"match_id": match_id, "sequence_no": session.next_seq()})
                yield _to_proto_frame(numbered)

                if (
                    frame.kind == FrameKind.EVENT
                    and frame.event is not None
                    and sim_req.commentary.enabled
                    and compute_salience_score(frame.event) >= 0.40
                ):
                    minute = max(1, frame.match_clock_seconds // 60 + 1)
                    cue = await self.commentary.narrate_event(
                        event=frame.event,
                        minute=minute,
                        home_team=home_name,
                        away_team=away_name,
                        recent_chain=list(recent_events),
                        roster_names=roster_names,
                        config=sim_req.commentary,
                        tactical_context=session.active_tactics,
                    )
                    cue_frame = MatchStreamFrameV2(
                        match_id=match_id,
                        sequence_no=session.next_seq(),
                        match_clock_seconds=frame.match_clock_seconds,
                        period=frame.period,
                        kind=FrameKind.COMMENTARY,
                        commentary=cue,
                    )
                    yield _to_proto_frame(cue_frame)

            if not saw_match_end and not context.cancelled():
                end_frame = MatchStreamFrameV2(
                    match_id=match_id,
                    sequence_no=session.next_seq(),
                    match_clock_seconds=latest_clock,
                    period=latest_period,
                    kind=FrameKind.LIFECYCLE,
                    lifecycle=MatchLifecycleV2(
                        status="truncated_max_events",
                        message=f"Stream truncated at max_events: {home_name} {last_scores[0]} - {last_scores[1]} {away_name}",
                        home_score=last_scores[0],
                        away_score=last_scores[1],
                        execution_tier=routing.tier,
                    ),
                )
                yield _to_proto_frame(end_frame)
        finally:
            session.finish()

    async def ApplyLiveTacticalOverride(
        self,
        request: simulation_v2_pb2.LiveTacticalOverrideRequest,
        context: grpc.aio.ServicerContext,
    ) -> simulation_v2_pb2.LiveTacticalOverrideAck:
        session = self.presentation._active_matches.get(request.match_id)
        if session is None:
            return simulation_v2_pb2.LiveTacticalOverrideAck(
                match_id=request.match_id,
                applied=False,
                sequence_no=0,
                message=f"Active match session '{request.match_id}' not found",
                active_tactics_json="{}",
            )

        patch: Optional[Dict[str, float]] = None
        if request.tactical_instructions_patch_json:
            patch = json.loads(request.tactical_instructions_patch_json)

        sub: Optional[Dict[str, Any]] = None
        if request.substitution_json:
            sub = json.loads(request.substitution_json)

        override_req = PydanticLiveTacticalOverrideRequest(
            team_side=request.team_side or "home",
            tactical_instructions_patch=patch,
            formation_base_name=request.formation_base_name or None,
            substitution=sub,
            playback_speed=request.playback_speed if request.HasField("playback_speed") else None,
        )
        updated = await self.presentation.bridge.apply_tactical_override(session, override_req)
        return simulation_v2_pb2.LiveTacticalOverrideAck(
            match_id=request.match_id,
            applied=True,
            sequence_no=session.next_seq(),
            message="Tactical override queued",
            active_tactics_json=json.dumps(updated),
        )

    async def RunMonteCarloExperiment(
        self,
        request: simulation_v2_pb2.MonteCarloExperimentRequest,
        context: grpc.aio.ServicerContext,
    ) -> AsyncIterator[simulation_v2_pb2.MonteCarloProgressFrame]:
        baseline_cfg = json.loads(request.baseline_config_json) if request.baseline_config_json else {}
        interventions = json.loads(request.interventions_json) if request.interventions_json else []
        n_sims = max(1, int(request.n_simulations or 100))

        mc_req = MonteCarloExperimentRequestV2(
            experiment_id=request.experiment_id or "exp_v2",
            n_simulations=n_sims,
            base_seed=int(request.base_seed or 42),
            compute_target=request.compute_target or "auto",
            baseline_config=baseline_cfg,
            interventions=interventions,
            allow_ood_extrapolation=bool(request.allow_ood_extrapolation),
        )
        routing = self.compute_router.select_tier(
            "monte_carlo",
            n_simulations=mc_req.n_simulations,
            requested=mc_req.compute_target,
        )

        ood_report = await self.presentation.bridge.check_ood(mc_req)
        ood_level = str(ood_report.get("warning_level", "ok"))

        progress_queue: asyncio.Queue[Optional[simulation_v2_pb2.MonteCarloProgressFrame]] = asyncio.Queue()

        async def _progress_cb(completed: int, total: int, stage: str = "simulating") -> None:
            await progress_queue.put(
                simulation_v2_pb2.MonteCarloProgressFrame(
                    experiment_id=mc_req.experiment_id,
                    completed=completed,
                    total=total,
                    stage=stage,
                    is_final=False,
                    ood_warning_level=ood_level,
                )
            )

        async def _runner() -> Dict[str, Any]:
            try:
                return await self.presentation.bridge.run_monte_carlo(mc_req, routing, _progress_cb)
            finally:
                await progress_queue.put(None)

        task = asyncio.create_task(_runner())
        while True:
            item = await progress_queue.get()
            if item is None:
                break
            yield item

        try:
            result = await task
            yield simulation_v2_pb2.MonteCarloProgressFrame(
                experiment_id=mc_req.experiment_id,
                completed=mc_req.n_simulations,
                total=mc_req.n_simulations,
                stage="completed",
                is_final=True,
                results_json=json.dumps(result),
                ood_warning_level=ood_level,
            )
        except Exception as exc:
            logger.exception("RunMonteCarloExperiment failed: %s", exc)
            yield simulation_v2_pb2.MonteCarloProgressFrame(
                experiment_id=mc_req.experiment_id,
                completed=0,
                total=mc_req.n_simulations,
                stage="error",
                is_final=True,
                ood_warning_level=ood_level,
                error_message=str(exc),
            )

    async def GetHealthV2(
        self,
        request: simulation_v2_pb2.HealthV2Request,
        context: grpc.aio.ServicerContext,
    ) -> simulation_v2_pb2.HealthV2Response:
        readiness = self.presentation.bridge.probe_readiness()
        tiers = self.compute_router.available_tiers()
        details: Dict[str, Any] = {"readiness": readiness}
        checkpoints_ready = False
        if request.include_checkpoints:
            ckpt = self.presentation.bridge.inspect_checkpoints()
            details["checkpoints"] = ckpt
            checkpoints_ready = bool(ckpt.get("all_present", False))
        return simulation_v2_pb2.HealthV2Response(
            healthy=True,
            version="2.0.0",
            available_tiers=tiers,
            checkpoints_ready=checkpoints_ready,
            details_json=json.dumps(details),
        )
