# Performance Tests Quick Start Guide

## Overview

This guide helps you quickly get started with running performance tests for the Footex API.

## Prerequisites

1. .NET 8.0 SDK installed
2. Footex API running (locally or on a test environment)
3. PostgreSQL database running and accessible

## Quick Start

### 1. Build the Performance Tests

```bash
cd Footex.PerformanceTests
dotnet build
```

### 2. Configure Test Settings

Edit `appsettings.json` to match your environment:

```json
{
  "PerformanceTests": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

### 3. Run Tests Using PowerShell Script

#### Run All Performance Tests

```powershell
./run-performance-tests.ps1 -TestType all
```

#### Run Specific Test Types

```powershell
# Load tests only
./run-performance-tests.ps1 -TestType load

# Stress tests only
./run-performance-tests.ps1 -TestType stress

# Cache performance tests
./run-performance-tests.ps1 -TestType cache

# Search performance tests
./run-performance-tests.ps1 -TestType search

# Benchmarks only
./run-performance-tests.ps1 -TestType benchmark
```

### 4. Run Tests Using dotnet CLI

#### Run by Category

```bash
# Run all load tests
dotnet test --filter Category=LoadTest

# Run all benchmarks
dotnet test --filter Category=Benchmark

# Run health checks
dotnet test --filter Category=HealthCheck
```

#### Run Specific Test Classes

```bash
# API load tests
dotnet test --filter ClassName=ApiLoadTests

# Cache performance tests
dotnet test --filter ClassName=CachePerformanceTests

# Search performance tests
dotnet test --filter ClassName=SearchPerformanceTests

# Stress tests
dotnet test --filter ClassName=StressTests
```

### 5. Run Individual Test Methods

```bash
# Specific load test
dotnet test --filter TestMethodName=GetAllMatches_LoadTest

# Specific benchmark
dotnet test --filter TestMethodName=GetAllMatches
```

## Test Results

### NBomber Results

- **Location**: `./performance-results/nbomber/`
- **Formats**: HTML reports, CSV data
- **Metrics**: RPS, response times, percentiles, error rates

### BenchmarkDotNet Results

- **Location**: `./BenchmarkDotNet.Artifacts/`
- **Formats**: HTML, JSON, CSV
- **Metrics**: Execution time, memory allocation, GC collections

## Understanding Test Results

### Key Metrics to Monitor

#### Load Test Metrics (NBomber)

- **RPS (Requests Per Second)**: Throughput of the API
- **Mean Response Time**: Average response time
- **P95/P99**: 95th/99th percentile response times
- **Success Rate**: Percentage of successful requests
- **Error Rate**: Percentage of failed requests

#### Benchmark Metrics (BenchmarkDotNet)

- **Mean**: Average execution time per operation
- **StdDev**: Standard deviation of execution times
- **Allocated**: Memory allocated per operation
- **Gen 0/1/2**: Garbage collection counts

### Performance Thresholds

- **Success Rate**: ≥ 95%
- **Average Response Time**: ≤ 1000ms
- **P95 Response Time**: ≤ 2000ms
- **Error Rate**: ≤ 5%

## Troubleshooting

### Common Issues

1. **Connection Refused**

   - Ensure the Footex API is running
   - Check the BaseUrl in appsettings.json
   - Verify firewall/network settings

2. **Database Connection Errors**

   - Ensure PostgreSQL is running
   - Check database connection string
   - Verify database permissions

3. **High Error Rates**

   - Check API logs for errors
   - Verify test data is properly seeded
   - Consider reducing load levels

4. **Out of Memory**
   - Reduce concurrent users
   - Decrease test duration
   - Check for memory leaks in the API

### Performance Optimization Tips

1. **API Optimization**

   - Enable response compression
   - Implement proper caching strategies
   - Optimize database queries
   - Use connection pooling

2. **Test Optimization**
   - Start with light loads and gradually increase
   - Use realistic test data
   - Run tests in isolated environments
   - Monitor system resources during tests

## Advanced Usage

### Custom Test Scenarios

You can create custom test scenarios by:

1. Creating new test classes in the appropriate folders
2. Using NBomber for load tests
3. Using BenchmarkDotNet for micro-benchmarks

### Continuous Integration

To run performance tests in CI/CD:

```yaml
- name: Run Performance Tests
  run: |
    cd Footex.PerformanceTests
    dotnet test --filter Category=LoadTest --logger "trx;LogFileName=performance-results.trx"
```

### Monitoring and Alerting

Set up monitoring to track:

- Response times over time
- Error rates
- Throughput metrics
- Resource utilization

## Support

For questions or issues with performance tests:

1. Check the test logs in `./TestResults/`
2. Review the NBomber and BenchmarkDotNet documentation
3. Check the Footex API health endpoints
4. Verify database connectivity and performance
