using Xunit;
using Footex.PerformanceTests.Benchmarks;

namespace Footex.PerformanceTests.Runners;

public class BenchmarkTestRunner
{
    [Fact]
    public void RunAllBenchmarks()
    {
        // Run all benchmark classes
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>();
        BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>();
        BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>();
    }

    [Fact]
    public void RunApiBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>();
    }

    [Fact]
    public void RunSearchBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>();
    }

    [Fact]
    public void RunCacheBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>();
    }
}
