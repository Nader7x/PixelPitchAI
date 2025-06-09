using Footex.IntegrationTests.Common;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

public class StressTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public StressTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public void HighLoad_StressTest()
    {
        var scenario = Scenario.Create("high_load_stress", async context =>
            {
                var endpoints = new[]
                {
                    "/api/matches",
                    "/api/players",
                    "/api/teams",
                    "/api/stadiums",
                    "/api/health"
                };

                var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
                var request = Http.CreateRequest("GET", endpoint)
                    .WithHeader("Accept", "application/json");

                return await Http.Send(_httpClient, request);
            })
            .WithLoadSimulations(
                // Gradually increase load
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
    public void SpikeLoad_StressTest()
    {
        var scenario = Scenario.Create("spike_load_stress", async context =>
            {
                var request = Http.CreateRequest("GET", "/api/matches")
                    .WithHeader("Accept", "application/json");

                return await Http.Send(_httpClient, request);
            })
            .WithLoadSimulations(
                // Normal load
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(2)),
                // Sudden spike
                Simulation.KeepConstant(100, TimeSpan.FromMinutes(1)),
                // Back to normal
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(2))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("spike-test-results")
            .Run();
    }

    [Fact]
    public void DatabaseIntensive_StressTest()
    {
        var playerScenario = Scenario.Create("player_queries", async context =>
            {
                var playerId = Random.Shared.Next(1, 1000);
                var request = Http.CreateRequest("GET", $"/api/players/{playerId}")
                    .WithHeader("Accept", "application/json");

                return await Http.Send(_httpClient, request);
            })
            .WithWeight(40)
            .WithLoadSimulations(
                Simulation.KeepConstant(20, TimeSpan.FromMinutes(5))
            );

        var matchScenario = Scenario.Create("match_queries", async context =>
            {
                var request = Http.CreateRequest("GET", "/api/matches?status=Scheduled&matchWeek=1")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWeight(30)
            .WithLoadSimulations(
                Simulation.KeepConstant(15, TimeSpan.FromMinutes(5))
            );

        var searchScenario = Scenario.Create("search_queries", async context =>
            {
                var queries = new[] { "manchester", "liverpool", "barcelona", "real", "juventus" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/players?query={query}&limit=20")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithWeight(30)
            .WithLoadSimulations(
                Simulation.KeepConstant(10, TimeSpan.FromMinutes(5))
            );

        NBomberRunner
            .RegisterScenarios(playerScenario, matchScenario, searchScenario)
            .WithReportFolder("db-intensive-test-results")
            .Run();
    }

    [Fact]
    public void ConcurrentUsers_StressTest()
    {
        var userScenario = Scenario.Create("concurrent_user_simulation", async context =>
            {
                // Simulate user browsing behavior
                var actions = new[]
                {
                    // View matches
                    async () =>
                    {
                        var request = Http.CreateRequest("GET", "/api/matches")
                            .WithHeader("Accept", "application/json");
                        return await Http.Send(_httpClient, request);
                    },

                    // View specific match
                    async () =>
                    {
                        var matchId = Random.Shared.Next(1, 50);
                        var request = Http.CreateRequest("GET", $"/api/matches/{matchId}")
                            .WithHeader("Accept", "application/json");
                        return await Http.Send(_httpClient, request);
                    },

                    // Search for players
                    async () =>
                    {
                        var queries = new[] { "manchester", "liverpool", "messi", "ronaldo" };
                        var query = queries[Random.Shared.Next(queries.Length)];
                        var request = Http.CreateRequest("GET", $"/api/search/players?query={query}");
                        return await Http.Send(_httpClient, request);
                    },

                    // View player details
                    async () =>
                    {
                        var playerId = Random.Shared.Next(1, 100);
                        var request = Http.CreateRequest("GET", $"/api/players/{playerId}");
                        return await Http.Send(_httpClient, request);
                    },

                    // View teams
                    async () =>
                    {
                        var request = Http.CreateRequest("GET", "/api/teams");
                        return await Http.Send(_httpClient, request);
                    }
                };

                // Random user action
                var action = actions[Random.Shared.Next(actions.Length)];
                var response = await action();

                // Add some think time between actions (100-500ms)
                await Task.Delay(Random.Shared.Next(100, 501));
                return response;
            })
            .WithLoadSimulations(
                // Simulate 50 concurrent users for 10 minutes
                Simulation.KeepConstant(50, TimeSpan.FromMinutes(10))
            );

        NBomberRunner
            .RegisterScenarios(userScenario)
            .WithReportFolder("concurrent-users-test-results")
            .Run();
    }

    [Fact]
    public void EnduranceTest()
    {
        var scenario = Scenario.Create("endurance_test", async context =>
            {
                var endpoints = new[]
                {
                    "/api/health",
                    "/api/matches",
                    "/api/players?pageNumber=1&pageSize=10",
                    "/api/teams"
                };

                var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
                var request = Http.CreateRequest("GET", endpoint)
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                // Sustained load for 30 minutes
                Simulation.KeepConstant(20, TimeSpan.FromMinutes(30))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("endurance-test-results")
            .Run();
    }

    [Fact]
    public void MemoryLeak_DetectionTest()
    {
        var scenario = Scenario.Create("memory_leak_detection", async context =>
            {
                // Focus on endpoints that might cause memory issues
                var heavyEndpoints = new[]
                {
                    "/api/players", // Large dataset
                    "/api/matches", // Complex queries
                    "/api/search/players?query=a&limit=50", // Search results
                    "/api/matches/Details/1" // Detailed data
                };

                var endpoint = heavyEndpoints[Random.Shared.Next(heavyEndpoints.Length)];
                var request = Http.CreateRequest("GET", endpoint)
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                // Run for extended period to detect memory leaks
                Simulation.KeepConstant(15, TimeSpan.FromHours(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("memory-leak-test-results")
            .Run();
    }
}