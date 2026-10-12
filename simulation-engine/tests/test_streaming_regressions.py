"""Regression coverage for wire-level streaming and the production byte buffer."""
import asyncio
import contextlib
import json
import os
import re
import sys
import tempfile
import threading
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT / "api" / "protos"))

import grpc
import torch
import simulation_pb2
import simulation_pb2_grpc
from transformers import GPT2Tokenizer
from transformers.models.gpt2.tokenization_gpt2 import bytes_to_unicode
from api.grpc_server import SimulationServiceServicer
from api.services.optimized_simulation_service import UltraOptimizedSimulationService, CancellationStoppingCriteria
from api.services.streaming import StreamFailure, TokenLineBuffer, put_from_thread


class TokenizerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files = tempfile.TemporaryDirectory()
        path = Path(cls.files.name)
        vocab = {char: byte for byte, char in bytes_to_unicode().items()}
        (path / "vocab.json").write_text(json.dumps(vocab), encoding="utf-8")
        (path / "merges.txt").write_text("#version: 0.2\n", encoding="utf-8")
        cls.tokenizer = GPT2Tokenizer(str(path / "vocab.json"), str(path / "merges.txt"))
        cls.tokenizer.add_tokens(["[SECOND HALF START]", "[MATCH END]"])
        cls.tokenizer.pad_token = cls.tokenizer.eos_token

    @classmethod
    def tearDownClass(cls):
        cls.files.cleanup()

    def test_every_token_boundary_preserves_names_and_control_tokens(self):
        text = "00:01 - pass by P\n00:02 - pass by Saúl Ñíguez, João Félix, García\n[SECOND HALF START]\n[MATCH END]"
        ids = self.tokenizer.encode(text, add_special_tokens=False)
        for cut in range(len(ids) + 1):
            buffer = TokenLineBuffer(self.tokenizer)
            buffer.append(ids[:cut])
            lines = list(buffer.lines())
            buffer.append(ids[cut:])
            lines.extend(buffer.lines(final=True))
            self.assertEqual("\n".join(lines), text)

    def test_incomplete_final_utf8_is_an_error(self):
        buffer = TokenLineBuffer(self.tokenizer)
        buffer.append(self.tokenizer.encode("Ñ", add_special_tokens=False)[:1])
        with self.assertRaises(UnicodeDecodeError):
            list(buffer.lines(final=True))

    def test_model_stopping_criteria_observes_cancellation(self):
        cancel = threading.Event()
        criteria = CancellationStoppingCriteria(cancel)
        self.assertFalse(criteria(None, None))
        cancel.set()
        self.assertTrue(criteria(None, None))


class StreamingTests(unittest.IsolatedAsyncioTestCase):
    async def test_full_queue_put_exits_on_cancellation(self):
        queue = asyncio.Queue(maxsize=1)
        await queue.put("occupied")
        cancel = threading.Event()
        worker = asyncio.create_task(asyncio.to_thread(
            put_from_thread, queue, "blocked", asyncio.get_running_loop(), cancel
        ))
        await asyncio.sleep(0.05)
        self.assertFalse(worker.done())
        cancel.set()
        self.assertFalse(await asyncio.wait_for(worker, 1))
        self.assertEqual(await queue.get(), "occupied")
        await asyncio.sleep(0)
        self.assertTrue(queue.empty())

    @contextlib.asynccontextmanager
    async def rpc(self, service):
        server = grpc.aio.server()
        servicer = SimulationServiceServicer(service)
        simulation_pb2_grpc.add_SimulationServiceServicer_to_server(servicer, server)
        port = server.add_insecure_port("127.0.0.1:0")
        await server.start()
        try:
            async with grpc.aio.insecure_channel(f"127.0.0.1:{port}") as channel:
                yield simulation_pb2_grpc.SimulationServiceStub(channel)
        finally:
            await server.stop(0)

    async def test_real_aio_context_delivers_start_goal_and_end(self):
        class Generator:
            async def stream_match_lines_direct(self, request, queue, loop, cancel):
                await queue.put("00:02 - Home - shot by Saúl Ñíguez, outcome: Goal")
                await queue.put("[MATCH END]")
                await queue.put(None)
        async with self.rpc(Generator()) as stub:
            events = [e async for e in stub.StartMatchSimulationStream(
                simulation_pb2.SimulateMatchRequest(match_id="42"), timeout=2)]
        self.assertEqual([e.event_index for e in events], [1, 2, 3])
        self.assertIn("Ñíguez", events[1].raw_event_text)
        self.assertTrue(events[-1].is_end_of_match)

    async def test_producer_exception_wakes_empty_queue_and_fails_rpc(self):
        class Generator:
            async def stream_match_lines_direct(self, request, queue, loop, cancel):
                raise RuntimeError("generation failed")
        async with self.rpc(Generator()) as stub:
            with self.assertRaises(grpc.aio.AioRpcError) as result:
                async for _ in stub.StartMatchSimulationStream(simulation_pb2.SimulateMatchRequest(match_id="42"), timeout=2):
                    pass
        self.assertEqual(result.exception.code(), grpc.StatusCode.INTERNAL)

    async def test_client_cancellation_signals_generation_worker(self):
        finished = threading.Event()
        class Generator:
            async def stream_match_lines_direct(self, request, queue, loop, cancel):
                await queue.put("00:01 - Home - pass by P")
                await asyncio.to_thread(cancel.wait)
                finished.set()
        async with self.rpc(Generator()) as stub:
            call = stub.StartMatchSimulationStream(simulation_pb2.SimulateMatchRequest(match_id="42"), timeout=2)
            await call.read()
            await call.read()
            call.cancel()
            self.assertTrue(await asyncio.wait_for(asyncio.to_thread(finished.wait, 1), 2))

    async def test_generation_preserves_first_half_goal_and_completes_injected_kickoff(self):
        TokenizerTests.setUpClass()
        try:
            tokenizer = TokenizerTests.tokenizer
            chunks = iter([
                "00:00 - Home_2020 - pass by P, type: Kick Off\n",
                "44:59 - Home_2020 - shot by P, outcome: Goal\n[END OF FIRST HALF]\n[SECOND HALF START]\n",
                " P, type: Kick Off\n[MATCH END]"
            ])
            class Model:
                def generate(self, input_ids, **kwargs):
                    return torch.cat((input_ids, torch.tensor([tokenizer.encode(next(chunks), add_special_tokens=False)])), dim=1)
            stat = SimpleNamespace(generate_features=lambda *a: [], convert_to_text=lambda *a: "",
                                   save_text_file=lambda *a: None, tokenize_and_save=lambda *a: None)
            service = UltraOptimizedSimulationService.__new__(UltraOptimizedSimulationService)
            service.generation_pool = None
            service.model_resources = SimpleNamespace(model=Model(), tokenizer=tokenizer, device="cpu", match_stat=stat,
                optimized_inference_context=contextlib.nullcontext, get_cached_bad_words_ids=lambda *a: [],
                kickoff_pattern=re.compile(r"\d{2}:\d{2}\s*-\s*([^\s]+_\d{4})\s*-\s*pass by .*?Kick Off"))
            request = simulation_pb2.SimulateMatchRequest(match_id="42", home_team_name="Home", away_team_name="Away",
                home_team_season="2019/2020", away_team_season="2019/2020", num_tokens_to_generate=1000, max_new_tokens=1024)
            queue = asyncio.Queue(maxsize=50)
            lines = []
            with patch.object(torch, "load", return_value=torch.tensor([[0]])), patch.object(os, "makedirs"):
                task = asyncio.create_task(service.stream_match_lines_direct(request, queue, asyncio.get_running_loop(), threading.Event()))
                while True:
                    line = await asyncio.wait_for(queue.get(), 3)
                    if line is None:
                        break
                    if isinstance(line, StreamFailure):
                        raise line.error
                    lines.append(line)
                await task
            self.assertIn("44:59 - Home_2020 - shot by P, outcome: Goal", lines)
            self.assertIn("[END OF FIRST HALF]", lines)
            self.assertEqual(lines.count("[SECOND HALF START]"), 1)
            self.assertIn("45:00 - Away_2020  - pass by P, type: Kick Off", lines)
            self.assertEqual(lines[-1], "[MATCH END]")
        finally:
            TokenizerTests.tearDownClass()


if __name__ == "__main__":
    unittest.main()
