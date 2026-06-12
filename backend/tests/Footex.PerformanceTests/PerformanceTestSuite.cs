using System.Diagnostics;
using Application.Helpers;
using Footex.PerformanceTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

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
            _logger.LogInformation("Load Test Suite includes:");
            _logger.LogInformation("- API Load Tests");
            _logger.LogInformation("- Cache Performance Tests");
            _logger.LogInformation("- Search Performance Tests");
            _logger.LogInformation("- Stress Tests");

            await RunPerformanceScript("load");
            await RunPerformanceScript("cache");
            await RunPerformanceScript("search");
            await RunPerformanceScript("stress");
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

            await RunPerformanceScript("benchmark");
        }
        finally
        {
            stopwatch.Stop();
            TestConfigurationHelper.LogTestEnd("Full Benchmark Suite", _logger, stopwatch.Elapsed);
        }
    }

    private async Task RunPerformanceScript(string testType)
    {
        _logger.LogInformation(
            "Executing performance test script for test type: {TestType}",
            testType
        );

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"-NoProfile ../scripts/run-performance-tests.ps1 -TestType {testType} -LogFilePath ./PerformanceLogs/{testType}.log",
            WorkingDirectory = DirectoryExtensions.GetSolutionDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;
            _logger.LogInformation(e.Data);
            output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;
            _logger.LogError(e.Data);
            error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Assert.Fail(
                $"Performance test script for '{testType}' failed with exit code {process.ExitCode}.\nErrors: {error}"
            );
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
        Assert.True(
            _settings.Cache.ExpectedCacheHitRatio is > 0 and <= 1
        );

        _logger.LogInformation("Configuration validation passed");
        _logger.LogInformation("Base URL: {BaseUrl}", _settings.BaseUrl);
        _logger.LogInformation(
            "Test Durations - Short: {Short}min, Medium: {Medium}min, Long: {Long}min",
            _settings.Duration.ShortTestMinutes,
            _settings.Duration.MediumTestMinutes,
            _settings.Duration.LongTestMinutes
        );
        _logger.LogInformation(
            "Load Settings - Light: {Light}rps, Medium: {Medium}rps, Heavy: {Heavy}rps",
            _settings.Load.LightLoadRps,
            _settings.Load.MediumLoadRps,
            _settings.Load.HeavyLoadRps
        );
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
            "/api/stadiums",
        };

        var accessibleEndpoints = 0;

        foreach (var endpoint in endpointsToTest)
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
                    _logger.LogWarning(
                        "⚠ {Endpoint} - Status: {StatusCode}",
                        endpoint,
                        response.StatusCode
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("✗ {Endpoint} - Error: {Error}", endpoint, ex.Message);
            }

        _logger.LogInformation(
            "Accessibility check completed: {Accessible}/{Total} endpoints accessible",
            accessibleEndpoints,
            endpointsToTest.Length
        );

        // We don't fail the test if endpoints aren't accessible as the API might not be running
        // This is just for validation purposes
    }

    [Fact]
    [Trait("Category", "Documentation")]
    public void GeneratePerformanceTestDocumentation()
    {
        _logger.LogInformation("Generating performance test documentation...");

        // Create documentation content
        var docBuilder = new System.Text.StringBuilder();
        docBuilder.AppendLine("# Performance Test Suite Documentation");
        docBuilder.AppendLine("==========================================");
        docBuilder.AppendLine();

        docBuilder.AppendLine("## Available Test Categories:");
        docBuilder.AppendLine("### 1. Load Tests (NBomber):");
        docBuilder.AppendLine("- **ApiLoadTests**: Tests individual API endpoints under load");
        docBuilder.AppendLine("- **CachePerformanceTests**: Tests caching effectiveness");
        docBuilder.AppendLine("- **SearchPerformanceTests**: Tests search functionality");
        docBuilder.AppendLine("- **StressTests**: Tests system under extreme conditions");
        docBuilder.AppendLine();

        docBuilder.AppendLine("### 2. Benchmarks (BenchmarkDotNet):");
        docBuilder.AppendLine(
            "- **ApiBenchmarks**: Detailed performance metrics for API endpoints"
        );
        docBuilder.AppendLine("- **SearchBenchmarks**: Micro-benchmarks for search functionality");
        docBuilder.AppendLine("- **CacheBenchmarks**: Cache performance analysis");
        docBuilder.AppendLine();

        docBuilder.AppendLine("## To run tests:");
        docBuilder.AppendLine(
            "- **PowerShell**: `./run-performance-tests.ps1 -TestType [load|stress|cache|search|benchmark|all]`"
        );
        docBuilder.AppendLine("- **dotnet CLI**: `dotnet test --filter Category=LoadTest`");
        docBuilder.AppendLine(
            "- **Visual Studio**: Run specific test classes or use Test Explorer"
        );
        docBuilder.AppendLine();

        docBuilder.AppendLine("## Test Configuration Settings");
        docBuilder.AppendLine($"- **Base URL**: {_settings.BaseUrl}");
        docBuilder.AppendLine("- **Test Durations**:");
        docBuilder.AppendLine($"  - Short: {_settings.Duration.ShortTestMinutes} minutes");
        docBuilder.AppendLine($"  - Medium: {_settings.Duration.MediumTestMinutes} minutes");
        docBuilder.AppendLine($"  - Long: {_settings.Duration.LongTestMinutes} minutes");
        docBuilder.AppendLine("- **Load Settings**:");
        docBuilder.AppendLine($"  - Light: {_settings.Load.LightLoadRps} requests per second");
        docBuilder.AppendLine($"  - Medium: {_settings.Load.MediumLoadRps} requests per second");
        docBuilder.AppendLine($"  - Heavy: {_settings.Load.HeavyLoadRps} requests per second");
        docBuilder.AppendLine($"- **Cache Settings**:");
        docBuilder.AppendLine(
            $"  - Expected Cache Hit Ratio: {_settings.Cache.ExpectedCacheHitRatio * 100}%"
        );

        // Generate timestamp for filename
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var docsDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "TestResults",
            "Documentation"
        );

        // Ensure directory exists
        if (!Directory.Exists(docsDirectory))
        {
            Directory.CreateDirectory(docsDirectory);
        }

        // Save documentation to file
        var docFilePath = Path.Combine(docsDirectory, $"performance_test_docs_{timestamp}.md");
        File.WriteAllText(docFilePath, docBuilder.ToString());

        _logger.LogInformation("Documentation generated successfully at: {FilePath}", docFilePath);

        // Also log the documentation to the console for immediate viewing
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
        _logger.LogInformation(
            "   - ApiBenchmarks: Detailed performance metrics for API endpoints"
        );
        _logger.LogInformation("   - SearchBenchmarks: Micro-benchmarks for search functionality");
        _logger.LogInformation("   - CacheBenchmarks: Cache performance analysis");
        _logger.LogInformation("");

        _logger.LogInformation("To run tests:");
        _logger.LogInformation(
            "- PowerShell: ./run-performance-tests.ps1 -TestType [load|stress|cache|search|benchmark|all]"
        );
        _logger.LogInformation("- dotnet CLI: dotnet test --filter Category=LoadTest");
        _logger.LogInformation("- Visual Studio: Run specific test classes or use Test Explorer");

        Assert.True(File.Exists(docFilePath), "Documentation file was not created successfully");
    }

    [Fact]
    [Trait("Category", "SmokeTest")]
    public async Task RunSmokeTest()
    {
        var stopwatch = Stopwatch.StartNew();
        TestConfigurationHelper.LogTestStart("Smoke Test", _logger);

        try
        {
            _logger.LogInformation("Running smoke test against API health endpoint...");

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.GetAsync("/api/health");

            Assert.True(response.IsSuccessStatusCode, "API health check failed.");

            _logger.LogInformation("✓ API health check passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smoke test failed");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            TestConfigurationHelper.LogTestEnd("Smoke Test", _logger, stopwatch.Elapsed);
        }
    }
}
