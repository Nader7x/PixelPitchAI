# CLAUDE.md

## Environment
- Backend: .NET 10 (SDK 10.0.300)
- Frontend: Next.js 16 (React 19, TypeScript, pnpm v11)
- AI/Simulation: Python 3.12 (FastAPI, ONNX Runtime CPU INT8, PyTorch CPU, XGBoost) — managed by `uv`
- Database/Infra: PostgreSQL, Redis, RabbitMQ
- Proxy: Caddy 2.7

## Setup
```bash
# Add worktree from bare repository root
git worktree add ./worktrees/<branch-name> <branch-name>
```

## Build
```bash
# Backend
dotnet build ./backend/Footex.sln

# Frontend
cd ./frontend && pnpm install && pnpm build

# AI/Simulation Engine (requires uv)
cd ./simulation-engine && uv sync --frozen
```

## Test
```bash
# .NET unit + integration tests
dotnet test ./backend/Footex.sln

# Specific test class
dotnet test ./backend/Footex.sln --filter "FullyQualifiedName~Footex.UnitTests.SomeClass"
```

## Docker
```bash
# Dev stack (all services)
docker compose -f docker-compose.dev.yml up -d

# Prod stack
docker compose -f docker-compose.yml up -d

# Build individual images
docker buildx build -t simulation-engine:latest ./simulation-engine --load
docker buildx build -t football-simulation:prod ./frontend --load
docker buildx build -t footex-api:latest ./backend -f ./backend/src/Footex/Dockerfile --load

# Tear down
docker compose -f docker-compose.dev.yml down
```

## Architecture & Constraints
- **VSA & Onion**: Vertical Slice Architecture grouping by feature. Domain layer strictly agnostic of infrastructure/serialization.
- **Source Generation**: Prioritize compile-time source generation over runtime reflection.
- **Zero-Allocation**: `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>` on hot paths. `ref struct` / `readonly struct` to avoid heap allocations.
- **No Synthetic Data**: Strict reliance on realistic domain data; no "John Doe" or fake placeholders.
- **AI Engine**: CPU execution via ONNX Runtime INT8 quantized model for local dev, PyTorch on GPU in production. Model weights mounted as read-only volume, never baked into image.
