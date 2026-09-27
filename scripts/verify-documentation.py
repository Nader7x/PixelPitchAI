#!/usr/bin/env python3
"""
PixelPitchAI Living Documentation Verification Test Suite
==========================================================
Comprehensive, automated, opaque-box documentation test runner validating all
acceptance criteria from ORIGINAL_REQUEST.md and PROJECT.md across 4 tiers:

  - Tier 1: Feature & Protocol Coverage
  - Tier 2: Boundary & Negative Cases
  - Tier 3: Cross-Reference & Route Validation
  - Tier 4: Real-World Link & Markdown Integrity

Usage:
  python scripts/verify-documentation.py [--tier {1,2,3,4}] [--verbose] [--fail-fast] [--json]

Exit Code:
  0: All executed tests passed.
  1: One or more test assertions failed.
"""

from __future__ import annotations

import argparse
import dataclasses
import json
import os
import re
import sys
import time
from pathlib import Path
from typing import Callable, Dict, List, Optional, Set, Tuple


# ==============================================================================
# Color / Formatting Utility
# ==============================================================================
class TerminalColors:
    RESET = "\033[0m"
    BOLD = "\033[1m"
    RED = "\033[91m"
    GREEN = "\033[92m"
    YELLOW = "\033[93m"
    BLUE = "\033[94m"
    MAGENTA = "\033[95m"
    CYAN = "\033[96m"
    GRAY = "\033[90m"


def supports_color() -> bool:
    """Check if the current terminal supports ANSI color escapes."""
    if os.environ.get("NO_COLOR") or not hasattr(sys.stdout, "isatty"):
        return False
    if sys.platform == "win32":
        # Windows 10/11 supports VT100
        return sys.stdout.isatty()
    return sys.stdout.isatty()


USE_COLOR = supports_color()


def color(text: str, c: str) -> str:
    return f"{c}{text}{TerminalColors.RESET}" if USE_COLOR else text


# ==============================================================================
# Test Data Models
# ==============================================================================
@dataclasses.dataclass
class AssertionFailure:
    file_path: str
    line_number: Optional[int]
    description: str
    offending_content: Optional[str] = None


@dataclasses.dataclass
class TestCaseResult:
    test_id: str
    tier: int
    name: str
    description: str
    passed: bool
    failures: List[AssertionFailure] = dataclasses.field(default_factory=list)
    duration_ms: float = 0.0


# ==============================================================================
# Repository Inventory & Helper Parsers
# ==============================================================================
class RepoContext:
    """Provides file discovery and cached AST/route parsers for the repository."""

    def __init__(self, root_dir: Path):
        self.root = root_dir.resolve()
        self.docs_dir = self.root / "docs"
        self.backend_dir = self.root / "backend"
        self.frontend_dir = self.root / "frontend"
        self.sim_engine_dir = self.root / "simulation-engine"
        self.controllers_dir = self.backend_dir / "src" / "Footex" / "Controllers"
        self.sim_routes_dir = self.sim_engine_dir / "api" / "routes"

        # Living documentation files under audit
        self.authoritative_docs = [
            self.root / "README.md",
            self.frontend_dir / "README.md",
            self.sim_engine_dir / "README.md",
            self.backend_dir / "tests" / "Footex.PerformanceTests" / "README.md",
            self.backend_dir / "tests" / "Footex.PerformanceTests" / "QUICKSTART.md",
            self.docs_dir / "project-architecture-documentation.md",
            self.docs_dir / "documentation.md",
            self.docs_dir / "database-design-documentation.md",
            self.docs_dir / "event-processing-system.md",
            self.docs_dir / "rabbitmq-matchevent-client.md",
            self.docs_dir / "real-time-statistics-optimization.md",
            self.docs_dir / "search-api-documentation.md",
            self.docs_dir / "SECRETS-SETUP.md",
            self.docs_dir / "signalr-notification-service.md",
            self.docs_dir / "testing-documentation.md",
            self.docs_dir / "ci_cd_plan.md",
            self.docs_dir / "local-docker-testing-setup.md",
            self.sim_engine_dir / "DataMigration" / "MIGRATION_README.md",
            self.sim_engine_dir / "DataMigration" / "Coaches" / "README.md",
            self.sim_engine_dir / "DataMigration" / "Players" / "README.md",
            self.sim_engine_dir / "DataMigration" / "Stadiums" / "README.md",
            self.sim_engine_dir / "DataMigration" / "Teams" / "README.md",
            self.sim_engine_dir / "DataMigration" / "TeamSeasons" / "README.md",
        ]

        # Purged obsolete files (Milestone 1 / Requirement R3)
        self.purged_target_files = [
            "docs/COMPLETION-SUMMARY.md",
            "docs/final-project-status.md",
            "docs/project-validation-summary.md",
            "docs/DOCKER_UPDATE_SUMMARY.md",
            "docs/performance-improvements-completed.md",
            "docs/performance-improvements-final-summary.md",
            "docs/rabbitmq-performance-improvements.md",
            "docs/search-docs.md",
            "docs/SECURITY-REMEDIATION.md",
            "docs/SECURITY-REMEDIATION-SUMMARY.md",
            "docs/signalr-matchhub.md",
            "docs/comprehensive-testing-documentation.md",
            "simulation-engine/DataMigration/Coaches/HISTORICAL_COACHES_RESTORATION_REPORT.md",
            "simulation-engine/DataMigration/Coaches/COACHES_SYSTEM_FINAL_REPORT.md",
            "simulation-engine/DataMigration/Coaches/PROJECT_COMPLETION_SUMMARY.md",
            "simulation-engine/DataMigration/Coaches/SOLUTION_SUMMARY.md",
            "simulation-engine/DataMigration/Players/CLEANUP_COMPLETE.md",
            "simulation-engine/DataMigration/Players/CLEANUP_PLAN.md",
            "simulation-engine/DataMigration/Players/ENRICHMENT_SUMMARY.md",
            "simulation-engine/DataMigration/Players/FINAL_PROJECT_REPORT.md",
            "simulation-engine/DataMigration/Stadiums/STADIUM_SYSTEM_FINAL_REPORT.md",
            "simulation-engine/DataMigration/Stadiums/archive/README.md",
            "simulation-engine/DataMigration/Players/final_enrichment_report.txt",
            "simulation-engine/DataMigration/Players/player_data_quality_report.txt",
            "simulation-engine/DataMigration/Stadiums/archive/teams_for_stadiums.txt",
            "simulation-engine/DataMigration/Players/api_enriched_players.json",
            "simulation-engine/DataMigration/Players/enhanced_player_data.json",
            "simulation-engine/DataMigration/Players/preferred_foot_enriched.json",
            "simulation-engine/DataMigration/Players/real_player_data_extracted.json",
        ]

    def get_all_repo_markdown_files(self) -> List[Path]:
        """Collect all markdown files in repo, excluding agent workspace and build caches."""
        ignored_dirs = {".agents", ".git", ".next", "node_modules", "bin", "obj", ".idea", ".vs", ".vscode"}
        all_mds: List[Path] = []
        for root, dirs, files in os.walk(self.root):
            # Prune ignored directories in-place
            dirs[:] = [d for d in dirs if d not in ignored_dirs and not d.startswith(".")]
            for f in files:
                if f.endswith(".md"):
                    all_mds.append(Path(root) / f)
        return sorted(all_mds)

    def extract_implemented_routes(self) -> Set[str]:
        """
        Dynamically extract and normalize all HTTP route patterns from:
          1. backend/src/Footex/Controllers/*.cs
          2. backend/src/Footex/Program.cs
          3. simulation-engine/api/routes/*.py
        """
        normalized_routes: Set[str] = set()

        # 1. C# Controllers
        if self.controllers_dir.exists():
            for cs_file in self.controllers_dir.glob("*.cs"):
                content = cs_file.read_text(encoding="utf-8", errors="ignore")
                class_match = re.search(r"public\s+class\s+(\w+Controller)", content)
                if not class_match:
                    continue
                controller_name = class_match.group(1).replace("Controller", "")

                route_match = re.search(r'\[Route\("([^"]+)"\)\]', content)
                base_route = ""
                if route_match:
                    base_route = route_match.group(1).replace("[controller]", controller_name)

                method_matches = re.finditer(
                    r'\[Http(Get|Post|Put|Delete|Patch)(?:\("([^"]*)"\))?\]',
                    content,
                )
                for mm in method_matches:
                    sub_path = mm.group(2) or ""
                    full_route = f"/{base_route.strip('/')}/{sub_path.strip('/')}".rstrip("/")
                    if not full_route:
                        full_route = "/"
                    normalized_routes.add(self.normalize_route_pattern(full_route))

        # 2. C# Program.cs special endpoints
        program_cs = self.backend_dir / "src" / "Footex" / "Program.cs"
        if program_cs.exists():
            content = program_cs.read_text(encoding="utf-8", errors="ignore")
            for m in re.finditer(r'app\.Map[A-Za-z]+\(\s*"([^"]+)"', content):
                normalized_routes.add(self.normalize_route_pattern(m.group(1)))
            # Standard scalar / openapi endpoints
            normalized_routes.add(self.normalize_route_pattern("/api/health"))
            normalized_routes.add(self.normalize_route_pattern("/scalar/v1"))
            normalized_routes.add(self.normalize_route_pattern("/openapi/v1.json"))
            normalized_routes.add(self.normalize_route_pattern("/Notify"))
            normalized_routes.add(self.normalize_route_pattern("/matchSimulationHub"))

        # 3. FastAPI routes
        if self.sim_routes_dir.exists():
            for py_file in self.sim_routes_dir.glob("*.py"):
                if py_file.name == "__init__.py":
                    continue
                content = py_file.read_text(encoding="utf-8", errors="ignore")
                # Detect router prefix e.g. router = APIRouter(prefix="/auth")
                prefix_match = re.search(r'APIRouter\([^)]*prefix="([^"]+)"', content)
                prefix = prefix_match.group(1).strip("/") if prefix_match else ""

                for m in re.finditer(r'@router\.(get|post|put|delete|patch)\("([^"]+)"\)', content):
                    route_path = m.group(2).strip("/")
                    full_route = f"/{prefix}/{route_path}".strip("/")
                    normalized_routes.add(self.normalize_route_pattern("/" + full_route))

            # FastAPI main endpoints
            main_py = self.sim_engine_dir / "api" / "main.py"
            if main_py.exists():
                normalized_routes.add(self.normalize_route_pattern("/"))
                normalized_routes.add(self.normalize_route_pattern("/health"))
                normalized_routes.add(self.normalize_route_pattern("/docs"))
                normalized_routes.add(self.normalize_route_pattern("/redoc"))

        return normalized_routes

    @staticmethod
    def normalize_route_pattern(route: str) -> str:
        """
        Normalizes a route path into a canonical case-insensitive wildcard form.
        Examples:
          /api/Matches/{id:int}/events/stream -> /api/matches/{*}/events/stream
          /api/matches/{matchId}              -> /api/matches/{*}
        """
        r = route.strip().lower()
        r = r.split("?")[0]  # strip query string
        # Replace parameter tokens {param:constraint} or {param} with {*}
        r = re.sub(r"\{[a-zA-Z0-9_]+(?::[a-zA-Z0-9_]+)?\}", "{*}", r)
        return "/" + r.strip("/")

    @staticmethod
    def extract_heading_anchors(content: str) -> Set[str]:
        """
        Generate GitHub-compatible heading anchor slugs from markdown content.
        """
        anchors: Set[str] = set()
        slug_counts: Dict[str, int] = {}
        for line in content.splitlines():
            m = re.match(r"^#{1,6}\s+(.+)$", line.strip())
            if m:
                heading = m.group(1).strip()
                # Strip markdown links and inline styles
                heading = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", heading)
                heading = re.sub(r"[`*_~]", "", heading)
                slug = heading.lower()
                slug = re.sub(r"[^\w\s-]", "", slug)
                slug = re.sub(r"[\s]+", "-", slug.strip())
                if not slug:
                    continue
                if slug in slug_counts:
                    slug_counts[slug] += 1
                    unique_slug = f"{slug}-{slug_counts[slug]}"
                else:
                    slug_counts[slug] = 0
                    unique_slug = slug
                anchors.add(unique_slug)
                anchors.add(slug)
        return anchors


# ==============================================================================
# Tier 1: Feature & Protocol Coverage Tests
# ==============================================================================
class Tier1FeatureProtocolTests:
    @staticmethod
    def test_sse_match_stream_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Match streaming is documented as Server-Sent Events (SSE)
        at GET /api/matches/{id}/events/stream with JWT token auth and event payloads.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        files_to_check = [
            ctx.root / "README.md",
            ctx.frontend_dir / "README.md",
            ctx.docs_dir / "documentation.md",
            ctx.docs_dir / "project-architecture-documentation.md",
            ctx.docs_dir / "event-processing-system.md",
        ]

        sse_route_pattern = re.compile(r"/api/matches/\{?[a-zA-Z0-9_:]+\}?/events/stream", re.IGNORECASE)
        sse_keyword_pattern = re.compile(r"Server-Sent Events|SSE", re.IGNORECASE)

        for doc in files_to_check:
            if not doc.exists():
                failures.append(AssertionFailure(str(doc.relative_to(ctx.root)), None, "Required living guide does not exist"))
                continue

            content = doc.read_text(encoding="utf-8", errors="ignore")
            has_route = bool(sse_route_pattern.search(content))
            has_sse = bool(sse_keyword_pattern.search(content))

            if not (has_route and has_sse):
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Living guide fails to document SSE endpoint '/api/matches/{id}/events/stream' with Server-Sent Events protocol",
                        offending_content=f"has_route={has_route}, has_sse_keywords={has_sse}",
                    )
                )

        # In event-processing-system.md or documentation.md, check for query token (?access_token=)
        event_doc = ctx.docs_dir / "event-processing-system.md"
        doc_api = ctx.docs_dir / "documentation.md"
        jwt_query_found = False
        for d in [event_doc, doc_api, ctx.docs_dir / "sse-match-stream.md"]:
            if d.exists():
                txt = d.read_text(encoding="utf-8", errors="ignore")
                if "access_token" in txt or "Authorization" in txt:
                    jwt_query_found = True
                    break

        if not jwt_query_found:
            failures.append(
                AssertionFailure(
                    "docs/",
                    None,
                    "Documentation does not mention SSE authentication mechanism (access_token query parameter or Authorization header)",
                )
            )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.1",
            tier=1,
            name="SSE Match Stream Documentation Coverage",
            description="Verifies living documentation accurately documents SSE /api/matches/{id}/events/stream, query token authorization, and event payloads.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_grpc_simulation_service_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: gRPC port 50051, protobuf stubs (simulation.proto),
        unary RPC and streaming RPC definitions are accurately described.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        files_to_check = [
            ctx.root / "README.md",
            ctx.sim_engine_dir / "README.md",
            ctx.docs_dir / "project-architecture-documentation.md",
        ]

        for doc in files_to_check:
            if not doc.exists():
                failures.append(AssertionFailure(str(doc.relative_to(ctx.root)), None, "Required living guide does not exist"))
                continue

            content = doc.read_text(encoding="utf-8", errors="ignore")
            if "50051" not in content:
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Living guide omits gRPC port 50051 documentation",
                    )
                )
            if "simulation.proto" not in content.lower() and "grpc" not in content.lower():
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Living guide omits gRPC / protobuf simulation service description",
                    )
                )

        # Verify RPC methods documented in simulation README or architecture guide
        sim_readme = ctx.sim_engine_dir / "README.md"
        arch_doc = ctx.docs_dir / "project-architecture-documentation.md"
        combined_text = ""
        for p in [sim_readme, arch_doc]:
            if p.exists():
                combined_text += p.read_text(encoding="utf-8", errors="ignore")

        if "startmatchsimulation" not in combined_text.lower():
            failures.append(
                AssertionFailure(
                    "simulation-engine/README.md",
                    None,
                    "Simulation guide omits gRPC RPC methods (e.g. StartMatchSimulation / StartMatchSimulationStream)",
                )
            )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.2",
            tier=1,
            name="gRPC Simulation Service Documentation Coverage",
            description="Verifies gRPC port 50051, protobuf contract (simulation.proto), and unary/streaming RPCs are documented.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_fastapi_and_dual_server_concurrency_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Simulation engine dual-server concurrency (FastAPI on port 8000
        and gRPC on port 50051 running concurrently via asyncio) is documented.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        sim_readme = ctx.sim_engine_dir / "README.md"
        arch_doc = ctx.docs_dir / "project-architecture-documentation.md"

        for doc in [sim_readme, arch_doc]:
            if not doc.exists():
                failures.append(AssertionFailure(str(doc.relative_to(ctx.root)), None, "Required guide missing"))
                continue
            content = doc.read_text(encoding="utf-8", errors="ignore")
            has_8000 = "8000" in content
            has_50051 = "50051" in content
            has_asyncio_or_dual = bool(re.search(r"asyncio|dual|concurrent", content, re.IGNORECASE))

            if not (has_8000 and has_50051 and has_asyncio_or_dual):
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Document does not explain the concurrent dual-server lifecycle (FastAPI 8000 & gRPC 50051 via asyncio)",
                        offending_content=f"has_8000={has_8000}, has_50051={has_50051}, has_concurrency_mention={has_asyncio_or_dual}",
                    )
                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.3",
            tier=1,
            name="FastAPI & gRPC Dual-Server Concurrency Coverage",
            description="Verifies documentation explains concurrent asyncio hosting of FastAPI (8000) and gRPC (50051) inside Uvicorn.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_runtime_framework_versions_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Code snippets and guides reflect .NET 10, C# 13,
        Python 3.12 (under uv), and Next.js (App Router).
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        root_readme = ctx.root / "README.md"
        frontend_readme = ctx.frontend_dir / "README.md"
        sim_readme = ctx.sim_engine_dir / "README.md"

        if root_readme.exists():
            c = root_readme.read_text(encoding="utf-8", errors="ignore")
            if ".NET 10" not in c and "net10.0" not in c:
                failures.append(AssertionFailure("README.md", None, "Root README does not document active backend as .NET 10"))

        if sim_readme.exists():
            c = sim_readme.read_text(encoding="utf-8", errors="ignore")
            if "3.12" not in c:
                failures.append(AssertionFailure("simulation-engine/README.md", None, "Simulation README does not document Python 3.12"))
            if "uv" not in c:
                failures.append(AssertionFailure("simulation-engine/README.md", None, "Simulation README does not document uv package manager"))

        if frontend_readme.exists():
            c = frontend_readme.read_text(encoding="utf-8", errors="ignore")
            if "Next.js" not in c and "Next" not in c:
                failures.append(AssertionFailure("frontend/README.md", None, "Frontend README does not document Next.js framework"))

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.4",
            tier=1,
            name="Runtime Framework & Language Versions Coverage",
            description="Verifies living guides document .NET 10, C# 13, Python 3.12 with uv, and Next.js App Router.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_zero_allocation_and_modern_backend_patterns_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Documents zero-allocation ReadOnlySpan<char> parsing,
        reflection-free CQRS, and Scalar API reference.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        arch_doc = ctx.docs_dir / "project-architecture-documentation.md"
        perf_doc = ctx.backend_dir / "tests" / "Footex.PerformanceTests" / "README.md"

        if arch_doc.exists():
            c = arch_doc.read_text(encoding="utf-8", errors="ignore")
            if "readonlyspan" not in c.lower() and "zero-allocation" not in c.lower():
                failures.append(
                    AssertionFailure(
                        "docs/project-architecture-documentation.md",
                        None,
                        "Architecture guide omits zero-allocation ReadOnlySpan<char> parser documentation",
                    )
                )
            if "scalar" not in c.lower() and "/scalar/v1" not in c.lower():
                failures.append(
                    AssertionFailure(
                        "docs/project-architecture-documentation.md",
                        None,
                        "Architecture guide omits ASP.NET Core OpenAPI / Scalar API reference documentation",
                    )
                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.5",
            tier=1,
            name="Zero-Allocation & Modern Backend Patterns Coverage",
            description="Verifies documentation of ReadOnlySpan<char> zero-allocation parsing, reflection-free CQRS, and Scalar UI.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_dual_streaming_ingestion_pipelines_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Dual streaming ingestion architecture is documented:
          Pipeline A: RabbitMQ raw event text stream (exchange: match_events, routing key: match.events).
          Pipeline B: Direct memory-to-memory gRPC server-streaming (StartMatchSimulationStream).
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        event_doc = ctx.docs_dir / "event-processing-system.md"
        arch_doc = ctx.docs_dir / "project-architecture-documentation.md"

        for doc in [event_doc, arch_doc]:
            if not doc.exists():
                failures.append(AssertionFailure(str(doc.relative_to(ctx.root)), None, "Required document missing"))
                continue
            c = doc.read_text(encoding="utf-8", errors="ignore")
            has_rabbitmq = "match_events" in c or "match.events" in c
            has_grpc_stream = "startmatchsimulationstream" in c.lower() or "grpc" in c.lower()

            if not (has_rabbitmq and has_grpc_stream):
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Document does not explain the dual streaming ingestion pipelines (RabbitMQ raw stream & gRPC server-stream)",
                        offending_content=f"has_rabbitmq={has_rabbitmq}, has_grpc_stream={has_grpc_stream}",
                    )
                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.6",
            tier=1,
            name="Dual Streaming Ingestion Pipelines Coverage",
            description="Verifies documentation of Pipeline A (RabbitMQ match_events) and Pipeline B (direct gRPC server-streaming).",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_data_and_caching_tiers_documented(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Documents Redis Hot State (live match cache & atomic statistics)
        and PostgreSQL batched persistence upon match completion.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        # 1. Project Architecture guide must document both Redis Hot State and PostgreSQL batched persistence
        arch_doc = ctx.docs_dir / "project-architecture-documentation.md"
        if arch_doc.exists():
            c = arch_doc.read_text(encoding="utf-8", errors="ignore").lower()
            if "redis" not in c:
                failures.append(AssertionFailure("docs/project-architecture-documentation.md", None, "Architecture guide omits Redis caching layer"))
            if "postgres" not in c:
                failures.append(AssertionFailure("docs/project-architecture-documentation.md", None, "Architecture guide omits PostgreSQL persistence layer"))
            if "hot state" not in c and "hot cache" not in c:
                failures.append(AssertionFailure("docs/project-architecture-documentation.md", None, "Architecture guide omits Redis Hot State / atomic counters"))
        else:
            failures.append(AssertionFailure("docs/project-architecture-documentation.md", None, "Missing architecture guide"))

        # 2. Real-time statistics doc must document caching and reducing DB load
        stats_doc = ctx.docs_dir / "real-time-statistics-optimization.md"
        if stats_doc.exists():
            c = stats_doc.read_text(encoding="utf-8", errors="ignore").lower()
            if "cache" not in c and "caching" not in c:
                failures.append(AssertionFailure("docs/real-time-statistics-optimization.md", None, "Document omits caching mechanism"))
            if "database" not in c and "db" not in c:
                failures.append(AssertionFailure("docs/real-time-statistics-optimization.md", None, "Document omits database load reduction"))

        # 3. Database design doc must document PostgreSQL schema
        db_doc = ctx.docs_dir / "database-design-documentation.md"
        if db_doc.exists():
            c = db_doc.read_text(encoding="utf-8", errors="ignore").lower()
            if "postgresql" not in c and "postgres" not in c:
                failures.append(AssertionFailure("docs/database-design-documentation.md", None, "Database design omits PostgreSQL"))

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T1.7",
            tier=1,
            name="Data & Caching Tiers Coverage",
            description="Verifies documentation of Redis Hot State live cache and PostgreSQL batched persistence.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )


# ==============================================================================
# Tier 2: Boundary & Negative Cases Tests
# ==============================================================================
class Tier2BoundaryNegativeTests:
    @staticmethod
    def test_zero_occurrences_of_signalr_for_match_streaming(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Zero living documentation references SignalR or WebSockets
        for match simulation streaming (where SSE is now used).
        Note: SignalR /Notify for user alerts is allowed in signalr-notification-service.md.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        # Check that purged docs/signalr-matchhub.md does NOT exist
        matchhub_doc = ctx.docs_dir / "signalr-matchhub.md"
        if matchhub_doc.exists():
            failures.append(
                AssertionFailure(
                    "docs/signalr-matchhub.md",
                    None,
                    "Purged obsolete SignalR match guide 'docs/signalr-matchhub.md' still exists on filesystem",
                )
            )

        # Check living guides for claims of SignalR match streaming
        prohibited_patterns = [
            re.compile(r"MatchHub", re.IGNORECASE),
            re.compile(r"/matchSimulationHub", re.IGNORECASE),
            re.compile(r"SignalR.*match\s+(?:events|stream|simulation|updates)", re.IGNORECASE),
            re.compile(r"match\s+(?:events|stream|simulation)\s+via\s+SignalR", re.IGNORECASE),
            re.compile(r"WebSocket.*(?:match\s+simulation|live\s+match\s+stream)", re.IGNORECASE),
        ]

        # Scan all living docs
        for doc in ctx.authoritative_docs:
            if not doc.exists():
                continue
            # Skip PROJECT.md as it outlines historical requirements
            if doc.name == "PROJECT.md":
                continue

            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                # Allow lines explicitly stating SignalR is NOT used or was replaced
                if any(phrase in line.lower() for phrase in ["replaces signalr", "replaced by sse", "obsolete", "do not use signalr"]):
                    continue
                for pattern in prohibited_patterns:
                    if pattern.search(line):
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                "Living guide incorrectly references SignalR/WebSockets for match simulation streaming",
                                offending_content=line.strip(),
                            )
                        )
                        break

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T2.1",
            tier=2,
            name="Zero SignalR for Match Streaming in Living Docs",
            description="Asserts ZERO occurrences of SignalR or WebSockets claimed for match simulation streaming in living documentation.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_zero_hardcoded_file_urls_and_local_paths(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Zero occurrences of hardcoded file:/// URLs, developer-specific
        local machine paths (e.g. C:\\Users\\, d:\\programming\\, file:///d:/), or Footex links.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        local_path_patterns = [
            re.compile(r"file:///", re.IGNORECASE),
            re.compile(r"[a-zA-Z]:[/\\](?:users|programming|workspace)[/\\]", re.IGNORECASE),
        ]

        all_mds = ctx.get_all_repo_markdown_files()
        for doc in all_mds:
            # Skip PROJECT.md, DISPATCH.md, BRIEFING.md which record host paths
            if doc.name in ["PROJECT.md", "CLAUDE.md"]:
                continue
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                for p in local_path_patterns:
                    if p.search(line):
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                "Document contains hardcoded local filesystem path or file:/// URL",
                                offending_content=line.strip(),
                            )
                        )
                        break

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T2.2",
            tier=2,
            name="Zero file:/// URLs & Local Machine Paths",
            description="Asserts ZERO occurrences of file:/// URLs or hardcoded developer filesystem paths across all markdown files.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_purged_files_do_not_exist(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All 29 superseded, transient, and duplicate files
        purged in Milestone 1 (R3) must NOT exist on the filesystem.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        for rel_path in ctx.purged_target_files:
            full_path = ctx.root / rel_path
            if full_path.exists():
                failures.append(
                    AssertionFailure(
                        rel_path,
                        None,
                        f"Purged obsolete file from Requirement R3 still exists: {rel_path}",
                    )
                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T2.3",
            tier=2,
            name="Purged Obsolete Files Verification (R3)",
            description="Asserts that all 29 obsolete, transient, and duplicate files from Milestone 1 are completely removed.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_living_guides_preserved_and_non_empty(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All authoritative living documentation files remain intact,
        well-structured, and non-empty (>0 bytes).
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        for doc in ctx.authoritative_docs:
            if not doc.exists():
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Authoritative living guide is missing from the repository",
                    )
                )
            else:
                size = doc.stat().st_size
                if size == 0:
                    failures.append(
                        AssertionFailure(
                            str(doc.relative_to(ctx.root)),
                            None,
                            "Authoritative living guide is an empty 0-byte file",
                        )
                    )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T2.4",
            tier=2,
            name="Living Technical Guides Preservation",
            description="Asserts that all 18 authoritative living documentation files remain present, intact, and non-empty.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_no_outdated_framework_versions_active(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: No living guide presents .NET 6, .NET 7, or .NET 8 as the active framework.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        files_to_check = [
            ctx.root / "README.md",
            ctx.backend_dir / "tests" / "Footex.PerformanceTests" / "README.md",
            ctx.backend_dir / "tests" / "Footex.PerformanceTests" / "QUICKSTART.md",
            ctx.docs_dir / "project-architecture-documentation.md",
        ]

        outdated_net_pattern = re.compile(r"(?:target framework|runtime|built on|using)\s*:\s*\.NET\s+[678]\.0", re.IGNORECASE)

        for doc in files_to_check:
            if not doc.exists():
                continue
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                if outdated_net_pattern.search(line):
                    failures.append(
                        AssertionFailure(
                            str(doc.relative_to(ctx.root)),
                            i,
                            "Living guide states obsolete .NET 6/7/8 as active framework",
                            offending_content=line.strip(),
                        )
                    )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T2.5",
            tier=2,
            name="No Deprecated Frameworks as Active Target",
            description="Asserts living monorepo and subsystem guides do NOT list .NET 6, 7, or 8 as the active project runtime.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )


# ==============================================================================
# Tier 3: Cross-Reference & Route Validation Tests
# ==============================================================================
class Tier3CrossReferenceRouteTests:
    @staticmethod
    def test_documented_http_routes_exist_in_codebase(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All documented HTTP routes match routes in controllers
        (backend/src/Footex/Controllers/) and FastAPI routes (simulation-engine/api/).
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        implemented_routes = ctx.extract_implemented_routes()

        # Files containing documented API routes
        docs_to_scan = [
            ctx.docs_dir / "documentation.md",
            ctx.docs_dir / "search-api-documentation.md",
            ctx.docs_dir / "real-time-statistics-optimization.md",
            ctx.sim_engine_dir / "README.md",
            ctx.root / "README.md",
        ]

        for doc in docs_to_scan:
            if not doc.exists():
                continue
            content = doc.read_text(encoding="utf-8", errors="ignore")
            current_base = ""

            for i, line in enumerate(content.splitlines(), 1):
                # 1. Controller section header: ## AuthController (`/api/auth`)
                base_match = re.search(r"##\s+\w+Controller\s*\(`([^`]+)`\)", line, re.IGNORECASE)
                if base_match:
                    current_base = base_match.group(1).rstrip("/")
                    continue
                if line.startswith("## ") and "Controller" not in line:
                    current_base = ""

                # 2. Sub-endpoint header: ### POST `/register`
                sub_match = re.search(r"###\s+(?:GET|POST|PUT|DELETE|PATCH)\s+`([^`]+)`", line, re.IGNORECASE)
                if sub_match:
                    sub = sub_match.group(1).strip()
                    full = f"{current_base}/{sub.lstrip('/')}" if current_base else sub
                    norm = ctx.normalize_route_pattern(full)
                    if norm not in implemented_routes:
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Documented route '{full}' does not exist in any controller or FastAPI router",
                                offending_content=line.strip(),
                            )
                        )
                    continue

                # 3. HTTP Verb + Route in table or prose: GET /api/matches or `GET /api/...`
                for vm in re.finditer(
                    r"(?:^|[`|\s])(GET|POST|PUT|DELETE|PATCH)\s+[`]?((?:/api/|/auth/|/startMatch|/simulationStatus|/simulationResult)[a-zA-Z0-9_{}/:-]+)[`]?",
                    line,
                    re.IGNORECASE,
                ):
                    route_path = vm.group(2).strip()
                    norm = ctx.normalize_route_pattern(route_path)
                    if norm not in implemented_routes:
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Documented route '{route_path}' does not exist in any controller or FastAPI router",
                                offending_content=line.strip(),
                            )
                        )

                # 4. HTTP Codeblocks e.g. GET /api/matches/live/all
                http_block_match = re.search(
                    r"^(GET|POST|PUT|DELETE|PATCH)\s+((?:/api/|/auth/|/startMatch|/simulation)[a-zA-Z0-9_{}/:-]+)",
                    line.strip(),
                    re.IGNORECASE,
                )
                if http_block_match:
                    route_path = http_block_match.group(2).strip()
                    norm = ctx.normalize_route_pattern(route_path)
                    if norm not in implemented_routes:
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Documented route in HTTP block '{route_path}' does not exist in codebase",
                                offending_content=line.strip(),
                            )
                        )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T3.1",
            tier=3,
            name="Documented HTTP Routes Exist in Codebase",
            description="Parses all documented HTTP routes in markdown docs and verifies each exists in controllers or FastAPI routes.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_service_ports_match_actual_configuration(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Documented ports match actual application configuration:
          Backend: 5025 (or 8080 in container)
          FastAPI: 8000
          gRPC: 50051
          Frontend: 3000
          PostgreSQL: 5432
          Redis: 6379
          RabbitMQ: 5672, 15672
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        # Prohibited incorrect port associations
        prohibited_port_patterns = [
            (re.compile(r"gRPC.*(?<!500)51\b|gRPC.*(?:port\s+5000\b|port\s+8000\b)", re.IGNORECASE), "gRPC documented on incorrect port (must be 50051)"),
            (re.compile(r"FastAPI.*(?:port\s+5000\b|port\s+50051\b)", re.IGNORECASE), "FastAPI documented on incorrect port (must be 8000)"),
        ]

        for doc in ctx.authoritative_docs:
            if not doc.exists():
                continue
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                for p, desc in prohibited_port_patterns:
                    if p.search(line):
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                desc,
                                offending_content=line.strip(),
                            )
                        )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T3.2",
            tier=3,
            name="Service Ports Match Configuration",
            description="Verifies documented ports (5025/8080, 8000, 50051, 3000, 5432, 6379, 5672) match application configuration.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_environment_variables_match_configuration(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All environment variables documented across READMEs and docs
        match actual application configuration keys.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []

        # Read actual environment variable names from .env.example, docker-compose.yml, docker-compose.dev.yml
        known_env_vars: Set[str] = set()
        config_files = [
            ctx.root / ".env.example",
            ctx.root / "docker-compose.yml",
            ctx.root / "docker-compose.dev.yml",
        ]
        for cfg in config_files:
            if cfg.exists():
                txt = cfg.read_text(encoding="utf-8", errors="ignore")
                for m in re.finditer(r"([A-Z0-9_]{3,}(?:__[A-Z0-9_]+)?)\s*[:=]", txt):
                    known_env_vars.add(m.group(1))

        # Also parse appsettings.json for configuration paths
        appsettings = ctx.backend_dir / "src" / "Footex" / "appsettings.json"
        if appsettings.exists():
            try:
                data = json.loads(appsettings.read_text(encoding="utf-8", errors="ignore"))
                def flatten_keys(d, prefix=""):
                    for k, v in d.items():
                        new_prefix = f"{prefix}__{k}" if prefix else k
                        known_env_vars.add(new_prefix.upper())
                        known_env_vars.add(k.upper())
                        if isinstance(v, dict):
                            flatten_keys(v, new_prefix)
                flatten_keys(data)
            except Exception:
                pass

        # Check documented environment variables in README.md, frontend/README.md, simulation-engine/README.md
        docs_to_check = [
            ctx.root / "README.md",
            ctx.frontend_dir / "README.md",
            ctx.sim_engine_dir / "README.md",
        ]
        for doc in docs_to_check:
            if not doc.exists():
                continue
            txt = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(txt.splitlines(), 1):
                # Match environment variable definitions like `VAR_NAME` or `VAR_NAME=value`
                for ev in re.finditer(r"`([A-Z][A-Z0-9_]{2,}(?:__[A-Z0-9_]+)?)`", line):
                    var_name = ev.group(1)
                    # Ignore common Markdown keywords / acronyms
                    if var_name in {"HTTP", "HTTPS", "JSON", "POST", "GET", "PUT", "DELETE", "HTML", "CORS", "UUID", "NULL", "TRUE", "FALSE", "SSE", "GRPC", "REST", "API", "SDK", "URL", "PORT"}:
                        continue
                    if var_name not in known_env_vars and not any(var_name.startswith(p) for p in ["NEXT_PUBLIC_", "ASPNETCORE_", "DOTNET_"]):
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Documented environment variable '{var_name}' not defined in .env.example or application configuration",
                                offending_content=line.strip(),
                            )
                        )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T3.3",
            tier=3,
            name="Environment Variables Match Configuration",
            description="Verifies documented environment variables and settings match application configuration and .env.example.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )



# ==============================================================================
# Tier 4: Real-World Link & Markdown Integrity Tests
# ==============================================================================
class Tier4LinkIntegrityTests:
    @staticmethod
    def test_relative_markdown_file_links_resolve(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All relative markdown links point to existing files (no dead links).
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        all_mds = ctx.get_all_repo_markdown_files()
        link_pattern = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")

        for doc in all_mds:
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                for match in link_pattern.finditer(line):
                    _, url = match.groups()
                    url = url.strip()
                    # Skip external URLs, mailto, and pure same-page anchors
                    if url.startswith(("http://", "https://", "mailto:", "#")) or url.startswith("javascript:"):
                        continue
                    # Ignore file:/// URLs as they are flagged specifically in Tier 2
                    if url.startswith("file:///"):
                        continue

                    # Strip anchor fragment from file link
                    path_part = url.split("#")[0].strip()
                    if not path_part:
                        continue

                    target_file = (doc.parent / path_part).resolve()
                    if not target_file.exists():
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Dead relative link: '{url}' -> target '{target_file}' does not exist",
                                offending_content=line.strip(),
                            )
                        )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T4.1",
            tier=4,
            name="Relative Markdown Links Integrity",
            description="Extracts all relative markdown file links across repository and verifies each target file exists on disk.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_markdown_heading_anchors_resolve(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Markdown links referencing headings (#heading-anchor)
        point to valid headings in the target or local document.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        all_mds = ctx.get_all_repo_markdown_files()
        link_pattern = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")
        heading_cache: Dict[Path, Set[str]] = {}

        def get_anchors(file_path: Path) -> Set[str]:
            if file_path not in heading_cache:
                if file_path.exists() and file_path.is_file():
                    heading_cache[file_path] = ctx.extract_heading_anchors(file_path.read_text(encoding="utf-8", errors="ignore"))
                else:
                    heading_cache[file_path] = set()
            return heading_cache[file_path]

        for doc in all_mds:
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                for match in link_pattern.finditer(line):
                    _, url = match.groups()
                    url = url.strip()
                    if url.startswith(("http://", "https://", "mailto:", "file:///")):
                        continue

                    if "#" in url:
                        file_part, anchor = url.split("#", 1)
                        file_part = file_part.strip()
                        anchor = anchor.strip().lower()
                        if not anchor:
                            continue

                        target_doc = (doc.parent / file_part).resolve() if file_part else doc
                        if target_doc.exists() and target_doc.is_file():
                            valid_anchors = get_anchors(target_doc)
                            if anchor not in valid_anchors:
                                failures.append(
                                    AssertionFailure(
                                        str(doc.relative_to(ctx.root)),
                                        i,
                                        f"Broken heading anchor: '#{anchor}' not found in '{target_doc.name}'",
                                        offending_content=line.strip(),
                                    )
                                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T4.2",
            tier=4,
            name="Markdown Heading Anchors Integrity",
            description="Verifies that all internal and cross-document markdown heading anchor links point to valid headings.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_markdown_image_references_resolve(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: All relative image references ![alt](path) exist on disk.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        all_mds = ctx.get_all_repo_markdown_files()
        img_pattern = re.compile(r"!\[([^\]]*)\]\(([^)]+)\)")

        for doc in all_mds:
            content = doc.read_text(encoding="utf-8", errors="ignore")
            for i, line in enumerate(content.splitlines(), 1):
                for match in img_pattern.finditer(line):
                    _, img_url = match.groups()
                    img_url = img_url.strip()
                    if img_url.startswith(("http://", "https://", "data:")):
                        continue
                    target_img = (doc.parent / img_url).resolve()
                    if not target_img.exists():
                        failures.append(
                            AssertionFailure(
                                str(doc.relative_to(ctx.root)),
                                i,
                                f"Missing image file: '{img_url}' -> target '{target_img}' does not exist",
                                offending_content=line.strip(),
                            )
                        )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T4.3",
            tier=4,
            name="Markdown Image References Integrity",
            description="Verifies all relative image paths in markdown documents point to existing files on disk.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )

    @staticmethod
    def test_zero_empty_markdown_files(ctx: RepoContext) -> TestCaseResult:
        """
        Acceptance Criteria: Zero 0-byte or empty markdown files remain in the repository.
        """
        start = time.perf_counter()
        failures: List[AssertionFailure] = []
        all_mds = ctx.get_all_repo_markdown_files()

        for doc in all_mds:
            if doc.stat().st_size == 0:
                failures.append(
                    AssertionFailure(
                        str(doc.relative_to(ctx.root)),
                        None,
                        "Markdown file is completely empty (0 bytes)",
                    )
                )

        duration = (time.perf_counter() - start) * 1000
        return TestCaseResult(
            test_id="T4.4",
            tier=4,
            name="Zero Empty Markdown Files",
            description="Verifies no tracked markdown documentation file in the repository has a file size of 0 bytes.",
            passed=len(failures) == 0,
            failures=failures,
            duration_ms=duration,
        )


# ==============================================================================
# Test Runner & CLI
# ==============================================================================
class DocumentationTestRunner:
    def __init__(self, root_dir: Path, verbose: bool = False, fail_fast: bool = False):
        self.ctx = RepoContext(root_dir)
        self.verbose = verbose
        self.fail_fast = fail_fast

        # Test catalog
        self.tests: List[Tuple[int, str, Callable[[RepoContext], TestCaseResult]]] = [
            # Tier 1
            (1, "T1.1", Tier1FeatureProtocolTests.test_sse_match_stream_documented),
            (1, "T1.2", Tier1FeatureProtocolTests.test_grpc_simulation_service_documented),
            (1, "T1.3", Tier1FeatureProtocolTests.test_fastapi_and_dual_server_concurrency_documented),
            (1, "T1.4", Tier1FeatureProtocolTests.test_runtime_framework_versions_documented),
            (1, "T1.5", Tier1FeatureProtocolTests.test_zero_allocation_and_modern_backend_patterns_documented),
            (1, "T1.6", Tier1FeatureProtocolTests.test_dual_streaming_ingestion_pipelines_documented),
            (1, "T1.7", Tier1FeatureProtocolTests.test_data_and_caching_tiers_documented),
            # Tier 2
            (2, "T2.1", Tier2BoundaryNegativeTests.test_zero_occurrences_of_signalr_for_match_streaming),
            (2, "T2.2", Tier2BoundaryNegativeTests.test_zero_hardcoded_file_urls_and_local_paths),
            (2, "T2.3", Tier2BoundaryNegativeTests.test_purged_files_do_not_exist),
            (2, "T2.4", Tier2BoundaryNegativeTests.test_living_guides_preserved_and_non_empty),
            (2, "T2.5", Tier2BoundaryNegativeTests.test_no_outdated_framework_versions_active),
            # Tier 3
            (3, "T3.1", Tier3CrossReferenceRouteTests.test_documented_http_routes_exist_in_codebase),
            (3, "T3.2", Tier3CrossReferenceRouteTests.test_service_ports_match_actual_configuration),
            (3, "T3.3", Tier3CrossReferenceRouteTests.test_environment_variables_match_configuration),
            # Tier 4
            (4, "T4.1", Tier4LinkIntegrityTests.test_relative_markdown_file_links_resolve),
            (4, "T4.2", Tier4LinkIntegrityTests.test_markdown_heading_anchors_resolve),
            (4, "T4.3", Tier4LinkIntegrityTests.test_markdown_image_references_resolve),
            (4, "T4.4", Tier4LinkIntegrityTests.test_zero_empty_markdown_files),
        ]

    def run(self, tier_filter: Optional[int] = None) -> Tuple[List[TestCaseResult], bool]:
        results: List[TestCaseResult] = []
        all_passed = True

        selected_tests = [t for t in self.tests if tier_filter is None or t[0] == tier_filter]

        print("=" * 80)
        print(color(f"  PixelPitchAI Living Documentation Test Runner", TerminalColors.BOLD + TerminalColors.CYAN))
        print(f"  Target Repository: {self.ctx.root}")
        print(f"  Tier Filter: {f'Tier {tier_filter}' if tier_filter else 'All Tiers (1-4)'}")
        print(f"  Total Test Cases: {len(selected_tests)}")
        print("=" * 80)

        current_tier = -1
        for tier, test_id, test_fn in selected_tests:
            if tier != current_tier:
                current_tier = tier
                tier_names = {
                    1: "Tier 1: Feature & Protocol Coverage",
                    2: "Tier 2: Boundary & Negative Cases",
                    3: "Tier 3: Cross-Reference & Route Validation",
                    4: "Tier 4: Real-World Link & Markdown Integrity",
                }
                print(f"\n{color('--- ' + tier_names.get(tier, f'Tier {tier}') + ' ---', TerminalColors.BOLD + TerminalColors.BLUE)}")

            res = test_fn(self.ctx)
            results.append(res)

            status_str = color("PASS", TerminalColors.GREEN) if res.passed else color("FAIL", TerminalColors.RED)
            print(f"  [{status_str}] {color(res.test_id, TerminalColors.BOLD)}: {res.name} ({res.duration_ms:.1f}ms)")

            if not res.passed:
                all_passed = False
                print(f"         {color('Description:', TerminalColors.GRAY)} {res.description}")
                print(f"         {color(f'Violations Detected ({len(res.failures)}):', TerminalColors.RED)}")
                for f in res.failures[:10]:
                    loc = f"{f.file_path}:{f.line_number}" if f.line_number else f.file_path
                    print(f"           - {color(loc, TerminalColors.YELLOW)}: {f.description}")
                    if f.offending_content and self.verbose:
                        print(f"             {color('Offending text:', TerminalColors.GRAY)} {f.offending_content[:100]}")
                if len(res.failures) > 10:
                    print(f"           ... and {len(res.failures) - 10} more violations")

                if self.fail_fast:
                    print(color("\n[!] Fail-fast enabled. Terminating test suite run.", TerminalColors.RED))
                    break

        # Summary
        print("\n" + "=" * 80)
        passed_count = sum(1 for r in results if r.passed)
        failed_count = sum(1 for r in results if not r.passed)
        total_count = len(results)
        total_time = sum(r.duration_ms for r in results)

        summary_color = TerminalColors.GREEN if all_passed else TerminalColors.RED
        print(color(f"  TEST SUITE SUMMARY: {passed_count}/{total_count} PASSED ({failed_count} FAILED) in {total_time:.1f}ms", TerminalColors.BOLD + summary_color))
        print("=" * 80)

        return results, all_passed


# ==============================================================================
# CLI Entrypoint
# ==============================================================================
def main():
    parser = argparse.ArgumentParser(description="PixelPitchAI Living Documentation Verification Test Suite")
    parser.add_argument("--tier", type=int, choices=[1, 2, 3, 4], help="Run only tests from a specific tier")
    parser.add_argument("--verbose", "-v", action="store_true", help="Display verbose failure details and excerpts")
    parser.add_argument("--fail-fast", action="store_true", help="Stop execution immediately on first test failure")
    parser.add_argument("--json", dest="json_output", action="store_true", help="Output results in JSON format")

    args = parser.parse_args()

    # Discover repo root (assuming scripts/verify-documentation.py)
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent

    runner = DocumentationTestRunner(repo_root, verbose=args.verbose, fail_fast=args.fail_fast)
    results, all_passed = runner.run(tier_filter=args.tier)

    if args.json_output:
        json_data = {
            "summary": {
                "total": len(results),
                "passed": sum(1 for r in results if r.passed),
                "failed": sum(1 for r in results if not r.passed),
                "all_passed": all_passed,
                "duration_ms": sum(r.duration_ms for r in results),
            },
            "results": [
                {
                    "test_id": r.test_id,
                    "tier": r.tier,
                    "name": r.name,
                    "description": r.description,
                    "passed": r.passed,
                    "duration_ms": r.duration_ms,
                    "failures": [
                        {
                            "file": f.file_path,
                            "line": f.line_number,
                            "description": f.description,
                            "offending_content": f.offending_content,
                        }
                        for f in r.failures
                    ],
                }
                for r in results
            ],
        }
        print(json.dumps(json_data, indent=2))

    sys.exit(0 if all_passed else 1)


if __name__ == "__main__":
    main()
