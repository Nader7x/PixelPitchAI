"""
System Routes

This module contains system-level API endpoints including health checks,
API information, and system statistics.
"""

import time
from datetime import datetime
from fastapi import APIRouter, Depends

from ..auth.security import get_auth
from ..models.schemas import HealthCheck, APIInfo
from ..services.optimized_simulation_service import get_optimized_simulation_service as get_simulation_service

def get_resources():
    return get_simulation_service().model_resources

router = APIRouter(tags=["System"])


@router.get("/", response_model=APIInfo)
async def api_info():
    """
    Enhanced API information endpoint
    
    Returns:
        APIInfo: API information including title, version, and description
    """
    resources = get_resources()
    model_loaded, xgboost_loaded = resources.is_healthy()

    # Create features and endpoints for model_info
    features = [
        "Asynchronous match simulation processing",
        "JWT and API Key authentication",
        "Real-time simulation status tracking",
        "Webhook notifications",
        "Server-Sent Events streaming",
        "Comprehensive error handling and logging",
        "Statistics and monitoring endpoints"
    ]

    endpoints = {
        "simulation": [
            "POST /startMatch - Start match simulation",
            "GET /simulationStatus/{id} - Get simulation status",
            "GET /simulationResult/{id} - Get simulation result",
            "POST /simulations/{id}/webhook - Register webhook",
            "GET /simulationResult/{id}/wait - Wait for completion",
            "GET /simulationResult/{id}/stream - Stream status updates"
        ],
        "authentication": [
            "POST /auth/login - Login and get token",
            "GET /auth/me - Get current user info",
            "POST /auth/api-keys - Create API key",
            "GET /auth/api-keys - List API keys",
            "DELETE /auth/api-keys/{key} - Revoke API key"
        ],
        "system": [
            "GET / - API information",
            "GET /health - Health check",
            "GET /stats - System statistics"
        ]
    }

    return APIInfo(
        name="Football Match Simulation API",  # Changed from title to name
        version="2.0.0",
        description="Advanced API for generating football match simulations using fine-tuned GPT-2 and XGBoost models",
        status="online" if model_loaded and xgboost_loaded else "degraded",
        timestamp=datetime.now().isoformat(),
        model_info={
            "features": features,
            "endpoints": endpoints,
            "models": {
                "gpt2_loaded": model_loaded,
                "xgboost_loaded": xgboost_loaded,
                "device": str(resources.device)
            }
        }
    )


@router.get("/health", response_model=HealthCheck)
async def health_check():
    """
    Health check endpoint

    Returns:
        HealthCheck: System health status and model information
    """
    # Get model resources to check their status
    resources = get_resources()
    model_loaded, xgboost_loaded = resources.is_healthy()

    return HealthCheck(
        status="healthy" if (model_loaded and xgboost_loaded) else "degraded",
        timestamp=datetime.now().isoformat(),
        version="2.0.0",
        model_loaded=model_loaded,
        xgboost_loaded=xgboost_loaded
    )


@router.get("/stats")
async def system_stats():
    """
    System statistics endpoint

    Returns:
        dict: Detailed system and model statistics
    """
    # Get basic system info
    import psutil
    import torch

    system_info = {
        "cpu_percent": psutil.cpu_percent(interval=1),
        "memory_percent": psutil.virtual_memory().percent,
        "disk_percent": psutil.disk_usage('/').percent if hasattr(psutil.disk_usage('/'), 'percent') else 0,
        "cuda_available": torch.cuda.is_available(),
        "gpu_count": torch.cuda.device_count() if torch.cuda.is_available() else 0
    }

    if torch.cuda.is_available():
        try:
            gpu_memory = torch.cuda.get_device_properties(0).total_memory / (1024 ** 3)  # GB
            gpu_memory_used = torch.cuda.memory_allocated(0) / (1024 ** 3)  # GB
            system_info.update({
                "gpu_memory_total_gb": round(gpu_memory, 2),
                "gpu_memory_used_gb": round(gpu_memory_used, 2),
                "gpu_memory_percent": round((gpu_memory_used / gpu_memory) * 100, 2)
            })
        except Exception:
            pass

    # Get resources to check model status
    resources = get_resources()
    model_loaded, xgboost_loaded = resources.is_healthy()

    # Return comprehensive stats
    return {
        "status": "healthy" if (model_loaded and xgboost_loaded) else "degraded",
        "timestamp": datetime.now().isoformat(),
        "version": "2.0.0",
        "system_info": system_info,
        "services": {
            "gpt2_model": "healthy" if model_loaded else "offline",
            "xgboost_model": "healthy" if xgboost_loaded else "offline",
            "authentication": "healthy",
            "simulation_service": "healthy"
        }
    }
