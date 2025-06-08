# Performance Testing Guide

This document provides guidance on running and interpreting the performance tests for the Footex API.

## Overview

The Footex.PerformanceTests project includes comprehensive performance testing using:

- **NBomber** for load testing and stress testing
- **BenchmarkDotNet** for micro-benchmarks and detailed performance analysis

## Test Categories

### 1. Load Tests (NBomber)

#### ApiLoadTests

- **Purpose**: Test individual API endpoints under load
- **Endpoints Covered**: Matches, Players, Teams, Stadiums, Search, Health Check
- **Load Patterns**: Injection rate and constant load simulations

#### CachePerformanceTests

- **Purpose**: Evaluate caching effectiveness and performance
- **Tests**: Cache hit vs miss scenarios, cache header verification
- **Focus**: Redis cache performance for Players, Stadiums, and Coaches

#### SearchPerformanceTests

- **Purpose**: Test search functionality under load
- **Features**: Fuzzy search comparison, different query lengths, various result limits
- **Endpoints**: Player, Team, Coach, and Match search

#### StressTests

- **Purpose**: Evaluate system behavior under extreme conditions
- **Tests**: High load, spike load, endurance, memory leak detection
- **Scenarios**: Concurrent users, database-intensive operations

### 2. Benchmarks (BenchmarkDotNet)

#### ApiBenchmarks

- **Purpose**: Detailed performance metrics for API endpoints
- **Metrics**: Execution time, memory allocation, throughput
- **Features**: Parameterized tests for filters and pagination

#### SearchBenchmarks

- **Purpose**: Micro-benchmarks for search functionality
- **Comparisons**: Fuzzy vs exact search, different query complexities
- **Metrics**: Response time analysis for various search parameters

#### CacheBenchmarks

- **Purpose**: Cache performance analysis
- **Comparisons**: Cache hit vs miss performance
- **Scenarios**: Sequential hits, mixed cache scenarios

## Running the Tests

### Prerequisites

1. Ensure Docker is running (for PostgreSQL test containers)
2. Build the solution: `dotnet build`

### Running Load Tests (NBomber)

```powershell
# Run all load tests
dotnet test --project Footex.PerformanceTests --filter "Category=LoadTest"

# Run specific test classes
dotnet test --project Footex.PerformanceTests --filter "ClassName=ApiLoadTests"
dotnet test --project Footex.PerformanceTests --filter "ClassName=CachePerformanceTests"
dotnet test --project Footex.PerformanceTests --filter "ClassName=SearchPerformanceTests"
dotnet test --project Footex.PerformanceTests --filter "ClassName=StressTests"
```

### Running Benchmarks (BenchmarkDotNet)

```powershell
# Run specific benchmark suites
dotnet run --project Footex.PerformanceTests --configuration Release api
dotnet run --project Footex.PerformanceTests --configuration Release search
dotnet run --project Footex.PerformanceTests --configuration Release cache

# Run all benchmarks
dotnet run --project Footex.PerformanceTests --configuration Release all
```

## Test Results and Reports

### NBomber Reports

- Generated in test output folders: `stress-test-results/`, `spike-test-results/`, etc.
- Formats: HTML (detailed analysis), CSV (data export)
- Key Metrics:
  - **RPS (Requests Per Second)**: Throughput measurement
  - **Response Time**: P50, P75, P95, P99 percentiles
  - **Error Rate**: Failed requests percentage
  - **Data Transfer**: MB/s transferred

### BenchmarkDotNet Reports

- Generated in `BenchmarkDotNet.Artifacts/` folder
- Formats: HTML, CSV, Markdown
- Key Metrics:
  - **Mean Execution Time**: Average response time
  - **Memory Allocation**: Bytes allocated per operation
  - **Standard Deviation**: Performance consistency
  - **Baseline Comparisons**: Relative performance

## Performance Expectations

### Response Time Targets

- **Health Check**: < 50ms
- **Simple GET requests**: < 200ms
- **Complex queries with joins**: < 500ms
- **Search operations**: < 300ms
- **Cached responses**: < 100ms

### Throughput Targets

- **Health endpoint**: > 1000 RPS
- **Data endpoints**: > 100 RPS
- **Search endpoints**: > 50 RPS
- **Complex operations**: > 20 RPS

### Cache Performance

- **Cache Hit Ratio**: > 80% for frequently accessed data
- **Cache Response Time**: < 50ms
- **Cache Miss Overhead**: < 2x uncached response time

## Interpreting Results

### Warning Signs

- **High P95/P99 Response Times**: Indicates occasional slow responses
- **Memory Growth**: Potential memory leaks
- **Declining RPS**: Performance degradation under load
- **High Error Rates**: System instability

### Optimization Areas

- **Database Queries**: Index optimization, query tuning
- **Caching Strategy**: TTL adjustment, cache key optimization
- **Connection Pooling**: Database connection settings
- **Memory Management**: Object lifecycle, disposal patterns

## Best Practices

### Running Tests

1. **Use Release Configuration** for benchmarks
2. **Consistent Environment** for comparative testing
3. **Warm-up Periods** for accurate measurements
4. **Multiple Runs** for statistical significance

### Test Maintenance

1. **Regular Execution** as part of CI/CD
2. **Baseline Comparisons** for regression detection
3. **Performance Budgets** for acceptable thresholds
4. **Documentation Updates** for new endpoints

## Troubleshooting

### Common Issues

- **Container Startup Failures**: Check Docker daemon
- **Connection Timeouts**: Verify database availability
- **Memory Issues**: Adjust test parameters
- **Port Conflicts**: Ensure test isolation

### Debug Mode

Add logging to performance tests for troubleshooting:

```csharp
.WithReportFolder("debug-results")
.WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
```

## Integration with CI/CD

### Performance Gates

- Set thresholds for automated pass/fail
- Compare against baseline performance
- Alert on significant regressions

### Scheduled Testing

- Daily performance monitoring
- Weekly comprehensive stress tests
- Monthly endurance testing

## Monitoring and Alerting

### Key Metrics to Monitor

- API response times
- Database query performance
- Cache hit ratios
- Memory and CPU usage
- Error rates and availability

### Alerting Thresholds

- Response time > 1s (P95)
- Error rate > 1%
- Cache hit ratio < 70%
- Memory usage > 80%
