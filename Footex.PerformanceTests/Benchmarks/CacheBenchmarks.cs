using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Footex.IntegrationTests.Common;

namespace Footex.PerformanceTests.Benchmarks;

[Config(typeof(CacheBenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CacheBenchmarks
{
    private FootexWebApplicationFactory _factory = null!;
    private HttpClient _httpClient = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _factory = new FootexWebApplicationFactory();
        await _factory.InitializeAsync();
        _httpClient = await _factory.CreateAuthenticatedClientAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _httpClient?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task<string> PlayerById_CacheHit()
    {
        // First request to warm up cache
        await _httpClient.GetAsync("/api/players/1");

        // Measured request (should hit cache)
        var response = await _httpClient.GetAsync("/api/players/1");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> PlayerById_CacheMiss()
    {
        // Use random ID to ensure cache miss
        var playerId = Random.Shared.Next(1000, 2000);
        var response = await _httpClient.GetAsync($"/api/players/{playerId}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> StadiumById_CacheHit()
    {
        // First request to warm up cache
        await _httpClient.GetAsync("/api/stadiums/1");

        // Measured request (should hit cache)
        var response = await _httpClient.GetAsync("/api/stadiums/1");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> StadiumById_CacheMiss()
    {
        var stadiumId = Random.Shared.Next(1000, 2000);
        var response = await _httpClient.GetAsync($"/api/stadiums/{stadiumId}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> CoachesFilter_CacheHit()
    {
        // First request to warm up cache
        await _httpClient.GetAsync("/api/coaches/filter?nationality=Brazil");

        // Measured request (should hit cache)
        var response = await _httpClient.GetAsync("/api/coaches/filter?nationality=Brazil");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> CoachesFilter_CacheMiss()
    {
        var nationality = $"TestNationality{Random.Shared.Next(1000, 2000)}";
        var response = await _httpClient.GetAsync($"/api/coaches/filter?nationality={nationality}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> StadiumsAll_CacheHit()
    {
        // First request to warm up cache
        await _httpClient.GetAsync("/api/stadiums?country=Spain");

        // Measured request (should hit cache)
        var response = await _httpClient.GetAsync("/api/stadiums?country=Spain");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> StadiumsAll_CacheMiss()
    {
        var country = $"TestCountry{Random.Shared.Next(1000, 2000)}";
        var response = await _httpClient.GetAsync($"/api/stadiums?country={country}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<string> SequentialCacheHits(int iterations)
    {
        // Warm up cache
        await _httpClient.GetAsync("/api/players/1");

        var results = new List<string>();
        for (var i = 0; i < iterations; i++)
        {
            var response = await _httpClient.GetAsync("/api/players/1");
            results.Add(await response.Content.ReadAsStringAsync());
        }

        return string.Join(",", results.Select(r => r.Length.ToString()));
    }

    [Benchmark]
    public async Task<string> MixedCacheScenario()
    {
        var results = new List<string>();

        // Cache hit
        await _httpClient.GetAsync("/api/players/1");
        var hitResponse = await _httpClient.GetAsync("/api/players/1");
        results.Add(await hitResponse.Content.ReadAsStringAsync());

        // Cache miss
        var missId = Random.Shared.Next(1000, 2000);
        var missResponse = await _httpClient.GetAsync($"/api/players/{missId}");
        results.Add(await missResponse.Content.ReadAsStringAsync());

        return string.Join(",", results.Select(r => r.Length.ToString()));
    }
}

public class CacheBenchmarkConfig : ManualConfig
{
    public CacheBenchmarkConfig()
    {
        AddJob(
            Job.Default.WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
        );
    }
}
