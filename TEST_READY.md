# Living Documentation Test Suite Readiness Report (TEST_READY)

## 1. Test Suite Status: READY

The automated opaque-box test suite for **PixelPitchAI Living Documentation Audit & Overhaul** has been designed, implemented, and verified. It is active and ready for continuous regression testing throughout Milestones M2, M3, and M4.

- **Test Runner Location**: `scripts/verify-documentation.py`
- **Specification & Architecture**: `TEST_INFRA.md`
- **Execution Command**: `python scripts/verify-documentation.py`
- **Test Framework / Runtime**: Python 3.12+ (zero third-party dependencies, standard library only)
- **Execution Speed**: ~350 milliseconds for full 19-test suite

---

## 2. Test Suite Baseline Execution Summary

Running `python scripts/verify-documentation.py` against the current repository state (following completion of Milestone 1 purge) produces the following baseline:

```
================================================================================
  PixelPitchAI Living Documentation Test Runner
  Target Repository: PixelPitchAI/feature-arch
  Tier Filter: All Tiers (1-4)
  Total Test Cases: 19
================================================================================

--- Tier 1: Feature & Protocol Coverage ---
  [FAIL] T1.1: SSE Match Stream Documentation Coverage
  [FAIL] T1.2: gRPC Simulation Service Documentation Coverage
  [FAIL] T1.3: FastAPI & gRPC Dual-Server Concurrency Coverage
  [PASS] T1.4: Runtime Framework & Language Versions Coverage
  [PASS] T1.5: Zero-Allocation & Modern Backend Patterns Coverage
  [PASS] T1.6: Dual Streaming Ingestion Pipelines Coverage
  [PASS] T1.7: Data & Caching Tiers Coverage

--- Tier 2: Boundary & Negative Cases ---
  [FAIL] T2.1: Zero SignalR for Match Streaming in Living Docs
  [FAIL] T2.2: Zero file-scheme URLs & Local Machine Paths
  [PASS] T2.3: Purged Obsolete Files Verification (R3)
  [PASS] T2.4: Living Technical Guides Preservation
  [PASS] T2.5: No Deprecated Frameworks as Active Target

--- Tier 3: Cross-Reference & Route Validation ---
  [FAIL] T3.1: Documented HTTP Routes Exist in Codebase
  [PASS] T3.2: Service Ports Match Configuration
  [FAIL] T3.3: Environment Variables Match Configuration

--- Tier 4: Real-World Link & Markdown Integrity ---
  [FAIL] T4.1: Relative Markdown Links Integrity
  [FAIL] T4.2: Markdown Heading Anchors Integrity
  [PASS] T4.3: Markdown Image References Integrity
  [PASS] T4.4: Zero Empty Markdown Files

================================================================================
  TEST SUITE SUMMARY: 10/19 PASSED (9 FAILED) in 378.2ms
================================================================================
```

---

## 3. Verified Passing Tests (Milestone 1 Validation & Architectural Conformance)

The following 10 tests are currently **PASSING**, confirming that prerequisite Milestone 1 purge succeeded and base architectural conformance exists:

1. **T1.4 (Runtime Framework & Language Versions)**: Confirms .NET 10, C# 13, Python 3.12 with `uv`, and Next.js are cited as active runtimes.
2. **T1.5 (Zero-Allocation & Modern Backend Patterns)**: Confirms `ReadOnlySpan<char>` parsing and reflection-free CQRS are documented.
3. **T1.6 (Dual Streaming Ingestion Pipelines)**: Confirms RabbitMQ Pipeline A and gRPC Pipeline B streaming models are documented.
4. **T1.7 (Data & Caching Tiers Coverage)**: Confirms Redis Hot State live match cache and PostgreSQL batched persistence are documented.
5. **T2.3 (Purged Obsolete Files Verification - R3)**: Confirms that **all 29** purged files across `docs/` and `simulation-engine/DataMigration/` have been removed from the filesystem.
6. **T2.4 (Living Technical Guides Preservation)**: Confirms that all 18 authoritative living documentation files exist and have non-zero file sizes.
7. **T2.5 (No Deprecated Frameworks as Active Target)**: Confirms that obsolete .NET 6, 7, and 8 are not claimed as active project targets.
8. **T3.2 (Service Ports Match Configuration)**: Confirms that documented ports (5025/8080, 8000, 50051, 3000, 5432, 6379, 5672) match application configuration without port conflicts.
9. **T4.3 (Markdown Image References Integrity)**: Confirms all relative image references point to existing image assets on disk.
10. **T4.4 (Zero Empty Markdown Files)**: Confirms that zero tracked markdown files have a file size of 0 bytes.

---

## 4. Discovered Documentation Defects to be Resolved in Milestones M2, M3, and M4

The 9 failing tests pinpoint the exact defect backlog to be addressed by subsequent milestone workers:

### Assigned to Milestone M2 (Subsystem Guides & Monorepo READMEs)
1. **T1.1**: Root `README.md` and `frontend/README.md` fail to document the active Server-Sent Events (SSE) match streaming endpoint (`GET /api/matches/{id}/events/stream`).
2. **T1.2**: `README.md` and `simulation-engine/README.md` omit gRPC service port `50051`, protobuf contract `simulation.proto`, and RPC streaming methods.
3. **T1.3**: `simulation-engine/README.md` omits explanation of concurrent dual-server lifecycle (FastAPI 8000 + gRPC 50051 via `asyncio` inside Uvicorn).
4. **T2.2**: Hardcoded file URLs and local workstation paths exist in:
   - `README.md` (lines 37, 43, 50)
   - `frontend/README.md` (lines 72-75)
   - `simulation-engine/README.md` (lines 28, 29)
   - `docs/local-docker-testing-setup.md` (line 39)
5. **T3.3**: `README.md` line 80 lists invalid environment variables `JWT_KEY` and `JWT_ISSUER` instead of `JWT_SECRET` and `JWT_VALID_ISSUER` defined in `.env.example`.
6. **T4.1**: `frontend/README.md` line 66 links to non-existent files `./DOCKER_DEPLOYMENT_GUIDE.md` and `./HTTPS_HOSTING_GUIDE.md`.

### Assigned to Milestone M3 (Core Architecture & API Docs Overhaul)
7. **T2.1**: Obsolete SignalR match streaming claims remain in:
   - `docs/project-architecture-documentation.md` (lines 617, 646)
   - `docs/documentation.md` (line 20)
   - `docs/event-processing-system.md` (lines 5, 233, 694, 715)
   - `docs/rabbitmq-matchevent-client.md` (line 134)
   - `docs/signalr-notification-service.md` (line 138)
8. **T3.1**: `docs/real-time-statistics-optimization.md` documents 4 hallucinated endpoints that do not exist in controller code:
   - `GET /api/matches/live/all` (lines 80, 80)
   - `GET /api/matches/live/cached/{matchId}` (lines 89, 89)
   - `POST /api/matches/live/preload/{matchId}` (lines 98, 98)
   - `POST /api/matches/live/preload` (lines 107, 107)

### Assigned to Milestone M4 (Link & Heading Anchor Audit)
9. **T4.1**: `docs/project-architecture-documentation.md` line 1296 links to purged file `./DOCKER_UPDATE_SUMMARY.md`.
10. **T4.2**: Broken heading anchors exist in:
   - `docs/database-design-documentation.md` line 11: `#indexing-strategy--performance-impact` (double hyphen typo in TOC)
   - `docs/project-architecture-documentation.md` line 13: `#development--deployment` (double hyphen typo in TOC)

---

## 5. Instructions for Implementing Agents (M2, M3, M4)

When working on Milestones M2, M3, or M4:
1. Run `python scripts/verify-documentation.py --tier <tier>` during development to verify specific tier progress.
2. Run `python scripts/verify-documentation.py --verbose` to inspect exact failure line numbers and offending text.
3. Once edits are made, run `python scripts/verify-documentation.py` across all tiers to verify regression-free compliance.
4. When all 19 tests pass (`19/19 PASSED`), exit code `0` is returned, signifying 100% documentation synchronization with the active codebase.
