# Project: PixelPitchAI Living Documentation Audit & Overhaul

## Architecture
- **Backend**: .NET 10 Web API (`backend/src/Footex`), C# 13, Native AOT ready, reflection-free CQRS (45 explicit DI registrations), EF Core 10 (`FootballDbContext`), zero-allocation `ReadOnlySpan<char>` parsing (`ZeroAllocationEventParser`), `Riok.Mapperly` compile-time mapping, ASP.NET Core OpenAPI + Scalar API Reference (`/scalar/v1`).
- **Simulation Engine**: Python 3.12 under `uv` (`simulation-engine/`), FastAPI REST (port 8000) and gRPC `SimulationService` (port 50051) running concurrently via `asyncio` in Uvicorn lifespan, PyTorch + ONNX Runtime INT8 + XGBoost, stateless event generator.
- **Protocols & Streaming**:
  - *Pipeline A*: Asynchronous raw event text published to RabbitMQ exchange `match_events` (topic, routing key `match.events`), consumed by .NET `MatchEventRabbitMqClient`.
  - *Pipeline B*: Direct memory-to-memory gRPC server-streaming (`SimulationService.StartMatchSimulationStream`), consumed by .NET `MatchEventGrpcStreamConsumer`.
  - *Client Streaming*: Server-Sent Events (SSE) at `GET /api/matches/{id}/events/stream` (`MatchStreamController.cs`), JWT query token auth (`?access_token=`), channel-based `MatchEventBroadcaster.cs` pushing `match_event` and `match_statistics`. SignalR / WebSockets for match simulation streaming are completely obsolete.
- **Data & Caching Tiers**: Redis Hot State live match cache and atomic statistics (`RedisCacheService`, `LiveMatchStatisticsService`), PostgreSQL batched persistence on match completion (`SaveChangesAsync`).
- **Frontend**: Next.js 16/15 (`frontend/`), App Router, native browser `EventSource` consumption in `MatchStreamService.ts`.
- **Infrastructure**: Caddy reverse proxy, Docker Compose (dev & prod), PostgreSQL 15, Redis 7, RabbitMQ 3.12.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Purge Obsolete Docs in `docs/` | Delete 14 superseded/transient/duplicate reports in `docs/` | M1 | Survey E3 |
| 2 | Purge Obsolete Files in `DataMigration/` | Delete 11 redundant reports/empty files and transient logs/JSONs | M1 | Survey E2, E3 |
| 3 | Root `README.md` Overhaul | Synchronize root guide with .NET 10, Scalar, gRPC 50051, dual streaming, remove Footex links | M2 | Survey E1, E2, E3 |
| 4 | `frontend/README.md` Overhaul | Synchronize with Next.js 16/15, native EventSource SSE, remove SignalR match claims, fix dead links | M2 | Survey E3 |
| 5 | `simulation-engine/README.md` Overhaul | Document Python 3.12, uv, asyncio dual server (8000 & 50051), gRPC stubs, Pipeline A & B | M2 | Survey E2 |
| 6 | Performance Test Guides Update | Update `QUICKSTART.md` and `README.md` in `Footex.PerformanceTests` to .NET 10 and correct paths | M2 | Survey E1, E3 |
| 7 | Core Architecture Overhaul | Overhaul `docs/project-architecture-documentation.md` for .NET 10, Native AOT, gRPC, SSE | M3 | Survey E1, E2 |
| 8 | API Reference Overhaul | Overhaul `docs/documentation.md` to catalog all 68 endpoints, `MatchStreamController`, remove SignalR | M3 | Survey E1 |
| 9 | Event Processing Guide Overhaul | Overhaul `docs/event-processing-system.md` for dual pipelines and SSE client stream | M3 | Survey E1, E2 |
| 10 | Authoritative SSE Guide | Create `docs/sse-match-stream.md` documenting Server-Sent Events architecture and protocol | M3 | Survey E1 |
| 11 | Subsystem Docs Alignment | Synchronize database design, RabbitMQ client, Redis caching, testing, and notification docs | M3 | Survey E1, E2 |
| 12 | Link & Path Integrity Verification | Verify zero dead links, no hardcoded file:/// URLs, and valid symbol/route references across all docs | M4 | Survey E3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Obsolete Docs Purge & Consolidation | Purge 25 obsolete markdown files and transient logs across `docs/` and `DataMigration/` | none | DONE (commit d4ee2c1, 29 files purged, 18 living guides intact, Gate PASS) |
| M2 | Subsystem Guides & READMEs Overhaul | Overhaul `README.md`, `frontend/README.md`, `simulation-engine/README.md`, and test guides | M1 | DONE (commit 8e738c2, 5 files overhauled, 0 test violations in touched files) |
| M3 | Core Architecture & API Docs Overhaul | Overhaul `project-architecture-documentation.md`, `documentation.md`, `event-processing-system.md`, `sse-match-stream.md`, etc. | M2 | DONE (commit 8cc617f, 10 core docs overhauled, sse-match-stream.md created, Mermaid erDiagram added) |
| M4 | Link, Schema & Route Verification | E2E verification of all markdown links, routes, ports, environment variables, and integrity audit | M3 | DONE (19/19 verification tests passed, 631 unit tests passed) |

## Interface Contracts
### Documentation Standards & Nomenclature
- **Match Streaming Protocol**: Must be referred to exclusively as **Server-Sent Events (SSE)** via `GET /api/matches/{id}/events/stream`.
- **Obsolete Match Protocols**: Never describe SignalR or WebSockets as the active protocol for match event streaming. (SignalR `/Notify` is reserved exclusively for user alerts).
- **Simulation Engine Concurrency**: Concurrently runs FastAPI on port 8000 and gRPC on port 50051 via `asyncio` inside Uvicorn.
- **Dual Streaming Ingestion**:
  - Pipeline A: RabbitMQ raw text stream (`match_events` exchange, `match.events` routing key).
  - Pipeline B: Direct gRPC server-streaming (`SimulationService.StartMatchSimulationStream`).
- **Code Syntax & Stacks**: .NET 10 (C# 13), Python 3.12 (`uv`), Next.js 16/15 (React 19).

## Code Layout
- Living docs: `README.md`, `frontend/README.md`, `simulation-engine/README.md`, `docs/*.md`.
- Removed docs: 14 files in `docs/`, 11 files in `simulation-engine/DataMigration/`.
