using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Footex.IntegrationTests.Common;

namespace Footex.PerformanceTests.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ApiBenchmarks
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
    public async Task<string> GetAllMatches()
    {
        var response = await _httpClient.GetAsync("/api/matches");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetMatchById()
    {
        var response = await _httpClient.GetAsync("/api/matches/1");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetAllPlayers()
    {
        var response = await _httpClient.GetAsync("/api/players");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetPlayerById()
    {
        var response = await _httpClient.GetAsync("/api/players/1");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetAllTeams()
    {
        var response = await _httpClient.GetAsync("/api/teams");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetAllStadiums()
    {
        var response = await _httpClient.GetAsync("/api/stadiums");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetAllCoaches()
    {
        var response = await _httpClient.GetAsync("/api/coaches/filter");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> SearchPlayers()
    {
        var response = await _httpClient.GetAsync("/api/search/players?query=manchester&limit=10");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> HealthCheck()
    {
        var response = await _httpClient.GetAsync("/api/health");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [ArgumentsSource(nameof(FilterParameters))]
    public async Task<string> GetPlayersWithFilters(
        string nationality,
        string preferredFoot,
        int? teamId
    )
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(nationality))
            queryParams.Add($"nationality={nationality}");

        if (!string.IsNullOrEmpty(preferredFoot))
            queryParams.Add($"preferredFoot={preferredFoot}");

        if (teamId.HasValue)
            queryParams.Add($"teamId={teamId}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var response = await _httpClient.GetAsync($"/api/players{queryString}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [ArgumentsSource(nameof(PaginationParameters))]
    public async Task<string> GetPlayersWithPagination(int pageNumber, int pageSize)
    {
        var response = await _httpClient.GetAsync(
            $"/api/players?pageNumber={pageNumber}&pageSize={pageSize}"
        );
        return await response.Content.ReadAsStringAsync();
    }

    public IEnumerable<object[]> FilterParameters()
    {
        yield return new object[] { "Brazil", "Right", 1 };
        yield return new object[] { "Argentina", "Left", null! };
        yield return new object[] { "", "Right", 2 };
        yield return new object[] { "Spain", "", null! };
    }

    public IEnumerable<object[]> PaginationParameters()
    {
        yield return new object[] { 1, 10 };
        yield return new object[] { 1, 25 };
        yield return new object[] { 2, 10 };
        yield return new object[] { 1, 50 };
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(
            Job.Default.WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
        );
    }
}
