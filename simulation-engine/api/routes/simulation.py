"""
Simulation Routes

This module contains all simulation-related API endpoints including
match simulation, status tracking, result retrieval, and enhanced endpoints.
"""

import asyncio
import json
import os
import time
import uuid

from fastapi import APIRouter, BackgroundTasks, Depends, HTTPException, Query, status

# Try to import SSE, use fallback if not available
try:
    from sse_starlette.sse import EventSourceResponse

    SSE_AVAILABLE = True
except ImportError:
    SSE_AVAILABLE = False
    EventSourceResponse = None

from ..models.schemas import (
    MatchRequest, MatchResponse, SimulationStatus, WebhookRequest
)
from ..auth.security import get_auth
from ..services.optimized_simulation_service import get_optimized_simulation_service as get_simulation_service
from ..services.webhook_service import get_webhook_service

router = APIRouter(tags=["Simulation"])


@router.post("/startMatch", response_model=MatchResponse)
async def start_match(
        request: MatchRequest,
        background_tasks: BackgroundTasks,
        simulation_service=Depends(get_simulation_service),
        auth: dict = Depends(get_auth)
):
    """
    Start a match simulation asynchronously
    
    This endpoint immediately returns with a simulation ID, and the processing 
    happens in the background. Use the /simulationStatus/{simulation_id} endpoint 
    to check progress and get results.
    
    Args:
        request: Match simulation request parameters
        background_tasks: FastAPI background tasks
        simulation_service: Simulation service dependency
        auth: Authentication information
        
    Returns:
        MatchResponse: Initial response with simulation ID and status
    """
    start_time = time.time()
    simulation_id = str(uuid.uuid4())

    try:
        # Create initial status entry
        simulation_service.create_simulation_status(
            simulation_id=simulation_id,
            match_id=request.match_id,
            start_time=start_time,
            metadata={
                "home_team": request.home_team_name,
                "away_team": request.away_team_name,
                "home_team_season": request.home_team_season,
                "away_team_season": request.away_team_season,
                "requester": auth.get("username", "unknown")
            }
        )

        # Start the background task for simulation
        background_tasks.add_task(
            simulation_service.process_match_simulation,
            simulation_id,
            request
        )

        # Return immediate response
        return MatchResponse(
            match_id=request.match_id,
            home_team_name=request.home_team_name,
            away_team_name=request.away_team_name,
            home_team_season=request.home_team_season,
            away_team_season=request.away_team_season,
            events_count=0,
            execution_time=time.time() - start_time,
            simulation_id=simulation_id,
            status="pending"
        )

    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to start match simulation: {str(e)}"
        )


@router.get("/simulationStatus/{simulation_id}", response_model=SimulationStatus)
async def get_simulation_status(
        simulation_id: str,
        simulation_service=Depends(get_simulation_service),
        auth: dict = Depends(get_auth)
):
    """
    Get the status of a simulation by its ID
    
    Args:
        simulation_id: Unique simulation identifier
        simulation_service: Simulation service dependency
        auth: Authentication information
        
    Returns:
        SimulationStatus: Current simulation status and progress
        
    Raises:
        HTTPException: If simulation not found
    """
    sim_status = simulation_service.get_simulation_status(simulation_id)

    if not sim_status:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Simulation with ID {simulation_id} not found"
        )

    return sim_status


@router.get("/simulationResult/{simulation_id}")
async def get_simulation_result(
        simulation_id: str,
        simulation_service=Depends(get_simulation_service),
        auth: dict = Depends(get_auth)
):
    """
    Get the raw text result of a completed simulation
    
    Args:
        simulation_id: Unique simulation identifier
        simulation_service: Simulation service dependency
        auth: Authentication information
        
    Returns:
        dict: Simulation result with content and metadata
        
    Raises:
        HTTPException: If simulation not found or not completed
    """
    sim_status = simulation_service.get_simulation_status(simulation_id)

    if not sim_status:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Simulation with ID {simulation_id} not found"
        )

    if sim_status.status != "completed":
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Simulation is not completed yet. Current status: {sim_status.status}"
        )

    if not sim_status.result_path or not os.path.exists(sim_status.result_path):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Simulation result file not found"
        )

    try:
        with open(sim_status.result_path, "r", encoding="utf-8") as f:
            content = f.read()

        return {
            "simulation_id": simulation_id,
            "status": sim_status.status,
            "content": content,
            "events_count": sim_status.events_count,
            "start_time": sim_status.start_time,
            "end_time": sim_status.end_time,
            "execution_time": (sim_status.end_time or time.time()) - sim_status.start_time,
            "metadata": sim_status.metadata
        }

    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error reading simulation result: {str(e)}"
        )


@router.post("/simulations/{simulation_id}/webhook")
async def register_simulation_webhook(
        simulation_id: str,
        webhook_request: WebhookRequest,
        simulation_service=Depends(get_simulation_service),
        webhook_service=Depends(get_webhook_service),
        auth: dict = Depends(get_auth)
):
    """
    Register a webhook to be called when simulation completes
    
    Args:
        simulation_id: Unique simulation identifier
        webhook_request: Webhook configuration
        simulation_service: Simulation service dependency
        webhook_service: Webhook service dependency
        auth: Authentication information
        
    Returns:
        dict: Success message
        
    Raises:
        HTTPException: If simulation not found or already completed
    """

    sim_status = simulation_service.get_simulation_status(simulation_id)

    if not sim_status:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Simulation with ID {simulation_id} not found"
        )

    # Don't allow webhook registration for completed/failed simulations
    if sim_status.status in ["completed", "failed"]:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Cannot register webhook for {sim_status.status} simulation"
        )

    # Validate webhook URL
    if not webhook_service.validate_webhook_url(webhook_request.webhook_url):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Invalid webhook URL format"
        )

    # Add webhook to the simulation
    simulation_service.add_webhook(
        simulation_id,
        webhook_request.webhook_url,
        webhook_request.webhook_secret
    )

    return {"message": "Webhook registered successfully", "simulation_id": simulation_id}


@router.get("/simulationResult/{simulation_id}/wait")
async def get_simulation_result_with_wait(
        simulation_id: str,
        timeout_seconds: int = Query(900, ge=1, le=3000, description="Maximum time to wait in seconds"),
        poll_interval: float = Query(2.0, ge=0.5, le=10.0, description="Polling interval in seconds"),
        simulation_service=Depends(get_simulation_service),
        auth: dict = Depends(get_auth)
):
    """
    Wait for simulation completion with timeout and return result
    
    This endpoint polls the simulation status at regular intervals and returns
    either when the simulation completes or when the timeout is reached.
    
    Args:
        simulation_id: Unique simulation identifier
        timeout_seconds: Maximum time to wait (1-1800 seconds)
        poll_interval: Time between status checks (0.5-10 seconds)
        simulation_service: Simulation service dependency
        auth: Authentication information
        
    Returns:
        dict: Simulation result or timeout information
    """
    start_wait_time = time.time()

    # Check if simulation exists
    sim_status = simulation_service.get_simulation_status(simulation_id)
    if not sim_status:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Simulation with ID {simulation_id} not found"
        )

    # If already completed, return immediately
    if sim_status.status == "completed":
        return await get_simulation_result(simulation_id, simulation_service, auth)

    # If already failed, return error
    if sim_status.status == "failed":
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Simulation failed: {sim_status.error_message}"
        )

    # Wait for completion
    while time.time() - start_wait_time < timeout_seconds:
        await asyncio.sleep(poll_interval)

        sim_status = simulation_service.get_simulation_status(simulation_id)

        if sim_status.status == "completed":
            result = await get_simulation_result(simulation_id, simulation_service, auth)
            result["waited_seconds"] = time.time() - start_wait_time
            return result
        elif sim_status.status == "failed":
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Simulation failed: {sim_status.error_message}"
            )

    # Timeout reached
    return {
        "simulation_id": simulation_id,
        "status": "timeout",
        "message": f"Simulation did not complete within {timeout_seconds} seconds",
        "waited_seconds": time.time() - start_wait_time,
        "current_simulation_status": sim_status.status,
        "current_progress": sim_status.progress
    }


@router.get("/simulationResult/{simulation_id}/stream")
async def stream_simulation_status(
        simulation_id: str,
        simulation_service=Depends(get_simulation_service),
        auth: dict = Depends(get_auth)
):
    """
    Stream simulation status updates using Server-Sent Events (SSE)
    
    This endpoint provides real-time updates about simulation progress
    using Server-Sent Events. The stream automatically closes when
    the simulation completes or fails.
    
    Args:
        simulation_id: Unique simulation identifier
        simulation_service: Simulation service dependency
        auth: Authentication information
        
    Returns:
        EventSourceResponse: SSE stream with status updates
    """
    if not SSE_AVAILABLE:
        raise HTTPException(
            status_code=status.HTTP_501_NOT_IMPLEMENTED,
            detail="Server-Sent Events not available. Install sse-starlette package."
        )

    # Check if simulation exists
    sim_status = simulation_service.get_simulation_status(simulation_id)
    if not sim_status:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Simulation with ID {simulation_id} not found"
        )

    async def event_generator():
        """Generate SSE events for simulation status"""
        last_status = None
        last_progress = None

        try:
            while True:
                sim_status = simulation_service.get_simulation_status(simulation_id)

                if not sim_status:
                    yield {
                        "event": "error",
                        "data": json.dumps({"error": "Simulation not found"})
                    }
                    break

                # Send update if status or progress changed
                if (sim_status.status != last_status or
                        sim_status.progress != last_progress):

                    event_data = {
                        "simulation_id": simulation_id,
                        "status": sim_status.status,
                        "progress": sim_status.progress,
                        "timestamp": time.time()
                    }

                    if sim_status.error_message:
                        event_data["error_message"] = sim_status.error_message

                    if sim_status.events_count:
                        event_data["events_count"] = sim_status.events_count

                    yield {
                        "event": "status_update",
                        "data": json.dumps(event_data)
                    }

                    last_status = sim_status.status
                    last_progress = sim_status.progress

                # End stream if simulation is complete or failed
                if sim_status.status in ["completed", "failed"]:
                    yield {
                        "event": "simulation_finished",
                        "data": json.dumps({
                            "simulation_id": simulation_id,
                            "final_status": sim_status.status,
                            "events_count": sim_status.events_count,
                            "execution_time": (sim_status.end_time or time.time()) - sim_status.start_time
                        })
                    }
                    break

                # Wait before next update
                await asyncio.sleep(1.0)

        except Exception as e:
            yield {
                "event": "error",
                "data": json.dumps({"error": f"Stream error: {str(e)}"})
            }

    return EventSourceResponse(
        event_generator(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "X-Accel-Buffering": "no"  # Disable nginx buffering
        }
    )
