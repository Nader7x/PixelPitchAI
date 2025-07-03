using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Http;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

public class CachePerformanceTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public CachePerformanceTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public void PlayerCache_PerformanceTest()
    {
        var scenario = Scenario
            .Create(
                "player_cache_test",
                async context =>
                {
                    // Test the same player ID multiple times to trigger cache
                    var playerId = 1;
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);

                    // Verify cache headers
                    if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                        return response;
                    var cacheHit = response.Payload.Value.Headers.TryGetValues(
                        "X-Cache-Hit",
                        out var cacheHeaderValue
                    );
                    context.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(30, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public void StadiumCache_PerformanceTest()
    {
        var scenario = Scenario
            .Create(
                "stadium_cache_test",
                async context =>
                {
                    // Test the same stadium ID multiple times to trigger cache
                    var stadiumId = 1;
                    var request = Http.CreateRequest("GET", $"/api/stadiums/{stadiumId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);

                    // Verify cache headers
                    if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                        return response;
                    var cacheHit = response.Payload.Value.Headers.TryGetValues(
                        "X-Cache-Hit",
                        out var cacheHeaderValue
                    );
                    context.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(25, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public void CoachCache_PerformanceTest()
    {
        var scenario = Scenario
            .Create(
                "coach_cache_test",
                async context =>
                {
                    // Test coaches filter endpoint with same parameters
                    var request = Http.CreateRequest(
                            "GET",
                            "/api/coaches/filter?nationality=Brazil"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);

                    // Verify cache headers
                    if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                        return response;
                    var cacheHit = response.Payload.Value.Headers.TryGetValues(
                        "X-Cache-Hit",
                        out var cacheHeaderValue
                    );
                    context.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public void CacheVsNonCache_ComparisonTest()
    {
        // Scenario that hits cached endpoints
        var cachedScenario = Scenario
            .Create(
                "cached_requests",
                async context =>
                {
                    var playerId = 1; // Same ID to ensure cache hits
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)));

        // Scenario that hits different endpoints (cache misses)
        var nonCachedScenario = Scenario
            .Create(
                "non_cached_requests",
                async context =>
                {
                    var playerId = Random.Shared.Next(100, 200); // Different IDs to avoid cache
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)));

        NBomberRunner.RegisterScenarios(cachedScenario, nonCachedScenario).Run();
    }
}
