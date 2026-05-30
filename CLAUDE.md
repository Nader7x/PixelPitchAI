# CLAUDE.md

## Environment
- Backend: .NET 10
- Frontend: Next.js (React 19, TypeScript)
- Database/Infra: PostgreSQL, Redis, RabbitMQ

## Setup
```bash
# Add worktree from bare repository root
git worktree add ./worktrees/Footex/upgrade-to-dotnet-10 upgrade-to-dotnet-10
```

## Build
```bash
# Backend build
dotnet build ./Footex/Footex.sln

# Frontend build
cd ./Football-Simulation_frontend && pnpm install && pnpm build
```

## Test
```bash
# Run unit & integration tests
dotnet test ./Footex/Footex.sln

# Run specific test class
dotnet test ./Footex/Footex.sln --filter "FullyQualifiedName~Footex.UnitTests.SomeClass"
```

## Database/Infra
```bash
# Spin up local infrastructure
docker compose -f ./docker-compose.dev.yml up -d
# Tear down local infrastructure
docker compose -f ./docker-compose.dev.yml down
```

## Architecture & Constraints
- **VSA & Onion**: Follow Vertical Slice Architecture (VSA) grouping by feature. Domain layer remains strictly agnostic of infrastructure/serialization.
- **Native AOT Compliance**: NOT enforced. This project is a standard .NET 10 web application with dynamic features, EF Core, etc.
- **Source Generation**: Always prioritize compile-time source generation over runtime reflection when alternative source generation solutions exist.
- **Zero-Allocation**: Use `Span<T>`, `ReadOnlySpan<T>`, and `Memory<T>` on performance-critical paths. Use `ref struct` / `readonly struct` to avoid heap allocations.
- **No Synthetic Data**: Strict reliance on realistic domain data; absolutely no "John Doe" or synthetic/fake placeholders.
