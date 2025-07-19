using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Footex.PerformanceTests.Common;

public class PerformanceTestAnalyzer(
    ILogger<PerformanceTestAnalyzer> logger,
    PerformanceTestSettings settings
)
{
    public async Task<PerformanceTestReport> AnalyzeTestResults(string resultsDirectory)
    {
        logger.LogInformation(
            "Analyzing performance test results in {Directory}",
            resultsDirectory
        );

        var report = new PerformanceTestReport
        {
            TestDate = DateTime.UtcNow,
            ResultsDirectory = resultsDirectory,
            TestConfiguration = settings,
        };

        try
        {
            // Analyze NBomber results (HTML and CSV files)
            await AnalyzeNBomberResults(resultsDirectory, report);

            // Analyze BenchmarkDotNet results
            await AnalyzeBenchmarkResults(resultsDirectory, report);

            // Generate summary
            GenerateSummary(report);

            logger.LogInformation("Performance analysis completed");
            return report;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing performance test results");
            throw;
        }
    }

    private async Task AnalyzeNBomberResults(string resultsDirectory, PerformanceTestReport report)
    {
        var nbomberResultsPath = Path.Combine(resultsDirectory, "nbomber");
        if (!Directory.Exists(nbomberResultsPath))
        {
            logger.LogWarning("NBomber results directory not found: {Path}", nbomberResultsPath);
            return;
        }

        // Look for CSV files with performance metrics
        var csvFiles = Directory.GetFiles(nbomberResultsPath, "*.csv", SearchOption.AllDirectories);

        foreach (var csvFile in csvFiles)
        {
            logger.LogInformation("Processing NBomber CSV file: {File}", csvFile);

            var testResult = new LoadTestResult
            {
                TestName = Path.GetFileNameWithoutExtension(csvFile),
                FilePath = csvFile,
            };

            // Parse CSV and extract key metrics
            await ParseNBomberCsv(csvFile, testResult);
            report.LoadTestResults.Add(testResult);
        }
    }

    private async Task ParseNBomberCsv(string csvFile, LoadTestResult result)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(csvFile);
            if (lines.Length < 2)
                return;

            // Skip header line and process data
            for (var i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(',');
                if (fields.Length >= 10)
                {
                    var metric = new PerformanceMetric
                    {
                        ScenarioName = fields[0].Trim('"'),
                        RequestCount = int.TryParse(fields[1], out var reqCount) ? reqCount : 0,
                        OkCount = int.TryParse(fields[2], out var okCount) ? okCount : 0,
                        FailCount = int.TryParse(fields[3], out var failCount) ? failCount : 0,
                        AllDataMB = double.TryParse(fields[4], out var dataMb) ? dataMb : 0,
                        ScenarioRPS = double.TryParse(fields[5], out var rps) ? rps : 0,
                        MeanMs = double.TryParse(fields[6], out var mean) ? mean : 0,
                        MinMs = double.TryParse(fields[7], out var min) ? min : 0,
                        MaxMs = double.TryParse(fields[8], out var max) ? max : 0,
                        P95Ms = double.TryParse(fields[9], out var p95) ? p95 : 0,
                    };

                    if (fields.Length > 10)
                        metric.P99Ms = double.TryParse(fields[10], out var p99) ? p99 : 0;

                    result.Metrics.Add(metric);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing NBomber CSV file: {File}", csvFile);
        }
    }

    private async Task AnalyzeBenchmarkResults(
        string resultsDirectory,
        PerformanceTestReport report
    )
    {
        var benchmarkResultsPath = Path.Combine(resultsDirectory, "BenchmarkDotNet.Artifacts");
        if (!Directory.Exists(benchmarkResultsPath))
        {
            logger.LogWarning(
                "BenchmarkDotNet results directory not found: {Path}",
                benchmarkResultsPath
            );
            return;
        }

        // Look for JSON result files
        var jsonFiles = Directory.GetFiles(
            benchmarkResultsPath,
            "*-report-full.json",
            SearchOption.AllDirectories
        );

        foreach (var jsonFile in jsonFiles)
        {
            logger.LogInformation("Processing BenchmarkDotNet JSON file: {File}", jsonFile);
            await ParseBenchmarkJson(jsonFile, report);
        }
    }

    private async Task ParseBenchmarkJson(string jsonFile, PerformanceTestReport report)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(jsonFile);
            var benchmarkData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            if (benchmarkData.TryGetProperty("Benchmarks", out var benchmarks))
                foreach (var benchmark in benchmarks.EnumerateArray())
                {
                    var benchmarkResult = new BenchmarkResult
                    {
                        TestName = benchmark.GetProperty("DisplayInfo").GetString() ?? "Unknown",
                        MethodName =
                            benchmark
                                .GetProperty("Target")
                                .GetProperty("Method")
                                .GetProperty("Name")
                                .GetString() ?? "Unknown",
                    };

                    if (benchmark.TryGetProperty("Statistics", out var stats))
                    {
                        benchmarkResult.MeanNs = stats.GetProperty("Mean").GetDouble();
                        benchmarkResult.StdDevNs = stats
                            .GetProperty("StandardDeviation")
                            .GetDouble();
                        benchmarkResult.MinNs = stats.GetProperty("Min").GetDouble();
                        benchmarkResult.MaxNs = stats.GetProperty("Max").GetDouble();
                    }

                    if (benchmark.TryGetProperty("Memory", out var memory))
                    {
                        benchmarkResult.AllocatedBytes = memory
                            .GetProperty("BytesAllocatedPerOperation")
                            .GetInt64();
                        benchmarkResult.Gen0Collections = memory
                            .GetProperty("Gen0CollectionsPerOperation")
                            .GetInt32();
                        benchmarkResult.Gen1Collections = memory
                            .GetProperty("Gen1CollectionsPerOperation")
                            .GetInt32();
                        benchmarkResult.Gen2Collections = memory
                            .GetProperty("Gen2CollectionsPerOperation")
                            .GetInt32();
                    }

                    report.BenchmarkResults.Add(benchmarkResult);
                }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing BenchmarkDotNet JSON file: {File}", jsonFile);
        }
    }

    private void GenerateSummary(PerformanceTestReport report)
    {
        logger.LogInformation("Generating performance test summary...");

        // Calculate overall statistics
        if (report.LoadTestResults.Any())
        {
            var allMetrics = report.LoadTestResults.SelectMany(r => r.Metrics).ToList();

            report.Summary.TotalRequests = allMetrics.Sum(m => m.RequestCount);
            report.Summary.TotalFailures = allMetrics.Sum(m => m.FailCount);
            report.Summary.AverageRPS = allMetrics.Average(m => m.ScenarioRPS);
            report.Summary.AverageResponseTime = allMetrics.Average(m => m.MeanMs);
            report.Summary.MaxResponseTime = allMetrics.Max(m => m.MaxMs);
            report.Summary.P95ResponseTime = allMetrics.Average(m => m.P95Ms);

            report.Summary.SuccessRate =
                report.Summary.TotalRequests > 0
                    ? (double)(report.Summary.TotalRequests - report.Summary.TotalFailures)
                        / report.Summary.TotalRequests
                    : 0;
        }

        // Performance thresholds validation
        ValidatePerformanceThresholds(report);
    }

    private void ValidatePerformanceThresholds(PerformanceTestReport report)
    {
        var issues = new List<string>();

        // Check success rate
        if (report.Summary.SuccessRate < 0.95)
            issues.Add($"Low success rate: {report.Summary.SuccessRate:P2} (expected: ≥95%)");

        // Check average response time
        if (report.Summary.AverageResponseTime > 1000) // 1 second
            issues.Add(
                $"High average response time: {report.Summary.AverageResponseTime:F2}ms (expected: ≤1000ms)"
            );

        // Check P95 response time
        if (report.Summary.P95ResponseTime > 2000) // 2 seconds
            issues.Add(
                $"High P95 response time: {report.Summary.P95ResponseTime:F2}ms (expected: ≤2000ms)"
            );

        report.Summary.PerformanceIssues = issues;

        if (issues.Any())
        {
            logger.LogWarning("Performance issues detected:");
            foreach (var issue in issues)
                logger.LogWarning("- {Issue}", issue);
        }
        else
        {
            logger.LogInformation("All performance thresholds met!");
        }
    }
}
