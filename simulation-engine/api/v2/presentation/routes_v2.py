"""FastAPI v2 presentation routes: SSE match streaming, live tactical overrides, Monte Carlo, and commentary."""

from __future__ import annotations

import asyncio
from collections import deque
from dataclasses import dataclass, field
import importlib
import inspect
import json
import os
from pathlib import Path
from typing import Any, AsyncIterator, Awaitable, Callable, Deque, Dict, List, Optional, Sequence, Union
import uuid

from fastapi import APIRouter, Depends, Header, HTTPException, Query, status
from fastapi.responses import JSONResponse
from sse_starlette.sse import AppStatus, EventSourceResponse

from ...auth.security import get_auth
from .commentary import CommentaryOrchestrator, compute_salience_score
from .compute_router import ComputeBackendRouter, ComputeRoutingDecision, SpatialTrackStorageService
from .schemas_v2 import (
    CommentaryCueV2,
    CommentaryNarrateRequestV2,
    FrameKind,
    HealthV2Response,
    LiveTacticalOverrideRequest,
    LiveTacticalOverrideResponse,
    MatchLifecycleV2,
    MatchStreamFrameV2,
    MonteCarloExperimentRequestV2,
    NotificationSseEventV2,
    PitchSpatialFrameV2,
    SimulateMatchV2Request,
    StructuredEventV2,
    TelemetrySnapshotV2,
)

SSE_HEADERS = {
    "Cache-Control": "no-cache",
    "X-Accel-Buffering": "no",
    "Connection": "keep-alive",
}

EVENT_NAME_BY_KIND: Dict[FrameKind, str] = {
    FrameKind.LIFECYCLE: "match_lifecycle",
    FrameKind.EVENT: "match_event",
    FrameKind.SPATIAL: "spatial_frame",
    FrameKind.TELEMETRY: "telemetry",
    FrameKind.COMMENTARY: "commentary",
}

PRODUCTION_CHECKPOINT_FILES: tuple[str, ...] = (
    "pixelpitch_event_tf_112m.pt",
    "module_d_suite.pt",
    "spatial_predictor.pt",
    "xt_grid_24x16.npy",
    "player_profiles_v2.parquet",
    "tactical_manifold_v2.pkl",
    "training_metrics.json",
)


@dataclass
class ActiveMatchSession:
    """In-memory state for a streaming v2 match enabling live tactical overrides and Last-Event-ID replay."""

    match_id: str
    request: SimulateMatchV2Request
    execution_tier: str
    active_tactics: Dict[str, Any] = field(default_factory=dict)
    override_queue: asyncio.Queue[Dict[str, Any]] = field(default_factory=asyncio.Queue)
    replay_buffer: Deque[Dict[str, Any]] = field(default_factory=lambda: deque(maxlen=100))
    subscribers: List[asyncio.Queue[Optional[Dict[str, Any]]]] = field(default_factory=list)
    sequence_no: int = 0
    completed: bool = False
    tactical_state_handle: Any = None

    def next_seq(self) -> int:
        self.sequence_no += 1
        return self.sequence_no

    def record_and_publish(self, sse_item: Dict[str, Any]) -> None:
        self.replay_buffer.append(sse_item)
        for q in list(self.subscribers):
            q.put_nowait(sse_item)

    def finish(self) -> None:
        self.completed = True
        for q in list(self.subscribers):
            q.put_nowait(None)


@dataclass
class ExperimentRecord:
    """Tracks async or sync Monte Carlo counterfactual experiment progress and SSE replay history."""

    experiment_id: str
    total_sims: int
    execution_tier: str
    status: str = "queued"
    progress_pct: float = 0.0
    completed_sims: int = 0
    result: Optional[Dict[str, Any]] = None
    error: Optional[str] = None
    replay_buffer: Deque[Dict[str, Any]] = field(default_factory=lambda: deque(maxlen=100))
    subscribers: List[asyncio.Queue[Optional[Dict[str, Any]]]] = field(default_factory=list)
    sequence_no: int = 0
    task: Optional[asyncio.Task[Any]] = None

    def emit(self, event_name: str, payload: Dict[str, Any], close: bool = False) -> None:
        self.sequence_no += 1
        item = {
            "id": str(self.sequence_no),
            "event": event_name,
            "data": json.dumps(payload, separators=(",", ":")),
        }
        self.replay_buffer.append(item)
        for q in list(self.subscribers):
            q.put_nowait(item)
            if close:
                q.put_nowait(None)


def _parse_last_event_id(raw_value: Optional[str]) -> int:
    if not raw_value:
        return 0
    try:
        return max(0, int(str(raw_value).strip()))
    except (ValueError, TypeError):
        return 0


def _try_int_match_id(match_id: str) -> Optional[int]:
    try:
        return int(match_id)
    except (ValueError, TypeError):
        return None


def _sse_response(content: AsyncIterator[Dict[str, Any]], ping: int = 15) -> EventSourceResponse:
    """Create an EventSourceResponse, resetting AppStatus event if bound to an old event loop."""
    ev = getattr(AppStatus, "should_exit_event", None)
    if ev is not None:
        inner = getattr(ev, "_event", None)
        try:
            running = asyncio.get_running_loop()
            if inner is None or getattr(inner, "_loop", None) is not running:
                AppStatus.should_exit_event = None
                AppStatus.should_exit = False
        except RuntimeError:
            pass
    return EventSourceResponse(content, headers=SSE_HEADERS, ping=ping)


def _build_sse_frame(
    frame: MatchStreamFrameV2,
    compact_spatial: bool = False,
) -> Dict[str, Any]:
    event_name = EVENT_NAME_BY_KIND[frame.kind]
    if compact_spatial and frame.kind == FrameKind.SPATIAL and frame.spatial is not None:
        payload = {
            "match_id": frame.match_id,
            "sequence_no": frame.sequence_no,
            "match_clock_seconds": frame.match_clock_seconds,
            "period": frame.period,
            "kind": frame.kind.value,
            "spatial_compact": frame.spatial.to_compact_wire(),
        }
    else:
        payload = frame.model_dump(exclude_none=True, mode="json")
    return {
        "id": str(frame.sequence_no),
        "event": event_name,
        "data": json.dumps(payload, separators=(",", ":")),
    }


class EngineV2Bridge:
    """Thin adapter connecting Module E routes to `api.v2.tactical` and `api.v2.simulator`."""

    def __init__(
        self,
        stream_override: Optional[Callable[..., AsyncIterator[MatchStreamFrameV2]]] = None,
        monte_carlo_override: Optional[
            Callable[[MonteCarloExperimentRequestV2, ComputeRoutingDecision, Callable[[int, int, str], Awaitable[None]]], Awaitable[Dict[str, Any]]]
        ] = None,
        ood_check_override: Optional[Callable[[MonteCarloExperimentRequestV2], Awaitable[Dict[str, Any]]]] = None,
        tactical_override_fn: Optional[Callable[[ActiveMatchSession, LiveTacticalOverrideRequest], Awaitable[Dict[str, Any]]]] = None,
        models_dir: Optional[Union[str, Path]] = None,
    ) -> None:
        self._stream_override = stream_override
        self._monte_carlo_override = monte_carlo_override
        self._ood_check_override = ood_check_override
        self._tactical_override_fn = tactical_override_fn
        self._explicit_models_dir = Path(models_dir) if models_dir is not None else None
        self._tactical_bundle: Optional[Dict[str, Any]] = None
        self._rollout_engine: Any = None
        self._loaded_signature: Optional[tuple[str, float]] = None

    def resolve_models_dir(self) -> Path:
        if self._explicit_models_dir is not None:
            return self._explicit_models_dir
        return Path(os.getenv("MODEL_V2_DIR", "./models_v2"))

    def inspect_checkpoints(self, models_dir: Optional[Union[str, Path]] = None) -> Dict[str, Any]:
        """Inspect and auto-load trained production checkpoints from `MODEL_V2_DIR` (`./models_v2`)."""
        target_dir = Path(models_dir) if models_dir is not None else self.resolve_models_dir()
        present: Dict[str, bool] = {}
        found_files: List[str] = []

        for fname in PRODUCTION_CHECKPOINT_FILES:
            exists = (target_dir / fname).is_file()
            present[fname] = exists
            if exists:
                found_files.append(fname)

        for extra in (
            "events_gold_v2.parquet",
            "player_id_crosswalk.parquet",
            "calibration_temperature.json",
            "pixelpitch_event_tf_int8.onnx",
            "lakehouse_manifest.json",
        ):
            if (target_dir / extra).is_file() and extra not in found_files:
                found_files.append(extra)

        training_metrics: Dict[str, Any] = {}
        metrics_path = target_dir / "training_metrics.json"
        if metrics_path.is_file():
            try:
                training_metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
            except Exception:
                training_metrics = {}
        calib_path = target_dir / "calibration_temperature.json"
        if calib_path.is_file() and "calibration_temperature" not in training_metrics:
            try:
                training_metrics["calibration_temperature"] = json.loads(calib_path.read_text(encoding="utf-8"))
            except Exception:
                pass

        if any(present.values()):
            self._autoload_from_dir(target_dir)

        weights_loaded = bool(
            present.get("pixelpitch_event_tf_112m.pt") and present.get("module_d_suite.pt")
        )
        return {
            "models_dir": str(target_dir),
            "weights_loaded": weights_loaded,
            "checkpoints_present": present,
            "checkpoint_filenames": found_files,
            "training_metrics": training_metrics,
        }

    def _autoload_from_dir(self, target_dir: Path, device: str = "cpu") -> None:
        if not target_dir.exists():
            return
        latest_mtime = max(
            ((target_dir / f).stat().st_mtime for f in PRODUCTION_CHECKPOINT_FILES if (target_dir / f).is_file()),
            default=0.0,
        )
        sig = (str(target_dir.resolve()), latest_mtime)
        if self._loaded_signature == sig:
            return
        try:
            tactical_mod = importlib.import_module("api.v2.tactical")
            loader = getattr(tactical_mod, "load_from_models_dir", None)
            if callable(loader):
                res = loader(target_dir, device=device)
                if isinstance(res, dict):
                    self._tactical_bundle = res
        except Exception:
            pass
        try:
            sim_mod = importlib.import_module("api.v2.simulator")
            rollout_cls = getattr(sim_mod, "HybridRolloutEngine", None)
            from_dir = getattr(rollout_cls, "from_models_dir", None) if rollout_cls is not None else None
            if callable(from_dir):
                self._rollout_engine = from_dir(target_dir, device=device)
        except Exception:
            pass
        self._loaded_signature = sig

    def probe_readiness(self) -> Dict[str, bool]:
        """Inspect whether Modules B/C/D and tactical subpackages are importable/ready."""
        probes = {
            "tactical_engine_loaded": self._has_symbol("api.v2.tactical", ("TacticalStateEngine", "MatchConfigurationV2", "config_schema")),
            "event_transformer_loaded": self._has_symbol("api.v2.simulator", ("RolloutEngine", "event_transformer", "PixelPitchEventTransformer")),
            "outcome_models_loaded": self._has_symbol("api.v2.simulator", ("OutcomeModelsV2", "outcome_models")),
            "player_profiler_loaded": self._has_symbol("api.v2.tactical", ("PlayerProfilerV2", "spatial_solver")),
        }
        return probes

    @staticmethod
    def _has_symbol(module_name: str, names: Sequence[str]) -> bool:
        try:
            mod = importlib.import_module(module_name)
        except ImportError:
            return False
        for name in names:
            if hasattr(mod, name):
                return True
            try:
                importlib.import_module(f"{module_name}.{name}")
                return True
            except ImportError:
                continue
        return False

    async def check_ood(self, request: MonteCarloExperimentRequestV2) -> Dict[str, Any]:
        if self._ood_check_override is not None:
            return await self._ood_check_override(request)
        self._autoload_from_dir(self.resolve_models_dir())
        try:
            tactical_mod = importlib.import_module("api.v2.tactical")
            checker = getattr(tactical_mod, "assess_experiment_ood", None) or getattr(tactical_mod, "evaluate_ood", None)
            if callable(checker):
                reg = self._tactical_bundle.get("registry") if isinstance(self._tactical_bundle, dict) else None
                res = checker(request.baseline_config, [a.model_dump() for a in request.interventions], registry=reg)
                return await res if inspect.isawaitable(res) else res
        except ImportError:
            pass
        return {"in_distribution": True, "warning_level": "ok", "ood_reasons": []}

    async def stream_match(
        self,
        request: SimulateMatchV2Request,
        session: ActiveMatchSession,
        routing: ComputeRoutingDecision,
    ) -> AsyncIterator[MatchStreamFrameV2]:
        if self._stream_override is not None:
            async for frame in self._stream_override(request, session, routing):
                yield frame
            return

        models_dir = self.resolve_models_dir()
        if self._explicit_models_dir is not None:
            os.environ["MODEL_V2_DIR"] = str(models_dir)
        self._autoload_from_dir(models_dir, device=routing.device if routing.device in ("cpu", "cuda") else "cpu")

        try:
            sim_mod = importlib.import_module("api.v2.simulator")
            stream_fn = getattr(sim_mod, "stream_match_v2", None) or getattr(sim_mod, "simulate_match_stream", None)
            if callable(stream_fn):
                async for raw_frame in stream_fn(request=request, session=session, routing=routing):
                    yield _coerce_frame(raw_frame, session.match_id)
                return
        except ImportError:
            pass

        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="v2 Hybrid Simulator stream entrypoint not mounted yet",
        )

    async def apply_tactical_override(
        self,
        session: ActiveMatchSession,
        override: LiveTacticalOverrideRequest,
    ) -> Dict[str, Any]:
        if self._tactical_override_fn is not None:
            return await self._tactical_override_fn(session, override)

        state = dict(session.active_tactics)
        side_key = override.team_side
        side_state = dict(state.get(side_key, {}))
        if override.tactical_instructions_patch:
            side_state.update(override.tactical_instructions_patch)
        if override.formation_base_name:
            side_state["formation_base_name"] = override.formation_base_name
        if override.substitution:
            subs = list(side_state.get("substitutions", []))
            subs.append(override.substitution)
            side_state["substitutions"] = subs
        state[side_key] = side_state
        if override.playback_speed is not None:
            state["playback_speed"] = override.playback_speed

        try:
            tactical_mod = importlib.import_module("api.v2.tactical")
            apply_fn = getattr(tactical_mod, "apply_live_tactical_override", None)
            if callable(apply_fn) and session.tactical_state_handle is not None:
                res = apply_fn(session.tactical_state_handle, override.model_dump(exclude_none=True))
                updated = await res if inspect.isawaitable(res) else res
                if isinstance(updated, dict):
                    state.update(updated)
        except ImportError:
            pass

        session.active_tactics = state
        await session.override_queue.put(override.model_dump(exclude_none=True))
        return state

    async def run_monte_carlo(
        self,
        request: MonteCarloExperimentRequestV2,
        routing: ComputeRoutingDecision,
        progress_cb: Callable[[int, int, str], Awaitable[None]],
    ) -> Dict[str, Any]:
        if self._monte_carlo_override is not None:
            return await self._monte_carlo_override(request, routing, progress_cb)

        models_dir = self.resolve_models_dir()
        if self._explicit_models_dir is not None:
            os.environ["MODEL_V2_DIR"] = str(models_dir)
        self._autoload_from_dir(models_dir, device=routing.device if routing.device in ("cpu", "cuda") else "cpu")

        try:
            sim_mod = importlib.import_module("api.v2.simulator")
            mc_fn = getattr(sim_mod, "run_monte_carlo_experiment", None) or getattr(sim_mod, "execute_monte_carlo", None)
            if callable(mc_fn):
                return await mc_fn(request=request, routing=routing, progress_cb=progress_cb)
        except ImportError:
            pass

        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="v2 Monte Carlo runner entrypoint not mounted yet",
        )


def _coerce_frame(raw: Any, match_id: str) -> MatchStreamFrameV2:
    if isinstance(raw, MatchStreamFrameV2):
        return raw
    if isinstance(raw, dict):
        data = dict(raw)
        data.setdefault("match_id", match_id)
        data.setdefault("sequence_no", 1)
        return MatchStreamFrameV2.model_validate(data)
    raise TypeError(f"Unsupported stream frame type: {type(raw).__name__}")


class PresentationServiceV2:
    """Owns SSE session buffers, async experiment tracking, commentary, compute routing & storage."""

    def __init__(
        self,
        commentary: Optional[CommentaryOrchestrator] = None,
        compute_router: Optional[ComputeBackendRouter] = None,
        storage: Optional[SpatialTrackStorageService] = None,
        bridge: Optional[EngineV2Bridge] = None,
        models_dir: Optional[Union[str, Path]] = None,
    ) -> None:
        self.commentary = commentary or CommentaryOrchestrator()
        self.compute_router = compute_router or ComputeBackendRouter()
        self.storage = storage or SpatialTrackStorageService()
        self.bridge = bridge or EngineV2Bridge(models_dir=models_dir)

        self._active_matches: Dict[str, ActiveMatchSession] = {}
        self._experiments: Dict[str, ExperimentRecord] = {}
        self._notifications: Deque[tuple[int, NotificationSseEventV2]] = deque(maxlen=200)
        self._notification_seq: int = 0
        self._notification_subs: List[tuple[str, asyncio.Queue[NotificationSseEventV2]]] = []

    def emit_notification(self, event: NotificationSseEventV2) -> int:
        """Append a notification to bounded replay buffer and fan out to live SSE subscribers."""
        self._notification_seq += 1
        seq = self._notification_seq
        self._notifications.append((seq, event))
        for sub_user, queue in list(self._notification_subs):
            if sub_user == "all" or event.user_id in ("all", sub_user):
                queue.put_nowait(event)
        return seq

    def get_notifications_since(self, user_id: str, last_seq: int) -> List[tuple[int, NotificationSseEventV2]]:
        return [
            (seq, ev)
            for seq, ev in self._notifications
            if seq > last_seq and (user_id == "all" or ev.user_id in ("all", user_id))
        ]

    @staticmethod
    def _extract_match_meta(match_config: Dict[str, Any]) -> tuple[str, str, str, List[str]]:
        match_id = str(match_config.get("match_id") or uuid.uuid4())
        home_cfg = match_config.get("home_team") or match_config.get("home") or {}
        away_cfg = match_config.get("away_team") or match_config.get("away") or {}
        home_name = str(home_cfg.get("team_name") or home_cfg.get("name") or match_config.get("home_team_name") or "Home")
        away_name = str(away_cfg.get("team_name") or away_cfg.get("name") or match_config.get("away_team_name") or "Away")
        roster_names: List[str] = []
        for team in (home_cfg, away_cfg):
            for p in team.get("lineup", []) if isinstance(team, dict) else []:
                if isinstance(p, dict) and p.get("name"):
                    roster_names.append(str(p["name"]))
        return match_id, home_name, away_name, roster_names

    async def replay_or_subscribe_match(
        self,
        session: ActiveMatchSession,
        last_event_id: int = 0,
        max_events: Optional[int] = None,
    ) -> AsyncIterator[Dict[str, Any]]:
        """Yield missed frames above `last_event_id` and stream live frames until match end."""
        queue: asyncio.Queue[Optional[Dict[str, Any]]] = asyncio.Queue()
        session.subscribers.append(queue)
        emitted = 0
        last_sent = last_event_id
        try:
            for item in list(session.replay_buffer):
                seq = int(item["id"])
                if seq > last_sent:
                    yield item
                    last_sent = seq
                    emitted += 1
                    if max_events is not None and emitted >= max_events:
                        return
            if session.completed:
                return
            while True:
                item = await queue.get()
                if item is None:
                    return
                seq = int(item["id"])
                if seq > last_sent:
                    yield item
                    last_sent = seq
                    emitted += 1
                    if max_events is not None and emitted >= max_events:
                        return
        finally:
            if queue in session.subscribers:
                session.subscribers.remove(queue)

    async def _maybe_persist_spatial(
        self,
        match_id: str,
        frames: List[PitchSpatialFrameV2],
    ) -> tuple[Optional[str], Optional[str]]:
        if not frames:
            return None, None
        artifact = await self.storage.save_track(match_id, frames)
        return artifact.object_key, artifact.codec

    async def _maybe_narrate_stream_event(
        self,
        frame: MatchStreamFrameV2,
        match_id: str,
        home_name: str,
        away_name: str,
        roster_names: List[str],
        recent_events: Sequence[StructuredEventV2],
        session: ActiveMatchSession,
        request: SimulateMatchV2Request,
    ) -> Optional[Dict[str, Any]]:
        if frame.kind != FrameKind.EVENT or frame.event is None or not request.commentary.enabled:
            return None
        if compute_salience_score(frame.event) < 0.40:
            return None
        minute = max(1, frame.match_clock_seconds // 60 + 1)
        cue = await self.commentary.narrate_event(
            event=frame.event,
            minute=minute,
            home_team=home_name,
            away_team=away_name,
            recent_chain=recent_events,
            roster_names=roster_names,
            config=request.commentary,
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
        cue_sse = _build_sse_frame(cue_frame)
        session.record_and_publish(cue_sse)
        return cue_sse

    async def stream_match_sse(
        self,
        request: SimulateMatchV2Request,
        last_event_id: int = 0,
    ) -> AsyncIterator[Dict[str, Any]]:
        match_id, home_name, away_name, roster_names = self._extract_match_meta(request.match_config)

        existing = self._active_matches.get(match_id)
        if existing is not None and last_event_id > 0:
            async for item in self.replay_or_subscribe_match(existing, last_event_id=last_event_id):
                yield item
            return

        routing = self.compute_router.select_tier("stream", requested=request.compute_target)
        home_raw = request.match_config.get("home_team") or request.match_config.get("home") or {}
        away_raw = request.match_config.get("away_team") or request.match_config.get("away") or {}
        session = ActiveMatchSession(
            match_id=match_id,
            request=request,
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
                "playback_speed": request.playback_speed,
            },
        )
        self._active_matches[match_id] = session

        self.emit_notification(
            NotificationSseEventV2(
                notification_id=str(uuid.uuid4()),
                event_type="simulation_started",
                title=f"Simulation started: {home_name} vs {away_name}",
                content=f"Streaming on compute tier {routing.tier}.",
                match_id=_try_int_match_id(match_id),
                metadata={"match_id": match_id, "execution_tier": routing.tier},
            )
        )

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
        start_sse = _build_sse_frame(start_frame)
        session.record_and_publish(start_sse)
        yield start_sse

        compact = request.stream_mode == "compact"
        collected_spatial: List[PitchSpatialFrameV2] = []
        recent_events: Deque[StructuredEventV2] = deque(maxlen=8)
        last_scores = (0, 0)
        latest_clock = 0
        latest_period = 1
        saw_match_end = False

        try:
            async for frame in self.bridge.stream_match(request, session, routing):
                latest_clock = frame.match_clock_seconds
                latest_period = frame.period

                if frame.kind == FrameKind.SPATIAL and frame.spatial is not None:
                    collected_spatial.append(frame.spatial)

                if frame.kind == FrameKind.EVENT and frame.event is not None:
                    recent_events.append(frame.event)
                    last_scores = (frame.event.home_score, frame.event.away_score)

                if frame.kind == FrameKind.LIFECYCLE and frame.lifecycle is not None:
                    if frame.lifecycle.status == "match_start":
                        continue
                    last_scores = (frame.lifecycle.home_score, frame.lifecycle.away_score)
                    if frame.lifecycle.status == "match_end":
                        saw_match_end = True
                        if collected_spatial and not frame.lifecycle.spatial_track_object_key:
                            obj_key, codec = await self._maybe_persist_spatial(match_id, collected_spatial)
                            frame = frame.model_copy(
                                update={
                                    "lifecycle": frame.lifecycle.model_copy(
                                        update={
                                            "spatial_track_object_key": obj_key,
                                            "spatial_track_codec": codec,
                                            "execution_tier": routing.tier,
                                        }
                                    )
                                }
                            )

                numbered = frame.model_copy(update={"match_id": match_id, "sequence_no": session.next_seq()})
                sse_item = _build_sse_frame(numbered, compact_spatial=compact)
                session.record_and_publish(sse_item)
                yield sse_item

                cue_sse = await self._maybe_narrate_stream_event(
                    frame, match_id, home_name, away_name, roster_names, list(recent_events), session, request
                )
                if cue_sse is not None:
                    yield cue_sse

            if not saw_match_end:
                obj_key, codec = await self._maybe_persist_spatial(match_id, collected_spatial)
                clock_min = f"{latest_clock // 60:02d}:{latest_clock % 60:02d}"
                end_frame = MatchStreamFrameV2(
                    match_id=match_id,
                    sequence_no=session.next_seq(),
                    match_clock_seconds=latest_clock,
                    period=latest_period,
                    kind=FrameKind.LIFECYCLE,
                    lifecycle=MatchLifecycleV2(
                        status="truncated_max_events",
                        message=f"Stream truncated at max_events ({clock_min}): {home_name} {last_scores[0]} - {last_scores[1]} {away_name}",
                        home_score=last_scores[0],
                        away_score=last_scores[1],
                        execution_tier=routing.tier,
                        spatial_track_object_key=obj_key,
                        spatial_track_codec=codec,
                    ),
                )
                end_sse = _build_sse_frame(end_frame)
                session.record_and_publish(end_sse)
                yield end_sse

            final_clock_min = f"{latest_clock // 60:02d}:{latest_clock % 60:02d}"
            self.emit_notification(
                NotificationSseEventV2(
                    notification_id=str(uuid.uuid4()),
                    event_type="match_completed",
                    title=(
                        f"Full time: {home_name} {last_scores[0]} - {last_scores[1]} {away_name}"
                        if saw_match_end
                        else f"Stream truncated at {final_clock_min}: {home_name} {last_scores[0]} - {last_scores[1]} {away_name}"
                    ),
                    content=(
                        "Match simulation and analytics stream completed."
                        if saw_match_end
                        else f"Match stream reached max_events at {final_clock_min} (period {latest_period})."
                    ),
                    match_id=_try_int_match_id(match_id),
                    metadata={"match_id": match_id, "score": list(last_scores), "truncated": not saw_match_end},
                )
            )
        finally:
            session.finish()

    async def execute_experiment(
        self,
        record: ExperimentRecord,
        request: MonteCarloExperimentRequestV2,
        routing: ComputeRoutingDecision,
        ood_report: Dict[str, Any],
    ) -> Dict[str, Any]:
        record.status = "running"
        record.emit(
            "experiment_progress",
            {
                "experiment_id": record.experiment_id,
                "status": "running",
                "completed_sims": 0,
                "total_sims": record.total_sims,
                "progress_pct": 0.0,
                "execution_tier": routing.tier,
            },
        )

        async def _on_progress(completed: int, total: int, stage: str = "simulating") -> None:
            record.completed_sims = completed
            record.progress_pct = round(100.0 * completed / max(1, total), 2)
            record.emit(
                "experiment_progress",
                {
                    "experiment_id": record.experiment_id,
                    "status": stage,
                    "completed_sims": completed,
                    "total_sims": total,
                    "progress_pct": record.progress_pct,
                    "execution_tier": routing.tier,
                },
            )

        try:
            raw_result = await self.bridge.run_monte_carlo(request, routing, _on_progress)
            payload = dict(raw_result)
            payload.setdefault("experiment_id", request.experiment_id)
            payload["execution_tier"] = routing.tier
            payload["routing"] = {
                "tier": routing.tier,
                "device": routing.device,
                "remote_worker_url": routing.remote_worker_url,
                "fallback_reason": routing.fallback_reason,
            }
            payload.setdefault("ood_report", ood_report)

            if request.diagnostics_options.generate_llm_briefing:
                cfg_commentary = CommentaryNarrateRequestV2(
                    match_id=request.experiment_id,
                    home_team="Baseline",
                    away_team="Intervention",
                    experiment_summary=payload,
                ).commentary.model_copy(
                    update={
                        "provider": request.diagnostics_options.commentary_provider,
                        "language": request.diagnostics_options.language,
                    }
                )
                cue = await self.commentary.narrate_experiment(payload, cfg_commentary)
                payload["narrative_briefing"] = cue.model_dump(mode="json")

            payload["ab_commentary_telemetry"] = self.commentary.get_ab_telemetry()
            payload["status"] = "completed"
            record.status = "completed"
            record.completed_sims = record.total_sims
            record.progress_pct = 100.0
            record.result = payload
            record.emit("experiment_completed", payload, close=True)

            self.emit_notification(
                NotificationSseEventV2(
                    notification_id=str(uuid.uuid4()),
                    event_type="experiment_completed",
                    title=f"Experiment {request.experiment_id} complete",
                    content=f"Completed {request.n_simulations} paired simulations on {routing.tier}.",
                    experiment_id=request.experiment_id,
                    metadata={"execution_tier": routing.tier},
                )
            )
            return payload
        except Exception as exc:
            record.status = "failed"
            record.error = str(exc)
            record.emit(
                "experiment_error",
                {"experiment_id": record.experiment_id, "status": "failed", "error": str(exc)},
                close=True,
            )
            raise


_presentation_service_v2: Optional[PresentationServiceV2] = None


def get_presentation_service_v2() -> PresentationServiceV2:
    """Return process-wide singleton PresentationServiceV2 (overridable via FastAPI Depends)."""
    global _presentation_service_v2
    if _presentation_service_v2 is None:
        _presentation_service_v2 = PresentationServiceV2()
    return _presentation_service_v2


def set_presentation_service_v2(service: Optional[PresentationServiceV2]) -> None:
    """Replace or reset PresentationServiceV2 singleton (used in tests or custom startup)."""
    global _presentation_service_v2
    _presentation_service_v2 = service


router = APIRouter(prefix="/v2", tags=["Simulation V2"])


@router.get("/health", response_model=HealthV2Response)
async def health_v2(
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
) -> HealthV2Response:
    probes = service.bridge.probe_readiness()
    ckpt_info = service.bridge.inspect_checkpoints()
    all_ready = all(probes.values())
    return HealthV2Response(
        status="healthy" if all_ready else "degraded",
        event_transformer_loaded=probes["event_transformer_loaded"],
        outcome_models_loaded=probes["outcome_models_loaded"],
        player_profiler_loaded=probes["player_profiler_loaded"],
        tactical_engine_loaded=probes["tactical_engine_loaded"],
        weights_loaded=ckpt_info["weights_loaded"],
        models_dir=ckpt_info["models_dir"],
        checkpoints_present=ckpt_info["checkpoints_present"],
        checkpoint_filenames=ckpt_info["checkpoint_filenames"],
        training_metrics=ckpt_info["training_metrics"],
        commentary_providers=service.commentary.provider_readiness(),
        available_compute_tiers=service.compute_router.available_tiers(),
        spatial_storage_backend=service.storage.backend,
    )


@router.post("/simulate/stream")
async def simulate_stream_v2(
    request: SimulateMatchV2Request,
    last_event_id: Optional[str] = Header(default=None, alias="Last-Event-ID"),
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> EventSourceResponse:
    """Stream live structured match events, 5-10 Hz spatial tracks, telemetry & commentary via SSE."""
    del auth
    resume_id = _parse_last_event_id(last_event_id)
    return _sse_response(
        service.stream_match_sse(request, last_event_id=resume_id),
        ping=15,
    )


@router.get("/matches/{match_id}/stream")
async def attach_match_stream_v2(
    match_id: str,
    last_event_id: Optional[str] = Header(default=None, alias="Last-Event-ID"),
    max_events: Optional[int] = Query(default=None, ge=1, le=5000),
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> EventSourceResponse:
    """Attach or resume an active match SSE stream using Last-Event-ID replay buffer."""
    del auth
    session = service._active_matches.get(match_id)
    if session is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Match {match_id} not found")

    resume_seq = _parse_last_event_id(last_event_id)
    return _sse_response(
        service.replay_or_subscribe_match(session, last_event_id=resume_seq, max_events=max_events),
        ping=15,
    )


@router.post("/matches/{match_id}/tactics", response_model=LiveTacticalOverrideResponse)
async def update_live_tactics_v2(
    match_id: str,
    override: LiveTacticalOverrideRequest,
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> LiveTacticalOverrideResponse:
    """Apply a mid-match tactical instruction patch, formation switch, or substitution."""
    del auth
    session = service._active_matches.get(match_id)
    if session is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Active match {match_id} not found")

    updated = await service.bridge.apply_tactical_override(session, override)
    if override.playback_speed is not None:
        session.request = session.request.model_copy(update={"playback_speed": override.playback_speed})

    seq = session.next_seq()
    lifecycle_frame = MatchStreamFrameV2(
        match_id=match_id,
        sequence_no=seq,
        kind=FrameKind.LIFECYCLE,
        lifecycle=MatchLifecycleV2(
            status="tactical_override",
            message=f"Tactical override applied for {override.team_side}",
            execution_tier=session.execution_tier,
        ),
    )
    session.record_and_publish(_build_sse_frame(lifecycle_frame))

    return LiveTacticalOverrideResponse(
        match_id=match_id,
        applied=True,
        sequence_no=seq,
        message=f"Applied tactical override for {override.team_side}",
        active_tactics=updated,
    )


@router.post("/experiments/monte-carlo")
async def run_monte_carlo_v2(
    request: MonteCarloExperimentRequestV2,
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> JSONResponse:
    """Run paired CRN Monte Carlo counterfactual experiment (sync JSON or async 202 + SSE stream)."""
    del auth
    routing = service.compute_router.select_tier(
        "monte_carlo",
        n_simulations=request.n_simulations,
        requested=request.compute_target,
    )

    ood_report: Dict[str, Any] = {"in_distribution": True, "warning_level": "ok", "ood_reasons": []}
    if request.diagnostics_options.include_ood_check:
        ood_report = await service.bridge.check_ood(request)
        in_dist = ood_report.get("in_distribution", ood_report.get("warning_level") != "extrapolation_risk")
        if not in_dist and not request.allow_ood_extrapolation:
            raise HTTPException(
                status_code=getattr(status, "HTTP_422_UNPROCESSABLE_CONTENT", 422),
                detail={
                    "message": "Intervention fails OOD extrapolation gate; set allow_ood_extrapolation=true to override.",
                    "ood_report": ood_report,
                },
            )

    record = ExperimentRecord(
        experiment_id=request.experiment_id,
        total_sims=request.n_simulations * len(request.interventions),
        execution_tier=routing.tier,
    )
    service._experiments[request.experiment_id] = record

    run_async = (request.execution_mode == "async") or (
        request.execution_mode == "auto" and request.n_simulations > 50
    )
    if run_async:
        record.task = asyncio.create_task(
            service.execute_experiment(record, request, routing, ood_report)
        )
        return JSONResponse(
            status_code=status.HTTP_202_ACCEPTED,
            content={
                "experiment_id": request.experiment_id,
                "status": "queued",
                "execution_tier": routing.tier,
                "stream_url": f"/v2/experiments/{request.experiment_id}/stream",
                "status_url": f"/v2/experiments/{request.experiment_id}",
                "ood_report": ood_report,
            },
        )

    result = await service.execute_experiment(record, request, routing, ood_report)
    return JSONResponse(status_code=status.HTTP_200_OK, content=result)


@router.get("/experiments/{experiment_id}")
async def get_experiment_v2(
    experiment_id: str,
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> Dict[str, Any]:
    del auth
    record = service._experiments.get(experiment_id)
    if record is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Experiment {experiment_id} not found")
    return {
        "experiment_id": record.experiment_id,
        "status": record.status,
        "progress_pct": record.progress_pct,
        "completed_sims": record.completed_sims,
        "total_sims": record.total_sims,
        "execution_tier": record.execution_tier,
        "error": record.error,
        "result": record.result,
    }


@router.get("/experiments/{experiment_id}/stream")
async def stream_experiment_v2(
    experiment_id: str,
    last_event_id: Optional[str] = Header(default=None, alias="Last-Event-ID"),
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> EventSourceResponse:
    """Stream Monte Carlo progress & final counterfactual diagnostic payload via SSE."""
    del auth
    record = service._experiments.get(experiment_id)
    if record is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=f"Experiment {experiment_id} not found")

    resume_seq = _parse_last_event_id(last_event_id)
    queue: asyncio.Queue[Optional[Dict[str, Any]]] = asyncio.Queue()
    record.subscribers.append(queue)

    async def _gen() -> AsyncIterator[Dict[str, Any]]:
        last_sent = resume_seq
        try:
            for item in list(record.replay_buffer):
                seq = int(item["id"])
                if seq > last_sent:
                    yield item
                    last_sent = seq
            if record.status in ("completed", "failed"):
                return
            while True:
                item = await queue.get()
                if item is None:
                    return
                seq = int(item["id"])
                if seq > last_sent:
                    yield item
                    last_sent = seq
        finally:
            if queue in record.subscribers:
                record.subscribers.remove(queue)

    return _sse_response(_gen(), ping=15)


@router.get("/notifications/stream")
async def notifications_stream_v2(
    user_id: str = Query(default="all"),
    last_event_id: Optional[str] = Header(default=None, alias="Last-Event-ID"),
    max_events: Optional[int] = Query(default=None, ge=1, le=500),
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> EventSourceResponse:
    """User-scoped SSE notification feed replacing SignalR /Notify."""
    del auth
    resume_seq = _parse_last_event_id(last_event_id)
    queue: asyncio.Queue[NotificationSseEventV2] = asyncio.Queue()
    sub_entry = (user_id, queue)
    service._notification_subs.append(sub_entry)

    async def _gen() -> AsyncIterator[Dict[str, Any]]:
        emitted = 0
        last_sent = resume_seq
        try:
            for seq, ev in service.get_notifications_since(user_id, last_sent):
                yield {
                    "id": str(seq),
                    "event": ev.event_type,
                    "data": ev.model_dump_json(),
                }
                last_sent = seq
                emitted += 1
                if max_events is not None and emitted >= max_events:
                    return

            while True:
                ev = await queue.get()
                # Match sequence assigned in emit_notification if present in deque
                seq = next(
                    (s for s, item in reversed(service._notifications) if item.notification_id == ev.notification_id),
                    last_sent + 1,
                )
                if seq <= last_sent:
                    continue
                yield {
                    "id": str(seq),
                    "event": ev.event_type,
                    "data": ev.model_dump_json(),
                }
                last_sent = seq
                emitted += 1
                if max_events is not None and emitted >= max_events:
                    return
        finally:
            if sub_entry in service._notification_subs:
                service._notification_subs.remove(sub_entry)

    return _sse_response(_gen(), ping=20)


@router.post("/commentary/narrate")
async def narrate_commentary_v2(
    request: CommentaryNarrateRequestV2,
    service: PresentationServiceV2 = Depends(get_presentation_service_v2),
    auth: dict = Depends(get_auth),
) -> Dict[str, Any]:
    """Generate grounded broadcast commentary or tactical experiment briefing on demand."""
    del auth
    if request.experiment_summary is not None:
        cue = await service.commentary.narrate_experiment(request.experiment_summary, request.commentary)
        return {
            "match_id": request.match_id,
            "cues": [cue.model_dump(mode="json")],
            "ab_telemetry": service.commentary.get_ab_telemetry(),
        }

    cues: List[Dict[str, Any]] = []
    history: List[StructuredEventV2] = []
    for ev in request.events:
        history.append(ev)
        minute = max(1, (ev.event_index // 18) + 1)
        cue: CommentaryCueV2 = await service.commentary.narrate_event(
            event=ev,
            minute=minute,
            home_team=request.home_team,
            away_team=request.away_team,
            recent_chain=history[-5:],
            roster_names=request.roster_names,
            config=request.commentary,
        )
        cues.append(cue.model_dump(mode="json"))

    return {
        "match_id": request.match_id,
        "cues": cues,
        "ab_telemetry": service.commentary.get_ab_telemetry(),
    }
