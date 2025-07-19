using Footex.IntegrationTests.Common;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

[Collection("Performance tests collection")]
public class ApiLoadTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllMatches_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_all_matches",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/matches")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task GetMatchById_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_match_by_id",
                async _ =>
                {
                    var matchId = Random.Shared.Next(1, 101);
                    var request = Http.CreateRequest("GET", $"/api/matches/{matchId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(15, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task GetAllPlayers_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_all_players",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/players")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(12, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(6, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task GetPlayerById_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_player_by_id",
                async _ =>
                {
                    var playerId = Random.Shared.Next(1, 101);
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task GetAllTeams_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_all_teams",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/teams")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(4, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task GetAllStadiums_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "get_all_stadiums",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/stadiums")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(3, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task SearchEndpoints_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var searchQueries = new[] { "manchester", "liverpool", "barcelona", "madrid", "juventus" };

        var scenario = Scenario
            .Create(
                "search_players",
                async _ =>
                {
                    var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit=10"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task HealthCheck_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "health_check",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/health")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(20, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task MixedWorkload_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var loadSimulation = Simulation.KeepConstant(5, TimeSpan.FromMinutes(3));
        var matchesScenario = Scenario
            .Create(
                "mixed_matches",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/matches")
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(30)
            .WithLoadSimulations(loadSimulation);
        var playersScenario = Scenario
            .Create(
                "mixed_players",
                async _ =>
                {
                    var playerId = Random.Shared.Next(1, 101);
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(40)
            .WithLoadSimulations(loadSimulation);

        var teamsScenario = Scenario
            .Create(
                "mixed_teams",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/teams")
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(20)
            .WithLoadSimulations(loadSimulation);

        var searchScenario = Scenario
            .Create(
                "mixed_search",
                async _ =>
                {
                    var queries = new[] { "manchester", "liverpool", "barcelona" };
                    var query = queries[Random.Shared.Next(queries.Length)];
                    var request = Http.CreateRequest("GET", $"/api/search/players?query={query}")
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(10)
            .WithLoadSimulations(loadSimulation);

        NBomberRunner
            .RegisterScenarios(matchesScenario, playersScenario, teamsScenario, searchScenario)
            .Run();
    }
}
