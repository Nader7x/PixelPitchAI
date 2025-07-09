using Footex.IntegrationTests.Common;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

public class StressTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    private static readonly string[] EnduranceEndpointsArray =
    [
        "/api/health",
        "/api/matches",
        "/api/players?pageNumber=1&pageSize=10",
        "/api/teams",
    ];
    private static readonly string[] HighLoadEndpointsArray =
    [
        "/api/matches",
        "/api/players",
        "/api/teams",
        "/api/stadiums",
        "/api/health",
    ];
    private static readonly string[] MemoryLeakEndpointsArray =
    [
        "/api/players",
        "/api/matches",
        "/api/search/players?query=a&limit=50",
        "/api/matches/Details/1"
    ];
    private static readonly string[] DbQueriesArray =
    [
        "valencia",
        "villarreal",
        "barcelona",
        "real",
        "athletic"
    ];
    private static readonly string[] ConcurrentUsersQueries = ["real", "barcelona", "messi", "ronaldo"];

    [Fact]
    public async Task HighLoad_StressTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "high_load_stress",
                async _ =>
                {
                    var endpoints = HighLoadEndpointsArray;

                    var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
                    var request = Http.CreateRequest("GET", endpoint)
                        .WithHeader("Accept", "application/json");

                    return await Http.Send(httpClient, request);
                }
            )
            .WithLoadSimulations(
                // Gradually increase the load
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.Inject(100, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.Inject(200, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1)),
                // Spike test
                Simulation.Inject(500, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30)),
                // Cool down
                Simulation.Inject(50, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("stress-test-results")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();
    }

    [Fact]
    public async Task SpikeLoad_StressTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "spike_load_stress",
                async _ =>
                {
                    var request = Http.CreateRequest("GET", "/api/matches")
                        .WithHeader("Accept", "application/json");

                    return await Http.Send(httpClient, request);
                }
            )
            .WithLoadSimulations(
                // Normal load
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)),
                // Sudden spike
                Simulation.KeepConstant(100, TimeSpan.FromMinutes(1)),
                // Back to normal
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(2))
            );

        NBomberRunner.RegisterScenarios(scenario).WithReportFolder("spike-test-results").Run();
    }

    [Fact]
    public async Task DatabaseIntensive_StressTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var loadSimulation = Simulation.KeepConstant(20, TimeSpan.FromMinutes(5));
        var playerScenario = Scenario
            .Create(
                "player_queries",
                async _ =>
                {
                    var playerId = Random.Shared.Next(1, 1000);
                    var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                        .WithHeader("Accept", "application/json");

                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(40)
            .WithLoadSimulations(loadSimulation);

        var matchScenario = Scenario
            .Create(
                "match_queries",
                async _ =>
                {
                    var request = Http.CreateRequest(
                            "GET",
                            "/api/matches?status=Scheduled&matchWeek=1"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithWeight(30)
            .WithLoadSimulations(loadSimulation);

        var searchScenario = Scenario
            .Create(
                "search_queries",
                async _ =>
                {
                    var queries = DbQueriesArray;
                    var query = queries[Random.Shared.Next(queries.Length)];
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit=20"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithWeight(30)
            .WithLoadSimulations(loadSimulation);

        NBomberRunner
            .RegisterScenarios(playerScenario, matchScenario, searchScenario)
            .WithReportFolder("db-intensive-test-results")
            .Run();
    }

    [Fact]
    public async Task ConcurrentUsers_StressTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var userScenario = Scenario
            .Create(
                "concurrent_user_simulation",
                async _ =>
                {
                    var actions = new[]
                    {
                        async () =>
                        {
                            var request = Http.CreateRequest("GET", "/api/matches")
                                .WithHeader("Accept", "application/json");
                            return await Http.Send(httpClient, request);
                        },
                        async () =>
                        {
                            var matchId = Random.Shared.Next(1, 50);
                            var request = Http.CreateRequest("GET", $"/api/matches/{matchId}")
                                .WithHeader("Accept", "application/json");
                            return await Http.Send(httpClient, request);
                        },
                        async () =>
                        {
                            var queries = ConcurrentUsersQueries;
                            var query = queries[Random.Shared.Next(queries.Length)];
                            var request = Http.CreateRequest(
                                "GET",
                                $"/api/search/players?query={query}"
                            );
                            return await Http.Send(httpClient, request);
                        },
                        async () =>
                        {
                            var playerId = Random.Shared.Next(1, 100);
                            var request = Http.CreateRequest("GET", $"/api/players/{playerId}");
                            return await Http.Send(httpClient, request);
                        },
                        async () =>
                        {
                            var request = Http.CreateRequest("GET", "/api/teams");
                            return await Http.Send(httpClient, request);
                        },
                    };

                    var action = actions[Random.Shared.Next(actions.Length)];
                    var response = await action();

                    await Task.Delay(Random.Shared.Next(100, 501));
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.KeepConstant(50, TimeSpan.FromMinutes(10))
            );

        NBomberRunner
            .RegisterScenarios(userScenario)
            .WithReportFolder("concurrent-users-test-results")
            .Run();
    }

    [Fact]
    public async Task EnduranceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "endurance_test",
                async _ =>
                {
                    var endpoints = EnduranceEndpointsArray;

                    var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
                    var request = Http.CreateRequest("GET", endpoint)
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.KeepConstant(20, TimeSpan.FromMinutes(30))
            );

        NBomberRunner.RegisterScenarios(scenario).WithReportFolder("endurance-test-results").Run();
    }

    [Fact]
    public async Task MemoryLeak_DetectionTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var scenario = Scenario
            .Create(
                "memory_leak_detection",
                async _ =>
                {
                    var heavyEndpoints = MemoryLeakEndpointsArray;

                    var endpoint = heavyEndpoints[Random.Shared.Next(heavyEndpoints.Length)];
                    var request = Http.CreateRequest("GET", endpoint)
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.KeepConstant(15, TimeSpan.FromHours(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("memory-leak-test-results")
            .Run();
    }
}
