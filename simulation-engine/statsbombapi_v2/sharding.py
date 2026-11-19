"""
Match-Grouped Stratified Splitting, 16,384-Step Varlen Sequence Packing, and
HuggingFace Datasets / Parquet / Compressed Shard Export (`sharding.py`).
"""

from __future__ import annotations

import json
import random
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import numpy as np

from statsbombapi_v2.schemas import EventGoldV2

try:
    import pyarrow as pa  # type: ignore
    import pyarrow.parquet as pq  # type: ignore

    PYARROW_AVAILABLE = True
except ImportError:
    pa = None  # type: ignore
    pq = None  # type: ignore
    PYARROW_AVAILABLE = False

try:
    import datasets as hf_datasets  # type: ignore

    HF_DATASETS_AVAILABLE = True
except ImportError:
    hf_datasets = None  # type: ignore
    HF_DATASETS_AVAILABLE = False


MAX_CONTEXT_LEN: int = 16_384


@dataclass
class PackedBlock:
    """Single packed varlen block up to `16,384` event steps with strict match isolation."""

    block_id: int
    split: str
    match_ids: List[int]
    cu_seqlens: List[int]
    discrete_tokens: np.ndarray  # [L, 14] int32
    continuous_features: np.ndarray  # [L, 10] float32

    @property
    def length(self) -> int:
        return int(self.discrete_tokens.shape[0])


def assign_match_splits(
    match_ids: Sequence[int],
    match_metadata: Optional[Dict[int, Dict[str, Any]]] = None,
    train_ratio: float = 0.85,
    val_ratio: float = 0.075,
    ood_competition_ids: Optional[Iterable[int]] = None,
    seed: int = 42,
) -> Dict[str, List[int]]:
    """
    Create strictly disjoint match-grouped splits (`train`, `val_calib`, `test_eval`, `ood_eval`).
    Optional stratification by competition_id prevents tournament/season leakage.
    """
    ood_comps = set(ood_competition_ids or ())
    match_metadata = match_metadata or {}

    ood_ids: List[int] = []
    by_stratum: Dict[Any, List[int]] = {}
    for mid in sorted(set(int(m) for m in match_ids)):
        meta = match_metadata.get(mid, {})
        cid = meta.get("competition_id")
        if cid is not None and int(cid) in ood_comps:
            ood_ids.append(mid)
            continue
        stratum = cid if cid is not None else "default"
        by_stratum.setdefault(stratum, []).append(mid)

    rng = random.Random(seed)
    train_ids: List[int] = []
    val_ids: List[int] = []
    test_ids: List[int] = []

    for stratum in sorted(by_stratum.keys(), key=str):
        ids = list(by_stratum[stratum])
        rng.shuffle(ids)
        n = len(ids)
        if n == 1:
            train_ids.extend(ids)
            continue
        if n == 2:
            train_ids.append(ids[0])
            test_ids.append(ids[1])
            continue
        n_train = max(1, int(round(n * train_ratio)))
        n_val = max(1, int(round(n * val_ratio))) if n - n_train >= 2 else 0
        if n_train + n_val >= n:
            n_train = max(1, n - 2)
            n_val = 1
        train_ids.extend(ids[:n_train])
        val_ids.extend(ids[n_train : n_train + n_val])
        test_ids.extend(ids[n_train + n_val :])

    return {
        "train": sorted(train_ids),
        "val_calib": sorted(val_ids),
        "test_eval": sorted(test_ids),
        "ood_eval": sorted(ood_ids),
    }


def encode_event_step(ev: EventGoldV2) -> Tuple[List[int], List[float]]:
    """
    Encode one EventGoldV2 into Module C's 10-field composite step tensors (`EventInputEmbedder`):
    - discrete [14]: [dt_bucket, period, poss_phase, actor_slot, action_type, start_cell,
                      end_cell, end_z_bin (0..31), target_slot, restart_type, pass_height,
                      body_part, modifier_bitmask_8, outcome]
    - continuous [10]: [dt_res, dx_res_start, dy_res_start, dx_res_end, dy_res_end,
                        xg_v2_pre_shot, psxg_v2, xpass_prob, xt_delta, vaep_value]
    """
    disc = [
        int(ev.dt_bucket_id),
        int(ev.period),
        int(ev.poss_phase),
        int(ev.actor_slot),
        int(ev.action_type),
        int(ev.start_cell_id),
        int(ev.end_cell_id),
        max(0, min(int(ev.end_z_bin), 31)),  # 0..31 matching EventInputEmbedder nn.Embedding(32, d)
        int(ev.target_slot),
        int(ev.restart_type),
        int(ev.pass_height),
        int(ev.body_part),
        int(ev.modifier_bitmask_8),
        int(ev.outcome),
    ]
    cont = [
        float(ev.dt_res),
        float(ev.dx_res_start),
        float(ev.dy_res_start),
        float(ev.dx_res_end),
        float(ev.dy_res_end),
        float(ev.xg_v2_pre_shot),
        float(ev.psxg_v2),
        float(ev.xpass_prob),
        float(ev.xt_delta),
        float(ev.vaep_value),
    ]
    return disc, cont


def pack_sequences_16k(
    events_by_match: Dict[int, Sequence[EventGoldV2]],
    split_name: str = "train",
    max_seq_len: int = MAX_CONTEXT_LEN,
) -> List[PackedBlock]:
    """
    Pack complete match sequences into fixed <=`max_seq_len` (default 16,384) blocks
    with exact `cu_seqlens` arrays for FlashAttention-2 / flex_attention block-diagonal masking.
    Matches longer than `max_seq_len` are truncated cleanly only as a fallback safeguard.
    """
    blocks: List[PackedBlock] = []
    cur_disc: List[List[int]] = []
    cur_cont: List[List[float]] = []
    cur_mids: List[int] = []
    cur_cu: List[int] = [0]

    def _flush() -> None:
        nonlocal cur_disc, cur_cont, cur_mids, cur_cu
        if not cur_disc:
            return
        blocks.append(
            PackedBlock(
                block_id=len(blocks),
                split=split_name,
                match_ids=list(cur_mids),
                cu_seqlens=list(cur_cu),
                discrete_tokens=np.asarray(cur_disc, dtype=np.int32),
                continuous_features=np.asarray(cur_cont, dtype=np.float32),
            )
        )
        cur_disc, cur_cont, cur_mids, cur_cu = [], [], [], [0]

    for mid in sorted(events_by_match.keys()):
        evs = events_by_match[mid]
        if not evs:
            continue
        if len(evs) > max_seq_len:
            evs = evs[:max_seq_len]
        if cur_cu[-1] + len(evs) > max_seq_len:
            _flush()
        for ev in evs:
            d, c = encode_event_step(ev)
            cur_disc.append(d)
            cur_cont.append(c)
        cur_mids.append(int(mid))
        cur_cu.append(cur_cu[-1] + len(evs))

    _flush()
    return blocks


def export_packed_shards(
    events: Sequence[EventGoldV2],
    output_dir: Path,
    splits: Optional[Dict[str, List[int]]] = None,
    max_seq_len: int = MAX_CONTEXT_LEN,
) -> Dict[str, Any]:
    """
    Export match-grouped packed 16k shards to `output_dir` as HuggingFace DatasetDict /
    Parquet when `datasets`/`pyarrow` are installed, plus portable `.npz` + manifest.json.
    """
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    by_match: Dict[int, List[EventGoldV2]] = {}
    for ev in events:
        by_match.setdefault(ev.match_id, []).append(ev)
    for mid in by_match:
        by_match[mid].sort(key=lambda e: e.event_seq_idx)

    if splits is None:
        splits = assign_match_splits(list(by_match.keys()))

    manifest: Dict[str, Any] = {
        "max_seq_len": max_seq_len,
        "splits": {},
        "hf_datasets_exported": False,
        "parquet_exported": False,
    }

    hf_dict_payload: Dict[str, Any] = {}
    for split_name, mids in splits.items():
        subset = {m: by_match[m] for m in mids if m in by_match}
        blocks = pack_sequences_16k(subset, split_name=split_name, max_seq_len=max_seq_len)
        split_dir = output_dir / split_name
        split_dir.mkdir(parents=True, exist_ok=True)

        block_files: List[str] = []
        hf_rows: List[Dict[str, Any]] = []
        for blk in blocks:
            npz_path = split_dir / f"block_{blk.block_id:05d}.npz"
            np.savez_compressed(
                npz_path,
                discrete_tokens=blk.discrete_tokens,
                continuous_features=blk.continuous_features,
                cu_seqlens=np.asarray(blk.cu_seqlens, dtype=np.int32),
                match_ids=np.asarray(blk.match_ids, dtype=np.int64),
            )
            block_files.append(str(npz_path.name))
            hf_rows.append(
                {
                    "block_id": blk.block_id,
                    "match_ids": blk.match_ids,
                    "cu_seqlens": blk.cu_seqlens,
                    "discrete_tokens": blk.discrete_tokens.tolist(),
                    "continuous_features": blk.continuous_features.tolist(),
                }
            )

        if PYARROW_AVAILABLE and hf_rows:
            table = pa.Table.from_pylist(hf_rows)
            pq.write_table(table, split_dir / "packed_blocks.parquet", compression="zstd")
            manifest["parquet_exported"] = True

        if HF_DATASETS_AVAILABLE and hf_rows:
            hf_dict_payload[split_name] = hf_datasets.Dataset.from_list(hf_rows)

        manifest["splits"][split_name] = {
            "num_matches": len(subset),
            "num_blocks": len(blocks),
            "total_events": sum(b.length for b in blocks),
            "files": block_files,
        }

    if HF_DATASETS_AVAILABLE and hf_dict_payload:
        ds_dict = hf_datasets.DatasetDict(hf_dict_payload)
        ds_dict.save_to_disk(str(output_dir / "hf_dataset_v2"))
        manifest["hf_datasets_exported"] = True

    (output_dir / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest
