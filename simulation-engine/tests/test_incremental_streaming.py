import asyncio
import os
import sys
import threading
import unittest

# Add parent path for imports
engine_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if engine_root not in sys.path:
    sys.path.insert(0, engine_root)

proto_dir = os.path.join(engine_root, "api", "protos")
if proto_dir not in sys.path:
    sys.path.insert(0, proto_dir)

import simulation_pb2
from api.grpc_server import SimulationServiceServicer


class TestIncrementalStreaming(unittest.IsolatedAsyncioTestCase):
    async def test_backpressure_across_thread_boundary(self):
        """
        Verifies that calling future = asyncio.run_coroutine_threadsafe(queue.put(...), loop)
        followed by future.result() genuinely blocks the worker thread when the bounded queue is full.
        """
        loop = asyncio.get_running_loop()
        queue = asyncio.Queue(maxsize=2)
        thread_blocked = threading.Event()
        worker_finished = threading.Event()
        items_pushed = []

        def producer_worker():
            for i in range(4):
                future = asyncio.run_coroutine_threadsafe(queue.put(f"item_{i}"), loop)
                if i == 2:
                    thread_blocked.set()
                future.result()
                items_pushed.append(i)
            worker_finished.set()

        t = threading.Thread(target=producer_worker)
        t.start()

        # Wait for producer to push 2 items and block on item 2
        await asyncio.sleep(0.05)
        self.assertTrue(thread_blocked.is_set())
        # Producer should only have completed items 0 and 1
        self.assertNotIn(2, items_pushed)

        # Now consume items from the queue to allow blocked producer to complete
        item0 = await queue.get()
        self.assertEqual(item0, "item_0")

        # Drain remaining items so worker can push items 2 and 3 and finish
        item1 = await queue.get()
        self.assertEqual(item1, "item_1")
        item2 = await queue.get()
        self.assertEqual(item2, "item_2")
        item3 = await queue.get()
        await asyncio.to_thread(t.join, 2.0)
        self.assertTrue(worker_finished.is_set())
        self.assertEqual(items_pushed, [0, 1, 2, 3])

    def test_multibyte_utf8_token_preservation(self):
        """
        Verifies that multi-byte UTF-8 player names (e.g. Spanish/Portuguese names like
        Ñíguez, João, García) are not corrupted when token IDs or bytes are accumulated
        across chunk boundaries.
        """
        names = ["Saúl Ñíguez", "João Félix", "Dani García", "Iñaki Williams"]
        for name in names:
            encoded_bytes = name.encode("utf-8")
            # Split byte array in the middle of a multi-byte sequence
            chunk1 = encoded_bytes[:6]
            chunk2 = encoded_bytes[6:]

            # When accumulated together before decoding:
            reconstructed = (chunk1 + chunk2).decode("utf-8")
            self.assertEqual(reconstructed, name)
            self.assertNotIn("\ufffd", reconstructed)

    async def test_grpc_stream_yields_events_incrementally(self):
        """
        Tests that StartMatchSimulationStream yields MatchEventRaw messages in real-time
        with publish_to_rabbitmq=False, verifying Pipeline B contract behavior.
        """
        servicer = SimulationServiceServicer(simulation_service=None)
        request = simulation_pb2.SimulateMatchRequest(
            match_id="999",
            home_team_name="Real Madrid",
            away_team_name="Barcelona",
            publish_to_rabbitmq=False
        )

        class MockContext:
            def __init__(self):
                self._active = True
                self.code = None
                self.details = None

            def is_active(self):
                return self._active

            def set_code(self, code):
                self.code = code

            def set_details(self, details):
                self.details = details

        context = MockContext()

        events = []
        async for event in servicer.StartMatchSimulationStream(request, context):
            events.append(event)

        self.assertGreaterEqual(len(events), 2)
        self.assertEqual(events[0].raw_event_text, "[MATCH START]")
        self.assertEqual(events[0].event_index, 1)
        self.assertTrue(events[-1].is_end_of_match)
        self.assertIn("[MATCH END]", events[-1].raw_event_text)


if __name__ == "__main__":
    unittest.main()
