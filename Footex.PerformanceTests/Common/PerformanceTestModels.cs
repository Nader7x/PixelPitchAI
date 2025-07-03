namespace Footex.PerformanceTests.Common;

public class PerformanceTestReport
{
    public DateTime TestDate { get; set; }
    public string ResultsDirectory { get; set; } = string.Empty;
    public PerformanceTestSettings TestConfiguration { get; set; } = new();
    public List<LoadTestResult> LoadTestResults { get; set; } = new();
    public List<BenchmarkResult> BenchmarkResults { get; set; } = new();
    public PerformanceSummary Summary { get; set; } = new();
}

public class LoadTestResult
{
    public string TestName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<PerformanceMetric> Metrics { get; set; } = new();
}

public class PerformanceMetric
{
    public string ScenarioName { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int OkCount { get; set; }
    public int FailCount { get; set; }
    public double AllDataMB { get; set; }
    public double ScenarioRPS { get; set; }
    public double MeanMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }

    public double SuccessRate => RequestCount > 0 ? (double)OkCount / RequestCount : 0;
    public double FailureRate => RequestCount > 0 ? (double)FailCount / RequestCount : 0;
}

public class BenchmarkResult
{
    public string TestName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public double MeanNs { get; set; }
    public double StdDevNs { get; set; }
    public double MinNs { get; set; }
    public double MaxNs { get; set; }
    public long AllocatedBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }

    public double MeanMs => MeanNs / 1_000_000;
    public double MeanSeconds => MeanNs / 1_000_000_000;
}

public class PerformanceSummary
{
    public int TotalRequests { get; set; }
    public int TotalFailures { get; set; }
    public double SuccessRate { get; set; }
    public double AverageRPS { get; set; }
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public List<string> PerformanceIssues { get; set; } = new();

    public bool HasPerformanceIssues => PerformanceIssues.Any();
    public string Grade => HasPerformanceIssues ? GetGrade() : "A";

    private string GetGrade()
    {
        var issueCount = PerformanceIssues.Count;
        return issueCount switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            _ => "F",
        };
    }
}
