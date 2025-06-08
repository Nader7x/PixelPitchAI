using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Mvc.Testing;
using Footex.IntegrationTests.Common;
using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Footex.PerformanceTests.Benchmarks;

[Config(typeof(SearchBenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SearchBenchmarks
{
    private FootexWebApplicationFactory _factory = null!;
    private HttpClient _httpClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new FootexWebApplicationFactory();
        _factory.InitializeAsync().GetAwaiter().GetResult();
        _httpClient = _factory.CreateClient();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
        _factory?.DisposeAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    [Arguments("manchester", 10, false)]
    [Arguments("manchester", 10, true)]
    [Arguments("liverpool", 20, false)]
    [Arguments("liverpool", 20, true)]
    public async Task<string> SearchPlayers(string query, int limit, bool enableFuzzySearch)
    {
        var response = await _httpClient.GetAsync(
            $"/api/search/players?query={query}&limit={limit}&enableFuzzySearch={enableFuzzySearch}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments("manchester", 10, false)]
    [Arguments("barcelona", 15, true)]
    public async Task<string> SearchTeams(string query, int limit, bool enableFuzzySearch)
    {
        var response = await _httpClient.GetAsync(
            $"/api/search/teams?query={query}&limit={limit}&enableFuzzySearch={enableFuzzySearch}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments("guardiola", 10, false)]
    [Arguments("klopp", 10, true)]
    public async Task<string> SearchCoaches(string query, int limit, bool enableFuzzySearch)
    {
        var response = await _httpClient.GetAsync(
            $"/api/search/coaches?query={query}&limit={limit}&enableFuzzySearch={enableFuzzySearch}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments("manchester liverpool", 10, false)]
    [Arguments("barcelona real", 10, true)]
    public async Task<string> SearchMatches(string query, int limit, bool enableFuzzySearch)
    {
        var response = await _httpClient.GetAsync(
            $"/api/search/matches?query={query}&limit={limit}&enableFuzzySearch={enableFuzzySearch}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> SearchPlayersShortQuery()
    {
        var response = await _httpClient.GetAsync("/api/search/players?query=m&limit=10");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> SearchPlayersLongQuery()
    {
        var response = await _httpClient.GetAsync(
            "/api/search/players?query=manchester united midfielder attacking&limit=10");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(25)]
    [Arguments(50)]
    public async Task<string> SearchPlayersWithDifferentLimits(int limit)
    {
        var response = await _httpClient.GetAsync($"/api/search/players?query=manchester&limit={limit}");
        return await response.Content.ReadAsStringAsync();
    }
}

public class SearchBenchmarkConfig : ManualConfig
{
    public SearchBenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(2)
            .WithIterationCount(8)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));
    }
}
