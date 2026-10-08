import asyncio
import logging
import os
import sys
import time
from typing import AsyncIterator

import grpc

# Add protos directory to path for generated modules
proto_dir = os.path.join(os.path.dirname(__file__), "protos")
if proto_dir not in sys.path:
    sys.path.insert(0, proto_dir)

import simulation_pb2
import simulation_pb2_grpc
from api.core.parser import MatchEventProducer

logger = logging.getLogger("simulation_grpc")


class SimulationServiceServicer(simulation_pb2_grpc.SimulationServiceServicer):
    """
    gRPC Servicer implementing SimulationService.
    Supports unary StartMatchSimulation and direct server-streaming StartMatchSimulationStream.
    """

    def __init__(self, simulation_service=None):
        self.simulation_service = simulation_service
        self.producer = MatchEventProducer()
        self._background_tasks = set()

    async def StartMatchSimulation(self, request, context):
        logger.info(f"[gRPC] StartMatchSimulation received for match {request.match_id}")
        sim_id = f"sim_{request.match_id}_{int(time.time())}"

        # If simulation_service is available, trigger simulation in background
        if self.simulation_service:
            task = asyncio.create_task(
                self._run_simulation_background(request, sim_id)
            )
            self._background_tasks.add(task)
            task.add_done_callback(self._background_tasks.discard)

        return simulation_pb2.StartMatchResponse(
            simulation_id=sim_id,
            match_id=request.match_id,
            status="started",
            message="Match simulation started successfully via gRPC"
        )

    async def StartMatchSimulationStream(self, request, context) -> AsyncIterator[simulation_pb2.MatchEventRaw]:
        logger.info(f"[gRPC] StartMatchSimulationStream initiated for match {request.match_id}")
        sim_id = f"sim_{request.match_id}_{int(time.time())}"
        event_index = 0

        # Yield MATCH START event immediately
        event_index += 1
        yield simulation_pb2.MatchEventRaw(
            match_id=request.match_id,
            event_index=event_index,
            raw_event_text="[MATCH START]",
            timestamp_utc=int(time.time() * 1000),
            is_end_of_match=False
        )

        # Run text generation and stream events as they are produced
        try:
            if self.simulation_service:
                # Generate simulation text
                generated_text = await self.simulation_service.generate_match_text_direct(request)
                lines = generated_text.strip().split('\n')
                for line in lines:
                    line = line.strip()
                    if not line:
                        continue

                    event_index += 1
                    is_end = "[MATCH END]" in line

                    # Also publish to RabbitMQ (Pipeline A raw stream) concurrently
                    try:
                        self.producer.publish_raw_event(line, match_id=request.match_id)
                    except Exception as mq_ex:
                        logger.warning(f"RabbitMQ publish warning: {mq_ex}")

                    # Stream to direct gRPC caller (Pipeline B)
                    yield simulation_pb2.MatchEventRaw(
                        match_id=request.match_id,
                        event_index=event_index,
                        raw_event_text=line,
                        timestamp_utc=int(time.time() * 1000),
                        is_end_of_match=is_end
                    )
            else:
                # Fallback mock for testing
                yield simulation_pb2.MatchEventRaw(
                    match_id=request.match_id,
                    event_index=2,
                    raw_event_text=f"00:00 - {request.home_team_name} - pass by Player at (50.0, 30.0), outcome: Complete",
                    timestamp_utc=int(time.time() * 1000),
                    is_end_of_match=False
                )
                yield simulation_pb2.MatchEventRaw(
                    match_id=request.match_id,
                    event_index=3,
                    raw_event_text="[MATCH END]",
                    timestamp_utc=int(time.time() * 1000),
                    is_end_of_match=True
                )

        except Exception as ex:
            logger.error(f"[gRPC] Error in StartMatchSimulationStream: {ex}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(ex))

    async def GetHealth(self, request, context):
        model_loaded = False
        xgboost_loaded = False

        if self.simulation_service and hasattr(self.simulation_service, "model_resources"):
            res = self.simulation_service.model_resources
            model_loaded = getattr(res, "model", None) is not None
            xgboost_loaded = getattr(res, "xgboost_model", None) is not None

        is_ready = model_loaded and xgboost_loaded

        return simulation_pb2.HealthResponse(
            status=is_ready,
            model_loaded=model_loaded,
            xgboost_loaded=xgboost_loaded,
            message="Simulation engine gRPC service is ready" if is_ready else "Models not loaded"
        )

    async def _run_simulation_background(self, request, sim_id: str):
        try:
            logger.info(f"[gRPC Background] Running simulation for match {request.match_id} (sim {sim_id})")
            generated_text = await self.simulation_service.generate_match_text_direct(request)
            lines = generated_text.strip().split('\n')
            for line in lines:
                line = line.strip()
                if line:
                    self.producer.publish_raw_event(line, match_id=request.match_id)
            logger.info(f"[gRPC Background] Completed simulation for match {request.match_id}")
        except Exception as ex:
            logger.error(f"[gRPC Background] Simulation failed: {ex}")


async def start_grpc_server(host: str = "0.0.0.0", port: int = 50051, simulation_service=None) -> grpc.aio.Server:
    server = grpc.aio.server(
        options=[
            ('grpc.max_send_message_length', 16 * 1024 * 1024),
            ('grpc.max_receive_message_length', 16 * 1024 * 1024),
        ]
    )
    servicer = SimulationServiceServicer(simulation_service=simulation_service)
    simulation_pb2_grpc.add_SimulationServiceServicer_to_server(servicer, server)
    listen_addr = f"{host}:{port}"
    bound_port = server.add_insecure_port(listen_addr)
    if bound_port == 0:
        raise RuntimeError(f"Failed to bind gRPC server to {listen_addr}")
    await server.start()
    logger.info(f"🚀 gRPC Server listening at {listen_addr}")
    return server


async def stop_grpc_server(server: grpc.aio.Server, grace: float = 5.0):
    logger.info("Stopping gRPC Server...")
    await server.stop(grace)
    logger.info("gRPC Server stopped.")
