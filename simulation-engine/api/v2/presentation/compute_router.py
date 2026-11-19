"""Module E: Three-Tier ComputeBackendRouter & SpatialTrackStorageService (R2 / Blob / Local)."""

from __future__ import annotations

import hashlib
import hmac
import importlib.util
import json
import os
import zlib
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Literal, Optional, Sequence
from urllib.parse import urlparse

import aiohttp
import torch

from .schemas_v2 import PitchSpatialFrameV2

ComputeTier = Literal["local_cpu_onnx", "local_cuda", "remote_gpu"]


@dataclass(frozen=True)
class ComputeRoutingDecision:
    """Resolved execution tier and metadata echoed on API responses."""

    tier: ComputeTier
    device: str
    remote_worker_url: Optional[str] = None
    fallback_reason: Optional[str] = None


class ComputeBackendRouter:
    """Routes live streaming and Monte Carlo experiments across Remote A100/L4, Local CUDA, or CPU ONNX."""

    def __init__(
        self,
        remote_gpu_url: Optional[str] = None,
        cuda_available: Optional[bool] = None,
        onnx_available: Optional[bool] = None,
    ) -> None:
        self.remote_gpu_url = (
            remote_gpu_url if remote_gpu_url is not None else os.getenv("REMOTE_GPU_WORKER_URL", "")
        ).strip() or None
        self._cuda_override = cuda_available
        self._onnx_override = onnx_available

    @property
    def cuda_available(self) -> bool:
        if self._cuda_override is not None:
            return self._cuda_override
        return torch.cuda.is_available()

    @property
    def onnx_available(self) -> bool:
        if self._onnx_override is not None:
            return self._onnx_override
        return importlib.util.find_spec("onnxruntime") is not None

    def available_tiers(self) -> List[str]:
        tiers: List[str] = ["local_cpu_onnx"] if self.onnx_available else ["local_cpu_onnx"]
        if self.cuda_available:
            tiers.append("local_cuda")
        if self.remote_gpu_url:
            tiers.append("remote_gpu")
        return tiers

    def select_tier(
        self,
        workload: Literal["stream", "monte_carlo"],
        n_simulations: int = 1,
        requested: Literal["auto", "local_cpu_onnx", "local_cuda", "remote_gpu"] = "auto",
    ) -> ComputeRoutingDecision:
        if requested != "auto":
            if requested == "remote_gpu":
                if self.remote_gpu_url:
                    return ComputeRoutingDecision(tier="remote_gpu", device="cuda:remote", remote_worker_url=self.remote_gpu_url)
                return self._auto_select(workload, n_simulations, fallback_reason="remote_gpu_unconfigured")
            if requested == "local_cuda":
                if self.cuda_available:
                    return ComputeRoutingDecision(tier="local_cuda", device="cuda")
                return self._auto_select(workload, n_simulations, fallback_reason="local_cuda_unavailable")
            return ComputeRoutingDecision(tier="local_cpu_onnx", device="cpu")

        return self._auto_select(workload, n_simulations)

    def _auto_select(
        self,
        workload: Literal["stream", "monte_carlo"],
        n_simulations: int,
        fallback_reason: Optional[str] = None,
    ) -> ComputeRoutingDecision:
        if workload == "monte_carlo":
            if (n_simulations > 100 or (n_simulations > 25 and not self.cuda_available)) and self.remote_gpu_url:
                return ComputeRoutingDecision(
                    tier="remote_gpu",
                    device="cuda:remote",
                    remote_worker_url=self.remote_gpu_url,
                    fallback_reason=fallback_reason,
                )
            if self.cuda_available:
                return ComputeRoutingDecision(tier="local_cuda", device="cuda", fallback_reason=fallback_reason)
            return ComputeRoutingDecision(tier="local_cpu_onnx", device="cpu", fallback_reason=fallback_reason)

        if self.cuda_available:
            return ComputeRoutingDecision(tier="local_cuda", device="cuda", fallback_reason=fallback_reason)
        return ComputeRoutingDecision(tier="local_cpu_onnx", device="cpu", fallback_reason=fallback_reason)


@dataclass(frozen=True)
class SpatialTrackArtifact:
    """Metadata returned after persisting a compressed 22-player spatial track."""

    object_key: str
    backend: str
    codec: str
    raw_bytes: int
    compressed_bytes: int
    frame_count: int
    sha256: str


class SpatialTrackStorageService:
    """Compresses 5 Hz 22-player spatial trajectories and stores them in R2 / Azure Blob / Local FS."""

    def __init__(
        self,
        backend: Optional[Literal["auto", "r2", "azure_blob", "local_fs", "memory"]] = None,
        local_dir: Optional[str] = None,
        r2_endpoint: Optional[str] = None,
        r2_bucket: Optional[str] = None,
        r2_access_key: Optional[str] = None,
        r2_secret_key: Optional[str] = None,
        azure_sas_container_url: Optional[str] = None,
    ) -> None:
        self.r2_endpoint = (r2_endpoint or os.getenv("R2_ENDPOINT_URL", "")).rstrip("/")
        self.r2_bucket = r2_bucket or os.getenv("R2_BUCKET", "pixelpitch-spatial-tracks")
        self.r2_access_key = r2_access_key or os.getenv("R2_ACCESS_KEY_ID", "")
        self.r2_secret_key = r2_secret_key or os.getenv("R2_SECRET_ACCESS_KEY", "")
        self.azure_sas_container_url = (azure_sas_container_url or os.getenv("AZURE_SPATIAL_SAS_URL", "")).strip()
        self.local_dir = Path(local_dir or os.getenv("SPATIAL_TRACKS_DIR", "./SpatialTracks"))
        self._memory_store: Dict[str, bytes] = {}

        requested = backend or os.getenv("SPATIAL_STORAGE_BACKEND", "auto")
        if requested == "auto":
            if self.r2_endpoint and self.r2_access_key and self.r2_secret_key:
                self.backend = "r2"
            elif self.azure_sas_container_url:
                self.backend = "azure_blob"
            else:
                self.backend = "local_fs"
        else:
            self.backend = requested

        self._zstd = self._load_zstd()

    @staticmethod
    def _load_zstd():
        try:
            import zstandard as zstd  # type: ignore
            return zstd
        except ImportError:
            return None

    def encode_frames(self, frames: Sequence[PitchSpatialFrameV2 | Dict[str, Any]]) -> tuple[bytes, str, int]:
        wire_frames = [
            f.to_compact_wire() if isinstance(f, PitchSpatialFrameV2) else f
            for f in frames
        ]
        raw = json.dumps(wire_frames, separators=(",", ":")).encode("utf-8")
        if self._zstd is not None:
            compressor = self._zstd.ZstdCompressor(level=6)
            return compressor.compress(raw), "zstd", len(raw)
        return zlib.compress(raw, level=6), "zlib", len(raw)

    def decode_frames(self, payload: bytes, codec: str) -> List[Dict[str, Any]]:
        if codec == "zstd":
            if self._zstd is None:
                raise RuntimeError("zstandard package required to decode zstd payload")
            raw = self._zstd.ZstdDecompressor().decompress(payload)
        elif codec == "zlib":
            raw = zlib.decompress(payload)
        else:
            raise ValueError(f"Unsupported spatial track codec: {codec}")
        return json.loads(raw.decode("utf-8"))

    async def save_track(
        self,
        match_id: str,
        frames: Sequence[PitchSpatialFrameV2 | Dict[str, Any]],
    ) -> SpatialTrackArtifact:
        compressed, codec, raw_len = self.encode_frames(frames)
        digest = hashlib.sha256(compressed).hexdigest()
        object_key = f"tracks/v2/{match_id}.{codec}"

        if self.backend == "memory":
            self._memory_store[object_key] = compressed
        elif self.backend == "local_fs":
            target = self.local_dir / f"{match_id}.{codec}"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(compressed)
        elif self.backend == "r2":
            await self._put_r2_s3(object_key, compressed)
        elif self.backend == "azure_blob":
            await self._put_azure_sas(object_key, compressed)
        else:
            raise ValueError(f"Unsupported storage backend: {self.backend}")

        return SpatialTrackArtifact(
            object_key=object_key,
            backend=self.backend,
            codec=codec,
            raw_bytes=raw_len,
            compressed_bytes=len(compressed),
            frame_count=len(frames),
            sha256=digest,
        )

    async def load_track(self, object_key: str, codec: Optional[str] = None) -> List[Dict[str, Any]]:
        resolved_codec = codec or ("zstd" if object_key.endswith(".zstd") else "zlib")
        if self.backend == "memory":
            if object_key not in self._memory_store:
                raise FileNotFoundError(object_key)
            return self.decode_frames(self._memory_store[object_key], resolved_codec)
        if self.backend == "local_fs":
            name = Path(object_key).name
            path = self.local_dir / name
            return self.decode_frames(path.read_bytes(), resolved_codec)
        if self.backend == "r2":
            data = await self._get_r2_s3(object_key)
            return self.decode_frames(data, resolved_codec)
        if self.backend == "azure_blob":
            data = await self._get_azure_sas(object_key)
            return self.decode_frames(data, resolved_codec)
        raise ValueError(f"Unsupported storage backend: {self.backend}")

    def _s3_v4_headers(self, method: str, object_key: str, payload: bytes) -> tuple[str, Dict[str, str]]:
        now = datetime.now(timezone.utc)
        amz_date = now.strftime("%Y%m%dT%H%M%SZ")
        date_stamp = now.strftime("%Y%m%d")
        parsed = urlparse(self.r2_endpoint)
        host = parsed.netloc
        canonical_uri = f"/{self.r2_bucket}/{object_key.lstrip('/')}"
        payload_hash = hashlib.sha256(payload).hexdigest()
        canonical_headers = f"host:{host}\nx-amz-content-sha256:{payload_hash}\nx-amz-date:{amz_date}\n"
        signed_headers = "host;x-amz-content-sha256;x-amz-date"
        canonical_req = f"{method}\n{canonical_uri}\n\n{canonical_headers}\n{signed_headers}\n{payload_hash}"
        scope = f"{date_stamp}/auto/s3/aws4_request"
        str_to_sign = f"AWS4-HMAC-SHA256\n{amz_date}\n{scope}\n{hashlib.sha256(canonical_req.encode()).hexdigest()}"

        def _sign(key: bytes, msg: str) -> bytes:
            return hmac.new(key, msg.encode("utf-8"), hashlib.sha256).digest()

        k_date = _sign(("AWS4" + self.r2_secret_key).encode("utf-8"), date_stamp)
        k_region = _sign(k_date, "auto")
        k_service = _sign(k_region, "s3")
        k_signing = _sign(k_service, "aws4_request")
        sig = hmac.new(k_signing, str_to_sign.encode("utf-8"), hashlib.sha256).hexdigest()
        auth_header = (
            f"AWS4-HMAC-SHA256 Credential={self.r2_access_key}/{scope}, "
            f"SignedHeaders={signed_headers}, Signature={sig}"
        )
        headers = {
            "Host": host,
            "x-amz-date": amz_date,
            "x-amz-content-sha256": payload_hash,
            "Authorization": auth_header,
            "Content-Type": "application/octet-stream",
        }
        return f"{self.r2_endpoint}{canonical_uri}", headers

    async def _put_r2_s3(self, object_key: str, data: bytes) -> None:
        url, headers = self._s3_v4_headers("PUT", object_key, data)
        async with aiohttp.ClientSession() as session:
            async with session.put(url, data=data, headers=headers) as resp:
                resp.raise_for_status()

    async def _get_r2_s3(self, object_key: str) -> bytes:
        url, headers = self._s3_v4_headers("GET", object_key, b"")
        async with aiohttp.ClientSession() as session:
            async with session.get(url, headers=headers) as resp:
                resp.raise_for_status()
                return await resp.read()

    async def _put_azure_sas(self, object_key: str, data: bytes) -> None:
        base, sep, query = self.azure_sas_container_url.partition("?")
        blob_url = f"{base.rstrip('/')}/{object_key.lstrip('/')}{sep}{query}"
        headers = {"x-ms-blob-type": "BlockBlob", "Content-Type": "application/octet-stream"}
        async with aiohttp.ClientSession() as session:
            async with session.put(blob_url, data=data, headers=headers) as resp:
                resp.raise_for_status()

    async def _get_azure_sas(self, object_key: str) -> bytes:
        base, sep, query = self.azure_sas_container_url.partition("?")
        blob_url = f"{base.rstrip('/')}/{object_key.lstrip('/')}{sep}{query}"
        async with aiohttp.ClientSession() as session:
            async with session.get(blob_url) as resp:
                resp.raise_for_status()
                return await resp.read()
