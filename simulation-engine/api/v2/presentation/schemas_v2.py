"""Pydantic v2 contracts for PixelPitchAI v2 Presentation, SSE Streaming & Experimentation APIs."""

from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Literal, Optional

from pydantic import BaseModel, ConfigDict, Field


class FrameKind(str, Enum):
    """Discriminator for MatchStreamFrameV2 wire payloads."""

    LIFECYCLE = "LIFECYCLE"
    EVENT = "EVENT"
    SPATIAL = "SPATIAL"
    TELEMETRY = "TELEMETRY"
    COMMENTARY = "COMMENTARY"


class StructuredEventV2(BaseModel):
    """Canonical structured football event emitted by Hybrid FSM + Transformer + Module D."""

    model_config = ConfigDict(extra="allow")

    event_index: int = Field(..., ge=0)
    possession_phase_id: int = Field(0, ge=0)
    possessing_team_side: int = Field(0, ge=0, le=2)  # 0=none, 1=home, 2=away
    actor_slot: int = Field(0, ge=0, le=22)  # 0=SYSTEM, 1..11=Home, 12..22=Away
    recipient_slot: int = Field(0, ge=0, le=22)
    player_id: int = Field(0, ge=0)
    player_name: str = ""
    team_name: str = ""
    action_type: str
    sub_type: str = ""
    outcome: str = ""
    start_x: float = Field(60.0, ge=-5.0, le=125.0)  # Canonical StatsBomb yards [0, 120]
    start_y: float = Field(40.0, ge=-5.0, le=85.0)   # Canonical StatsBomb yards [0, 80]
    end_x: float = Field(60.0, ge=-5.0, le=125.0)
    end_y: float = Field(40.0, ge=-5.0, le=85.0)
    end_z: float = Field(0.0, ge=0.0, le=10.0)       # Aerial / goal-mouth height in meters
    under_pressure: bool = False
    body_part: str = ""
    pass_height: str = ""
    card: str = ""
    home_score: int = Field(0, ge=0)
    away_score: int = Field(0, ge=0)

    # Module D calibrated per-event analytics
    xg: float = Field(0.0, ge=0.0, le=1.0)
    post_shot_xg: float = Field(0.0, ge=0.0, le=1.0)
    xpass: float = Field(0.0, ge=0.0, le=1.0)
    xt_delta: float = Field(0.0, ge=-1.0, le=1.0)
    vaep_value: float = Field(0.0, ge=-2.0, le=2.0)
    turnover_risk: float = Field(0.0, ge=0.0, le=1.0)


class PlayerSpatialStateV2(BaseModel):
    """Per-player spatial & physiological snapshot in Canonical StatsBomb yards."""

    slot: int = Field(..., ge=1, le=22)
    player_id: int = Field(0, ge=0)
    x: float = Field(..., ge=0.0, le=120.0)
    y: float = Field(..., ge=0.0, le=80.0)
    vx: float = 0.0  # Canonical yards/s
    vy: float = 0.0  # Canonical yards/s
    facing_rad: float = 0.0
    stamina: float = Field(1.0, ge=0.0, le=1.0)
    has_ball: bool = False
    is_sent_off: bool = False


class PitchSpatialFrameV2(BaseModel):
    """5-10 Hz 22-player + 3D ball spatial keyframe for 60 FPS client Hermite interpolation."""

    sim_timestamp_ms: int = Field(..., ge=0)
    ball_x: float = Field(60.0, ge=-5.0, le=125.0)
    ball_y: float = Field(40.0, ge=-5.0, le=85.0)
    ball_z: float = Field(0.0, ge=0.0, le=15.0)
    players: List[PlayerSpatialStateV2] = Field(default_factory=list, max_length=22)
    home_voronoi_control: float = Field(0.5, ge=0.0, le=1.0)

    def to_compact_wire(self) -> Dict[str, Any]:
        """Pack spatial frame into a compact tuple layout (~450B/frame) for SSE."""
        return {
            "t": self.sim_timestamp_ms,
            "b": [round(self.ball_x, 2), round(self.ball_y, 2), round(self.ball_z, 2)],
            "p": [
                [
                    p.slot,
                    round(p.x, 2),
                    round(p.y, 2),
                    round(p.vx, 2),
                    round(p.vy, 2),
                    round(p.stamina, 3),
                    1 if p.has_ball else 0,
                    1 if p.is_sent_off else 0,
                ]
                for p in self.players
            ],
            "hc": round(self.home_voronoi_control, 3),
        }


class TelemetrySnapshotV2(BaseModel):
    """Rolling live match telemetry metrics emitted over SSE."""

    home_xg: float = 0.0
    away_xg: float = 0.0
    home_psxg: float = 0.0
    away_psxg: float = 0.0
    home_xt: float = 0.0
    away_xt: float = 0.0
    home_possession_pct: float = 50.0
    home_ppda: float = 10.0
    away_ppda: float = 10.0
    home_field_tilt_pct: float = 50.0
    momentum_index: float = Field(0.0, ge=-1.0, le=1.0)
    home_shots: int = 0
    away_shots: int = 0
    home_shots_on_target: int = 0
    away_shots_on_target: int = 0
    home_passes: int = 0
    away_passes: int = 0
    home_pass_accuracy: float = 0.0
    away_pass_accuracy: float = 0.0
    home_avg_xpass: float = 0.0
    away_avg_xpass: float = 0.0
    home_high_turnovers: int = 0
    away_high_turnovers: int = 0


class CommentaryCueV2(BaseModel):
    """Supporting play-by-play commentary or tactical briefing cue (read-only downstream)."""

    anchor_event_index: int = Field(..., ge=0)
    tier: Literal["template", "llm_broadcast", "tactical_insight"]
    headline: str
    body: str
    salience_score: float = Field(0.0, ge=0.0, le=1.0)
    language: Literal["en", "es", "fr"] = "en"
    provider_id: str = "template"
    model_version: str = "tier1-template-v2"
    latency_ms: int = Field(0, ge=0)
    verifier_passed: bool = True
    fallback_used: bool = False


class MatchLifecycleV2(BaseModel):
    """Match lifecycle & persistence metadata frame."""

    status: Literal[
        "match_start",
        "first_half_end",
        "second_half_start",
        "substitution",
        "red_card",
        "tactical_override",
        "match_end",
        "truncated_max_events",
        "error",
    ]
    message: str = ""
    home_score: int = 0
    away_score: int = 0
    execution_tier: str = "local_cpu_onnx"
    spatial_track_object_key: Optional[str] = None
    spatial_track_codec: Optional[str] = None


class MatchStreamFrameV2(BaseModel):
    """Unified typed SSE/gRPC frame streamed during single-match simulation."""

    match_id: str
    sequence_no: int = Field(..., ge=1)
    match_clock_seconds: int = Field(0, ge=0)
    period: int = Field(1, ge=0, le=5)
    kind: FrameKind
    event: Optional[StructuredEventV2] = None
    spatial: Optional[PitchSpatialFrameV2] = None
    telemetry: Optional[TelemetrySnapshotV2] = None
    commentary: Optional[CommentaryCueV2] = None
    lifecycle: Optional[MatchLifecycleV2] = None


class LiveTacticalOverrideRequest(BaseModel):
    """Stateless REST request to apply a mid-match tactical or substitution override."""

    team_side: Literal["home", "away"] = "home"
    tactical_instructions_patch: Optional[Dict[str, float]] = None
    formation_base_name: Optional[str] = None
    substitution: Optional[Dict[str, Any]] = None
    playback_speed: Optional[float] = Field(None, ge=0.25, le=20.0)


class LiveTacticalOverrideResponse(BaseModel):
    """Acknowledgment of a live tactical override applied to an active stream."""

    match_id: str
    applied: bool
    sequence_no: int = 0
    message: str
    active_tactics: Dict[str, Any] = Field(default_factory=dict)


class NotificationSseEventV2(BaseModel):
    """User-scoped SSE notification replacing legacy SignalR /Notify."""

    notification_id: str
    user_id: str = "all"
    event_type: Literal[
        "notification",
        "simulation_started",
        "match_completed",
        "experiment_progress",
        "experiment_completed",
    ]
    title: str
    content: str
    match_id: Optional[int] = None
    experiment_id: Optional[str] = None
    timestamp_utc: str = Field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat()
    )
    metadata: Dict[str, Any] = Field(default_factory=dict)


class CommentaryConfigV2(BaseModel):
    """Configuration for Module E's Two-Tier Supporting LLM Narrator."""

    enabled: bool = True
    provider: Literal["template_only", "local_3b", "flash_api", "ab_split"] = "ab_split"
    language: Literal["en", "es", "fr"] = "en"
    salience_threshold: float = Field(0.65, ge=0.0, le=1.0)
    timeout_ms: int = Field(600, ge=50, le=5000)


class SimulateMatchV2Request(BaseModel):
    """Request payload for POST /v2/simulate/stream."""

    model_config = ConfigDict(extra="allow")

    stream_mode: Literal["broadcast", "instant", "compact"] = "broadcast"
    compute_target: Literal["auto", "local_cpu_onnx", "local_cuda", "remote_gpu"] = "auto"
    playback_speed: float = Field(1.0, ge=0.25, le=50.0)
    spatial_hz: int = Field(5, ge=1, le=25)
    max_events: int = Field(4200, ge=10, le=6000)
    commentary: CommentaryConfigV2 = Field(default_factory=CommentaryConfigV2)
    match_config: Dict[str, Any] = Field(
        ...,
        description="MatchConfigurationV2 payload (validated dynamically against api.v2.tactical when present).",
    )


class MonteCarloPatch(BaseModel):
    """Single tactical or lineup patch for an intervention arm."""

    target: str
    value: Any


class MonteCarloArmSpec(BaseModel):
    """Counterfactual intervention arm specification."""

    model_config = ConfigDict(extra="allow")

    arm_id: str
    label: str
    patches: List[MonteCarloPatch] = Field(default_factory=list)
    lineup_swaps: Optional[Dict[int, Dict[str, Any]]] = None
    tactical_overrides: Optional[Dict[str, float]] = None
    role_overrides: Optional[Dict[int, str]] = None
    formation_override: Optional[Dict[str, Any]] = None


class MonteCarloDiagnosticsOptions(BaseModel):
    compute_zone_progression: bool = True
    compute_turnover_heatmaps: bool = True
    compute_role_contributions: bool = True
    include_ood_check: bool = True
    top_representative_seeds: int = Field(3, ge=1, le=10)
    generate_llm_briefing: bool = True
    commentary_provider: Literal["template_only", "local_3b", "flash_api", "ab_split"] = "ab_split"
    language: Literal["en", "es", "fr"] = "en"


class MonteCarloExperimentRequestV2(BaseModel):
    """Request payload for POST /v2/experiments/monte-carlo."""

    model_config = ConfigDict(extra="allow")

    experiment_id: str
    question_type: str = "custom"
    execution_mode: Literal["auto", "sync", "async"] = "auto"
    compute_target: Literal["auto", "local_cpu_onnx", "local_cuda", "remote_gpu"] = "auto"
    n_simulations: int = Field(100, ge=2, le=1000)
    use_paired_crn: bool = True
    base_seed: int = 42000
    sample_player_epistemic_uncertainty: bool = True
    lock_game_state_adaptivity: Optional[float] = Field(None, ge=0.0, le=1.0)
    allow_ood_extrapolation: bool = False
    baseline_config: Dict[str, Any]
    opponent_suite: Optional[List[Dict[str, Any]]] = None
    interventions: List[MonteCarloArmSpec] = Field(..., min_length=1, max_length=8)
    diagnostics_options: MonteCarloDiagnosticsOptions = Field(
        default_factory=MonteCarloDiagnosticsOptions
    )


class CommentaryNarrateRequestV2(BaseModel):
    """Standalone/replay narration endpoint request for historical or persisted matches."""

    match_id: str
    home_team: str
    away_team: str
    home_score: int = 0
    away_score: int = 0
    roster_names: List[str] = Field(default_factory=list)
    events: List[StructuredEventV2] = Field(default_factory=list)
    experiment_summary: Optional[Dict[str, Any]] = None
    commentary: CommentaryConfigV2 = Field(default_factory=CommentaryConfigV2)


class HealthV2Response(BaseModel):
    """Readiness probe for PixelPitchAI v2 components, trained checkpoints, and compute tiers."""

    status: Literal["healthy", "degraded"]
    version: str = "2.0.0"
    model_architecture: str = "PixelPitch-EventTF-112M"
    event_transformer_loaded: bool
    outcome_models_loaded: bool
    player_profiler_loaded: bool
    tactical_engine_loaded: bool
    weights_loaded: bool = False
    models_dir: str = "./models_v2"
    checkpoints_present: Dict[str, bool] = Field(default_factory=dict)
    checkpoint_filenames: List[str] = Field(default_factory=list)
    training_metrics: Dict[str, Any] = Field(default_factory=dict)
    commentary_providers: Dict[str, bool]
    available_compute_tiers: List[str]
    spatial_storage_backend: str
