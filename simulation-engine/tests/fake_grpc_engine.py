"""Local fixture for the opt-in .NET Pipeline B integration smoke test."""
import asyncio
import sys
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "api" / "protos"))
import grpc
import simulation_pb2_grpc
from api.grpc_server import SimulationServiceServicer


class FakeGenerator:
    model_resources = SimpleNamespace(model=object(), xgboost_model=object())

    async def stream_match_lines_direct(self, request, queue, loop, cancel):
        await queue.put(f"00:02 - {request.home_team_name} - shot by Saúl Ñíguez, outcome: Goal")
        await queue.put("[MATCH END]")
        await queue.put(None)


async def main():
    server = grpc.aio.server()
    simulation_pb2_grpc.add_SimulationServiceServicer_to_server(SimulationServiceServicer(FakeGenerator()), server)
    port = server.add_insecure_port("127.0.0.1:0")
    await server.start()
    print(port, flush=True)
    try:
        await asyncio.to_thread(sys.stdin.readline)
    finally:
        await server.stop(0)


if __name__ == "__main__":
    asyncio.run(main())
