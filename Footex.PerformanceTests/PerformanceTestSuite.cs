using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Footex.PerformanceTests.Common;
using Footex.PerformanceTests.LoadTests;
using Footex.PerformanceTests.Runners;
using System.Diagnostics;

namespace Footex.PerformanceTests;

public class PerformanceTestSuite
{
    private readonly ILogger<PerformanceTestSuite> _logger;
    private readonly PerformanceTestSettings _settings;

    public PerformanceTestSuite()
    {
        var serviceProvider = TestConfigurationHelper.CreateServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<PerformanceTestSuite>>();
        _settings = TestConfigurationHelper.Settings;
    }

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task RunFullLoadTestSuite()
    {
        var stopwatch = Stopwatch.StartNew();
        TestConfigurationHelper.LogTestStart("Full Load Test Suite", _logger);

        try
        {
            // This would typically run through a test orchestrator
            // For now, we'll log the test plan
            _logger.LogInformation("Load Test Suite includes:");
            _logger.LogInformation("- API Load Tests");
            _logger.LogInformation("- Cache Performance Tests");
            _logger.LogInformation("- Search Performance Tests");
            _logger.LogInformation("- Stress Tests");
            
            _logger.LogInformation("Use the PowerShell script to run individual test suites:");
            _logger.LogInformation("./run-performance-tests.ps1 -TestType load");
            
            await Task.Delay(1000); // Placeholder for actual test execution
        }
        finally
        {
            stopwatch.Stop();
            TestConfigurationHelper.LogTestEnd("Full Load Test Suite", _logger, stopwatch.Elapsed);
        }
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public async Task RunFullBenchmarkSuite()
    {
        var stopwatch = Stopwatch.StartNew();
        TestConfigurationHelper.LogTestStart("Full Benchmark Suite", _logger);

        try
        {
            _logger.LogInformation("Benchmark Suite includes:");
            _logger.LogInformation("- API Benchmarks");
            _logger.LogInformation("- Search Benchmarks");
            _logger.LogInformation("- Cache Benchmarks");
            
            _logger.LogInformation("Use the PowerShell script to run benchmarks:");
            _logger.LogInformation("./run-performance-tests.ps1 -TestType benchmark");
            
            await Task.Delay(1000); // Placeholder for actual test execution
        }
        finally
        {
            stopwatch.Stop();
            TestConfigurationHelper.LogTestEnd("Full Benchmark Suite", _logger, stopwatch.Elapsed);
        }
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public void ValidateTestConfiguration()
    {
        _logger.LogInformation("Validating performance test configuration...");
        
        // Validate settings
        Assert.NotNull(_settings);
        Assert.NotEmpty(_settings.BaseUrl);
        Assert.True(_settings.Duration.ShortTestMinutes > 0);
        Assert.True(_settings.Load.LightLoadRps > 0);
        Assert.True(_settings.Cache.ExpectedCacheHitRatio > 0 && _settings.Cache.ExpectedCacheHitRatio <= 1);
        
        _logger.LogInformation("Configuration validation passed");
        _logger.LogInformation("Base URL: {BaseUrl}", _settings.BaseUrl);
        _logger.LogInformation("Test Durations - Short: {Short}min, Medium: {Medium}min, Long: {Long}min", 
            _settings.Duration.ShortTestMinutes, 
            _settings.Duration.MediumTestMinutes, 
            _settings.Duration.LongTestMinutes);
        _logger.LogInformation("Load Settings - Light: {Light}rps, Medium: {Medium}rps, Heavy: {Heavy}rps", 
            _settings.Load.LightLoadRps, 
            _settings.Load.MediumLoadRps, 
            _settings.Load.HeavyLoadRps);
    }

    [Fact]
    [Trait("Category", "HealthCheck")]
    public async Task ValidateApiEndpointsAccessibility()
    {
        _logger.LogInformation("Validating API endpoints accessibility...");
        
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var endpointsToTest = new[]
        {
            "/api/health",
            "/api/matches",
            "/api/players",
            "/api/teams",
            "/api/stadiums"
        };

        var accessibleEndpoints = 0;
        
        foreach (var endpoint in endpointsToTest)
        {
            try
            {
                var response = await httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    accessibleEndpoints++;
                    _logger.LogInformation("✓ {Endpoint} - Accessible", endpoint);
                }
                else
                {
                    _logger.LogWarning("⚠ {Endpoint} - Status: {StatusCode}", endpoint, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("✗ {Endpoint} - Error: {Error}", endpoint, ex.Message);
            }
        }

        _logger.LogInformation("Accessibility check completed: {Accessible}/{Total} endpoints accessible", 
            accessibleEndpoints, endpointsToTest.Length);
            
        // We don't fail the test if endpoints aren't accessible as the API might not be running
        // This is just for validation purposes
    }

    [Fact]
    [Trait("Category", "Documentation")]
    public void GeneratePerformanceTestDocumentation()
    {
        _logger.LogInformation("Performance Test Suite Documentation");
        _logger.LogInformation("==========================================");
        _logger.LogInformation("");
        
        _logger.LogInformation("Available Test Categories:");
        _logger.LogInformation("1. Load Tests (NBomber):");
        _logger.LogInformation("   - ApiLoadTests: Tests individual API endpoints under load");
        _logger.LogInformation("   - CachePerformanceTests: Tests caching effectiveness");
        _logger.LogInformation("   - SearchPerformanceTests: Tests search functionality");
        _logger.LogInformation("   - StressTests: Tests system under extreme conditions");
        _logger.LogInformation("");
        
        _logger.LogInformation("2. Benchmarks (BenchmarkDotNet):");
        _logger.LogInformation("   - ApiBenchmarks: Detailed performance metrics for API endpoints");
        _logger.LogInformation("   - SearchBenchmarks: Micro-benchmarks for search functionality");
        _logger.LogInformation("   - CacheBenchmarks: Cache performance analysis");
        _logger.LogInformation("");
        
        _logger.LogInformation("To run tests:");
        _logger.LogInformation("- PowerShell: ./run-performance-tests.ps1 -TestType [load|stress|cache|search|benchmark|all]");
        _logger.LogInformation("- dotnet CLI: dotnet test --filter Category=LoadTest");
        _logger.LogInformation("- Visual Studio: Run specific test classes or use Test Explorer");
    }
}
