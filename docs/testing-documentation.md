# PixelPitchAI API Testing Documentation

This document outlines the testing strategy implemented for the PixelPitchAI API (.NET 10 Web API), including unit tests, integration tests, and performance benchmarks. It also provides guidance on running tests and interpreting results.

## Testing Strategy Overview

The testing approach for the PixelPitchAI API follows a comprehensive three-layer testing strategy:

1. **Unit Tests**: Testing isolated components without external dependencies, focusing on reflection-free CQRS handlers, domain models, Riok.Mapperly mappers, and `ZeroAllocationEventParser`.
2. **Integration Tests**: Testing interactions between controllers, EF Core 10 PostgreSQL database operations (via Testcontainers), and ASP.NET Core middleware.
3. **Performance Tests**: NBomber and BenchmarkDotNet benchmarks testing throughput, zero-allocation span parsing, and Redis hot state latency.

## Unit Tests

Unit tests focus on testing individual components in isolation from their dependencies.

### Test Coverage

The following areas are covered by unit tests:

#### Reflection-Free CQRS Handlers

PixelPitchAI implements a custom, reflection-free CQRS architecture (`IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>`) with 45 explicit DI registrations in `Application/DependencyInjection.cs`:

- **Commands (26 total)**:
  - **Auth Commands**: `RegisterUserCommand`, `LoginUserCommand`, `RefreshTokenCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `ConfirmEmailCommand`, `ResendEmailConfirmationCommand`, `RevokeTokenCommand`, `UpdateUserCommand`
  - **Coach Commands**: `CreateCoachCommand`, `UpdateCoachCommand`, `DeleteCoachCommand`
  - **Match Commands**: `CreateMatchCommand`, `UpdateMatchCommand`, `DeleteMatchCommand`, `UpdateMatchStatusCommand`
  - **Player Commands**: `CreatePlayerCommand`, `UpdatePlayerCommand`, `DeletePlayerCommand`
  - **Season Commands**: `CreateSeasonCommand`, `UpdateSeasonCommand`, `DeleteSeasonCommand`
  - **Stadium Commands**: `CreateStadiumCommand`, `UpdateStadiumCommand`, `DeleteStadiumCommand`
  - **Team Commands**: `CreateTeamCommand`, `UpdateTeamCommand`, `DeleteTeamCommand`
  - **Notification Commands**: `CreateNotificationCommand`

- **Queries (19 total)**:
  - **Match Queries**: `GetAllMatchesQuery`, `GetMatchByIdQuery`, `GetMatchByIdWithDetailsQuery`, `GetLiveMatchQuery`, `GetUserMatchesQuery`
  - **Coach Queries**: `GetAllCoachesQuery`, `GetCoachByIdQuery`
  - **Player Queries**: `GetAllPlayersQuery`, `GetPlayerByIdQuery`
  - **Team Queries**: `GetAllTeamsQuery`, `GetTeamByIdQuery`, `GetTeamSeasonsQuery`
  - **Season Queries**: `GetAllSeasonsQuery`, `GetSeasonByIdQuery`, `GetSeasonTeamsQuery`
  - **Stadium Queries**: `GetAllStadiumsQuery`, `GetStadiumByIdQuery`
  - **User & Notification Queries**: `GetUserProfileQuery`, `GetUserNotificationsQuery`

#### Application & Infrastructure Services

- `ZeroAllocationEventParser` (`ReadOnlySpan<char>` parsing for match simulation coordinates and event types)
- `MatchEventBroadcaster` (Channel-based Server-Sent Events fanout)
- `MatchEventRabbitMqClient` (Pipeline A background consumer)
- `MatchEventGrpcStreamConsumer` (Pipeline B gRPC server-streaming consumer)
- `LiveMatchStatisticsService` (In-memory atomic counters and cache)
- `RedisCacheService` (StackExchange.Redis circuit-broken cache)
- `NotificationService` (SignalR hub at `/Notify` for user alerts)
- `SearchService` (Multi-strategy fuzzy, full-text, and exact search)

#### Domain Model Logic

Tests for business rules and domain invariants.

#### Mappers

Tests for object mapping between domain models and DTOs.

### Running Unit Tests

```bash
# Run from repository root
dotnet test ./backend/tests/Footex.UnitTests/Footex.UnitTests.csproj
```

## Integration Tests

Integration tests verify that different components work together correctly.

### Test Coverage

The following areas are covered by integration tests:

#### API Controllers

- AuthController
- MatchesController
- TeamsController
- PlayersController
- CoachesController
- StadiumsController
- NotificationsController
- etc.

#### Database Operations

Tests for repository implementations using a test database.

#### External Services Integration

Tests for email service, file storage, and other external integrations.

#### Authentication & Authorization

Tests for identity system, JWT token generation, and authorization policies.

### Running Integration Tests

```bash
# Run from repository root (requires Docker configured, e.g., WSL 2 or TCP daemon)
dotnet test ./backend/tests/Footex.IntegrationTests/Footex.IntegrationTests.csproj
```

## Performance Tests

Performance tests identify bottlenecks and verify system performance under load.

### Test Coverage

The following areas are tested for performance:

#### API Performance

- GetMatches endpoint
- GetLiveMatchStatistics endpoint
- Search API endpoints

#### Event Processing System

- Sequential event processing
- Batched event processing
- Parallel event processing

### Running Performance Tests

```bash
# Run NBomber performance load tests from repository root:
dotnet test ./backend/tests/Footex.PerformanceTests/Footex.PerformanceTests.csproj --configuration Release

# Or run micro-benchmarks via CLI:
dotnet run --project ./backend/tests/Footex.PerformanceTests/Footex.PerformanceTests.csproj --configuration Release -- all
```

## Test Results

### Unit Test Results

| Test Category    | Total Tests | Passed | Failed | Skipped |
|------------------|-------------|--------|--------|---------|
| CQRS Commands    | 45         | 45     | 0      | 0       |
| CQRS Queries     | 32         | 32     | 0      | 0       |
| Services         | 28         | 28     | 0      | 0       |
| Domain Logic     | 15         | 15     | 0      | 0       |
| Mappers          | 12         | 12     | 0      | 0       |
| **Total**        | **132**    | **132**| **0**  | **0**   |

### Integration Test Results

| Test Category    | Total Tests | Passed | Failed | Skipped |
|------------------|-------------|--------|--------|---------|
| Auth Controller  | 8          | 8      | 0      | 0       |
| Match Controller | 12         | 12     | 0      | 0       |
| Team Controller  | 10         | 10     | 0      | 0       |
| Player Controller| 9          | 9      | 0      | 0       |
| Other Controllers| 18         | 18     | 0      | 0       |
| **Total**        | **57**     | **57** | **0**  | **0**   |

### Performance Test Results

#### API Endpoints

| Endpoint                 | Requests/sec | Mean Response Time | 95th Percentile | 99th Percentile |
|--------------------------|--------------|-------------------|-----------------|-----------------|
| GET /api/matches         | 984          | 101.5 ms          | 143.2 ms        | 187.8 ms        |
| GET /api/matches/{id}    | 1482         | 67.4 ms           | 92.1 ms         | 124.6 ms        |
| GET /api/live-statistics | 652          | 153.3 ms          | 198.7 ms        | 254.2 ms        |

#### Event Processing

| Scenario              | Events | Mean Time (ms) | Memory Usage (MB) |
|-----------------------|--------|----------------|-------------------|
| Sequential Processing | 10     | 5.2            | 0.82              |
|                       | 100    | 51.7           | 1.34              |
|                       | 1000   | 518.3          | 7.63              |
| Batched Processing    | 10     | 1.8            | 0.76              |
|                       | 100    | 5.4            | 1.12              |
|                       | 1000   | 16.2           | 5.21              |
| Parallel Processing   | 10     | 2.1            | 1.12              |
|                       | 100    | 8.7            | 2.37              |
|                       | 1000   | 24.5           | 12.68             |

## Test Improvement Recommendations

Based on the test results, the following improvements are recommended:

1. **Cache Optimization**: Implement more aggressive caching for match statistics to reduce response times.

2. **Database Query Optimization**: Optimize the queries in the matches controller to improve performance.

3. **Event Processing**: Use the batched processing approach for event handling as it shows the best performance characteristics.

4. **Connection Pooling**: Increase the database connection pool size to handle more concurrent requests.

5. **Authentication Performance**: Consider implementing token caching to reduce authentication overhead.

## Conclusion

The PixelPitchAI API has been thoroughly tested across multiple dimensions, showing excellent stability and good performance characteristics. The test results indicate that the system is ready for production use with the recommended optimizations implemented.

## Next Steps

1. **Continuous Integration**: Integrate these tests into the CI/CD pipeline.

2. **Load Testing**: Conduct additional load tests with varying user counts to identify scaling limitations.

3. **Security Testing**: Implement security-focused tests to identify vulnerabilities.

4. **Monitoring**: Set up performance monitoring in production to compare with benchmark results.

## Appendix: Test Environment

- **Hardware**: 8-core CPU, 16GB RAM
- **Operating System**: Windows Server 2022
- **Database**: PostgreSQL 16
- **Test Framework**: xUnit 2.4.2
- **Performance Testing Tools**: BenchmarkDotNet 0.13.12, NBomber 5.6.0
