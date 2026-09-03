# PixelPitchAI Living Documentation Test Infrastructure (TEST_INFRA)

## 1. Executive Summary

This document defines the automated, opaque-box documentation testing infrastructure for **PixelPitchAI**. The documentation test suite ensures that all living guides across the monorepo remain strictly synchronized with active codebase implementations (.NET 10 Web API, Python simulation engine, Next.js frontend, and infrastructure tiers), while strictly prohibiting obsolete, broken, or hallucinated architectural claims.

The test infrastructure is driven by a standalone, dependency-free test runner:
- **Runner Path**: `scripts/verify-documentation.py`
- **Execution Command**: `python scripts/verify-documentation.py`
- **Python Compatibility**: Python 3.12+ (standard library only: `argparse`, `dataclasses`, `json`, `pathlib`, `re`, `sys`, `time`)
- **Exit Code Convention**: `0` on 100% test pass, `1` on one or more test assertion failures.

---

## 2. Test Architecture: 4-Tier Testing Methodology

The verification runner enforces a 4-tier validation model:

```
+-----------------------------------------------------------------------------------+
|                        PixelPitchAI Documentation Test Track                      |
+-----------------------------------------------------------------------------------+
| Tier 1: Feature & Protocol Coverage                                               |
|   - T1.1: Server-Sent Events (SSE) Match Stream (/api/matches/{id}/events/stream)  |
|   - T1.2: gRPC Simulation Service (Port 50051, simulation.proto, Streaming RPC)   |
|   - T1.3: FastAPI REST (Port 8000) & Asyncio Dual-Server Concurrency in Uvicorn   |
|   - T1.4: Active Runtime Frameworks (.NET 10, C# 13, Python 3.12, uv, Next.js)   |
|   - T1.5: Modern Backend Architecture (ReadOnlySpan<char>, CQRS, Scalar UI)       |
|   - T1.6: Dual Streaming Ingestion (Pipeline A: RabbitMQ, Pipeline B: gRPC Stream)|
|   - T1.7: Data & Caching Tiers (Redis Hot State Counters & PostgreSQL Batched DB) |
+-----------------------------------------------------------------------------------+
| Tier 2: Boundary & Negative Cases                                                 |
|   - T2.1: Zero Occurrences of SignalR/WebSockets for Match Simulation Streaming   |
|   - T2.2: Zero file-scheme URLs, Machine-Specific Paths, or Footex Local Paths    |
|   - T2.3: Verification of 29 Purged Obsolete Files from Milestone 1 (R3)          |
|   - T2.4: Preservation & Non-Emptiness of Authoritative Living Guides             |
|   - T2.5: Zero Outdated Frameworks (.NET 6/7/8) Claimed as Active Runtimes        |
+-----------------------------------------------------------------------------------+
| Tier 3: Cross-Reference & Route Validation                                        |
|   - T3.1: Complete Documented HTTP Routes Match Implemented Controllers & FastAPI |
|   - T3.2: Documented Service Ports Match Configuration (5025, 8000, 50051, etc.)  |
|   - T3.3: Documented Environment Variables Match .env.example & Docker Compose    |
+-----------------------------------------------------------------------------------+
| Tier 4: Real-World Link & Markdown Integrity                                      |
|   - T4.1: Relative Markdown File Link Resolution (No Dead Links Across Repo)      |
|   - T4.2: Markdown Heading Anchor Integrity (Valid GitHub Slugs & Anchors)        |
|   - T4.3: Relative Image Reference Integrity (Target Assets Exist on Disk)        |
|   - T4.4: Zero Empty (0-byte) Markdown Files Across Tracked Documentation         |
+-----------------------------------------------------------------------------------+
```

---

## 3. Comprehensive Test Inventory & Specification

### Tier 1: Feature & Protocol Coverage

| Test ID | Test Name | Authoritative Specification | Target Living Guides Under Audit |
| :--- | :--- | :--- | :--- |
| **T1.1** | SSE Match Stream Documentation Coverage | Must document Server-Sent Events (SSE) at `GET /api/matches/{id}/events/stream` (`MatchStreamController.cs`), JWT query token auth (`?access_token=`), and event types `match_event` and `match_statistics`. | `README.md`, `frontend/README.md`, `docs/documentation.md`, `docs/project-architecture-documentation.md`, `docs/event-processing-system.md` |
| **T1.2** | gRPC Simulation Service Coverage | Must document gRPC server listening on port `50051`, protobuf contract `simulation.proto` (`footex.simulation`), unary RPC (`StartMatchSimulation`, `GetHealth`), and streaming RPC (`StartMatchSimulationStream`). | `README.md`, `simulation-engine/README.md`, `docs/project-architecture-documentation.md` |
| **T1.3** | FastAPI REST & Concurrency Coverage | Must document FastAPI REST service on port `8000` running concurrently with gRPC server (port 50051) via `asyncio` inside the Uvicorn lifespan. | `simulation-engine/README.md`, `docs/project-architecture-documentation.md` |
| **T1.4** | Active Runtime Versions Coverage | Must document .NET 10 (`net10.0`, C# 13), Python 3.12 managed under `uv`, and Next.js 15/16 App Router. | `README.md`, `frontend/README.md`, `simulation-engine/README.md` |
| **T1.5** | Zero-Allocation & Backend Architecture Coverage | Must document zero-allocation `ReadOnlySpan<char>` parsing (`ZeroAllocationEventParser`), reflection-free CQRS (45 explicit DI registrations), EF Core 10, and Scalar API Reference (`/scalar/v1`). | `docs/project-architecture-documentation.md`, `backend/tests/Footex.PerformanceTests/README.md` |
| **T1.6** | Dual Streaming Ingestion Pipelines Coverage | Must document Pipeline A (RabbitMQ raw event text stream, exchange: `match_events`, routing key: `match.events`) and Pipeline B (direct gRPC server-streaming `SimulationService.StartMatchSimulationStream`). | `README.md`, `docs/project-architecture-documentation.md`, `docs/event-processing-system.md` |
| **T1.7** | Data & Caching Tiers Coverage | Must document Redis Hot State live match cache and atomic statistics (`LiveMatchStatisticsService`), alongside PostgreSQL batched background persistence upon `[MATCH END]`. | `docs/project-architecture-documentation.md`, `docs/database-design-documentation.md`, `docs/real-time-statistics-optimization.md` |

### Tier 2: Boundary & Negative Cases

| Test ID | Test Name | Negative Assertion / Boundary Condition | Enforcement Target |
| :--- | :--- | :--- | :--- |
| **T2.1** | Zero SignalR for Match Streaming | Assert ZERO occurrences where SignalR or WebSockets is presented as the active protocol for live match event broadcast. (SignalR `/Notify` for user alerts is distinguished and allowed). Assert `docs/signalr-matchhub.md` does not exist. | All living documentation in monorepo root, `docs/`, `frontend/`, `simulation-engine/` |
| **T2.2** | Zero File URLs & Local Paths | Assert ZERO occurrences of hardcoded file URLs or developer workstation absolute filesystem paths. | All markdown files across the repository |
| **T2.3** | Purged Obsolete Files Verification | Assert that all 29 purged historical, duplicate, and transient files (12 in `docs/` and 17 in `simulation-engine/DataMigration/`) are permanently deleted. | Repository filesystem |
| **T2.4** | Living Technical Guides Preservation | Assert that all 18 authoritative living technical documentation files remain present, readable, and non-empty (>0 bytes). | Core documentation inventory in `docs/` and `DataMigration/` |
| **T2.5** | No Deprecated Frameworks as Active | Assert that living monorepo and subsystem guides do NOT present obsolete .NET 6, .NET 7, or .NET 8 as the active project runtime. | `README.md`, performance test guides, architecture docs |

### Tier 3: Cross-Reference & Route Validation

| Test ID | Test Name | Cross-Reference Mechanism | Authoritative Source of Truth |
| :--- | :--- | :--- | :--- |
| **T3.1** | Documented HTTP Routes Exist in Codebase | Parses all HTTP routes documented in markdown files (`docs/documentation.md`, `search-api-documentation.md`, `real-time-statistics-optimization.md`, `simulation-engine/README.md`) and verifies each exists in controllers or FastAPI routes. Flags hallucinated routes. | `backend/src/Footex/Controllers/` (74 action endpoints), `Program.cs`, `simulation-engine/api/routes/` |
| **T3.2** | Service Ports Match Configuration | Validates that documented ports match actual service configs: Backend (5025 / 8080), FastAPI (8000), gRPC (50051), Frontend (3000), PostgreSQL (5432), Redis (6379), RabbitMQ (5672, 15672), Caddy (80, 443). | `launchSettings.json`, `run.py`, `main.py`, `docker-compose.dev.yml`, `Caddyfile` |
| **T3.3** | Environment Variables Match Configuration | Validates documented runtime environment variables against actual configuration keys defined in `.env.example`, `docker-compose.yml`, `docker-compose.dev.yml`, and `appsettings.json`. | Monorepo configuration files |

### Tier 4: Real-World Link & Markdown Integrity

| Test ID | Test Name | Validation Logic | Target Files |
| :--- | :--- | :--- | :--- |
| **T4.1** | Relative Markdown Links Integrity | Extracts all relative markdown links and verifies that every target file exists on the filesystem. Flags dead links. | All markdown documentation across repository |
| **T4.2** | Markdown Heading Anchors Integrity | Extracts all heading anchor references, computes GitHub-compatible anchor slugs, and verifies they resolve to actual headings. | All markdown documentation across repository |
| **T4.3** | Markdown Image References Integrity | Extracts all relative image links and verifies that the image asset exists on disk. | All markdown documentation across repository |
| **T4.4** | Zero Empty Markdown Files | Scans all tracked markdown files in the repository and verifies that no documentation file is empty (0 bytes). | All markdown documentation across repository |

---

## 4. Feature Inventory Coverage Mapping

This test suite provides 100% test coverage for all features specified in `PROJECT.md` and requirements from `ORIGINAL_REQUEST.md`:

| Feature # | Feature Description | Scope Milestone | Validating Test Cases |
| :---: | :--- | :---: | :--- |
| **1** | Purge Obsolete Docs in `docs/` | M1 | **T2.3**, **T2.4**, **T4.1** |
| **2** | Purge Obsolete Files in `DataMigration/` | M1 | **T2.3**, **T2.4**, **T4.4** |
| **3** | Root `README.md` Overhaul | M2 | **T1.1**, **T1.2**, **T1.4**, **T1.6**, **T2.2**, **T2.5**, **T3.3** |
| **4** | `frontend/README.md` Overhaul | M2 | **T1.1**, **T1.4**, **T2.1**, **T2.2**, **T4.1** |
| **5** | `simulation-engine/README.md` Overhaul | M2 | **T1.2**, **T1.3**, **T1.4**, **T2.2**, **T3.1**, **T3.2** |
| **6** | Performance Test Guides Update | M2 | **T1.4**, **T1.5**, **T2.5**, **T4.1** |
| **7** | Core Architecture Overhaul | M3 | **T1.1**, **T1.2**, **T1.3**, **T1.4**, **T1.5**, **T1.6**, **T1.7**, **T2.1**, **T4.1**, **T4.2** |
| **8** | API Reference Overhaul | M3 | **T1.1**, **T2.1**, **T3.1** |
| **9** | Event Processing Guide Overhaul | M3 | **T1.1**, **T1.6**, **T2.1** |
| **10** | Authoritative SSE Guide | M3 | **T1.1**, **T2.1** |
| **11** | Subsystem Docs Alignment | M3 | **T1.7**, **T2.1**, **T3.2**, **T4.2** |
| **12** | Link & Path Integrity Verification | M4 | **T2.2**, **T4.1**, **T4.2**, **T4.3**, **T4.4** |

---

## 5. Execution Command & CLI Reference

### 5.1 Standard Execution (All 19 Tests)
```bash
python scripts/verify-documentation.py
```
Executes all 4 tiers sequentially. Outputs structured progress and returns exit code `0` if all tests pass, `1` if any fail.

### 5.2 Tier-Filtered Execution
Run only a specific tier during targeted milestone implementation:
```bash
# Run only Tier 1 (Feature & Protocol Coverage)
python scripts/verify-documentation.py --tier 1

# Run only Tier 2 (Boundary & Negative Cases)
python scripts/verify-documentation.py --tier 2

# Run only Tier 3 (Cross-Reference & Route Validation)
python scripts/verify-documentation.py --tier 3

# Run only Tier 4 (Real-World Link & Markdown Integrity)
python scripts/verify-documentation.py --tier 4
```

### 5.3 Verbose Mode
Displays offending content, exact line excerpts, and full failure context:
```bash
python scripts/verify-documentation.py --verbose
```

### 5.4 Fail-Fast Mode
Stops execution immediately upon encountering the first failed assertion:
```bash
python scripts/verify-documentation.py --fail-fast
```

### 5.5 Machine-Readable JSON Output
Produces a JSON report containing test status, elapsed durations, and violation arrays for CI/CD pipeline ingestion:
```bash
python scripts/verify-documentation.py --json
```

---

## 6. Continuous Integration & Workflow Integration

The test runner is designed for direct integration into Git pre-commit hooks and GitHub Actions workflows:

```yaml
# .github/workflows/documentation-audit.yml
name: Documentation Audit & Verification
on: [push, pull_request]

jobs:
  verify-documentation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Set up Python 3.12
        uses: actions/setup-python@v5
        with:
          python-version: '3.12'
      - name: Run Documentation Verification Test Suite
        run: python scripts/verify-documentation.py
```

Any discrepancy between implementation code and living documentation will fail the build with exact file and line references, ensuring living documentation never falls out of sync with the codebase.
