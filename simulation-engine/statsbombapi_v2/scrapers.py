"""
Polite Async/Cached 2022-2026 Web Scraping Adapters & Unified Entity Resolution.

Implements Pillar 1 §2.5:
- Polite token-bucket rate limiting + immutable local disk caching + strict non-CAPTCHA guardrails
- Adapters for FBref, Understat, SofaScore/FotMob/WhoScored, and Transfermarkt/EA FC
- Unified Entity Resolver (`EntityResolver` -> `PlayerIdCrosswalk`) using Unicode NFKD normalization,
  club/birth-year blocking, Jaro-Winkler similarity, and Hungarian bipartite assignment (`scipy`).
"""

from __future__ import annotations

import asyncio
import hashlib
import json
import math
import re
import time
import unicodedata
from html.parser import HTMLParser
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import aiohttp
import numpy as np
from scipy.optimize import linear_sum_assignment

from statsbombapi_v2.schemas import (
    ActionType,
    BodyPart,
    EventGoldV2,
    EventOutcome,
    MatchPeriod,
    PITCH_LENGTH_YD,
    PITCH_WIDTH_YD,
    PlayerIdCrosswalk,
    PossPhase,
    RestartType,
    pack_modifier_bitmask,
    quantize_dt_bucket,
    quantize_pitch_cell,
    rescale_wyscout,
    to_canonical_global,
)


class ScraperBlockedError(RuntimeError):
    """Raised when an upstream site gates a request behind CAPTCHA, paywall, or 403/429 block."""


class AsyncTokenBucket:
    """Per-domain polite rate limiter with jittered delay."""

    def __init__(self, min_interval_sec: float = 2.5) -> None:
        self.min_interval_sec = max(0.0, float(min_interval_sec))
        self._last_request_at: Dict[str, float] = {}
        self._locks: Dict[str, asyncio.Lock] = {}

    async def acquire(self, domain: str) -> None:
        if self.min_interval_sec <= 0.0:
            return
        lock = self._locks.setdefault(domain, asyncio.Lock())
        async with lock:
            now = time.monotonic()
            last = self._last_request_at.get(domain, 0.0)
            wait_sec = (last + self.min_interval_sec) - now
            if wait_sec > 0:
                await asyncio.sleep(wait_sec)
            self._last_request_at[domain] = time.monotonic()


class PoliteScraperSession:
    """
    Cached async HTTP scraper with strict access-control & anti-bot compliance.
    Never attempts CAPTCHA solving or authentication bypasses; halts gracefully on gated responses.
    """

    CAPTCHA_MARKERS = (
        "cf-chl-bypass",
        "captcha",
        "g-recaptcha",
        "hcaptcha",
        "verify you are human",
        "access denied",
    )

    def __init__(
        self,
        cache_dir: Optional[Path] = None,
        min_interval_sec: float = 2.5,
        user_agent: str = "PixelPitchAI-ResearchBot/2.0 (respectful academic/analytics cache)",
    ) -> None:
        self.cache_dir = Path(cache_dir) if cache_dir else None
        if self.cache_dir:
            self.cache_dir.mkdir(parents=True, exist_ok=True)
        self.rate_limiter = AsyncTokenBucket(min_interval_sec=min_interval_sec)
        self.user_agent = user_agent

    def _cache_path(self, cache_key: str) -> Optional[Path]:
        if not self.cache_dir:
            return None
        digest = hashlib.sha256(cache_key.encode("utf-8")).hexdigest()
        return self.cache_dir / f"{digest}.json"

    def get_cached(self, cache_key: str) -> Optional[str]:
        path = self._cache_path(cache_key)
        if path and path.exists():
            payload = json.loads(path.read_text(encoding="utf-8"))
            return str(payload["body"])
        return None

    def put_cached(self, cache_key: str, url: str, body: str) -> None:
        path = self._cache_path(cache_key)
        if path:
            path.write_text(
                json.dumps({"url": url, "cached_at": time.time(), "body": body}),
                encoding="utf-8",
            )

    @classmethod
    def check_response_allowed(cls, status_code: int, body: str, domain: str) -> None:
        if status_code in (401, 403, 429):
            raise ScraperBlockedError(f"Domain {domain} returned HTTP {status_code}; halting connector gracefully.")
        sample = body[:4096].lower()
        for marker in cls.CAPTCHA_MARKERS:
            if marker in sample:
                raise ScraperBlockedError(f"Domain {domain} presented anti-bot gate ({marker}); skipping.")

    async def fetch_text(
        self,
        url: str,
        domain: str,
        cache_key: Optional[str] = None,
        immutable: bool = True,
        timeout_sec: float = 15.0,
    ) -> str:
        key = cache_key or url
        if immutable:
            cached = self.get_cached(key)
            if cached is not None:
                return cached

        await self.rate_limiter.acquire(domain)
        headers = {"User-Agent": self.user_agent, "Accept": "text/html,application/json"}
        timeout = aiohttp.ClientTimeout(total=timeout_sec)
        async with aiohttp.ClientSession(timeout=timeout, headers=headers) as session:
            async with session.get(url) as resp:
                text = await resp.text()
                self.check_response_allowed(resp.status, text, domain)
                if resp.status >= 400:
                    raise ScraperBlockedError(f"HTTP {resp.status} from {domain}")
                self.put_cached(key, url, text)
                return text


class _SimpleHTMLTableParser(HTMLParser):
    """Lightweight stdlib HTML table parser (avoids hard dependency on BeautifulSoup/lxml)."""

    def __init__(self) -> None:
        super().__init__()
        self.tables: List[Dict[str, Any]] = []
        self._in_table = False
        self._in_row = False
        self._in_cell = False
        self._current_table_id: Optional[str] = None
        self._current_rows: List[List[str]] = []
        self._current_row: List[str] = []
        self._cell_buf: List[str] = []

    def handle_starttag(self, tag: str, attrs: List[Tuple[str, Optional[str]]]) -> None:
        attr_map = dict(attrs)
        if tag == "table":
            self._in_table = True
            self._current_table_id = attr_map.get("id")
            self._current_rows = []
        elif self._in_table and tag == "tr":
            self._in_row = True
            self._current_row = []
        elif self._in_row and tag in ("td", "th"):
            self._in_cell = True
            self._cell_buf = []

    def handle_endtag(self, tag: str) -> None:
        if tag in ("td", "th") and self._in_cell:
            self._current_row.append("".join(self._cell_buf).strip())
            self._in_cell = False
        elif tag == "tr" and self._in_row:
            if any(c for c in self._current_row):
                self._current_rows.append(self._current_row)
            self._in_row = False
        elif tag == "table" and self._in_table:
            self.tables.append({"id": self._current_table_id, "rows": self._current_rows})
            self._in_table = False

    def handle_data(self, data: str) -> None:
        if self._in_cell:
            self._cell_buf.append(data)


def _safe_float(val: Any, default: float = 0.0) -> float:
    if val is None or val == "":
        return default
    try:
        return float(str(val).replace(",", "").replace("%", "").strip())
    except ValueError:
        return default


# --- Source Adapters (2022-2026 Enrichment) ---

class FBrefAdapter:
    """Parses FBref HTML scouting tables or JSON dicts into normalized per-90 player metrics."""

    @staticmethod
    def parse_scouting_html(html: str, season: str = "2024/2025") -> List[Dict[str, Any]]:
        parser = _SimpleHTMLTableParser()
        parser.feed(html)
        records: List[Dict[str, Any]] = []
        for tbl in parser.tables:
            rows = tbl["rows"]
            if len(rows) < 2:
                continue
            headers = [h.lower().strip() for h in rows[0]]
            for row in rows[1:]:
                if len(row) != len(headers):
                    continue
                item = dict(zip(headers, row))
                player_name = item.get("player") or item.get("name")
                if not player_name or player_name.lower() == "player":
                    continue
                mins = _safe_float(item.get("min") or item.get("minutes"), 0.0)
                ninety = max(mins / 90.0, 1e-3) if mins > 0 else 1.0
                records.append(
                    {
                        "source": "fbref",
                        "season": season,
                        "fbref_hex_id": item.get("player_id") or None,
                        "player_name": player_name,
                        "team_name": item.get("squad") or item.get("team") or "",
                        "position": item.get("pos") or "CM",
                        "birth_year": int(_safe_float(item.get("born"), 0)) or None,
                        "minutes_played": mins,
                        "prog_passes_per90": _safe_float(item.get("prgp")) / ninety,
                        "prog_carries_per90": _safe_float(item.get("prgc")) / ninety,
                        "npxg_per90": _safe_float(item.get("npxg")) / ninety,
                        "xag_per90": _safe_float(item.get("xag")) / ninety,
                        "pass_cmp_pct": _safe_float(item.get("cmp%"), 78.0) / 100.0,
                        "pressures_per90": _safe_float(item.get("press") or item.get("tkl+int")) / ninety,
                        "tackles_won_per90": _safe_float(item.get("tklw")) / ninety,
                        "interceptions_per90": _safe_float(item.get("int")) / ninety,
                        "aerial_win_pct": _safe_float(item.get("won%"), 50.0) / 100.0,
                        "psxg_minus_ga_per90": _safe_float(item.get("psxg+/-")) / ninety,
                    }
                )
        return records


class UnderstatAdapter:
    """Parses Understat shot JSON payloads into canonical [0,120]x[0,80] shot records."""

    @staticmethod
    def parse_match_shots(
        payload: Dict[str, Any] | str,
        match_id: int,
        season: str = "2024/2025",
    ) -> List[Dict[str, Any]]:
        if isinstance(payload, str):
            # Extract shotsData JSON.parse('...') if raw HTML is supplied
            m = re.search(r"shotsData\s*=\s*JSON\.parse\('([^']+)'\)", payload)
            if m:
                decoded = bytes(m.group(1), "utf-8").decode("unicode_escape")
                data = json.loads(decoded)
            else:
                data = json.loads(payload)
        else:
            data = payload

        shots_out: List[Dict[str, Any]] = []
        for side_key, is_home in (("h", True), ("a", False)):
            for s in data.get(side_key, []):
                # Understat X, Y are in [0, 1] attacking right
                x_att = float(s.get("X", 0.85)) * PITCH_LENGTH_YD
                y_att = (1.0 - float(s.get("Y", 0.50))) * PITCH_WIDTH_YD
                x_canon, y_canon = to_canonical_global(x_att, y_att, is_home=is_home)
                shots_out.append(
                    {
                        "source": "understat",
                        "match_id": int(match_id),
                        "season": season,
                        "understat_id": int(s["player_id"]) if s.get("player_id") else None,
                        "player_name": s.get("player", ""),
                        "team_name": s.get("h_team") if is_home else s.get("a_team"),
                        "is_home": is_home,
                        "minute": int(float(s.get("minute", 0))),
                        "xg_understat": float(s.get("xG", 0.0)),
                        "result": s.get("result", "MissedShots"),
                        "is_goal": s.get("result") == "Goal",
                        "shot_type": s.get("shotType", "RightFoot"),
                        "situation": s.get("situation", "OpenPlay"),
                        "last_action": s.get("lastAction", "Pass"),
                        "player_assisted": s.get("player_assisted"),
                        "x_att": x_att,
                        "y_att": y_att,
                        "x_canon": x_canon,
                        "y_canon": y_canon,
                    }
                )
        return shots_out


class SofaScoreFotMobAdapter:
    """
    Normalizes SofaScore / FotMob / WhoScored match JSON (lineups, 24x16 heatmaps,
    and [0,100]-scaled event logs) into canonical 2022-2026 training & profiling inputs.
    """

    @staticmethod
    def heatmap_points_to_24x16(points: Sequence[Tuple[float, float]]) -> List[float]:
        """Convert [(x_pct, y_pct), ...] in [0,100]x[0,100] into L1-normalized 24x16 grid."""
        grid = np.zeros((24, 16), dtype=np.float64)
        for xp, yp in points:
            col = min(max(int((float(xp) / 100.0) * 24), 0), 23)
            row = min(max(int((float(yp) / 100.0) * 16), 0), 15)
            grid[col, row] += 1.0
        total = float(grid.sum())
        if total > 0:
            grid /= total
        else:
            grid.fill(1.0 / 384.0)
        return grid.ravel().tolist()

    @staticmethod
    def parse_match_events(
        match_payload: Dict[str, Any],
        match_id: int,
        home_team_id: int,
        away_team_id: int,
    ) -> List[EventGoldV2]:
        """Convert SofaScore/WhoScored scraped events into canonical EventGoldV2 (`provider_source='scraped_event'`)."""
        events_raw = match_payload.get("events") or []
        home_xi = [int(p["id"]) for p in (match_payload.get("home_lineup") or [])][:11]
        away_xi = [int(p["id"]) for p in (match_payload.get("away_lineup") or [])][:11]
        slots_pids = (home_xi + [-1] * 11)[:11] + (away_xi + [-1] * 11)[:11]
        slots_roles = [11] * 22
        if slots_pids[0] > 0:
            slots_roles[0] = 1
        if slots_pids[11] > 0:
            slots_roles[11] = 1

        pid_to_slot = {pid: i + 1 for i, pid in enumerate(slots_pids) if pid > 0}
        gold: List[EventGoldV2] = []
        prev_ts = 0
        sh, sa = 0, 0
        for idx, ev in enumerate(events_raw):
            is_home = bool(ev.get("is_home", True))
            pid = int(ev.get("player_id", -1))
            team_id = home_team_id if is_home else away_team_id
            x_att, y_att = rescale_wyscout(float(ev.get("x", 50.0)), float(ev.get("y", 50.0)))
            ex_att, ey_att = rescale_wyscout(float(ev.get("end_x", ev.get("x", 50.0))), float(ev.get("end_y", ev.get("y", 50.0))))
            xc, yc = to_canonical_global(x_att, y_att, is_home)
            exc, eyc = to_canonical_global(ex_att, ey_att, is_home)
            sc, dxs, dys = quantize_pitch_cell(xc, yc)
            ec, dxe, dye = quantize_pitch_cell(exc, eyc)
            minute = int(ev.get("minute", 0))
            second = min(max(int(ev.get("second", 0)), 0), 59)
            ts_ms = max(prev_ts, (minute * 60 + second) * 1000)
            dt_sec = (ts_ms - prev_ts) / 1000.0 if idx > 0 else 0.0
            dtb, dtr = quantize_dt_bucket(dt_sec)

            ev_kind = str(ev.get("type", "pass")).lower()
            is_goal = bool(ev.get("is_goal", False))
            if "shot" in ev_kind:
                action = ActionType.SHOT
                outcome = EventOutcome.GOAL if is_goal else EventOutcome.OFF_TARGET
            elif "carry" in ev_kind:
                action = ActionType.CARRY
                outcome = EventOutcome.COMPLETE
            else:
                action = ActionType.PASS
                outcome = EventOutcome.COMPLETE if ev.get("accurate", True) else EventOutcome.INCOMPLETE

            through_ball = bool(ev.get("through_ball", False))
            cross = bool(ev.get("cross", False))
            under_pressure = bool(ev.get("under_pressure", False))

            gold.append(
                EventGoldV2(
                    match_id=match_id,
                    event_seq_idx=idx,
                    event_uuid=str(ev.get("id", f"sc_{match_id}_{idx}")),
                    provider_source="scraped_event",
                    period=MatchPeriod.FIRST_HALF if minute < 45 else MatchPeriod.SECOND_HALF,
                    minute=minute,
                    second=second,
                    timestamp_ms=ts_ms,
                    dt_sec=dt_sec,
                    dt_bucket_id=dtb,
                    dt_res=dtr,
                    possession_id=int(ev.get("possession_id", idx // 4 + 1)),
                    possession_team_id=team_id,
                    poss_phase=PossPhase.OPEN_PROGRESSION if x_att < 80 else PossPhase.OPEN_FINAL_THIRD,
                    score_home=sh,
                    score_away=sa,
                    actor_slot=pid_to_slot.get(pid, 1 if is_home else 12),
                    actor_player_id=pid,
                    actor_team_id=team_id,
                    is_home_actor=is_home,
                    target_slot=0,
                    target_player_id=-1,
                    active_slots_player_ids=list(slots_pids),
                    active_slots_roles=list(slots_roles),
                    action_type=action,
                    outcome=outcome,
                    x_canon=xc,
                    y_canon=yc,
                    end_x_canon=exc,
                    end_y_canon=eyc,
                    x_att=x_att,
                    y_att=y_att,
                    end_x_att=ex_att,
                    end_y_att=ey_att,
                    start_cell_id=sc,
                    end_cell_id=ec,
                    dx_res_start=dxs,
                    dy_res_start=dys,
                    dx_res_end=dxe,
                    dy_res_end=dye,
                    distance_yd=math.hypot(ex_att - x_att, ey_att - y_att),
                    angle_rad=math.atan2(ey_att - y_att, ex_att - x_att),
                    restart_type=RestartType.OPEN_PLAY,
                    body_part=BodyPart.RIGHT_FOOT,
                    under_pressure=under_pressure,
                    through_ball=through_ball,
                    cross=cross,
                    modifier_bitmask_8=pack_modifier_bitmask(
                        under_pressure=under_pressure, through_ball=through_ball, cross=cross
                    ),
                    has_360_frame=False,
                    xg_statsbomb=float(ev["xg"]) if ev.get("xg") is not None else None,
                )
            )
            if is_goal:
                if is_home:
                    sh += 1
                else:
                    sa += 1
            prev_ts = ts_ms
        return gold


class TransfermarktEAFCAdapter:
    """Normalizes current squad rosters, preferred foot, market values, and EA FC physical priors."""

    @staticmethod
    def parse_roster_records(records: Sequence[Dict[str, Any]]) -> List[Dict[str, Any]]:
        out: List[Dict[str, Any]] = []
        for r in records:
            foot_raw = str(r.get("preferred_foot", "Right")).strip().lower()
            foot = "Left" if "left" in foot_raw else ("Both" if "both" in foot_raw else "Right")
            weak_foot_stars = _safe_float(r.get("weak_foot_stars"), 3.0)
            sprint = _safe_float(r.get("sprint_speed"), 72.0) / 99.0
            accel = _safe_float(r.get("acceleration"), 72.0) / 99.0
            stamina = _safe_float(r.get("stamina"), 72.0) / 99.0
            out.append(
                {
                    "source": "transfermarkt_eafc",
                    "transfermarkt_id": r.get("transfermarkt_id"),
                    "eafc_id": r.get("eafc_id"),
                    "player_name": str(r.get("player_name", "")),
                    "team_name": str(r.get("team_name", "")),
                    "birth_year": int(r["birth_year"]) if r.get("birth_year") else None,
                    "nationality_iso": r.get("nationality_iso"),
                    "shirt_number": int(r["shirt_number"]) if r.get("shirt_number") else None,
                    "position_group": str(r.get("position_group", "MID")),
                    "preferred_foot": foot,
                    "weak_foot_prior": min(max((weak_foot_stars - 1.0) / 4.0, 0.0), 1.0),
                    "sprint_recovery_prior": min(max(0.5 * (sprint + accel), 0.1), 0.99),
                    "stamina_prior": min(max(stamina, 0.1), 0.99),
                    "market_value_eur": _safe_float(r.get("market_value_eur"), 0.0),
                }
            )
        return out


# --- Entity Resolution (`player_id_crosswalk`) ---

def normalize_player_name(name: str) -> str:
    """Unicode NFKD accent-folding and lowercase alphanumeric whitespace normalization."""
    nfkd = unicodedata.normalize("NFKD", str(name or ""))
    ascii_str = "".join(ch for ch in nfkd if not unicodedata.combining(ch))
    cleaned = re.sub(r"[^a-z0-9\s]", " ", ascii_str.lower())
    return re.sub(r"\s+", " ", cleaned).strip()


def jaro_winkler_similarity(s1: str, s2: str, p: float = 0.1) -> float:
    """Compute Jaro-Winkler string similarity in [0, 1]."""
    if s1 == s2:
        return 1.0
    len1, len2 = len(s1), len(s2)
    if len1 == 0 or len2 == 0:
        return 0.0
    match_distance = max(max(len1, len2) // 2 - 1, 0)
    s1_matches = [False] * len1
    s2_matches = [False] * len2
    matches = 0
    transpositions = 0
    for i in range(len1):
        start = max(0, i - match_distance)
        end = min(i + match_distance + 1, len2)
        for j in range(start, end):
            if s2_matches[j] or s1[i] != s2[j]:
                continue
            s1_matches[i] = True
            s2_matches[j] = True
            matches += 1
            break
    if matches == 0:
        return 0.0
    k = 0
    for i in range(len1):
        if not s1_matches[i]:
            continue
        while not s2_matches[k]:
            k += 1
        if s1[i] != s2[k]:
            transpositions += 1
        k += 1
    jaro = (matches / len1 + matches / len2 + (matches - transpositions / 2.0) / matches) / 3.0
    prefix = 0
    for c1, c2 in zip(s1[:4], s2[:4]):
        if c1 == c2:
            prefix += 1
        else:
            break
    return float(jaro + prefix * p * (1.0 - jaro))


class EntityResolver:
    """
    Resolves heterogeneous player records across StatsBomb, Wyscout, FBref, Understat,
    SofaScore/FotMob, Transfermarkt, and EA FC into canonical `PlayerIdCrosswalk` entries
    using deterministic ID seeds + Unicode blocking + Hungarian bipartite matching.
    """

    def __init__(
        self,
        existing_crosswalk: Optional[Sequence[PlayerIdCrosswalk]] = None,
        auto_accept_threshold: float = 0.92,
        conflict_lower_threshold: float = 0.78,
        next_new_player_id: int = 100_000,
    ) -> None:
        self.crosswalk: Dict[int, PlayerIdCrosswalk] = {}
        self.auto_accept_threshold = auto_accept_threshold
        self.conflict_lower_threshold = conflict_lower_threshold
        self._next_id = next_new_player_id
        self.conflict_queue: List[Dict[str, Any]] = []

        if existing_crosswalk:
            for rec in existing_crosswalk:
                self.crosswalk[rec.pixelpitch_player_id] = rec
                if rec.pixelpitch_player_id >= self._next_id:
                    self._next_id = rec.pixelpitch_player_id + 1

    @staticmethod
    def _pair_similarity(a: Dict[str, Any], b: Dict[str, Any]) -> float:
        name_a = normalize_player_name(a.get("player_name") or a.get("full_name_norm", ""))
        name_b = normalize_player_name(b.get("player_name") or b.get("full_name_norm", ""))
        jw = jaro_winkler_similarity(name_a, name_b)
        toks_a, toks_b = set(name_a.split()), set(name_b.split())
        # Handle shortened names like "L. Messi" vs "Lionel Messi"
        surname_bonus = 0.15 if toks_a and toks_b and list(name_a.split())[-1] == list(name_b.split())[-1] else 0.0
        tok_jacc = len(toks_a & toks_b) / max(len(toks_a | toks_b), 1)
        name_sim = min(1.0, 0.72 * jw + 0.28 * tok_jacc + surname_bonus)

        # Birth year & club consistency penalties/bonuses
        by_a, by_b = a.get("birth_year"), b.get("birth_year")
        if by_a and by_b:
            diff = abs(int(by_a) - int(by_b))
            if diff > 1:
                return 0.0
            dob_factor = 1.0 if diff == 0 else 0.96
        else:
            dob_factor = 0.98

        club_a = normalize_player_name(a.get("team_name") or a.get("current_team_name") or "")
        club_b = normalize_player_name(b.get("team_name") or b.get("current_team_name") or "")
        club_bonus = 0.05 if (club_a and club_b and (club_a in club_b or club_b in club_a)) else 0.0

        return float(min(1.0, name_sim * dob_factor + club_bonus))

    def link_external_records(
        self,
        records: Sequence[Dict[str, Any]],
        source_id_field: str,
    ) -> List[PlayerIdCrosswalk]:
        """
        Link external scraped records onto existing crosswalk entries via:
        1) Exact source ID lookup
        2) Optimal Hungarian bipartite matching against existing crosswalk pool
        3) Creation of new canonical `pixelpitch_player_id >= 100_000` for unmatched entities.
        """
        id_index: Dict[Any, int] = {}
        for pp_id, cw in self.crosswalk.items():
            src_val = getattr(cw, source_id_field, None)
            if src_val is not None:
                id_index[src_val] = pp_id

        unmatched: List[Dict[str, Any]] = []
        for rec in records:
            src_id = rec.get(source_id_field)
            if src_id is not None and src_id in id_index:
                pp_id = id_index[src_id]
                cw = self.crosswalk[pp_id]
                self._merge_fields(cw, rec, source_id_field, 1.0, "exact_id_table")
            else:
                unmatched.append(dict(rec))

        if unmatched and self.crosswalk:
            cw_ids = list(self.crosswalk.keys())
            cost_matrix = np.ones((len(unmatched), len(cw_ids)), dtype=np.float64)
            for i, rec in enumerate(unmatched):
                for j, pp_id in enumerate(cw_ids):
                    cw_dict = self.crosswalk[pp_id].model_dump()
                    sim = self._pair_similarity(rec, cw_dict)
                    cost_matrix[i, j] = 1.0 - sim

            row_ind, col_ind = linear_sum_assignment(cost_matrix)
            matched_rows: set[int] = set()
            for r_i, c_j in zip(row_ind, col_ind):
                sim = 1.0 - float(cost_matrix[r_i, c_j])
                rec = unmatched[r_i]
                pp_id = cw_ids[c_j]
                if sim >= self.auto_accept_threshold:
                    cw = self.crosswalk[pp_id]
                    method = "deterministic_name_dob_club" if sim >= 0.98 else "fuzzy_bipartite_hungarian"
                    self._merge_fields(cw, rec, source_id_field, sim, method)
                    matched_rows.add(r_i)
                elif sim >= self.conflict_lower_threshold:
                    self.conflict_queue.append(
                        {
                            "candidate_name": rec.get("player_name"),
                            "source_id_field": source_id_field,
                            "source_id": rec.get(source_id_field),
                            "matched_pixelpitch_id": pp_id,
                            "matched_name": self.crosswalk[pp_id].full_name_norm,
                            "similarity": round(sim, 4),
                        }
                    )

            still_unmatched = [rec for idx, rec in enumerate(unmatched) if idx not in matched_rows]
        else:
            still_unmatched = unmatched

        for rec in still_unmatched:
            new_id = self._next_id
            self._next_id += 1
            cw = PlayerIdCrosswalk(
                pixelpitch_player_id=new_id,
                full_name_norm=normalize_player_name(rec.get("player_name", "")),
                birth_year=rec.get("birth_year"),
                nationality_iso=rec.get("nationality_iso"),
                current_team_name=rec.get("team_name"),
                position_group=rec.get("position_group") or rec.get("position"),
                match_confidence=1.0,
                resolution_method="exact_id_table",
            )
            if rec.get(source_id_field) is not None:
                setattr(cw, source_id_field, rec[source_id_field])
            for fld in (
                "statsbomb_id",
                "wyscout_id",
                "fbref_hex_id",
                "understat_id",
                "sofascore_id",
                "fotmob_id",
                "whoscored_id",
                "transfermarkt_id",
                "eafc_id",
            ):
                if rec.get(fld) is not None:
                    setattr(cw, fld, rec[fld])
            self.crosswalk[new_id] = cw

        return list(self.crosswalk.values())

    @staticmethod
    def _merge_fields(
        cw: PlayerIdCrosswalk,
        rec: Dict[str, Any],
        source_id_field: str,
        sim: float,
        method: str,
    ) -> None:
        if rec.get(source_id_field) is not None:
            setattr(cw, source_id_field, rec[source_id_field])
        if rec.get("team_name"):
            cw.current_team_name = rec["team_name"]
        if rec.get("birth_year") and cw.birth_year is None:
            cw.birth_year = int(rec["birth_year"])
        if rec.get("nationality_iso") and cw.nationality_iso is None:
            cw.nationality_iso = str(rec["nationality_iso"])
        cw.match_confidence = min(cw.match_confidence, float(sim))
        cw.resolution_method = method  # type: ignore[assignment]
