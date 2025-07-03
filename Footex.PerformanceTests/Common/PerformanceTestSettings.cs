using System.ComponentModel.DataAnnotations;

namespace Footex.PerformanceTests.Common;

public class PerformanceTestSettings
{
    public const string SectionName = "PerformanceTests";

    /// <summary>
    ///     Base URL for the API under test
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = "https://localhost:7001";

    /// <summary>
    ///     Duration settings for load tests
    /// </summary>
    public DurationSettings Duration { get; set; } = new();

    /// <summary>
    ///     Load simulation settings
    /// </summary>
    public LoadSettings Load { get; set; } = new();

    /// <summary>
    ///     Cache test settings
    /// </summary>
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    ///     Search test settings
    /// </summary>
    public SearchSettings Search { get; set; } = new();

    /// <summary>
    ///     Benchmark settings
    /// </summary>
    public BenchmarkSettings Benchmark { get; set; } = new();
}

public class DurationSettings
{
    /// <summary>
    ///     Short test duration in minutes
    /// </summary>
    public int ShortTestMinutes { get; set; } = 1;

    /// <summary>
    ///     Medium test duration in minutes
    /// </summary>
    public int MediumTestMinutes { get; set; } = 2;

    /// <summary>
    ///     Long test duration in minutes
    /// </summary>
    public int LongTestMinutes { get; set; } = 5;

    /// <summary>
    ///     Stress test duration in minutes
    /// </summary>
    public int StressTestMinutes { get; set; } = 10;
}

public class LoadSettings
{
    /// <summary>
    ///     Light load - requests per second
    /// </summary>
    public int LightLoadRps { get; set; } = 5;

    /// <summary>
    ///     Medium load - requests per second
    /// </summary>
    public int MediumLoadRps { get; set; } = 15;

    /// <summary>
    ///     Heavy load - requests per second
    /// </summary>
    public int HeavyLoadRps { get; set; } = 50;

    /// <summary>
    ///     Stress load - requests per second
    /// </summary>
    public int StressLoadRps { get; set; } = 100;

    /// <summary>
    ///     Number of concurrent users for constant load
    /// </summary>
    public int ConcurrentUsers { get; set; } = 10;

    /// <summary>
    ///     Maximum concurrent users for stress tests
    /// </summary>
    public int MaxConcurrentUsers { get; set; } = 50;
}

public class CacheSettings
{
    /// <summary>
    ///     Cache warm-up requests
    /// </summary>
    public int WarmupRequests { get; set; } = 10;

    /// <summary>
    ///     Cache hit test duration in minutes
    /// </summary>
    public int CacheHitTestMinutes { get; set; } = 2;

    /// <summary>
    ///     Expected cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double ExpectedCacheHitRatio { get; set; } = 0.8;
}

public class SearchSettings
{
    /// <summary>
    ///     Common search queries for testing
    /// </summary>
    public string[] CommonQueries { get; set; } =
        {
            "manchester",
            "liverpool",
            "barcelona",
            "madrid",
            "juventus",
            "messi",
            "ronaldo",
            "neymar",
            "mbappe",
            "haaland",
        };

    /// <summary>
    ///     Search result limits to test
    /// </summary>
    public int[] ResultLimits { get; set; } = { 5, 10, 20, 50 };

    /// <summary>
    ///     Percentage of searches that use fuzzy search
    /// </summary>
    public double FuzzySearchPercentage { get; set; } = 0.3;
}

public class BenchmarkSettings
{
    /// <summary>
    ///     Number of warmup iterations
    /// </summary>
    public int WarmupCount { get; set; } = 3;

    /// <summary>
    ///     Number of benchmark iterations
    /// </summary>
    public int IterationCount { get; set; } = 10;

    /// <summary>
    ///     Enable memory diagnoser
    /// </summary>
    public bool EnableMemoryDiagnoser { get; set; } = true;

    /// <summary>
    ///     Enable detailed timing
    /// </summary>
    public bool EnableDetailedTiming { get; set; } = true;
}
