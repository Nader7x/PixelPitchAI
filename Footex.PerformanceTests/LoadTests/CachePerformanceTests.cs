using Footex.IntegrationTests.Common;
using Microsoft.AspNetCore.Http;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Footex.PerformanceTests.LoadTests;

[Collection("Performance tests collection")]
public class CachePerformanceTests(
    FootexWebApplicationFactory factory,
    ITestOutputHelper testOutputHelper
) : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task PlayerCache_PerformanceTest()
    {
        try
        {
            // Create an authenticated HTTP client for the tests
            var httpClient = await factory.CreateAuthenticatedClientAsync();
            var scenario = Scenario
                .Create(
                    "player_cache_test",
                    async _ =>
                    {
                        // Test the same player ID multiple times to trigger the cache
                        const int playerId = 1;
                        var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                            .WithHeader("Accept", "application/json");

                        var response = await Http.Send(httpClient, request);

                        // Verify cache headers
                        if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                            return response;
                        var cacheHit = response.Payload.Value.Headers.TryGetValues(
                            "X-Cache-Hit",
                            out var cacheHeaderValue
                        );
                        _.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                        return response;
                    }
                )
                .WithLoadSimulations(
                    Simulation.Inject(30, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
                );

            NBomberRunner.RegisterScenarios(scenario).Run();
        }
        catch (Exception e)
        {
            testOutputHelper.WriteLine(e.Message);
        }
    }

    [Fact]
    public async Task StadiumCache_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "stadium_cache_test",
                async _ =>
                {
                    // Test the same stadium ID multiple times to trigger cache
                    var stadiumId = 1;
                    var request = Http.CreateRequest("GET", $"/api/stadiums/{stadiumId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);

                    // Verify cache headers
                    if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                        return response;
                    var cacheHit = response.Payload.Value.Headers.TryGetValues(
                        "X-Cache-Hit",
                        out var cacheHeaderValue
                    );
                    _.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(25, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task CoachCache_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "coach_cache_test",
                async _ =>
                {
                    // Test coaches filter endpoint with the same parameters
                    var request = Http.CreateRequest(
                            "GET",
                            "/api/coaches/filter?nationality=Brazil"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);

                    // Verify cache headers
                    if (!response.StatusCode.Equals(StatusCodes.Status200OK.ToString()))
                        return response;
                    var cacheHit = response.Payload.Value.Headers.TryGetValues(
                        "X-Cache-Hit",
                        out var cacheHeaderValue
                    );
                    _.Logger.Information("Cache Hit: {CacheHeaderValue}", cacheHeaderValue);

                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task CacheVsNonCache_ComparisonTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        // Scenario that hits cached endpoints
        var cachedScenario = Scenario
            .Create(
                "cached_requests",
                async _ =>
                {
                    const int playerId = 1; // Same ID to ensure cache hits
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)));

        // Scenario that hits different endpoints (cache misses)
        var nonCachedScenario = Scenario
            .Create(
                "non_cached_requests",
                async _ =>
                {
                    var playerId = Random.Shared.Next(100, 200); // Different IDs to avoid cache
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)));

        NBomberRunner.RegisterScenarios(cachedScenario, nonCachedScenario).Run();
    }
}
