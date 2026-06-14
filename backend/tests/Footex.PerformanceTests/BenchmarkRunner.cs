using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Footex.PerformanceTests.Benchmarks;

namespace Footex.PerformanceTests;

public abstract class BenchmarkRunner
{
    public static void Main(string[] args)
    {
        bool isDryRun = args.Any(arg => 
            arg.Equals("dry", StringComparison.OrdinalIgnoreCase) || 
            arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("dryrun", StringComparison.OrdinalIgnoreCase)
        );

        var suite = args.FirstOrDefault(arg => 
            !arg.Equals("dry", StringComparison.OrdinalIgnoreCase) && 
            !arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase) &&
            !arg.Equals("dryrun", StringComparison.OrdinalIgnoreCase)
        );

        if (suite == null && args.Length > 0 && !isDryRun)
        {
            Console.WriteLine("Available benchmark suites:");
            Console.WriteLine("1. api - Run API benchmarks");
            Console.WriteLine("2. search - Run search benchmarks");
            Console.WriteLine("3. cache - Run cache benchmarks");
            Console.WriteLine("4. all - Run all benchmarks");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run --project Footex.PerformanceTests [suite] [dry]");
            return;
        }

        suite ??= "all";

        IConfig? config = null;
        if (isDryRun)
        {
            Console.WriteLine("Running in DRY-RUN mode (1 warmup, 1 iteration)...");
            config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Instance))
                .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
                .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
        }

        switch (suite.ToLower())
        {
            case "api":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>(config);
                break;

            case "search":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>(config);
                break;

            case "cache":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>(config);
                break;

            case "all":
                Console.WriteLine("Running API Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>(config);

                Console.WriteLine("Running Search Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>(config);

                Console.WriteLine("Running Cache Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>(config);
                break;

            default:
                Console.WriteLine($"Unknown benchmark suite: {suite}");
                Console.WriteLine("Available options: api, search, cache, all");
                break;
        }
    }
}
