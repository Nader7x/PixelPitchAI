using Footex.PerformanceTests.Benchmarks;

namespace Footex.PerformanceTests;

public class BenchmarkRunner
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Available benchmark suites:");
            Console.WriteLine("1. api - Run API benchmarks");
            Console.WriteLine("2. search - Run search benchmarks");
            Console.WriteLine("3. cache - Run cache benchmarks");
            Console.WriteLine("4. all - Run all benchmarks");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run --project Footex.PerformanceTests [suite]");
            return;
        }

        switch (args[0].ToLower())
        {
            case "api":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>();
                break;

            case "search":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>();
                break;

            case "cache":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>();
                break;

            case "all":
                Console.WriteLine("Running API Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ApiBenchmarks>();

                Console.WriteLine("Running Search Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<SearchBenchmarks>();

                Console.WriteLine("Running Cache Benchmarks...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CacheBenchmarks>();
                break;

            default:
                Console.WriteLine($"Unknown benchmark suite: {args[0]}");
                Console.WriteLine("Available options: api, search, cache, all");
                break;
        }
    }
}