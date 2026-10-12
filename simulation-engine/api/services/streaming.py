"""Lossless line assembly and cancellable transport for generation workers."""
import asyncio
import concurrent.futures
from dataclasses import dataclass


@dataclass(frozen=True)
class StreamFailure:
    error: Exception


def put_from_thread(queue, item, loop, cancel_event):
    """Apply bounded backpressure without retaining a worker after cancellation."""
    if cancel_event.is_set():
        return False
    future = asyncio.run_coroutine_threadsafe(queue.put(item), loop)
    while True:
        try:
            future.result(timeout=0.1)
            return True
        except concurrent.futures.TimeoutError:
            if cancel_event.is_set():
                future.cancel()
                return False


class TokenLineBuffer:
    """Keep GPT-2 bytes intact until a complete line can be decoded."""

    def __init__(self, tokenizer):
        self.tokenizer = tokenizer
        self.added_ids = set(tokenizer.get_added_vocab().values())
        self.pending = bytearray()

    def append(self, token_ids):
        for token_id in token_ids:
            token = self.tokenizer.convert_ids_to_tokens(token_id)
            if token_id in self.added_ids:
                self.pending.extend(token.encode("utf-8"))
            else:
                self.pending.extend(self.tokenizer.byte_decoder[c] for c in token)

    def lines(self, final=False):
        while b"\n" in self.pending:
            line, _, remainder = self.pending.partition(b"\n")
            self.pending = bytearray(remainder)
            yield line.decode("utf-8", errors="strict").strip()
        if final and self.pending:
            line = self.pending.decode("utf-8", errors="strict").strip()
            self.pending.clear()
            yield line
