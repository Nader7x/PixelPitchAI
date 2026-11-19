"""PixelPitchAI v2 Hybrid Simulation Engine package.

Subpackages:
- ``api.v2.tactical``: Modules A & B (MatchConfigurationV2, TacticalStateEngine, SpatialSolver, OOD/Diagnostics)
- ``api.v2.simulator``: Modules C & D (FootballFSM, PixelPitchEventTF, Module D Outcome Models, RolloutEngine)
- ``api.v2.presentation``: Module E (SSE/REST V2 API, Supporting LLM Commentary, ComputeBackendRouter, R2/Blob Storage)
"""

from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from fastapi import APIRouter


def get_v2_router() -> "APIRouter":
    """Lazily return the FastAPI v2 presentation router."""
    from .presentation.routes_v2 import router as v2_router

    return v2_router


def __getattr__(name: str):
    if name in ("router", "v2_router"):
        return get_v2_router()
    raise AttributeError(f"module 'api.v2' has no attribute {name!r}")


__all__ = ["get_v2_router"]
