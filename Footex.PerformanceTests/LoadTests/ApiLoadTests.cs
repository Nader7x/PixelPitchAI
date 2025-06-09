using Footex.IntegrationTests.Common;
using NBomber.CSharp;
using NBomber.Http.CSharp;
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
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(15, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(8, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(12, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(6, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(4, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(3, TimeSpan.FromMinutes(2))
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
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(1))
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
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.KeepConstant(20, TimeSpan.FromMinutes(1))
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
            .WithLoadSimulations(Simulation.KeepConstant(5, TimeSpan.FromMinutes(3)));

        var playersScenario = Scenario.Create("mixed_players", async context =>
            {
                var playerId = Random.Shared.Next(1, 101);
                var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(40)
            .WithLoadSimulations(Simulation.KeepConstant(8, TimeSpan.FromMinutes(3)));

        var teamsScenario = Scenario.Create("mixed_teams", async context =>
            {
                var request = Http.CreateRequest("GET", "/api/teams")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(20)
            .WithLoadSimulations(Simulation.KeepConstant(3, TimeSpan.FromMinutes(3)));

        var searchScenario = Scenario.Create("mixed_search", async context =>
            {
                var queries = new[] { "manchester", "liverpool", "barcelona" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/players?query={query}")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(10)
            .WithLoadSimulations(Simulation.KeepConstant(2, TimeSpan.FromMinutes(3)));

        NBomberRunner
            .RegisterScenarios(matchesScenario, playersScenario, teamsScenario, searchScenario)
            .Run();
    }
}