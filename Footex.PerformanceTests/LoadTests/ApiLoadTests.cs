using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Footex.IntegrationTests.Common;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

public class ApiLoadTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public ApiLoadTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public void GetAllMatches_LoadTest()
    {
        var scenario = Scenario.Create("get_all_matches", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/matches")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void GetMatchById_LoadTest()
    {
        var scenario = Scenario.Create("get_match_by_id", async context =>
        {
            // Use random IDs between 1-100 to simulate different matches
            var matchId = Random.Shared.Next(1, 101);
            var request = Http.CreateRequest("GET", $"/api/matches/{matchId}")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 8, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void GetAllPlayers_LoadTest()
    {
        var scenario = Scenario.Create("get_all_players", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/players")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 12, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 6, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void GetPlayerById_LoadTest()
    {
        var scenario = Scenario.Create("get_player_by_id", async context =>
        {
            var playerId = Random.Shared.Next(1, 101);
            var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void GetAllTeams_LoadTest()
    {
        var scenario = Scenario.Create("get_all_teams", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/teams")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 8, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 4, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void GetAllStadiums_LoadTest()
    {
        var scenario = Scenario.Create("get_all_stadiums", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/stadiums")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 3, during: TimeSpan.FromMinutes(2))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void SearchEndpoints_LoadTest()
    {
        var searchQueries = new[] { "manchester", "liverpool", "barcelona", "madrid", "juventus" };
        
        var scenario = Scenario.Create("search_players", async context =>
        {
            var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
            var request = Http.CreateRequest("GET", $"/api/search/players?query={query}&limit=10")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void HealthCheck_LoadTest()
    {
        var scenario = Scenario.Create("health_check", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/health")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(_httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void MixedWorkload_LoadTest()
    {
        var matchesScenario = Scenario.Create("mixed_matches", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/matches")
                .WithHeader("Accept", "application/json");
            return await Http.Send(_httpClient, request);
        })
        .WithWeight(30)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(3)));

        var playersScenario = Scenario.Create("mixed_players", async context =>
        {
            var playerId = Random.Shared.Next(1, 101);
            var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                .WithHeader("Accept", "application/json");
            return await Http.Send(_httpClient, request);
        })
        .WithWeight(40)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 8, during: TimeSpan.FromMinutes(3)));

        var teamsScenario = Scenario.Create("mixed_teams", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/teams")
                .WithHeader("Accept", "application/json");
            return await Http.Send(_httpClient, request);
        })
        .WithWeight(20)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 3, during: TimeSpan.FromMinutes(3)));

        var searchScenario = Scenario.Create("mixed_search", async context =>
        {
            var queries = new[] { "manchester", "liverpool", "barcelona" };
            var query = queries[Random.Shared.Next(queries.Length)];
            var request = Http.CreateRequest("GET", $"/api/search/players?query={query}")
                .WithHeader("Accept", "application/json");
            return await Http.Send(_httpClient, request);
        })
        .WithWeight(10)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 2, during: TimeSpan.FromMinutes(3)));

        NBomberRunner
            .RegisterScenarios(matchesScenario, playersScenario, teamsScenario, searchScenario)
            .Run();
    }
}
