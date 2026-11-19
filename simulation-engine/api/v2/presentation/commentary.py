"""Module E: Two-Tier Supporting LLM Narrator, Fact Verifier & A/B Telemetry.

The Language Model strictly narrates verified structured events and Monte Carlo
diagnostics downstream of Modules B/C/D; it never controls match physics or state.
"""

from __future__ import annotations

import abc
import asyncio
import json
import os
import re
import time
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Any, Awaitable, Callable, Dict, List, Literal, Optional, Sequence, Tuple

import aiohttp

from .schemas_v2 import CommentaryConfigV2, CommentaryCueV2, StructuredEventV2

TransportFn = Callable[[Dict[str, Any], int], Awaitable[Tuple[str, str]]]


def pitch_zone_name(x: float, y: float, attacking_right: bool = True, language: str = "en") -> str:
    """Map canonical [0,120]x[0,80] coordinates to readable football tactical zones."""
    x_att = x if attacking_right else (120.0 - x)
    y_att = y if attacking_right else (80.0 - y)

    if x_att >= 102.0 and 18.0 <= y_att <= 62.0:
        zones = {"en": "the penalty area", "es": "el área penal", "fr": "la surface de réparation"}
        return zones.get(language, zones["en"])
    if 80.0 <= x_att < 102.0 and 30.0 <= y_att <= 50.0:
        zones = {"en": "Zone 14", "es": "la zona 14", "fr": "la zone 14"}
        return zones.get(language, zones["en"])

    if x_att < 40.0:
        third = {"en": "own third", "es": "propio campo", "fr": "camp défensif"}.get(language, "own third")
    elif x_att < 80.0:
        third = {"en": "midfield", "es": "mediocampo", "fr": "milieu de terrain"}.get(language, "midfield")
    else:
        third = {"en": "final third", "es": "último tercio", "fr": "dernier tiers"}.get(language, "final third")

    if y_att < 18.0:
        lane = {"en": "left flank", "es": "banda izquierda", "fr": "côté gauche"}.get(language, "left flank")
    elif y_att < 30.0:
        lane = {"en": "left half-space", "es": "semiespacio izquierdo", "fr": "demi-espace gauche"}.get(
            language, "left half-space"
        )
    elif y_att <= 50.0:
        lane = {"en": "central corridor", "es": "carril central", "fr": "axe central"}.get(
            language, "central corridor"
        )
    elif y_att <= 62.0:
        lane = {"en": "right half-space", "es": "semiespacio derecho", "fr": "demi-espace droit"}.get(
            language, "right half-space"
        )
    else:
        lane = {"en": "right flank", "es": "banda derecha", "fr": "côté droit"}.get(language, "right flank")

    return f"{lane} ({third})"


def compute_salience_score(event: StructuredEventV2) -> float:
    """Score event broadcast importance in [0, 1] to gate Tier-2 LLM calls."""
    action = event.action_type.lower()
    outcome = event.outcome.lower()
    sub = event.sub_type.lower()
    card = event.card.lower()

    if outcome == "goal" or "own_goal" in action:
        return 1.0
    if "red" in card or "second" in card or outcome in ("red_card", "second_yellow"):
        return 0.96
    if sub == "penalty" or outcome == "penalty_won":
        return 0.92
    if action == "shot":
        if event.xg >= 0.25 or event.post_shot_xg >= 0.35 or outcome == "post":
            return 0.88
        if event.xg >= 0.12 or "saved" in outcome:
            return 0.72
        return 0.52
    if action in ("substitution", "card", "period_boundary") or "yellow" in card:
        return 0.68
    if abs(event.xt_delta) >= 0.09 or event.vaep_value >= 0.14:
        return 0.70
    if action in ("interception", "tackle_duel", "save") and event.vaep_value >= 0.08:
        return 0.66
    return 0.25


class Tier1TemplateNarrator:
    """Deterministic <1ms parameterized commentary generator with zero hallucination risk."""

    _GOAL_TEMPLATES: Dict[str, Tuple[str, str]] = {
        "en": (
            "{minute}' GOAL! {actor} ({home_score}-{away_score})",
            "{minute}' — Goal for {team}! {actor} finishes from {zone} ({xg:.2f} xG, {psxg:.2f} PSxG).",
        ),
        "es": (
            "{minute}' ¡GOL! {actor} ({home_score}-{away_score})",
            "{minute}' — ¡Gol de {team}! {actor} define desde {zone} ({xg:.2f} xG, {psxg:.2f} PSxG).",
        ),
        "fr": (
            "{minute}' BUT ! {actor} ({home_score}-{away_score})",
            "{minute}' — But pour {team} ! {actor} conclut depuis {zone} ({xg:.2f} xG, {psxg:.2f} PSxG).",
        ),
    }

    def narrate_event(
        self,
        event: StructuredEventV2,
        minute: int,
        language: Literal["en", "es", "fr"] = "en",
        salience: Optional[float] = None,
    ) -> CommentaryCueV2:
        t0 = time.perf_counter_ns()
        score = salience if salience is not None else compute_salience_score(event)
        actor = event.player_name or f"Slot {event.actor_slot}"
        team = event.team_name or ("Home" if event.possessing_team_side == 1 else "Away")
        att_right = event.actor_slot <= 11
        zone = pitch_zone_name(event.end_x, event.end_y, attacking_right=att_right, language=language)
        action = event.action_type.lower()
        outcome = event.outcome.lower()

        if action == "shot":
            if outcome == "goal":
                h_tpl, b_tpl = self._GOAL_TEMPLATES.get(language, self._GOAL_TEMPLATES["en"])
                fmt = dict(
                    minute=minute,
                    actor=actor,
                    team=team,
                    zone=zone,
                    home_score=event.home_score,
                    away_score=event.away_score,
                    xg=event.xg,
                    psxg=event.post_shot_xg,
                )
                headline = h_tpl.format(**fmt)
                body = b_tpl.format(**fmt)
            elif "saved" in outcome:
                headline = f"{minute}' Big save denies {actor} ({event.xg:.2f} xG)"
                body = (
                    f"{minute}' — {actor} strikes from {zone} ({event.xg:.2f} xG, "
                    f"{event.post_shot_xg:.2f} PSxG), saved cleanly."
                )
            elif outcome == "post":
                headline = f"{minute}' Off the woodwork by {actor}!"
                body = f"{minute}' — {actor} rattles the frame from {zone} ({event.xg:.2f} xG)."
            else:
                headline = f"{minute}' Shot by {actor} ({outcome or 'off target'})"
                body = f"{minute}' — {actor} tries from {zone} ({event.xg:.2f} xG); outcome: {event.outcome or 'Off Target'}."
        elif action == "pass":
            sub_tag = f" {event.sub_type.lower()}" if event.sub_type else ""
            press_note = " under pressure" if event.under_pressure else ""
            headline = f"{minute}' {actor} {sub_tag.strip() or 'pass'} ({event.xpass:.0%} xPass)"
            body = (
                f"{minute}' — {actor} plays a{sub_tag} pass into {zone}{press_note} "
                f"(xPass {event.xpass:.2f}, xT {event.xt_delta:+.3f})."
            )
        elif action in ("carry", "dribble"):
            headline = f"{minute}' {actor} drives into {zone}"
            body = f"{minute}' — {actor} progresses the ball into {zone} (xT {event.xt_delta:+.3f})."
        elif action in ("substitution",):
            headline = f"{minute}' Substitution ({team})"
            body = f"{minute}' — Tactical substitution executed for {team} involving {actor}."
        elif action == "card" or event.card:
            headline = f"{minute}' {event.card or 'Card'} shown to {actor}"
            body = f"{minute}' — Referee issues a {event.card or 'card'} to {actor} ({team})."
        elif action == "period_boundary":
            headline = f"{minute}' {event.outcome or 'Period Boundary'}"
            body = f"{minute}' — Match state transitions ({event.outcome}) at {event.home_score}-{event.away_score}."
        else:
            headline = f"{minute}' {event.action_type.replace('_', ' ').title()} by {actor}"
            body = (
                f"{minute}' — {actor} ({team}) executes {event.action_type} in {zone} "
                f"(VAEP {event.vaep_value:+.3f})."
            )

        elapsed_ms = max(0, (time.perf_counter_ns() - t0) // 1_000_000)
        return CommentaryCueV2(
            anchor_event_index=event.event_index,
            tier="template",
            headline=headline,
            body=body,
            salience_score=score,
            language=language,
            provider_id="template",
            model_version="tier1-template-v2",
            latency_ms=int(elapsed_ms),
            verifier_passed=True,
            fallback_used=False,
        )

    def narrate_experiment(self, summary: Dict[str, Any], language: Literal["en", "es", "fr"] = "en") -> CommentaryCueV2:
        headline = summary.get("headline") or "Counterfactual Monte Carlo diagnostic summary"
        arms = summary.get("arms", [])
        if len(arms) >= 2:
            best = max(arms, key=lambda a: a.get("expected_points", 0.0))
            body = (
                f"Across {summary.get('n_simulations', 100)} paired CRN runs, "
                f"arm '{best.get('arm_id')}' leads at {best.get('expected_points', 0.0):.3f} expected points "
                f"(OOD tier: {best.get('ood_guardrail', {}).get('confidence_tier', 'GREEN')})."
            )
        else:
            body = "Monte Carlo counterfactual simulation completed with paired CRN variance reduction."
        return CommentaryCueV2(
            anchor_event_index=0,
            tier="tactical_insight",
            headline=headline,
            body=body,
            salience_score=1.0,
            language=language,
            provider_id="template",
            model_version="tier1-template-v2",
            latency_ms=0,
            verifier_passed=True,
            fallback_used=False,
        )


class CommentaryFactVerifier:
    """Deterministic hallucination gate checking LLM output against canonical match state."""

    _SCORE_RE = re.compile(r"\b(\d{1,2})\s*[-:]\s*(\d{1,2})\b")
    _GOAL_CLAIM_RE = re.compile(r"\b(goal!|scores!|finds the net|into the back of the net)\b", re.IGNORECASE)
    _PROPER_NAME_RE = re.compile(r"\b([A-Z][a-zÀ-ÿ]{2,}(?:\s+[A-Z][a-zÀ-ÿ]{2,})+)\b")

    @classmethod
    def verify(
        cls,
        text: str,
        home_score: int,
        away_score: int,
        trigger_event: Optional[StructuredEventV2] = None,
        allowed_names: Optional[Sequence[str]] = None,
    ) -> Tuple[bool, Optional[str]]:
        if not text or not text.strip():
            return False, "empty_output"

        # 1. Verify any mentioned scorelines match canonical score
        for m in cls._SCORE_RE.finditer(text):
            s1, s2 = int(m.group(1)), int(m.group(2))
            if (s1, s2) not in ((home_score, away_score), (away_score, home_score)):
                return False, f"score_mismatch:{s1}-{s2}!=({home_score}-{away_score})"

        # 2. Verify shot outcome consistency (no fake goal claims when saved/missed)
        if trigger_event is not None and trigger_event.action_type.lower() == "shot":
            if trigger_event.outcome.lower() != "goal" and cls._GOAL_CLAIM_RE.search(text):
                return False, f"outcome_mismatch:claimed_goal_on_{trigger_event.outcome}"

        # 3. Verify named players/teams against canonical roster + teams if provided
        if allowed_names:
            allowed_tokens = {n.strip().lower() for n in allowed_names if n and n.strip()}
            allowed_words = {tok for name in allowed_tokens for tok in name.split()}
            non_player_phrases = {
                "zone 14",
                "half-space",
                "left flank",
                "right flank",
                "penalty area",
                "first half",
                "second half",
                "yellow card",
                "red card",
                "expected goals",
                "free kick",
                "goal kick",
                "corner kick",
                "through ball",
            }
            for m in cls._PROPER_NAME_RE.finditer(text):
                phrase = m.group(1).strip()
                low = phrase.lower()
                if low in allowed_tokens or low in non_player_phrases:
                    continue
                words = low.split()
                if not all(w in allowed_words for w in words):
                    return False, f"unknown_entity:{phrase}"

        return True, None


class CommentaryProvider(abc.ABC):
    """Abstract adapter for lightweight Tier-2 LLM commentary sidecars."""

    provider_id: str
    model_version: str

    @abc.abstractmethod
    def is_available(self) -> bool:
        raise NotImplementedError

    @abc.abstractmethod
    async def generate(self, prompt_payload: Dict[str, Any], timeout_ms: int) -> Tuple[str, str]:
        raise NotImplementedError


class LocalSidecarCommentaryProvider(CommentaryProvider):
    """Adapter for quantized local 3B LLM sidecar (llama.cpp / vLLM OpenAI-compatible API)."""

    def __init__(
        self,
        endpoint_url: Optional[str] = None,
        model_version: str = "Qwen2.5-3B-Instruct-INT4",
        transport_fn: Optional[TransportFn] = None,
    ) -> None:
        self.provider_id = "local_3b"
        self.model_version = model_version
        self.endpoint_url = endpoint_url if endpoint_url is not None else os.getenv("LOCAL_LLM_URL", "")
        self._transport_fn = transport_fn

    def is_available(self) -> bool:
        return self._transport_fn is not None or bool(self.endpoint_url)

    async def generate(self, prompt_payload: Dict[str, Any], timeout_ms: int) -> Tuple[str, str]:
        if self._transport_fn is not None:
            return await self._transport_fn(prompt_payload, timeout_ms)
        if not self.endpoint_url:
            raise RuntimeError("LOCAL_LLM_URL is not configured")

        timeout = aiohttp.ClientTimeout(total=timeout_ms / 1000.0)
        system_prompt = (
            "You are a concise football broadcast & tactical commentator. Use ONLY the verified JSON facts. "
            "Return JSON with keys 'headline' and 'body' (max 2 sentences). Never invent goals or players."
        )
        body = {
            "model": self.model_version,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": json.dumps(prompt_payload, ensure_ascii=False)},
            ],
            "max_tokens": 90,
            "temperature": 0.3,
        }
        async with aiohttp.ClientSession(timeout=timeout) as session:
            async with session.post(self.endpoint_url, json=body) as resp:
                resp.raise_for_status()
                data = await resp.json()
        content = data["choices"][0]["message"]["content"]
        return _parse_headline_body(content, fallback_headline="Live Tactical Update")


class FlashApiCommentaryProvider(CommentaryProvider):
    """Adapter for external fast Flash LLM REST API (e.g., Gemini Flash)."""

    def __init__(
        self,
        api_url: Optional[str] = None,
        api_key: Optional[str] = None,
        model_version: str = "gemini-2.5-flash",
        transport_fn: Optional[TransportFn] = None,
    ) -> None:
        self.provider_id = "flash_api"
        self.model_version = model_version
        self.api_url = api_url if api_url is not None else os.getenv("FLASH_LLM_API_URL", "")
        self.api_key = api_key if api_key is not None else os.getenv("FLASH_LLM_API_KEY", "")
        self._transport_fn = transport_fn

    def is_available(self) -> bool:
        return self._transport_fn is not None or (bool(self.api_url) and bool(self.api_key))

    async def generate(self, prompt_payload: Dict[str, Any], timeout_ms: int) -> Tuple[str, str]:
        if self._transport_fn is not None:
            return await self._transport_fn(prompt_payload, timeout_ms)
        if not self.api_url or not self.api_key:
            raise RuntimeError("FLASH_LLM_API_URL / FLASH_LLM_API_KEY not configured")

        timeout = aiohttp.ClientTimeout(total=timeout_ms / 1000.0)
        headers = {"Authorization": f"Bearer {self.api_key}", "Content-Type": "application/json"}
        body = {
            "model": self.model_version,
            "prompt_payload": prompt_payload,
            "max_output_tokens": 90,
            "temperature": 0.3,
        }
        async with aiohttp.ClientSession(timeout=timeout) as session:
            async with session.post(self.api_url, headers=headers, json=body) as resp:
                resp.raise_for_status()
                data = await resp.json()
        if "headline" in data and "body" in data:
            return str(data["headline"]), str(data["body"])
        text = data.get("text") or json.dumps(data)
        return _parse_headline_body(str(text), fallback_headline="Tactical Broadcast")


def _parse_headline_body(raw_text: str, fallback_headline: str) -> Tuple[str, str]:
    text = raw_text.strip()
    try:
        parsed = json.loads(text)
        if isinstance(parsed, dict) and "body" in parsed:
            return str(parsed.get("headline") or fallback_headline), str(parsed["body"])
    except json.JSONDecodeError:
        pass
    lines = [ln.strip() for ln in text.splitlines() if ln.strip()]
    if len(lines) >= 2:
        return lines[0][:120], " ".join(lines[1:])
    return fallback_headline, text


@dataclass
class ProviderStats:
    calls: int = 0
    successes: int = 0
    fallbacks: int = 0
    verifier_rejections: int = 0
    latencies_ms: List[int] = field(default_factory=list)

    def summary(self) -> Dict[str, Any]:
        sorted_lat = sorted(self.latencies_ms)
        avg = sum(sorted_lat) / len(sorted_lat) if sorted_lat else 0.0
        p95 = sorted_lat[int(0.95 * (len(sorted_lat) - 1))] if sorted_lat else 0
        return {
            "calls": self.calls,
            "successes": self.successes,
            "fallbacks": self.fallbacks,
            "verifier_rejections": self.verifier_rejections,
            "avg_latency_ms": round(avg, 2),
            "p95_latency_ms": p95,
        }


class CommentaryOrchestrator:
    """Two-Tier Hybrid Narrator with A/B switching, deterministic verification & template fallback."""

    def __init__(
        self,
        local_provider: Optional[CommentaryProvider] = None,
        flash_provider: Optional[CommentaryProvider] = None,
    ) -> None:
        self.template_narrator = Tier1TemplateNarrator()
        self.local_provider: CommentaryProvider = local_provider or LocalSidecarCommentaryProvider()
        self.flash_provider: CommentaryProvider = flash_provider or FlashApiCommentaryProvider()
        self._ab_stats: Dict[str, ProviderStats] = defaultdict(ProviderStats)

    def provider_readiness(self) -> Dict[str, bool]:
        return {
            "template_ready": True,
            "local_3b_ready": self.local_provider.is_available(),
            "flash_api_ready": self.flash_provider.is_available(),
        }

    def get_ab_telemetry(self) -> Dict[str, Dict[str, Any]]:
        return {pid: stats.summary() for pid, stats in self._ab_stats.items()}

    def _select_provider(self, mode: str, routing_key: int) -> Optional[CommentaryProvider]:
        if mode == "template_only":
            return None
        if mode == "local_3b":
            return self.local_provider if self.local_provider.is_available() else None
        if mode == "flash_api":
            return self.flash_provider if self.flash_provider.is_available() else None
        if mode == "ab_split":
            candidates = [p for p in (self.local_provider, self.flash_provider) if p.is_available()]
            if not candidates:
                return None
            return candidates[routing_key % len(candidates)]
        return None

    async def narrate_event(
        self,
        event: StructuredEventV2,
        minute: int,
        home_team: str,
        away_team: str,
        recent_chain: Sequence[StructuredEventV2],
        roster_names: Sequence[str],
        config: CommentaryConfigV2,
        tactical_context: Optional[Dict[str, Any]] = None,
    ) -> CommentaryCueV2:
        salience = compute_salience_score(event)
        base_cue = self.template_narrator.narrate_event(
            event=event, minute=minute, language=config.language, salience=salience
        )
        if not config.enabled or salience < config.salience_threshold or config.provider == "template_only":
            self._ab_stats["template"].calls += 1
            self._ab_stats["template"].successes += 1
            self._ab_stats["template"].latencies_ms.append(base_cue.latency_ms)
            return base_cue

        provider = self._select_provider(config.provider, routing_key=event.event_index)
        if provider is None:
            return base_cue

        prompt_payload = {
            "match": {
                "home": home_team,
                "away": away_team,
                "score": [event.home_score, event.away_score],
                "minute": minute,
            },
            "tactical_context": tactical_context or {},
            "recent_chain": [
                {
                    "actor": e.player_name or f"Slot {e.actor_slot}",
                    "action": e.action_type,
                    "sub_type": e.sub_type,
                    "xT": round(e.xt_delta, 3),
                    "xPass": round(e.xpass, 3),
                }
                for e in recent_chain[-5:]
            ],
            "trigger_event": event.model_dump(),
            "constraints": {"max_sentences": 2, "style": "tactical_broadcast", "language": config.language},
        }
        allowed = list(roster_names) + [
            home_team,
            away_team,
            event.player_name,
            *(e.player_name for e in recent_chain),
        ]

        stats = self._ab_stats[provider.provider_id]
        stats.calls += 1
        t0 = time.perf_counter()
        try:
            headline, body = await asyncio.wait_for(
                provider.generate(prompt_payload, config.timeout_ms),
                timeout=config.timeout_ms / 1000.0,
            )
            elapsed_ms = int((time.perf_counter() - t0) * 1000)
            stats.latencies_ms.append(elapsed_ms)

            verified, _reason = CommentaryFactVerifier.verify(
                text=f"{headline} {body}",
                home_score=event.home_score,
                away_score=event.away_score,
                trigger_event=event,
                allowed_names=allowed,
            )
            if not verified:
                stats.verifier_rejections += 1
                stats.fallbacks += 1
                return base_cue.model_copy(
                    update={
                        "provider_id": provider.provider_id,
                        "model_version": provider.model_version,
                        "latency_ms": elapsed_ms,
                        "verifier_passed": False,
                        "fallback_used": True,
                    }
                )

            stats.successes += 1
            return CommentaryCueV2(
                anchor_event_index=event.event_index,
                tier="llm_broadcast",
                headline=headline,
                body=body,
                salience_score=salience,
                language=config.language,
                provider_id=provider.provider_id,
                model_version=provider.model_version,
                latency_ms=elapsed_ms,
                verifier_passed=True,
                fallback_used=False,
            )
        except Exception:
            elapsed_ms = int((time.perf_counter() - t0) * 1000)
            stats.latencies_ms.append(elapsed_ms)
            stats.fallbacks += 1
            return base_cue.model_copy(
                update={
                    "provider_id": provider.provider_id,
                    "model_version": provider.model_version,
                    "latency_ms": elapsed_ms,
                    "verifier_passed": True,
                    "fallback_used": True,
                }
            )

    async def narrate_experiment(
        self,
        experiment_summary: Dict[str, Any],
        config: CommentaryConfigV2,
    ) -> CommentaryCueV2:
        base = self.template_narrator.narrate_experiment(experiment_summary, language=config.language)
        if not config.enabled or config.provider == "template_only":
            return base
        provider = self._select_provider(config.provider, routing_key=abs(hash(experiment_summary.get("experiment_id", ""))))
        if provider is None:
            return base

        stats = self._ab_stats[provider.provider_id]
        stats.calls += 1
        t0 = time.perf_counter()
        try:
            headline, body = await asyncio.wait_for(
                provider.generate(
                    {"experiment_diagnostics": experiment_summary, "language": config.language},
                    max(config.timeout_ms, 1200),
                ),
                timeout=max(config.timeout_ms, 1200) / 1000.0,
            )
            elapsed_ms = int((time.perf_counter() - t0) * 1000)
            stats.latencies_ms.append(elapsed_ms)
            stats.successes += 1
            return CommentaryCueV2(
                anchor_event_index=0,
                tier="tactical_insight",
                headline=headline,
                body=body,
                salience_score=1.0,
                language=config.language,
                provider_id=provider.provider_id,
                model_version=provider.model_version,
                latency_ms=elapsed_ms,
                verifier_passed=True,
                fallback_used=False,
            )
        except Exception:
            elapsed_ms = int((time.perf_counter() - t0) * 1000)
            stats.latencies_ms.append(elapsed_ms)
            stats.fallbacks += 1
            return base.model_copy(
                update={
                    "provider_id": provider.provider_id,
                    "model_version": provider.model_version,
                    "latency_ms": elapsed_ms,
                    "fallback_used": True,
                }
            )
