using Footex.IntegrationTests.Common;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

public class SearchPerformanceTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public SearchPerformanceTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public void PlayerSearch_PerformanceTest()
    {
        var searchQueries = new[]
        {
            "manchester", "liverpool", "barcelona", "madrid", "juventus",
            "messi", "ronaldo", "neymar", "mbappe", "haaland",
            "brazil", "argentina", "spain", "germany", "france"
        };

        var scenario = Scenario.Create("player_search", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var limit = Random.Shared.Next(5, 21); // 5-20 results
                var fuzzySearch = Random.Shared.Next(0, 2) == 1; // 50% chance of fuzzy search

                var request = Http.CreateRequest("GET",
                        $"/api/search/players?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.Inject(15, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(8, TimeSpan.FromMinutes(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void TeamSearch_PerformanceTest()
    {
        var searchQueries = new[]
        {
            "united", "city", "real", "barcelona", "juventus",
            "liverpool", "arsenal", "chelsea", "tottenham", "milan"
        };

        var scenario = Scenario.Create("team_search", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var limit = Random.Shared.Next(5, 16); // 5-15 results
                var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                var request = Http.CreateRequest("GET",
                        $"/api/search/teams?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.Inject(12, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(6, TimeSpan.FromMinutes(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void CoachSearch_PerformanceTest()
    {
        var searchQueries = new[]
        {
            "guardiola", "klopp", "mourinho", "ancelotti", "conte",
            "luis", "carlos", "jose", "antonio", "frank"
        };

        var scenario = Scenario.Create("coach_search", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var limit = Random.Shared.Next(5, 11); // 5-10 results
                var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                var request = Http.CreateRequest("GET",
                        $"/api/search/coaches?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void MatchSearch_PerformanceTest()
    {
        var searchQueries = new[]
        {
            "manchester liverpool", "barcelona real", "juventus milan",
            "arsenal chelsea", "tottenham city", "united liverpool"
        };

        var scenario = Scenario.Create("match_search", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var limit = Random.Shared.Next(5, 16); // 5-15 results
                var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                var request = Http.CreateRequest("GET",
                        $"/api/search/matches?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(4, TimeSpan.FromMinutes(1))
            );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }

    [Fact]
    public void FuzzySearchComparison_PerformanceTest()
    {
        var searchQueries = new[] { "manchester", "ronaldo", "barcelona", "guardiola" };

        // Scenario with fuzzy search enabled
        var fuzzySearchScenario = Scenario.Create("fuzzy_search_enabled", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var request = Http.CreateRequest("GET",
                        $"/api/search/players?query={query}&limit=10&enableFuzzySearch=true")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(2))
            );

        // Scenario with fuzzy search disabled
        var exactSearchScenario = Scenario.Create("exact_search_only", async context =>
            {
                var query = searchQueries[Random.Shared.Next(searchQueries.Length)];
                var request = Http.CreateRequest("GET",
                        $"/api/search/players?query={query}&limit=10&enableFuzzySearch=false")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(_httpClient, request);
                return response;
            })
            .WithLoadSimulations(
                Simulation.KeepConstant(5, TimeSpan.FromMinutes(2))
            );

        NBomberRunner
            .RegisterScenarios(fuzzySearchScenario, exactSearchScenario)
            .Run();
    }

    [Fact]
    public void SearchWithDifferentLimits_PerformanceTest()
    {
        var limits = new[] { 5, 10, 20, 50 };

        var scenarios = limits.Select(limit =>
            Scenario.Create($"search_limit_{limit}", async context =>
                {
                    var query = "manchester";
                    var request = Http.CreateRequest("GET",
                            $"/api/search/players?query={query}&limit={limit}")
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(_httpClient, request);
                    return response;
                })
                .WithLoadSimulations(
                    Simulation.KeepConstant(3, TimeSpan.FromMinutes(1))
                )
        ).ToArray();

        NBomberRunner
            .RegisterScenarios(scenarios)
            .Run();
    }

    [Fact]
    public void MixedSearch_LoadTest()
    {
        var playerSearchScenario = Scenario.Create("mixed_player_search", async context =>
            {
                var queries = new[] { "messi", "ronaldo", "neymar" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/players?query={query}&limit=10")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(40)
            .WithLoadSimulations(Simulation.KeepConstant(6, TimeSpan.FromMinutes(3)));

        var teamSearchScenario = Scenario.Create("mixed_team_search", async context =>
            {
                var queries = new[] { "manchester", "liverpool", "barcelona" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/teams?query={query}&limit=10")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(30)
            .WithLoadSimulations(Simulation.KeepConstant(4, TimeSpan.FromMinutes(3)));

        var coachSearchScenario = Scenario.Create("mixed_coach_search", async context =>
            {
                var queries = new[] { "guardiola", "klopp", "mourinho" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/coaches?query={query}&limit=10")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(20)
            .WithLoadSimulations(Simulation.KeepConstant(3, TimeSpan.FromMinutes(3)));

        var matchSearchScenario = Scenario.Create("mixed_match_search", async context =>
            {
                var queries = new[] { "manchester liverpool", "barcelona real" };
                var query = queries[Random.Shared.Next(queries.Length)];
                var request = Http.CreateRequest("GET", $"/api/search/matches?query={query}&limit=10")
                    .WithHeader("Accept", "application/json");
                return await Http.Send(_httpClient, request);
            })
            .WithWeight(10)
            .WithLoadSimulations(Simulation.KeepConstant(2, TimeSpan.FromMinutes(3)));

        NBomberRunner
            .RegisterScenarios(playerSearchScenario, teamSearchScenario, coachSearchScenario, matchSearchScenario)
            .Run();
    }
}