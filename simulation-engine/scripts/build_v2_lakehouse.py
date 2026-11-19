#!/usr/bin/env python3
"""
End-to-End Pillar 1 Lakehouse & Training Bundle Builder (`scripts/build_v2_lakehouse.py`).

Executes the full `statsbombapi_v2` pipeline across:
  1. StatsBomb Open Data (`--open-data-dir` pointing to `hudl/open-data` or local `Matches/` + `archive/Matches/` fallback)
  2. Wyscout Open Data (`--wyscout-dir` or built-in multi-competition open sample)
  3. 2022–2026 Modern Enrichment (`--scraped-cache-dir` + `DataMigration/Players` + 2022–2026 Top-5 European league seeds)

Outputs into `--out-dir` (default `./models_v2`):
  - `events_gold_v2.parquet`
  - `player_id_crosswalk.parquet`
  - `player_profiles_v2.parquet`
  - `tactical_manifold_v2.pkl`
  - `xt_grid_24x16.npy`
  - `packed_shards_v2.pt` (PyTorch training bundle for `PixelPitch-EventTF-112M`, `SpatialPredictor`, & `ModuleDOutcomeSuite`)
  - `lakehouse_manifest.json`
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
import time
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import numpy as np
import pandas as pd
import torch

# Ensure simulation-engine root is importable regardless of working directory
SIM_ENGINE_ROOT = Path(__file__).resolve().parent.parent
if str(SIM_ENGINE_ROOT) not in sys.path:
    sys.path.insert(0, str(SIM_ENGINE_ROOT))

from statsbombapi_v2.etl_duckdb import (
    DUCKDB_AVAILABLE,
    PYARROW_AVAILABLE,
    LakehouseEngine,
    ingest_statsbomb_match,
    ingest_wyscout_match,
)
from statsbombapi_v2.labels_and_xt import (
    ExpectedThreatModel,
    PostShotXGModel,
    PreShotXGModel,
    VAEPModel,
    XPassModel,
    enrich_events_with_labels,
)
from statsbombapi_v2.player_profiler import (
    HistoricalPlayerProfiler,
    TacticalManifoldV2,
    export_profiles_parquet,
    extract_team_match_tactics,
)
from statsbombapi_v2.schemas import (
    ActionType,
    EventGoldV2,
    EventOutcome,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PlayerIdCrosswalk,
    PlayerProfileV2,
)
from statsbombapi_v2.scrapers import EntityResolver, normalize_player_name
from statsbombapi_v2.sharding import (
    MAX_CONTEXT_LEN,
    assign_match_splits,
    encode_event_step,
    pack_sequences_16k,
)
from statsbombapi_v2.validate import run_all_gates


# ---------------------------------------------------------------------------
# Canonical 4-3-3 Baseline Coordinates & Role Mapping for Local / 360 Synthesis
# ---------------------------------------------------------------------------

_CANONICAL_HOME_11_XY = np.array(
    [
        [6.0, 40.0],    # 1: GK
        [26.0, 70.0],   # 2: RB
        [22.0, 50.0],   # 3: RCB
        [22.0, 30.0],   # 4: LCB
        [26.0, 10.0],   # 5: LB
        [44.0, 40.0],   # 6: CDM
        [56.0, 56.0],   # 7: RCM
        [56.0, 24.0],   # 8: LCM
        [82.0, 68.0],   # 9: RW
        [88.0, 40.0],   # 10: ST
        [82.0, 12.0],   # 11: LW
    ],
    dtype=np.float32,
)

_SLOT_SB_POSITION_NAMES = [
    "Goalkeeper",
    "Right Back",
    "Right Center Back",
    "Left Center Back",
    "Left Back",
    "Center Defensive Midfield",
    "Right Center Midfield",
    "Left Center Midfield",
    "Right Wing",
    "Center Forward",
    "Left Wing",
]

_KNOWN_PLAYER_POSITIONS: Dict[str, str] = {
    "Marc-André ter Stegen": "Goalkeeper",
    "Claudio Bravo": "Goalkeeper",
    "Neto": "Goalkeeper",
    "Thibaut Courtois": "Goalkeeper",
    "Keylor Navas": "Goalkeeper",
    "Jan Oblak": "Goalkeeper",
    "Yassine Bounou": "Goalkeeper",
    "Dani Alves": "Right Back",
    "Sergi Roberto": "Right Back",
    "Nélson Semedo": "Right Back",
    "Sergiño Dest": "Right Back",
    "Dani Carvajal": "Right Back",
    "Lucas Vázquez": "Right Back",
    "Jesús Navas": "Right Back",
    "Kieran Trippier": "Right Back",
    "Gerard Piqué": "Right Center Back",
    "Javier Mascherano": "Left Center Back",
    "Thomas Vermaelen": "Left Center Back",
    "Samuel Umtiti": "Left Center Back",
    "Clément Lenglet": "Left Center Back",
    "Ronald Araújo": "Right Center Back",
    "Sergio Ramos": "Right Center Back",
    "Raphaël Varane": "Left Center Back",
    "Éder Militão": "Right Center Back",
    "David Alaba": "Left Center Back",
    "Nacho": "Right Center Back",
    "Stefan Savić": "Right Center Back",
    "José María Giménez": "Right Center Back",
    "Pau Torres": "Left Center Back",
    "Jordi Alba": "Left Back",
    "Marcelo": "Left Back",
    "Ferland Mendy": "Left Back",
    "Renan Lodi": "Left Back",
    "José Gayà": "Left Back",
    "Sergio Busquets": "Center Defensive Midfield",
    "Casemiro": "Center Defensive Midfield",
    "Rodri": "Center Defensive Midfield",
    "Frenkie de Jong": "Left Center Midfield",
    "Ivan Rakitić": "Right Center Midfield",
    "Andrés Iniesta": "Left Center Midfield",
    "Pedri": "Left Center Midfield",
    "Gavi": "Left Center Midfield",
    "Arturo Vidal": "Right Center Midfield",
    "Luka Modrić": "Right Center Midfield",
    "Toni Kroos": "Left Center Midfield",
    "Federico Valverde": "Right Center Midfield",
    "Eduardo Camavinga": "Left Center Midfield",
    "Koke": "Right Center Midfield",
    "Saúl Ñíguez": "Left Center Midfield",
    "Marcos Llorente": "Right Center Midfield",
    "Dani Parejo": "Right Center Midfield",
    "Lionel Messi": "Right Wing",
    "Ousmane Dembélé": "Right Wing",
    "Marco Asensio": "Right Wing",
    "Rodrygo": "Right Wing",
    "Ángel Correa": "Right Wing",
    "Luis Suárez": "Center Forward",
    "Karim Benzema": "Center Forward",
    "Antoine Griezmann": "Center Forward",
    "João Félix": "Center Forward",
    "Memphis Depay": "Center Forward",
    "Youssef En-Nesyri": "Center Forward",
    "Alexander Isak": "Center Forward",
    "Gerard Moreno": "Center Forward",
    "Iago Aspas": "Center Forward",
    "Iñaki Williams": "Center Forward",
    "Neymar": "Left Wing",
    "Neymar Jr": "Left Wing",
    "Ansu Fati": "Left Wing",
    "Philippe Coutinho": "Left Wing",
    "Munir El Haddadi": "Left Wing",
    "Vinícius Júnior": "Left Wing",
    "Eden Hazard": "Left Wing",
    "Cristiano Ronaldo": "Left Wing",
    "Gareth Bale": "Right Wing",
    "Mikel Oyarzabal": "Left Wing",
    "Nico Williams": "Left Wing",
    "Gonçalo Guedes": "Left Wing",
}


def _stable_id(text: str, mod: int = 900_000, offset: int = 10_000) -> int:
    digest = hashlib.blake2b(text.encode("utf-8"), digest_size=4).digest()
    return offset + (int.from_bytes(digest, "big") % mod)


# ---------------------------------------------------------------------------
# 1. StatsBomb Open Data Ingestion (Prioritizing 360 Freeze-Frame Matches)
# ---------------------------------------------------------------------------

def _ingest_one_statsbomb_match_worker(
    args: Tuple[str, int, int, Dict[str, Any], bool],
) -> Tuple[int, List[EventGoldV2], Dict[str, Any], Dict[int, Dict[str, Any]]]:
    """Top-level worker for parallel StatsBomb match ingestion."""
    data_dir_str, cid, mid, m_dict, has_360 = args
    data_dir = Path(data_dir_str)
    ev_file = data_dir / "events" / f"{mid}.json"
    raw_ev = json.loads(ev_file.read_text(encoding="utf-8-sig"))
    frames_360 = None
    if has_360:
        try:
            frames_360 = json.loads((data_dir / "three-sixty" / f"{mid}.json").read_text(encoding="utf-8-sig"))
        except Exception:
            frames_360 = None

    p_meta: Dict[int, Dict[str, Any]] = {}
    lineup_file = data_dir / "lineups" / f"{mid}.json"
    if lineup_file.exists():
        try:
            lu_data = json.loads(lineup_file.read_text(encoding="utf-8-sig"))
            for team_entry in lu_data:
                t_name = str(team_entry.get("team_name", ""))
                for p_entry in team_entry.get("lineup", []):
                    pid = int(p_entry.get("player_id", -1))
                    if pid > 0 and pid not in p_meta:
                        country = p_entry.get("country")
                        nat = country.get("name", "UNK") if isinstance(country, dict) else str(country or "UNK")
                        p_meta[pid] = {
                            "canonical_name": str(p_entry.get("player_name") or p_entry.get("player_nickname") or f"Player_{pid}"),
                            "team_name": t_name,
                            "nationality": nat[:3].upper(),
                        }
        except Exception:
            pass

    match_events = ingest_statsbomb_match(m_dict, raw_ev, frames_360=frames_360)
    for ev in match_events:
        if ev.actor_player_id > 0 and ev.actor_player_id not in p_meta:
            p_meta[ev.actor_player_id] = {
                "canonical_name": f"Player_{ev.actor_player_id}",
                "preferred_foot": "Right",
            }
    m_meta = {
        "match_id": mid,
        "competition_id": cid,
        "season_id": int(m_dict.get("season_id", 1)),
        "match_date": str(m_dict.get("match_date", "2023-01-01"))[:10],
        "has_360": has_360,
    }
    return mid, match_events, m_meta, p_meta


STATSBOMB_FILTER_PRESETS: Dict[str, set[Tuple[int, int]]] = {
    "mens_top_360_core": {
        (9, 281),   # 1. Bundesliga 2023/24 (34)
        (11, 90),   # La Liga 2020/21 (35)
        (7, 108),   # Ligue 1 2021/22 (26)
        (7, 235),   # Ligue 1 2022/23 (32)
        (55, 43),   # UEFA Euro 2020 (51)
        (55, 282),  # UEFA Euro 2024 (51)
        (43, 106),  # FIFA World Cup 2022 (64)
    },
    "mens_big5_and_top_360": {
        (9, 281),   # 1. Bundesliga 2023/24 (34)
        (11, 90),   # La Liga 2020/21 (35)
        (7, 108),   # Ligue 1 2021/22 (26)
        (7, 235),   # Ligue 1 2022/23 (32)
        (55, 43),   # UEFA Euro 2020 (51)
        (55, 282),  # UEFA Euro 2024 (51)
        (43, 106),  # FIFA World Cup 2022 (64)
        (2, 27),    # Premier League 2015/16 (380)
        (11, 27),   # La Liga 2015/16 (380)
        (12, 27),   # Serie A 2015/16 (380)
        (7, 27),    # Ligue 1 2015/16 (377)
        (9, 27),    # 1. Bundesliga 2015/16 (34)
    },
}


def ingest_statsbomb_open_data_smart(
    open_data_root: Path,
    max_matches: Optional[int] = None,
    preset: Optional[str] = None,
) -> Tuple[List[EventGoldV2], Dict[int, Dict[str, Any]], Dict[int, Dict[str, Any]]]:
    """
    Stream a cloned `statsbomb/open-data` (`hudl/open-data`) repository.
    Supports curated `--preset` filters (`mens_top_360_core`, `mens_big5_and_top_360`).
    Always sorts `has_360 == True` matches first so Module B/D 360° distillation pulls
    from the modern 360-enabled core while Step C trains on the full filtered corpus.
    """
    data_dir = open_data_root / "data"
    comp_file = data_dir / "competitions.json"
    if not comp_file.exists():
        raise FileNotFoundError(f"StatsBomb competitions.json not found at {comp_file}")

    allowed_pairs: Optional[set[Tuple[int, int]]] = None
    if preset:
        if preset not in STATSBOMB_FILTER_PRESETS:
            raise ValueError(f"Unknown preset '{preset}'. Choose from: {sorted(STATSBOMB_FILTER_PRESETS)}")
        allowed_pairs = STATSBOMB_FILTER_PRESETS[preset]

    competitions = json.loads(comp_file.read_text(encoding="utf-8-sig"))
    candidate_matches: List[Tuple[int, int, int, Dict[str, Any], bool]] = []
    seen_mids: set[int] = set()
    three_sixty_dir = data_dir / "three-sixty"

    for comp in competitions:
        cid = int(comp["competition_id"])
        sid = int(comp["season_id"])
        if allowed_pairs is not None and (cid, sid) not in allowed_pairs:
            continue
        match_file = data_dir / "matches" / str(cid) / f"{sid}.json"
        if not match_file.exists():
            continue
        try:
            matches = json.loads(match_file.read_text(encoding="utf-8-sig"))
        except Exception:
            continue
        for m in matches:
            mid = int(m["match_id"])
            if mid in seen_mids:
                continue
            ev_file = data_dir / "events" / f"{mid}.json"
            if not ev_file.exists():
                continue
            seen_mids.add(mid)
            has_360 = (three_sixty_dir / f"{mid}.json").exists()
            m_enriched = dict(m)
            m_enriched.setdefault("competition_id", cid)
            m_enriched.setdefault("season_id", sid)
            candidate_matches.append((0 if has_360 else 1, cid, mid, m_enriched, has_360))

    # Sort so matches with 360 frames come first, then ordered deterministically by competition & match_id
    candidate_matches.sort(key=lambda item: (item[0], item[1], item[2]))
    if max_matches is not None and max_matches > 0:
        candidate_matches = candidate_matches[:max_matches]

    all_events: List[EventGoldV2] = []
    match_metadata: Dict[int, Dict[str, Any]] = {}
    player_metadata: Dict[int, Dict[str, Any]] = {}

    tasks = [(str(data_dir), cid, mid, m_dict, has_360 and (idx_m < 120)) for idx_m, (_, cid, mid, m_dict, has_360) in enumerate(candidate_matches)]
    total_m = len(tasks)
    import os
    from concurrent.futures import ProcessPoolExecutor

    n_workers = min(max(1, (os.cpu_count() or 2) - 1), 4)
    t_ing = time.perf_counter()
    if n_workers > 1 and total_m >= 16:
        print(f"[Step A] Parallel StatsBomb ingest: {total_m} matches across {n_workers} workers...", flush=True)
        with ProcessPoolExecutor(max_workers=n_workers) as pool:
            for done_i, (mid, match_events, m_meta, p_meta) in enumerate(
                pool.map(_ingest_one_statsbomb_match_worker, tasks, chunksize=8), start=1
            ):
                all_events.extend(match_events)
                match_metadata[mid] = m_meta
                for pid, pm in p_meta.items():
                    if pid not in player_metadata:
                        player_metadata[pid] = pm
                if done_i % 250 == 0 or done_i == total_m:
                    print(
                        f"  [Ingest] {done_i}/{total_m} matches ({len(all_events):,} events) in {time.perf_counter()-t_ing:.1f}s",
                        flush=True,
                    )
    else:
        for done_i, task in enumerate(tasks, start=1):
            mid, match_events, m_meta, p_meta = _ingest_one_statsbomb_match_worker(task)
            all_events.extend(match_events)
            match_metadata[mid] = m_meta
            for pid, pm in p_meta.items():
                if pid not in player_metadata:
                    player_metadata[pid] = pm

    return all_events, match_metadata, player_metadata


# ---------------------------------------------------------------------------
# 2. Local Match `.txt` -> StatsBomb V2 Converter (With 360 Freeze Frames)
# ---------------------------------------------------------------------------

_POS_RE = re.compile(r"\(([^)]+)\)")
_TO_RE = re.compile(r"to \(([^)]+)\)")
_AIMED_RE = re.compile(r"aimed at \(([^)]+)\)")
_OUTCOME_RE = re.compile(r"outcome:\s*([^,]+)")
_RESULT_RE = re.compile(r"resulted in\s+([^,]+)")
_HEIGHT_RE = re.compile(r"height:\s*([^,]+)")
_BODY_RE = re.compile(r"using\s+([^,]+)")
_TYPE_RE = re.compile(r"type:\s*([^,]+)")
_CARD_RE = re.compile(r"card:\s*([^,]+)")


def _parse_coords(coord_str: Optional[str], default: List[float]) -> List[float]:
    if not coord_str:
        return list(default)
    try:
        vals = [float(x.strip()) for x in coord_str.split(",")]
        if len(vals) >= 2:
            return vals[:3]
    except Exception:
        pass
    return list(default)


def _synthesize_360_freeze_frame(
    x_att: float,
    y_att: float,
    end_x_att: float,
    end_y_att: float,
    is_shot: bool,
    rng: np.random.Generator,
) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """
    Synthesize realistic 22-player StatsBomb 360 freeze-frame + shot_freeze_frame
    around the ball `(x_att, y_att)` in the acting team's attacking frame `[0,120]x[0,80]`.
    """
    ball_shift_x = (x_att - 60.0) * 0.35
    ball_shift_y = (y_att - 40.0) * 0.28

    ff_360: List[Dict[str, Any]] = []
    shot_ff: List[Dict[str, Any]] = []

    # 11 Teammates (slot 0 is GK, slot 9 is actor)
    for s in range(11):
        if s == 9:
            px, py = x_att, y_att
            is_actor = True
        elif s == 0:
            px = float(np.clip(8.0 + max(0.0, ball_shift_x * 0.3), 3.0, 28.0))
            py = float(np.clip(40.0 + ball_shift_y * 0.2, 30.0, 50.0))
            is_actor = False
        else:
            base_x, base_y = _CANONICAL_HOME_11_XY[s]
            px = float(np.clip(base_x + ball_shift_x + rng.normal(0.0, 2.2), 2.0, 118.0))
            py = float(np.clip(base_y + ball_shift_y + rng.normal(0.0, 2.0), 2.0, 78.0))
            is_actor = False
        ff_360.append(
            {
                "teammate": True,
                "actor": is_actor,
                "keeper": (s == 0),
                "location": [round(px, 2), round(py, 2)],
            }
        )
        if is_shot and not is_actor:
            shot_ff.append(
                {
                    "teammate": True,
                    "actor": False,
                    "keeper": (s == 0),
                    "location": [round(px, 2), round(py, 2)],
                    "position": {"name": _SLOT_SB_POSITION_NAMES[s]},
                }
            )

    # 11 Opponents (defending goal at x=120, y=40 in attacking-relative frame)
    for s in range(11):
        if s == 0:
            # Defending Goalkeeper near (118, 40) shifted slightly toward shooter angle
            px = float(np.clip(118.2 - max(0.0, (120.0 - x_att) * 0.06), 112.0, 119.5))
            py = float(np.clip(40.0 + (y_att - 40.0) * 0.14 + rng.normal(0.0, 0.35), 36.2, 43.8))
            is_gk = True
        elif s in (2, 3) and is_shot:
            # Central defenders partially contesting the shot cone
            frac = 0.35 if s == 2 else 0.58
            px = float(np.clip(x_att + (116.0 - x_att) * frac + rng.normal(0.0, 1.2), 2.0, 118.5))
            py = float(np.clip(y_att + (40.0 - y_att) * frac + rng.normal(0.0, 1.8), 4.0, 76.0))
            is_gk = False
        else:
            base_x = 120.0 - float(_CANONICAL_HOME_11_XY[s, 0])
            base_y = 80.0 - float(_CANONICAL_HOME_11_XY[s, 1])
            px = float(np.clip(base_x + ball_shift_x * 0.75 + rng.normal(0.0, 2.2), 2.0, 118.5))
            py = float(np.clip(base_y + ball_shift_y * 0.65 + rng.normal(0.0, 2.0), 2.0, 78.0))
            is_gk = False
        ff_360.append(
            {
                "teammate": False,
                "actor": False,
                "keeper": is_gk,
                "location": [round(px, 2), round(py, 2)],
            }
        )
        if is_shot:
            shot_ff.append(
                {
                    "teammate": False,
                    "actor": False,
                    "keeper": is_gk,
                    "location": [round(px, 2), round(py, 2)],
                    "position": {"name": "Goalkeeper" if is_gk else _SLOT_SB_POSITION_NAMES[s]},
                }
            )

    return ff_360, shot_ff


def convert_local_match_txt_to_statsbomb(
    file_path: Path,
    fallback_idx: int = 1,
) -> Tuple[Dict[str, Any], List[Dict[str, Any]], List[Dict[str, Any]], Dict[int, Dict[str, Any]]]:
    """
    Convert a local PixelPitchAI match `.txt` file (`Matches/*.txt` or `archive/Matches/*.txt`)
    into StatsBomb v4 `match_dict`, `raw_events`, `frames_360`, and `player_metadata`.
    """
    lines = file_path.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    home_raw = "Barcelona_2016"
    away_raw = "Real_Madrid_2021"
    for line in lines[:5]:
        if " faced " in line:
            parts = line.split(" faced ")
            home_raw = parts[0].split("(")[0].strip()
            away_raw = parts[1].split("(")[0].strip().rstrip(".")
            break

    # Extract match_id & season year
    stem = file_path.stem
    m_num = re.findall(r"\d+", stem)
    match_id = int(m_num[0]) if m_num else (3_900_000 + fallback_idx)
    if match_id < 100:
        match_id = 3_900_000 + match_id * 100 + fallback_idx

    year_matches = re.findall(r"20\d{2}", f"{home_raw} {away_raw}")
    season_year = int(year_matches[0]) if year_matches else (2018 + (fallback_idx % 6))
    match_date = f"{season_year}-03-{(fallback_idx % 27) + 1:02d}"

    home_clean = re.sub(r"_20\d{2}$", "", home_raw).replace("_", " ").strip()
    away_clean = re.sub(r"_20\d{2}$", "", away_raw).replace("_", " ").strip()
    home_team_id = _stable_id(home_clean, mod=5_000, offset=200)
    away_team_id = _stable_id(away_clean, mod=5_000, offset=200)
    if away_team_id == home_team_id:
        away_team_id += 1

    rng = np.random.default_rng(seed=match_id)

    # First pass: collect ordered distinct players per team and parse raw event lines
    team_players: Dict[str, List[str]] = {home_raw: [], away_raw: []}
    parsed_lines: List[Dict[str, Any]] = []
    period = 1
    for raw_line in lines:
        s = raw_line.strip()
        if not s:
            continue
        if "[SECOND HALF" in s:
            period = 2
            continue
        if s.startswith("["):
            continue
        parts = s.split(" - ", 2)
        if len(parts) < 3:
            continue
        ts_str, team_str, action_part = parts[0].strip(), parts[1].strip(), parts[2].strip()
        if " by " not in action_part:
            continue

        # Parse timestamp
        try:
            if "+" in ts_str:
                base_m, rest = ts_str.split("+", 1)
                ex_m, sec = rest.split(":")
                minute = int(base_m) + int(ex_m)
                second = int(sec)
            else:
                m_s, sec_s = ts_str.split(":")
                minute = int(m_s)
                second = int(sec_s)
        except Exception:
            continue

        act_str, player_part = action_part.split(" by ", 1)
        act_str = act_str.strip()
        if " at " in player_part:
            player_name, attr_str = player_part.split(" at ", 1)
        elif ", card:" in player_part:
            player_name, card_rest = player_part.split(", card:", 1)
            attr_str = f"card: {card_rest}"
        else:
            player_name, attr_str = player_part, ""
        player_name = player_name.strip()
        attr_str = attr_str.strip()

        if team_str not in team_players:
            # Match closest team string
            team_str = home_raw if home_clean.lower() in team_str.lower() else away_raw
        if player_name and player_name != "SYSTEM" and player_name not in team_players[team_str]:
            team_players[team_str].append(player_name)

        parsed_lines.append(
            {
                "period": period,
                "minute": minute,
                "second": second,
                "team_raw": team_str,
                "player_name": player_name,
                "act_str": act_str.lower(),
                "attr_str": attr_str,
            }
        )

    # Ensure each team has at least 11 starters
    for t_key, t_clean in ((home_raw, home_clean), (away_raw, away_clean)):
        while len(team_players[t_key]) < 11:
            idx_fill = len(team_players[t_key]) + 1
            team_players[t_key].append(f"{t_clean} Player {idx_fill}")

    player_metadata: Dict[int, Dict[str, Any]] = {}
    player_id_map: Dict[Tuple[str, str], int] = {}

    def _build_lineup(t_raw: str, t_clean: str) -> Tuple[List[Dict[str, Any]], List[Tuple[str, str]]]:
        p_list = team_players[t_raw]
        starters = p_list[:11]
        subs = p_list[11:]
        lineup_out: List[Dict[str, Any]] = []
        used_slots: set[int] = set()

        # Assign GK first if a known GK is in starters
        gk_idx = 0
        for i, pname in enumerate(starters):
            if _KNOWN_PLAYER_POSITIONS.get(pname) == "Goalkeeper":
                gk_idx = i
                break
        if gk_idx != 0:
            starters[0], starters[gk_idx] = starters[gk_idx], starters[0]

        for slot_idx, pname in enumerate(starters):
            pid = _stable_id(pname, mod=800_000, offset=10_000)
            player_id_map[(t_raw, pname)] = pid
            pos_name = _KNOWN_PLAYER_POSITIONS.get(pname, _SLOT_SB_POSITION_NAMES[slot_idx])
            if slot_idx == 0:
                pos_name = "Goalkeeper"
            elif pos_name == "Goalkeeper":
                pos_name = _SLOT_SB_POSITION_NAMES[slot_idx]
            player_metadata[pid] = {
                "canonical_name": pname,
                "team_name": t_clean,
                "primary_position": pos_name,
            }
            lineup_out.append(
                {
                    "player": {"id": pid, "name": pname},
                    "position": {"id": slot_idx + 1, "name": pos_name},
                    "jersey_number": slot_idx + 1,
                }
            )
            used_slots.add(slot_idx)

        sub_pairs: List[Tuple[str, str]] = []
        for s_i, sub_name in enumerate(subs):
            pid = _stable_id(sub_name, mod=800_000, offset=10_000)
            player_id_map[(t_raw, sub_name)] = pid
            replaced_starter = starters[min(10, max(1, 10 - (s_i % 9)))]
            pos_name = _KNOWN_PLAYER_POSITIONS.get(sub_name, "Center Midfield")
            player_metadata[pid] = {
                "canonical_name": sub_name,
                "team_name": t_clean,
                "primary_position": pos_name,
            }
            sub_pairs.append((replaced_starter, sub_name))

        return lineup_out, sub_pairs

    home_lineup, home_subs = _build_lineup(home_raw, home_clean)
    away_lineup, away_subs = _build_lineup(away_raw, away_clean)

    raw_events: List[Dict[str, Any]] = [
        {
            "id": f"ev_{match_id}_xi_home",
            "index": 1,
            "period": 1,
            "timestamp": "00:00:00.000",
            "minute": 0,
            "second": 0,
            "type": {"id": 35, "name": "Starting XI"},
            "possession": 1,
            "possession_team": {"id": home_team_id, "name": home_clean},
            "team": {"id": home_team_id, "name": home_clean},
            "tactics": {"formation": 433, "lineup": home_lineup},
        },
        {
            "id": f"ev_{match_id}_xi_away",
            "index": 2,
            "period": 1,
            "timestamp": "00:00:00.000",
            "minute": 0,
            "second": 0,
            "type": {"id": 35, "name": "Starting XI"},
            "possession": 1,
            "possession_team": {"id": home_team_id, "name": home_clean},
            "team": {"id": away_team_id, "name": away_clean},
            "tactics": {"formation": 433, "lineup": away_lineup},
        },
    ]

    frames_360: List[Dict[str, Any]] = []
    sub_triggered: set[Tuple[str, str]] = set()
    sub_lookup: Dict[Tuple[str, str], str] = {}
    for starter_n, sub_n in home_subs:
        sub_lookup[(home_raw, sub_n)] = starter_n
    for starter_n, sub_n in away_subs:
        sub_lookup[(away_raw, sub_n)] = starter_n

    possession_id = 1
    prev_team_id = home_team_id
    prev_sec_key = (-1, -1, -1)
    sub_ms_counter = 0

    for idx, pl in enumerate(parsed_lines):
        t_raw = pl["team_raw"]
        is_home = t_raw == home_raw
        t_id = home_team_id if is_home else away_team_id
        t_clean = home_clean if is_home else away_clean
        pname = pl["player_name"]
        pid = player_id_map.get((t_raw, pname), _stable_id(pname, mod=800_000, offset=10_000))

        p_num = int(pl["period"])
        minute = int(pl["minute"])
        second = int(pl["second"])
        sec_key = (p_num, minute, second)
        if sec_key == prev_sec_key:
            sub_ms_counter = min(950, sub_ms_counter + 180)
        else:
            prev_sec_key = sec_key
            sub_ms_counter = 0
        h_part = minute // 60
        m_part = minute % 60
        ts_formatted = f"{h_part:02d}:{m_part:02d}:{second:02d}.{sub_ms_counter:03d}"

        # Emit Substitution event the first time a substitute appears on pitch
        if (t_raw, pname) in sub_lookup and (t_raw, pname) not in sub_triggered:
            sub_triggered.add((t_raw, pname))
            starter_name = sub_lookup[(t_raw, pname)]
            starter_pid = player_id_map.get((t_raw, starter_name), -1)
            if starter_pid > 0:
                raw_events.append(
                    {
                        "id": f"ev_{match_id}_sub_{len(raw_events)}",
                        "index": len(raw_events) + 1,
                        "period": p_num,
                        "timestamp": ts_formatted,
                        "minute": minute,
                        "second": second,
                        "type": {"id": 19, "name": "Substitution"},
                        "possession": possession_id,
                        "possession_team": {"id": t_id, "name": t_clean},
                        "team": {"id": t_id, "name": t_clean},
                        "player": {"id": starter_pid, "name": starter_name},
                        "substitution": {
                            "replacement": {"id": pid, "name": pname},
                            "outcome": {"name": "Tactical"},
                        },
                    }
                )

        if t_id != prev_team_id:
            possession_id += 1
            prev_team_id = t_id

        attr_str = pl["attr_str"]
        pos_m = _POS_RE.search(attr_str)
        loc = _parse_coords(pos_m.group(1) if pos_m else None, [60.0, 40.0])
        x_att, y_att = float(loc[0]), float(loc[1])

        to_m = _TO_RE.search(attr_str)
        aim_m = _AIMED_RE.search(attr_str)
        out_m = _RESULT_RE.search(attr_str) or _OUTCOME_RE.search(attr_str)
        out_text = out_m.group(1).strip() if out_m else ""
        height_m = _HEIGHT_RE.search(attr_str)
        height_text = height_m.group(1).strip() if height_m else "Ground Pass"
        body_m = _BODY_RE.search(attr_str)
        body_text = body_m.group(1).strip() if body_m else "Right Foot"
        type_m = _TYPE_RE.search(attr_str)
        type_text = type_m.group(1).strip() if type_m else ""
        card_m = _CARD_RE.search(attr_str)
        card_text = card_m.group(1).strip() if card_m else ""

        act = pl["act_str"]
        ev_uuid = f"ev_{match_id}_{len(raw_events)}"
        play_pattern = "Regular Play"
        if "corner" in type_text.lower():
            play_pattern = "From Corner"
        elif "free kick" in type_text.lower():
            play_pattern = "From Free Kick"
        elif "throw-in" in type_text.lower():
            play_pattern = "From Throw In"
        elif "goal kick" in type_text.lower():
            play_pattern = "From Goal Kick"
        elif "kick off" in type_text.lower():
            play_pattern = "From Kick Off"

        ev_obj: Dict[str, Any] = {
            "id": ev_uuid,
            "index": len(raw_events) + 1,
            "period": p_num,
            "timestamp": ts_formatted,
            "minute": minute,
            "second": second,
            "possession": possession_id,
            "possession_team": {"id": t_id, "name": t_clean},
            "play_pattern": {"name": play_pattern},
            "team": {"id": t_id, "name": t_clean},
            "player": {"id": pid, "name": pname},
            "location": [x_att, y_att],
            "under_pressure": bool("pressure" in attr_str.lower() or rng.random() < 0.18),
        }

        if "ball receipt" in act:
            ev_obj["type"] = {"id": 42, "name": "Ball Receipt*"}
            raw_events.append(ev_obj)
            continue

        if "carry" in act:
            # Infer carry end location from next event by same player if available
            end_xy = [x_att, y_att]
            if idx + 1 < len(parsed_lines) and parsed_lines[idx + 1]["player_name"] == pname:
                nxt_pos = _POS_RE.search(parsed_lines[idx + 1]["attr_str"])
                if nxt_pos:
                    end_xy = _parse_coords(nxt_pos.group(1), [x_att, y_att])[:2]
            ev_obj["type"] = {"id": 43, "name": "Carry"}
            ev_obj["carry"] = {"end_location": end_xy}
            raw_events.append(ev_obj)
            continue

        if "pass" in act:
            end_xy = _parse_coords(to_m.group(1) if to_m else None, [min(120.0, x_att + 8.0), y_att])[:2]
            # Look ahead for Ball Receipt* to link recipient
            recipient_dict = None
            if idx + 1 < len(parsed_lines) and "ball receipt" in parsed_lines[idx + 1]["act_str"]:
                rec_name = parsed_lines[idx + 1]["player_name"]
                rec_pid = player_id_map.get((t_raw, rec_name), _stable_id(rec_name, mod=800_000, offset=10_000))
                recipient_dict = {"id": rec_pid, "name": rec_name}

            dy = abs(end_xy[1] - y_att)
            dx = end_xy[0] - x_att
            pass_payload: Dict[str, Any] = {
                "end_location": end_xy,
                "length": float(math.hypot(dx, end_xy[1] - y_att)),
                "angle": float(math.atan2(end_xy[1] - y_att, dx)),
                "height": {"name": height_text},
                "body_part": {"name": body_text},
                "switch": bool(dy >= 35.0),
                "through_ball": bool("through" in type_text.lower() or (dx > 18.0 and end_xy[0] > 88.0 and height_text == "Ground Pass")),
                "cross": bool("cross" in attr_str.lower() or (x_att > 85.0 and (y_att < 18.0 or y_att > 62.0) and 30.0 <= end_xy[1] <= 50.0)),
            }
            if type_text:
                pass_payload["type"] = {"name": type_text}
            if recipient_dict is not None:
                pass_payload["recipient"] = recipient_dict
            if out_text and out_text.lower() != "complete":
                pass_payload["outcome"] = {"name": out_text}
            ev_obj["type"] = {"id": 30, "name": "Pass"}
            ev_obj["pass"] = pass_payload
            raw_events.append(ev_obj)

            # Synthesize 360 frame for every 4th pass + all final-third passes
            if x_att >= 75.0 or (idx % 4 == 0):
                ff_360, _ = _synthesize_360_freeze_frame(x_att, y_att, end_xy[0], end_xy[1], is_shot=False, rng=rng)
                frames_360.append({"event_uuid": ev_uuid, "freeze_frame": ff_360, "visible_area": [0.0, 0.0, 120.0, 0.0, 120.0, 80.0, 0.0, 80.0]})
            continue

        if "shot" in act:
            end_xyz = _parse_coords(aim_m.group(1) if aim_m else (to_m.group(1) if to_m else None), [120.0, 40.0, 0.8])
            if len(end_xyz) == 2:
                end_xyz.append(0.8 if "goal" in out_text.lower() or "saved" in out_text.lower() else 0.2)
            ff_360, shot_ff = _synthesize_360_freeze_frame(x_att, y_att, end_xyz[0], end_xyz[1], is_shot=True, rng=rng)
            dist_g = max(1.0, math.hypot(120.0 - x_att, 40.0 - y_att))
            sb_xg = float(1.0 / (1.0 + math.exp(0.95 + 0.13 * dist_g)))
            ev_obj["type"] = {"id": 16, "name": "Shot"}
            ev_obj["shot"] = {
                "end_location": end_xyz,
                "statsbomb_xg": round(sb_xg, 4),
                "body_part": {"name": body_text},
                "type": {"name": type_text or "Open Play"},
                "outcome": {"name": out_text or "Off T"},
                "first_time": bool(rng.random() < 0.25),
                "one_on_one": bool(dist_g < 11.0 and rng.random() < 0.35),
                "freeze_frame": shot_ff,
            }
            raw_events.append(ev_obj)
            frames_360.append({"event_uuid": ev_uuid, "freeze_frame": ff_360, "visible_area": [0.0, 0.0, 120.0, 0.0, 120.0, 80.0, 0.0, 80.0]})
            continue

        if "dribble" in act:
            ev_obj["type"] = {"id": 14, "name": "Dribble"}
            ev_obj["dribble"] = {"outcome": {"name": out_text or "Complete"}}
            raw_events.append(ev_obj)
            continue

        if "clearance" in act:
            ev_obj["type"] = {"id": 9, "name": "Clearance"}
            ev_obj["clearance"] = {"body_part": {"name": body_text or "Head"}}
            raw_events.append(ev_obj)
            continue

        if "interception" in act:
            ev_obj["type"] = {"id": 10, "name": "Interception"}
            ev_obj["interception"] = {"outcome": {"name": out_text or "Won"}}
            raw_events.append(ev_obj)
            continue

        if "duel" in act or "tackle" in act or "50/50" in act:
            ev_obj["type"] = {"id": 4, "name": "Duel"}
            ev_obj["duel"] = {
                "type": {"name": type_text or "Tackle"},
                "outcome": {"name": out_text or "Won"},
            }
            raw_events.append(ev_obj)
            continue

        if "foul committed" in act:
            foul_dict: Dict[str, Any] = {}
            if card_text:
                foul_dict["card"] = {"name": card_text}
            if "penalty" in out_text.lower() or "penalty" in attr_str.lower():
                foul_dict["penalty"] = True
            ev_obj["type"] = {"id": 22, "name": "Foul Committed"}
            ev_obj["foul_committed"] = foul_dict
            raw_events.append(ev_obj)
            continue

        if "foul won" in act:
            ev_obj["type"] = {"id": 21, "name": "Foul Won"}
            ev_obj["foul_won"] = {"defensive": bool(x_att < 40.0)}
            raw_events.append(ev_obj)
            continue

        if "bad behaviour" in act:
            ev_obj["type"] = {"id": 24, "name": "Bad Behaviour"}
            ev_obj["bad_behaviour"] = {"card": {"name": card_text or "Yellow Card"}}
            raw_events.append(ev_obj)
            continue

        if "goalkeeper" in act or "save" in act:
            ev_obj["type"] = {"id": 23, "name": "Goal Keeper"}
            ev_obj["goalkeeper"] = {
                "type": {"name": type_text or ("Shot Saved" if "save" in act else "Collected")},
                "outcome": {"name": out_text or "Success"},
            }
            raw_events.append(ev_obj)
            continue

        if "block" in act:
            ev_obj["type"] = {"id": 6, "name": "Block"}
            raw_events.append(ev_obj)
            continue

        if "pressure" in act:
            ev_obj["type"] = {"id": 17, "name": "Pressure"}
            raw_events.append(ev_obj)
            continue

        if "miscontrol" in act:
            ev_obj["type"] = {"id": 38, "name": "Miscontrol"}
            raw_events.append(ev_obj)
            continue

        if "dispossessed" in act:
            ev_obj["type"] = {"id": 3, "name": "Dispossessed"}
            raw_events.append(ev_obj)
            continue

    match_dict = {
        "match_id": match_id,
        "match_date": match_date,
        "competition": {"competition_id": 11, "competition_name": "La Liga"},
        "season": {"season_id": season_year, "season_name": f"{season_year}/{season_year + 1}"},
        "home_team": {"home_team_id": home_team_id, "home_team_name": home_clean},
        "away_team": {"away_team_id": away_team_id, "away_team_name": away_clean},
    }
    return match_dict, raw_events, frames_360, player_metadata


def ingest_local_matches_fallback(
    search_root: Path,
    max_matches: Optional[int] = None,
) -> Tuple[List[EventGoldV2], Dict[int, Dict[str, Any]], Dict[int, Dict[str, Any]]]:
    """Ingest local `.txt` matches from `Matches/` and `archive/Matches/` when `open-data` is absent."""
    candidates: List[Path] = []
    for folder in (
        search_root,
        SIM_ENGINE_ROOT / "Matches",
        SIM_ENGINE_ROOT / "archive" / "Matches",
    ):
        if folder.exists() and folder.is_dir():
            for p in sorted(folder.glob("*.txt")):
                if p not in candidates:
                    candidates.append(p)

    # Deduplicate by file size & stem so identical match files aren't double-counted
    unique_files: List[Path] = []
    seen_stems: set[str] = set()
    for p in candidates:
        if p.stem not in seen_stems:
            seen_stems.add(p.stem)
            unique_files.append(p)

    if max_matches is not None and max_matches > 0:
        unique_files = unique_files[:max_matches]

    all_events: List[EventGoldV2] = []
    match_metadata: Dict[int, Dict[str, Any]] = {}
    player_metadata: Dict[int, Dict[str, Any]] = {}

    for idx, fpath in enumerate(unique_files, start=1):
        m_dict, raw_ev, f360, p_meta = convert_local_match_txt_to_statsbomb(fpath, fallback_idx=idx)
        mid = int(m_dict["match_id"])
        # Avoid match_id collisions across files
        while mid in match_metadata:
            mid += 1
            m_dict["match_id"] = mid
        evs = ingest_statsbomb_match(m_dict, raw_ev, frames_360=f360)
        all_events.extend(evs)
        match_metadata[mid] = {
            "match_id": mid,
            "competition_id": int(m_dict["competition"]["competition_id"]),
            "season_id": int(m_dict["season"]["season_id"]),
            "match_date": str(m_dict["match_date"]),
            "has_360": True,
        }
        player_metadata.update(p_meta)

    return all_events, match_metadata, player_metadata


# ---------------------------------------------------------------------------
# 3. Wyscout Ingestion (Directory or Multi-Competition Open Fallback)
# ---------------------------------------------------------------------------

def ingest_wyscout_data(
    wyscout_dir: Optional[Path],
    max_matches: Optional[int] = None,
) -> Tuple[List[EventGoldV2], Dict[int, Dict[str, Any]]]:
    """Ingest Wyscout JSON files from `wyscout_dir` or generate a calibrated Wyscout open sample."""
    events_out: List[EventGoldV2] = []
    meta_out: Dict[int, Dict[str, Any]] = {}

    if wyscout_dir is not None and wyscout_dir.exists():
        ev_files = sorted(wyscout_dir.glob("*events*.json"))
        for fpath in ev_files:
            try:
                raw_data = json.loads(fpath.read_text(encoding="utf-8-sig"))
                if isinstance(raw_data, list):
                    by_mid: Dict[int, List[Dict[str, Any]]] = {}
                    for item in raw_data:
                        mid = int(item.get("matchId", 5_000_001))
                        by_mid.setdefault(mid, []).append(item)
                    for mid, m_evs in sorted(by_mid.items()):
                        teams = sorted({int(e.get("teamId", 1)) for e in m_evs if e.get("teamId")})
                        h_id = teams[0] if len(teams) >= 1 else 501
                        a_id = teams[1] if len(teams) >= 2 else 502
                        m_info = {"wyId": mid, "home_team_id": h_id, "away_team_id": a_id}
                        evs = ingest_wyscout_match(m_info, m_evs)
                        events_out.extend(evs)
                        meta_out[mid] = {
                            "match_id": mid,
                            "competition_id": 500,
                            "season_id": 2018,
                            "match_date": "2018-05-12",
                            "has_360": False,
                        }
                        if max_matches is not None and len(meta_out) >= max(1, max_matches // 5):
                            return events_out, meta_out
            except Exception:
                continue

    if events_out:
        return events_out, meta_out

    # Compact Wyscout multi-match open calibration sample (ensures cross-provider coverage)
    wy_mid = 5_800_001
    wy_events = [
        {
            "id": 9001,
            "matchId": wy_mid,
            "matchPeriod": "1H",
            "eventSec": 2.0,
            "teamId": 601,
            "playerId": 20001,
            "eventId": 8,
            "subEventName": "Simple pass",
            "positions": [{"x": 50, "y": 50}, {"x": 64, "y": 42}],
            "tags": [{"id": 1801}],
        },
        {
            "id": 9002,
            "matchId": wy_mid,
            "matchPeriod": "1H",
            "eventSec": 5.5,
            "teamId": 601,
            "playerId": 20002,
            "eventId": 8,
            "subEventName": "Smart pass",
            "positions": [{"x": 66, "y": 40}, {"x": 88, "y": 48}],
            "tags": [{"id": 901}, {"id": 1801}],
        },
        {
            "id": 9003,
            "matchId": wy_mid,
            "matchPeriod": "1H",
            "eventSec": 8.2,
            "teamId": 601,
            "playerId": 20003,
            "eventId": 10,
            "subEventName": "Shot",
            "positions": [{"x": 88, "y": 48}, {"x": 100, "y": 50}],
            "tags": [{"id": 101}, {"id": 1801}],
        },
        {
            "id": 9004,
            "matchId": wy_mid,
            "matchPeriod": "2H",
            "eventSec": 12.0,
            "teamId": 602,
            "playerId": 20012,
            "eventId": 8,
            "subEventName": "High pass",
            "positions": [{"x": 45, "y": 60}, {"x": 78, "y": 25}],
            "tags": [{"id": 801}, {"id": 1801}],
        },
        {
            "id": 9005,
            "matchId": wy_mid,
            "matchPeriod": "2H",
            "eventSec": 16.0,
            "teamId": 602,
            "playerId": 20015,
            "eventId": 1,
            "subEventName": "Ground attacking duel",
            "positions": [{"x": 78, "y": 25}, {"x": 82, "y": 28}],
            "tags": [{"id": 703}, {"id": 1801}],
        },
    ]
    evs = ingest_wyscout_match({"wyId": wy_mid, "home_team_id": 601, "away_team_id": 602}, wy_events)
    meta_out[wy_mid] = {
        "match_id": wy_mid,
        "competition_id": 500,
        "season_id": 2018,
        "match_date": "2018-05-20",
        "has_360": False,
    }
    return evs, meta_out


# ---------------------------------------------------------------------------
# 4. 2022–2026 Scraped Cache & Entity Resolution (`player_id_crosswalk.parquet`)
# ---------------------------------------------------------------------------

def build_player_crosswalk(
    events: Sequence[EventGoldV2],
    player_metadata: Dict[int, Dict[str, Any]],
    scraped_cache_dir: Optional[Path] = None,
) -> List[PlayerIdCrosswalk]:
    """Resolve all observed players + 2022–2026 multi-provider enrichment records into `PlayerIdCrosswalk`."""
    observed_pids = sorted({ev.actor_player_id for ev in events if ev.actor_player_id > 0})
    seed_crosswalk: List[PlayerIdCrosswalk] = []
    for pid in observed_pids:
        meta = player_metadata.get(pid, {})
        cname = str(meta.get("canonical_name") or f"Player_{pid}")
        tname = str(meta.get("team_name") or "Unknown FC")
        nat = str(meta.get("nationality") or "ESP")[:3].upper()
        pos = str(meta.get("primary_position") or "CM")
        seed_crosswalk.append(
            PlayerIdCrosswalk(
                pixelpitch_player_id=int(pid),
                statsbomb_id=int(pid),
                full_name_norm=normalize_player_name(cname),
                birth_year=int(meta.get("birth_year", 1995)),
                nationality_iso=nat,
                current_team_name=tname,
                position_group=pos,
                match_confidence=1.0,
                resolution_method="exact_id_table",
            )
        )

    resolver = EntityResolver(existing_crosswalk=seed_crosswalk)

    if scraped_cache_dir is not None and scraped_cache_dir.exists():
        for jfile in sorted(scraped_cache_dir.glob("*.json")):
            try:
                records = json.loads(jfile.read_text(encoding="utf-8-sig"))
                if isinstance(records, list) and records:
                    resolver.link_external_records(records, source_id_field="fbref_hex_id")
            except Exception:
                continue

    modern_2022_2026_seeds = [
        ("Lionel Messi", 1987, "ARG", "Barcelona", "d70ce98e", 2097, 12994, 28003, 158023),
        ("Karim Benzema", 1987, "FRA", "Real Madrid", "70d74ece", 2370, 3306, 18922, 165153),
        ("Luka Modrić", 1985, "CRO", "Real Madrid", "6025fab1", 2250, 15466, 27992, 177003),
        ("Toni Kroos", 1990, "GER", "Real Madrid", "6ce1f46f", 2369, 26502, 31909, 182521),
        ("Vinícius Júnior", 2000, "BRA", "Real Madrid", "7111d552", 7008, 868812, 371998, 238794),
        ("Pedri", 2002, "ESP", "Barcelona", "0d9b2d31", 8935, 991230, 683840, 251854),
        ("Frenkie de Jong", 1997, "NED", "Barcelona", "1bacc518", 7781, 795222, 326330, 228702),
        ("Federico Valverde", 1998, "URU", "Real Madrid", "0959c2a2", 6241, 831808, 369081, 239053),
        ("Antoine Griezmann", 1991, "FRA", "Atlético Madrid", "df69b544", 2270, 85859, 125781, 194765),
        ("Jan Oblak", 1993, "SVN", "Atlético Madrid", "5c74c0f5", 2261, 69990, 121483, 200389),
        ("Thibaut Courtois", 1992, "BEL", "Real Madrid", "1840e36d", 680, 70988, 108390, 192119),
        ("Marc-André ter Stegen", 1992, "GER", "Barcelona", "6f5ec9e2", 2090, 96144, 74857, 192448),
    ]
    fbref_recs = [
        {
            "player_name": cname,
            "birth_year": byear,
            "nationality_iso": nat,
            "team_name": tname,
            "fbref_hex_id": fb_id,
            "understat_id": us_id,
            "sofascore_id": ss_id,
            "transfermarkt_id": tm_id,
            "eafc_id": ea_id,
        }
        for cname, byear, nat, tname, fb_id, us_id, ss_id, tm_id, ea_id in modern_2022_2026_seeds
    ]
    return resolver.link_external_records(fbref_recs, source_id_field="fbref_hex_id")


# ---------------------------------------------------------------------------
# 5. PyTorch Training Tensor Bundle Builder (`packed_shards_v2.pt`)
# ---------------------------------------------------------------------------

def _unpack_modifier_flags_8(bitmask: int) -> List[float]:
    return [float((bitmask >> b) & 1) for b in range(8)]


def _build_event_state_64d(ev: EventGoldV2) -> np.ndarray:
    """Construct the 64-dim contextual state vector `s_t` matching `PixelPitchEventTF` (`state_dim=64`)."""
    vec = np.zeros(64, dtype=np.float32)
    vec[0] = float(ev.period) / 5.0
    vec[1] = float(ev.minute) / 95.0
    vec[2] = float(ev.second) / 60.0
    vec[3] = float(ev.score_home - ev.score_away) / 5.0
    vec[4] = float(ev.score_home) / 5.0
    vec[5] = float(ev.score_away) / 5.0
    vec[6] = 1.0 if ev.is_home_actor else 0.0
    vec[7] = float(ev.x_canon) / PITCH_LENGTH_YD
    vec[8] = float(ev.y_canon) / PITCH_WIDTH_YD
    vec[9] = float(ev.end_x_canon) / PITCH_LENGTH_YD
    vec[10] = float(ev.end_y_canon) / PITCH_WIDTH_YD
    vec[11] = float(ev.x_att) / PITCH_LENGTH_YD
    vec[12] = float(ev.y_att) / PITCH_WIDTH_YD
    vec[13] = float(ev.poss_phase) / 7.0
    vec[14] = float(ev.restart_type) / 7.0
    vec[15] = 1.0 if ev.under_pressure else 0.0
    vec[16] = float(ev.ff_closest_defender_dist) / 30.0
    vec[17] = float(ev.ff_n_pressers_3m) / 4.0
    vec[18] = float(ev.ff_open_goal_fraction)
    vec[19] = float(ev.xt_start)
    vec[20] = float(ev.xt_end)
    vec[21] = float(ev.xt_delta)
    vec[22] = float(ev.xg_v2_pre_shot)
    vec[23] = float(ev.xpass_prob)
    return vec


def _extract_22_slot_freeze_frame(ev: EventGoldV2) -> Tuple[np.ndarray, np.ndarray, np.ndarray]:
    """
    Convert an `EventGoldV2`'s 360 freeze frame (or canonical tactical anchor fallback)
    into:
      - `ff_22_7`: `[22, 7]` (`[x, y, vx, vy, is_teammate, is_actor, is_gk]` in canonical `[0,120]x[0,80]`)
      - `anchors_22_2`: `[22, 2]` Layer-1 phase anchors in canonical `[0,120]x[0,80]`
      - `slot_feats_22_18`: `[22, 18]` input tensor for Module B `SpatialPredictor(in_features=18)`
    """
    is_home = bool(ev.is_home_actor)
    ball_x_c, ball_y_c = float(ev.x_canon), float(ev.y_canon)
    end_x_c, end_y_c = float(ev.end_x_canon), float(ev.end_y_canon)

    # Layer-1 anchors in canonical coordinates
    shift_x = (ball_x_c - 60.0) * 0.30
    shift_y = (ball_y_c - 40.0) * 0.24
    home_anchors = _CANONICAL_HOME_11_XY.copy()
    home_anchors[1:, 0] = np.clip(home_anchors[1:, 0] + shift_x, 2.0, 118.0)
    home_anchors[1:, 1] = np.clip(home_anchors[1:, 1] + shift_y, 2.0, 78.0)
    away_anchors = np.stack([PITCH_LENGTH_YD - _CANONICAL_HOME_11_XY[:, 0], PITCH_WIDTH_YD - _CANONICAL_HOME_11_XY[:, 1]], axis=-1)
    away_anchors[1:, 0] = np.clip(away_anchors[1:, 0] + shift_x, 2.0, 118.0)
    away_anchors[1:, 1] = np.clip(away_anchors[1:, 1] + shift_y, 2.0, 78.0)
    anchors_22_2 = np.concatenate([home_anchors, away_anchors], axis=0).astype(np.float32)

    positions_22_2 = anchors_22_2.copy()
    raw_ff = ev.freeze_frame_360 if ev.freeze_frame_360 else ev.shot_freeze_frame
    if raw_ff:
        tm_pts: List[Tuple[float, float, bool, bool]] = []
        opp_pts: List[Tuple[float, float, bool]] = []
        for p in raw_ff:
            if isinstance(p, dict):
                loc = p.get("location")
                if loc and len(loc) >= 2:
                    px_att, py_att = float(loc[0]), float(loc[1])
                    px_c = px_att if is_home else (PITCH_LENGTH_YD - px_att)
                    py_c = py_att if is_home else (PITCH_WIDTH_YD - py_att)
                else:
                    px_c = float(p.get("x_canon", ball_x_c))
                    py_c = float(p.get("y_canon", ball_y_c))
                is_tm = bool(p.get("teammate", False))
                is_act = bool(p.get("actor", False))
                is_gk = bool(p.get("keeper", p.get("is_gk", False)))
            else:
                px_c = float(p.x_canon)
                py_c = float(p.y_canon)
                is_tm = bool(p.teammate)
                is_act = bool(getattr(p, "actor", False))
                is_gk = bool(getattr(p, "keeper", getattr(p, "is_gk", False)))

            px_c = float(np.clip(px_c, 0.5, 119.5))
            py_c = float(np.clip(py_c, 0.5, 79.5))
            if is_tm:
                tm_pts.append((px_c, py_c, is_act, is_gk))
            else:
                opp_pts.append((px_c, py_c, is_gk))

        tm_offset = 0 if is_home else 11
        opp_offset = 11 if is_home else 0
        for idx_p, (px_c, py_c, _, is_gk) in enumerate(tm_pts[:11]):
            slot_i = tm_offset + (0 if is_gk else min(10, idx_p + (0 if idx_p > 0 else 1)))
            positions_22_2[slot_i] = [px_c, py_c]
        for idx_p, (px_c, py_c, is_gk) in enumerate(opp_pts[:11]):
            slot_i = opp_offset + (0 if is_gk else min(10, idx_p + (0 if idx_p > 0 else 1)))
            positions_22_2[slot_i] = [px_c, py_c]

    actor_idx = max(0, min(21, int(ev.actor_slot) - 1))
    positions_22_2[actor_idx] = [ball_x_c, ball_y_c]

    ff_22_7 = np.zeros((22, 7), dtype=np.float32)
    ff_22_7[:, 0:2] = positions_22_2
    # Small plausible velocities toward end_xy
    ff_22_7[:, 2] = np.clip((end_x_c - ball_x_c) * 0.08, -4.0, 4.0)
    ff_22_7[:, 3] = np.clip((end_y_c - ball_y_c) * 0.08, -4.0, 4.0)
    if is_home:
        ff_22_7[:11, 4] = 1.0
    else:
        ff_22_7[11:, 4] = 1.0
    ff_22_7[actor_idx, 5] = 1.0
    ff_22_7[0, 6] = 1.0
    ff_22_7[11, 6] = 1.0

    # Build 18-dim slot_features matching OffBallSpatialSolver.step
    slot_feats = np.zeros((22, 18), dtype=np.float32)
    scale_xy = np.array([PITCH_LENGTH_YD, PITCH_WIDTH_YD], dtype=np.float32)
    slot_feats[:, 0:2] = anchors_22_2 / scale_xy
    slot_feats[:, 2:4] = anchors_22_2 / scale_xy
    slot_feats[:, 4] = ball_x_c / PITCH_LENGTH_YD
    slot_feats[:, 5] = ball_y_c / PITCH_WIDTH_YD
    slot_feats[:, 6] = end_x_c / PITCH_LENGTH_YD
    slot_feats[:, 7] = end_y_c / PITCH_WIDTH_YD
    slot_feats[:11, 8] = 1.0  # is_home
    slot_feats[0, 9] = 1.0    # is_gk
    slot_feats[11, 9] = 1.0
    slot_feats[:, 10] = max(0.35, 1.0 - float(ev.minute) / 130.0)  # stamina
    slot_feats[:, 11] = 0.5   # freedom_to_roam
    slot_feats[:, 12] = 0.5   # role_forward_bias
    slot_feats[:, 13] = float(min(3, int(ev.poss_phase))) / 3.0
    slot_feats[:, 14] = 1.0   # active_mask
    if is_home:
        slot_feats[:11, 15] = 1.0
    else:
        slot_feats[11:, 15] = 1.0
    slot_feats[:, 16] = min(1.0, max(0.01, float(ev.dt_sec) / 10.0))
    slot_feats[:, 17] = 0.55  # pressing intensity

    return ff_22_7, anchors_22_2, slot_feats


def build_packed_shards_pt(
    events: Sequence[EventGoldV2],
    profiles: Sequence[PlayerProfileV2],
    splits: Dict[str, List[int]],
    out_path: Path,
) -> Dict[str, Any]:
    """
    Build and save `packed_shards_v2.pt` containing:
      1. `packed_blocks`: 16,384-step FlashAttention varlen blocks with `cu_seqlens`
      2. `event_sequences`: per-match teacher-forced input/target dicts ready for `PixelPitchEventTF`
         and `compute_multitask_pretrain_loss` + Stage-2 IPW propensities
      3. `spatial_360`: 360 freeze-frame tensors for Module B `SpatialPredictor`
      4. `outcome_distillation`: shot/pass/VAEP feature & teacher tensors for `ModuleDOutcomeSuite`
    """
    profile_by_id: Dict[int, np.ndarray] = {
        p.player_id: np.asarray(p.latent_style_64d, dtype=np.float32) for p in profiles
    }
    default_style = np.zeros(64, dtype=np.float32)

    by_match: Dict[int, List[EventGoldV2]] = {}
    for ev in events:
        by_match.setdefault(ev.match_id, []).append(ev)
    for mid in by_match:
        by_match[mid].sort(key=lambda e: e.event_seq_idx)

    # Empirical action marginal distribution for Stage 2 IPW weights
    action_counts = np.ones(18, dtype=np.float64)
    for ev in events:
        action_counts[int(ev.action_type) % 18] += 1.0
    action_marginals = (action_counts / action_counts.sum()).astype(np.float32)

    # 1. Packed 16k blocks per split
    packed_blocks_payload: Dict[str, List[Dict[str, Any]]] = {}
    for split_name, mids in splits.items():
        subset = {m: by_match[m] for m in mids if m in by_match}
        blocks = pack_sequences_16k(subset, split_name=split_name, max_seq_len=MAX_CONTEXT_LEN)
        packed_blocks_payload[split_name] = [
            {
                "block_id": blk.block_id,
                "split": blk.split,
                "match_ids": blk.match_ids,
                "cu_seqlens": torch.tensor(blk.cu_seqlens, dtype=torch.int32),
                "discrete_tokens": torch.from_numpy(blk.discrete_tokens),
                "continuous_features": torch.from_numpy(blk.continuous_features),
            }
            for blk in blocks
        ]

    # 2. Per-match sequences for `PixelPitchEventTF` & `compute_multitask_pretrain_loss`
    split_lookup: Dict[int, str] = {}
    for sp, mids in splits.items():
        for m in mids:
            split_lookup[m] = sp

    event_sequences: List[Dict[str, Any]] = []
    for mid, evs in sorted(by_match.items()):
        if len(evs) < 4:
            continue
        T = len(evs)
        disc_rows: List[List[int]] = []
        cont_rows: List[List[float]] = []
        flags_rows: List[List[float]] = []
        s_t_rows: List[np.ndarray] = []

        for ev in evs:
            d, c = encode_event_step(ev)
            disc_rows.append(d)
            cont_rows.append(c)
            flags_rows.append(_unpack_modifier_flags_8(ev.modifier_bitmask_8))
            s_t_rows.append(_build_event_state_64d(ev))

        disc_arr = np.asarray(disc_rows, dtype=np.int64)      # [T, 14]
        cont_arr = np.asarray(cont_rows, dtype=np.float32)    # [T, 10]
        flags_arr = np.asarray(flags_rows, dtype=np.float32)  # [T, 8]
        s_t_arr = np.stack(s_t_rows, axis=0)                  # [T, 64]

        # Build 22-slot player conditioning matrix [22, 64] from match starting slots
        slot_pids = evs[0].active_slots_player_ids
        p_cond = np.stack(
            [profile_by_id.get(int(pid), default_style) for pid in slot_pids[:22]],
            axis=0,
        ).astype(np.float32)

        # Next-event autoregressive teacher-forcing: inputs = [0..T-2], targets = [1..T-1]
        in_d = disc_arr[:-1]
        in_c = cont_arr[:-1]
        in_f = flags_arr[:-1]
        tg_d = disc_arr[1:]
        tg_c = cont_arr[1:]
        tg_f = flags_arr[1:]

        # Clamp outcome to [0..20] for Module C's 21-class outcome head
        in_outcome = np.clip(in_d[:, 13], 0, 20)
        tg_outcome = np.clip(tg_d[:, 13], 0, 20)
        in_action = np.clip(in_d[:, 4], 0, 17)
        tg_action = np.clip(tg_d[:, 4], 0, 17)

        inputs_dict = {
            "dt_bucket": torch.from_numpy(in_d[:, 0]),
            "period": torch.from_numpy(in_d[:, 1]),
            "poss_phase": torch.from_numpy(in_d[:, 2]),
            "actor_slot": torch.from_numpy(in_d[:, 3]),
            "action_type": torch.from_numpy(in_action),
            "start_cell": torch.from_numpy(in_d[:, 5]),
            "end_cell": torch.from_numpy(in_d[:, 6]),
            "end_z_bin": torch.from_numpy(in_d[:, 7]),
            "target_slot": torch.from_numpy(in_d[:, 8]),
            "restart_type": torch.from_numpy(in_d[:, 9]),
            "pass_height": torch.from_numpy(in_d[:, 10]),
            "body_part": torch.from_numpy(in_d[:, 11]),
            "outcome": torch.from_numpy(in_outcome),
            "flags": torch.from_numpy(in_f),
            "dt_res": torch.from_numpy(in_c[:, 0]),
            "start_offset": torch.from_numpy(in_c[:, 1:3]),
            "end_offset": torch.from_numpy(in_c[:, 3:5]),
        }
        targets_dict = {
            "dt_bucket": torch.from_numpy(tg_d[:, 0]),
            "actor_slot": torch.from_numpy(tg_d[:, 3]),
            "action_type": torch.from_numpy(tg_action),
            "pass_height": torch.from_numpy(tg_d[:, 10]),
            "body_part": torch.from_numpy(tg_d[:, 11]),
            "flags": torch.from_numpy(tg_f),
            "start_cell": torch.from_numpy(tg_d[:, 5]),
            "end_cell": torch.from_numpy(tg_d[:, 6]),
            "end_z_bin": torch.from_numpy(tg_d[:, 7]),
            "target_slot": torch.from_numpy(tg_d[:, 8]),
            "outcome": torch.from_numpy(tg_outcome),
            "dt_res_target": torch.from_numpy(tg_c[:, 0]),
            "start_offset_target": torch.from_numpy(tg_c[:, 1:3]),
            "end_offset_target": torch.from_numpy(tg_c[:, 3:5]),
            "aux_analytics_target": torch.from_numpy(tg_c[:, 5:10]),
        }
        # Build empirical [2, 18] team_tactics tensor aligned with inference rollout_engine.py ([:, :10] = 10D sliders)
        home_tids = sorted({ev.actor_team_id for ev in evs if ev.is_home_actor and ev.actor_team_id > 0})
        away_tids = sorted({ev.actor_team_id for ev in evs if (not ev.is_home_actor) and ev.actor_team_id > 0})
        team_tac_2_18 = np.zeros((2, 18), dtype=np.float32)
        team_tac_2_18[:, :10] = 0.5
        team_tac_2_18[:, 12:18] = 0.5
        if home_tids:
            team_tac_2_18[0, :10] = np.asarray(
                extract_team_match_tactics(evs, match_id=mid, team_id=home_tids[0], half=1).as_vector_10d(),
                dtype=np.float32,
            )
        if away_tids:
            team_tac_2_18[1, :10] = np.asarray(
                extract_team_match_tactics(evs, match_id=mid, team_id=away_tids[0], half=1).as_vector_10d(),
                dtype=np.float32,
            )

        propensity = torch.from_numpy(action_marginals[tg_action])
        event_sequences.append(
            {
                "match_id": int(mid),
                "split": split_lookup.get(mid, "train"),
                "length": int(T - 1),
                "inputs": inputs_dict,
                "targets": targets_dict,
                "state_features": torch.from_numpy(s_t_arr[:-1]),
                "player_cond": torch.from_numpy(p_cond),
                "team_tactics": torch.from_numpy(team_tac_2_18),
                "action_propensity": propensity,
            }
        )

    # 3. Spatial 360 Samples (`SpatialPredictor`) & 4. Outcome Distillation Samples (`ModuleDOutcomeSuite`)
    sp_slot_feats: List[np.ndarray] = []
    sp_active_mask: List[np.ndarray] = []
    sp_target_delta: List[np.ndarray] = []
    sp_ff_22_7: List[np.ndarray] = []
    sp_ball_xy: List[List[float]] = []
    sp_end_xy: List[List[float]] = []
    sp_end_z: List[float] = []
    sp_poss_team: List[int] = []
    sp_action: List[int] = []
    sp_outcome: List[int] = []

    shot_ff_list: List[np.ndarray] = []
    shot_start_xy: List[List[float]] = []
    shot_end_xy: List[List[float]] = []
    shot_end_z: List[float] = []
    shot_poss_team: List[int] = []
    shot_body_part: List[int] = []
    shot_restart: List[int] = []
    shot_flags: List[List[float]] = []
    shot_teacher_xg: List[float] = []
    shot_teacher_psxg: List[float] = []
    shot_outcome: List[int] = []

    pass_ff_list: List[np.ndarray] = []
    pass_start_xy: List[List[float]] = []
    pass_end_xy: List[List[float]] = []
    pass_poss_team: List[int] = []
    pass_height_list: List[int] = []
    pass_restart_list: List[int] = []
    pass_flags_list: List[List[float]] = []
    pass_teacher_xpass: List[float] = []
    pass_outcome_list: List[int] = []

    max_360_samples = 8_000
    max_pass_samples = 12_000

    for ev in events:
        is_shot = ev.action_type == ActionType.SHOT
        is_pass = ev.action_type == ActionType.PASS
        if not (ev.has_360_frame or is_shot or (is_pass and len(pass_ff_list) < max_pass_samples)):
            continue

        ff_22_7, anchors_22_2, slot_feats_22_18 = _extract_22_slot_freeze_frame(ev)
        poss_side = 1 if ev.is_home_actor else 2
        flags_8 = _unpack_modifier_flags_8(ev.modifier_bitmask_8)

        if (ev.has_360_frame or len(sp_slot_feats) < 512) and len(sp_slot_feats) < max_360_samples:
            delta_xy = np.clip(ff_22_7[:, 0:2] - anchors_22_2, -6.0, 6.0)
            sp_slot_feats.append(slot_feats_22_18)
            sp_active_mask.append(np.ones(22, dtype=bool))
            sp_target_delta.append(delta_xy)
            sp_ff_22_7.append(ff_22_7)
            sp_ball_xy.append([float(ev.x_canon), float(ev.y_canon)])
            sp_end_xy.append([float(ev.end_x_canon), float(ev.end_y_canon)])
            sp_end_z.append(float(ev.end_z_m))
            sp_poss_team.append(poss_side)
            sp_action.append(int(ev.action_type))
            sp_outcome.append(min(20, int(ev.outcome)))

        if is_shot:
            shot_ff_list.append(ff_22_7)
            shot_start_xy.append([float(ev.x_canon), float(ev.y_canon)])
            shot_end_xy.append([float(ev.end_x_canon), float(ev.end_y_canon)])
            shot_end_z.append(float(ev.end_z_m))
            shot_poss_team.append(poss_side)
            shot_body_part.append(int(ev.body_part))
            shot_restart.append(int(ev.restart_type))
            shot_flags.append(flags_8)
            shot_teacher_xg.append(float(ev.xg_v2_pre_shot))
            shot_teacher_psxg.append(float(ev.psxg_v2))
            shot_outcome.append(min(20, int(ev.outcome)))
        elif is_pass and len(pass_ff_list) < max_pass_samples:
            pass_ff_list.append(ff_22_7)
            pass_start_xy.append([float(ev.x_canon), float(ev.y_canon)])
            pass_end_xy.append([float(ev.end_x_canon), float(ev.end_y_canon)])
            pass_poss_team.append(poss_side)
            pass_height_list.append(int(ev.pass_height))
            pass_restart_list.append(int(ev.restart_type))
            pass_flags_list.append(flags_8)
            pass_teacher_xpass.append(float(ev.xpass_prob))
            pass_outcome_list.append(min(20, int(ev.outcome)))

    spatial_360_bundle = {
        "slot_features": torch.from_numpy(np.stack(sp_slot_feats, axis=0)) if sp_slot_feats else torch.zeros((0, 22, 18)),
        "active_mask": torch.from_numpy(np.stack(sp_active_mask, axis=0)) if sp_active_mask else torch.ones((0, 22), dtype=torch.bool),
        "target_delta_xy": torch.from_numpy(np.stack(sp_target_delta, axis=0)) if sp_target_delta else torch.zeros((0, 22, 2)),
        "freeze_frame_360": torch.from_numpy(np.stack(sp_ff_22_7, axis=0)) if sp_ff_22_7 else torch.zeros((0, 22, 7)),
        "ball_xy": torch.tensor(sp_ball_xy, dtype=torch.float32),
        "end_xy": torch.tensor(sp_end_xy, dtype=torch.float32),
        "end_z": torch.tensor(sp_end_z, dtype=torch.float32),
        "poss_team": torch.tensor(sp_poss_team, dtype=torch.long),
        "action_type": torch.tensor(sp_action, dtype=torch.long),
        "outcome": torch.tensor(sp_outcome, dtype=torch.long),
    }

    outcome_distillation_bundle = {
        "shot_freeze_frame": torch.from_numpy(np.stack(shot_ff_list, axis=0)) if shot_ff_list else torch.zeros((0, 22, 7)),
        "shot_start_xy": torch.tensor(shot_start_xy, dtype=torch.float32),
        "shot_end_xy": torch.tensor(shot_end_xy, dtype=torch.float32),
        "shot_end_z": torch.tensor(shot_end_z, dtype=torch.float32),
        "shot_poss_team": torch.tensor(shot_poss_team, dtype=torch.long),
        "shot_body_part": torch.tensor(shot_body_part, dtype=torch.long),
        "shot_restart_type": torch.tensor(shot_restart, dtype=torch.long),
        "shot_flags": torch.tensor(shot_flags, dtype=torch.float32),
        "shot_teacher_xg": torch.tensor(shot_teacher_xg, dtype=torch.float32),
        "shot_teacher_psxg": torch.tensor(shot_teacher_psxg, dtype=torch.float32),
        "shot_outcome": torch.tensor(shot_outcome, dtype=torch.long),
        "pass_freeze_frame": torch.from_numpy(np.stack(pass_ff_list, axis=0)) if pass_ff_list else torch.zeros((0, 22, 7)),
        "pass_start_xy": torch.tensor(pass_start_xy, dtype=torch.float32),
        "pass_end_xy": torch.tensor(pass_end_xy, dtype=torch.float32),
        "pass_poss_team": torch.tensor(pass_poss_team, dtype=torch.long),
        "pass_height": torch.tensor(pass_height_list, dtype=torch.long),
        "pass_restart_type": torch.tensor(pass_restart_list, dtype=torch.long),
        "pass_flags": torch.tensor(pass_flags_list, dtype=torch.float32),
        "pass_teacher_xpass": torch.tensor(pass_teacher_xpass, dtype=torch.float32),
        "pass_outcome": torch.tensor(pass_outcome_list, dtype=torch.long),
    }

    # Concatenate top-level discrete_tokens [N, 14] & continuous_features [N, 10] for direct batch slicing
    all_disc_list: List[torch.Tensor] = []
    all_cont_list: List[torch.Tensor] = []
    for sp_blocks in packed_blocks_payload.values():
        for b_dict in sp_blocks:
            all_disc_list.append(b_dict["discrete_tokens"].long())
            all_cont_list.append(b_dict["continuous_features"].float())
    top_discrete_tokens = torch.cat(all_disc_list, dim=0) if all_disc_list else torch.zeros((0, 14), dtype=torch.long)
    top_continuous_features = torch.cat(all_cont_list, dim=0) if all_cont_list else torch.zeros((0, 10), dtype=torch.float32)

    bundle = {
        "schema_version": "2.0.0",
        "splits": splits,
        "action_marginals": torch.from_numpy(action_marginals),
        "discrete_tokens": top_discrete_tokens,
        "continuous_features": top_continuous_features,
        "num_360_frames": int(sum(1 for ev in events if ev.has_360_frame)),
        "spatial_360_features": spatial_360_bundle["slot_features"],
        "spatial_360_targets": spatial_360_bundle["target_delta_xy"],
        "packed_blocks": packed_blocks_payload,
        "event_sequences": event_sequences,
        "spatial_360": spatial_360_bundle,
        "outcome_distillation": outcome_distillation_bundle,
    }
    out_path.parent.mkdir(parents=True, exist_ok=True)
    torch.save(bundle, out_path)
    return {
        "num_event_sequences": len(event_sequences),
        "num_packed_blocks": sum(len(v) for v in packed_blocks_payload.values()),
        "num_spatial_360_samples": len(sp_slot_feats),
        "num_shot_samples": len(shot_ff_list),
        "num_pass_samples": len(pass_ff_list),
    }


# ---------------------------------------------------------------------------
# 6. End-to-End Orchestrator (`build_v2_lakehouse`)
# ---------------------------------------------------------------------------

def build_v2_lakehouse(
    open_data_dir: Optional[Path] = None,
    wyscout_dir: Optional[Path] = None,
    scraped_cache_dir: Optional[Path] = None,
    out_dir: Path = Path("./models_v2"),
    max_matches: Optional[int] = None,
    preset: Optional[str] = None,
) -> Dict[str, Any]:
    """Execute the end-to-end Pillar 1 data lakehouse and training bundle build."""
    t0 = time.perf_counter()
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    # Step 1: Ingest StatsBomb Open Data (or local Matches/*.txt + archive/Matches/*.txt fallback)
    sb_root = Path(open_data_dir) if open_data_dir is not None else (SIM_ENGINE_ROOT / "open-data")
    if (sb_root / "data" / "competitions.json").exists():
        source_mode = f"statsbomb_open_data_repo:{preset}" if preset else "statsbomb_open_data_repo"
        sb_events, match_meta, player_meta = ingest_statsbomb_open_data_smart(
            sb_root, max_matches=max_matches, preset=preset
        )
    else:
        source_mode = "local_matches_txt_fallback"
        sb_events, match_meta, player_meta = ingest_local_matches_fallback(
            sb_root, max_matches=max_matches
        )

    # Step 2: Ingest Wyscout Open Data
    wy_events, wy_meta = ingest_wyscout_data(
        Path(wyscout_dir) if wyscout_dir is not None else None,
        max_matches=max_matches,
    )
    all_events: List[EventGoldV2] = list(sb_events)
    all_events.extend(wy_events)
    del sb_events, wy_events
    import gc
    gc.collect()
    match_meta.update(wy_meta)

    # Step 3: Fit Expected Threat (24x16) & Tier-1 Teacher Models (xG, PSxG, xPass, VAEP)
    # Subsample teacher fitting to <=300k events (~115 matches, 360-first) to avoid 25GB+ RAM spikes on 2M+ event corpora.
    fit_events = all_events if len(all_events) <= 300_000 else all_events[:300_000]
    print(f"[Step 3] Fitting teacher models on {len(fit_events):,} events (enriching all {len(all_events):,})...", flush=True)
    xt_model = ExpectedThreatModel().fit(fit_events)
    xt_model.apply_to_events(all_events)
    xt_path = xt_model.save_npy(out_dir / "xt_grid_24x16.npy")

    xg_model = PreShotXGModel().fit(fit_events)
    psxg_model = PostShotXGModel(pre_xg_model=xg_model).fit(fit_events)
    xpass_model = XPassModel().fit(fit_events)
    vaep_model = VAEPModel().fit(fit_events)
    del fit_events
    gc.collect()
    enrich_events_with_labels(
        all_events,
        xt_model=xt_model,
        xg_model=xg_model,
        psxg_model=psxg_model,
        xpass_model=xpass_model,
        vaep_model=vaep_model,
    )
    print(f"[Step 3 Done] Enriched {len(all_events):,} events.", flush=True)

    # Step 4: Export `events_gold_v2.parquet` in low-RAM 100k-row chunks via PyArrow ParquetWriter
    import gc
    print(f"[Step 4] Exporting {len(all_events):,} events to events_gold_v2.parquet...", flush=True)
    events_pq_path = out_dir / "events_gold_v2.parquet"
    if PYARROW_AVAILABLE and len(all_events) > 0:
        import pyarrow as pa
        import pyarrow.parquet as pq

        chunk_size = 100_000
        writer = None
        try:
            for start_i in range(0, len(all_events), chunk_size):
                chunk_df = LakehouseEngine.events_to_dataframe(all_events[start_i : start_i + chunk_size])
                tbl = pa.Table.from_pandas(chunk_df, preserve_index=False)
                if writer is None:
                    writer = pq.ParquetWriter(str(events_pq_path), tbl.schema, compression="zstd")
                writer.write_table(tbl)
                del chunk_df, tbl
        finally:
            if writer is not None:
                writer.close()
        gc.collect()
    else:
        lakehouse = LakehouseEngine()
        lakehouse.register_events(all_events, table_name="events_gold_v2")
        events_pq_path = lakehouse.export_table("events_gold_v2", events_pq_path)
        lakehouse.close()
    print(f"[Step 4 Done] Wrote {events_pq_path.name} ({events_pq_path.stat().st_size / 1e6:.1f} MB)", flush=True)

    # Step 5: Entity Resolution -> `player_id_crosswalk.parquet`
    crosswalks = build_player_crosswalk(
        all_events,
        player_metadata=player_meta,
        scraped_cache_dir=Path(scraped_cache_dir) if scraped_cache_dir is not None else None,
    )
    cw_df = pd.DataFrame([cw.model_dump() for cw in crosswalks])
    cw_path = out_dir / "player_id_crosswalk.parquet"
    if PYARROW_AVAILABLE:
        cw_df.to_parquet(cw_path, compression="zstd", index=False)
    else:
        cw_df.to_json(cw_path, orient="records", lines=True)

    # Step 6: Historical Player Capability & Role Profiler -> `player_profiles_v2.parquet`
    profiler = HistoricalPlayerProfiler(shrinkage_m0_minutes=900.0, decay_half_life_days=540.0)
    match_dates = {mid: str(m.get("match_date", "2023-05-01")) for mid, m in match_meta.items()}
    player_names = {
        int(pid): str(meta.get("canonical_name"))
        for pid, meta in player_meta.items()
        if meta.get("canonical_name")
    }
    profiles = profiler.build_profiles(
        all_events,
        match_dates=match_dates,
        player_names=player_names,
        as_of_date="2026-06-01",
    )
    profiles_path = export_profiles_parquet(profiles, out_dir / "player_profiles_v2.parquet")

    # Step 7: Historical Team Tactical Manifold -> `tactical_manifold_v2.pkl`
    by_match_evs: Dict[int, List[EventGoldV2]] = {}
    for ev in all_events:
        by_match_evs.setdefault(ev.match_id, []).append(ev)
    tactics_rows = []
    for mid, m_evs in sorted(by_match_evs.items()):
        team_ids = sorted({ev.actor_team_id for ev in m_evs if ev.actor_team_id > 0})
        for tid in team_ids:
            for half_idx in (1, 2):
                tactics_rows.append(extract_team_match_tactics(m_evs, match_id=mid, team_id=tid, half=half_idx))
    manifold = TacticalManifoldV2().fit(tactics_rows)
    manifold_path = manifold.save(out_dir / "tactical_manifold_v2.pkl")

    # Step 8: Match-Grouped Splits, Validation Gates & `packed_shards_v2.pt`
    all_mids = sorted({ev.match_id for ev in all_events})
    splits = assign_match_splits(all_mids, match_metadata=match_meta)
    gates_report = run_all_gates(all_events, profiles, splits)

    shards_pt_path = out_dir / "packed_shards_v2.pt"
    shards_stats = build_packed_shards_pt(all_events, profiles, splits, shards_pt_path)

    frames_360_count = sum(1 for ev in all_events if ev.has_360_frame)
    elapsed_sec = round(time.perf_counter() - t0, 3)

    manifest = {
        "schema_version": "2.0.0",
        "source_mode": source_mode,
        "duckdb_backend": DUCKDB_AVAILABLE,
        "pyarrow_backend": PYARROW_AVAILABLE,
        "match_count": len(all_mids),
        "event_count": len(all_events),
        "frames_360_count": frames_360_count,
        "players_profiled": len(profiles),
        "crosswalk_count": len(crosswalks),
        "team_match_tactics_count": len(tactics_rows),
        "splits_match_counts": {k: len(v) for k, v in splits.items()},
        "shards_stats": shards_stats,
        "validation_passed": bool(gates_report.passed),
        "validation_errors": list(gates_report.errors),
        "elapsed_seconds": elapsed_sec,
        "artifacts": {
            "events_gold_v2": str(events_pq_path.name),
            "player_id_crosswalk": str(cw_path.name),
            "player_profiles_v2": str(profiles_path.name),
            "tactical_manifold_v2": str(manifold_path.name),
            "xt_grid_24x16": str(xt_path.name),
            "packed_shards_v2": str(shards_pt_path.name),
        },
    }
    manifest_path = out_dir / "lakehouse_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build PixelPitchAI v2 Gold Lakehouse & PyTorch Training Shards"
    )
    parser.add_argument(
        "--open-data-dir",
        type=Path,
        default=None,
        help="Path to cloned statsbomb/open-data repo (falls back to local Matches/*.txt if absent)",
    )
    parser.add_argument(
        "--wyscout-dir",
        type=Path,
        default=None,
        help="Optional path to Wyscout open data JSON directory",
    )
    parser.add_argument(
        "--scraped-cache-dir",
        type=Path,
        default=None,
        help="Optional path to 2022-2026 FBref/Understat/SofaScore/Transfermarkt JSON cache directory",
    )
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("./models_v2"),
        help="Output directory for v2 data & model artifacts (default: ./models_v2)",
    )
    parser.add_argument(
        "--max-matches",
        type=int,
        default=None,
        help="Optional maximum number of matches to ingest (prioritizes 360 matches first)",
    )
    parser.add_argument(
        "--preset",
        type=str,
        default=None,
        choices=sorted(STATSBOMB_FILTER_PRESETS.keys()),
        help="Curated StatsBomb Open Data filter preset (mens_top_360_core=293, mens_big5_and_top_360=1844)",
    )
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    manifest = build_v2_lakehouse(
        open_data_dir=args.open_data_dir,
        wyscout_dir=args.wyscout_dir,
        scraped_cache_dir=args.scraped_cache_dir,
        out_dir=args.out_dir,
        max_matches=args.max_matches,
        preset=args.preset,
    )
    print(json.dumps(manifest, indent=2))
    if not manifest.get("validation_passed", False):
        print("ERROR: Pillar 1 validation gates failed!", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
