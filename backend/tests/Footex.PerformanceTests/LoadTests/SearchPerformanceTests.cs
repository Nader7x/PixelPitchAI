using Footex.IntegrationTests.Common;
using Footex.PerformanceTests.Common;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace Footex.PerformanceTests.LoadTests;

[Collection("Performance tests collection")]
[Trait("Category", "SearchTest")]
public class SearchPerformanceTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    private static readonly string[] SPlayersQueries =
    [
        "ronaldo",
        "messi",
        "neymar",
        "mbappe",
        "haaland",
    ];
    private static readonly string[] SPlayerSearchQueries =
    [
        "manchester",
        "liverpool",
        "barcelona",
        "madrid",
        "juventus",
        "messi",
        "ronaldo",
        "neymar",
        "mbappe",
        "haaland",
        "brazil",
        "argentina",
        "spain",
        "germany",
        "france",
    ];

    private static readonly string[] STeamSearchQueries =
    [
        "united",
        "city",
        "real",
        "barcelona",
        "juventus",
        "liverpool",
        "arsenal",
        "chelsea",
        "tottenham",
        "milan",
    ];

    private static readonly string[] SCoachSearchQueries =
    [
        "guardiola",
        "klopp",
        "mourinho",
        "ancelotti",
        "conte",
        "luis",
        "carlos",
        "jose",
        "antonio",
        "frank",
    ];

    private static readonly string[] SMatchSearchQueries =
    [
        "manchester liverpool",
        "barcelona real",
        "juventus milan",
        "arsenal chelsea",
        "tottenham city",
        "united liverpool",
    ];

    private static readonly string[] SFuzzySearchComparisonQueries =
    [
        "manchester",
        "ronaldo",
        "barcelona",
        "guardiola",
    ];

    private static readonly int[] SSearchLimits = [5, 10, 20, 50];

    [Fact]
    public async Task PlayerSearch_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var scenario = Scenario
            .Create(
                "player_search",
                async _ =>
                {
                    var query = SPlayerSearchQueries[
                        Random.Shared.Next(SPlayerSearchQueries.Length)
                    ];
                    var limit = Random.Shared.Next(5, 21); // 5-20 results
                    var fuzzySearch = Random.Shared.Next(0, 2) == 1; // 50% chance of fuzzy search

                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(15, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)),
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.ShortTestMinutes))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task TeamSearch_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var scenario = Scenario
            .Create(
                "team_search",
                async _ =>
                {
                    var query = STeamSearchQueries[Random.Shared.Next(STeamSearchQueries.Length)];
                    var limit = Random.Shared.Next(5, 16); // 5-15 results
                    var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/teams?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(12, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)),
                Simulation.Inject(6, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.ShortTestMinutes))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task CoachSearch_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var scenario = Scenario
            .Create(
                "coach_search",
                async _ =>
                {
                    var query = SCoachSearchQueries[Random.Shared.Next(SCoachSearchQueries.Length)];
                    var limit = Random.Shared.Next(5, 11); // 5-10 results
                    var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/coaches?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)),
                Simulation.Inject(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.ShortTestMinutes))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task MatchSearch_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var scenario = Scenario
            .Create(
                "match_search",
                async _ =>
                {
                    var query = SMatchSearchQueries[Random.Shared.Next(SMatchSearchQueries.Length)];
                    var limit = Random.Shared.Next(5, 16); // 5-15 results
                    var fuzzySearch = Random.Shared.Next(0, 2) == 1;

                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/matches?query={query}&limit={limit}&enableFuzzySearch={fuzzySearch}"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(
                Simulation.Inject(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)),
                Simulation.Inject(4, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.ShortTestMinutes))
            );

        NBomberRunner.RegisterScenarios(scenario).Run();
    }

    [Fact]
    public async Task FuzzySearchComparison_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Scenario with fuzzy search enabled
        var fuzzySearchScenario = Scenario
            .Create(
                "fuzzy_search_enabled",
                async _ =>
                {
                    var query = SFuzzySearchComparisonQueries[
                        Random.Shared.Next(SFuzzySearchComparisonQueries.Length)
                    ];
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit=10&enableFuzzySearch=true"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(5, TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)));

        // Scenario with fuzzy search disabled
        var exactSearchScenario = Scenario
            .Create(
                "exact_search_only",
                async _ =>
                {
                    var query = SFuzzySearchComparisonQueries[
                        Random.Shared.Next(SFuzzySearchComparisonQueries.Length)
                    ];
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit=10&enableFuzzySearch=false"
                        )
                        .WithHeader("Accept", "application/json");

                    var response = await Http.Send(httpClient, request);
                    return response;
                }
            )
            .WithLoadSimulations(Simulation.KeepConstant(5, TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes)));

        NBomberRunner.RegisterScenarios(fuzzySearchScenario, exactSearchScenario).Run();
    }

    [Fact]
    public async Task SearchWithDifferentLimits_PerformanceTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var scenarios = SSearchLimits
            .Select(limit =>
                Scenario
                    .Create(
                        $"search_limit_{limit}",
                        async _ =>
                        {
                            const string query = "manchester";
                            var request = Http.CreateRequest(
                                    "GET",
                                    $"/api/search/players?query={query}&limit={limit}"
                                )
                                .WithHeader("Accept", "application/json");

                            var response = await Http.Send(httpClient, request);
                            return response;
                        }
                    )
                    .WithLoadSimulations(Simulation.KeepConstant(3, TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.ShortTestMinutes)))
            )
            .ToArray();

        NBomberRunner.RegisterScenarios(scenarios).Run();
    }

    [Fact]
    public async Task MixedSearch_LoadTest()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var simulation = Simulation.KeepConstant(4, TimeSpan.FromMinutes(TestConfigurationHelper.Settings.Duration.MediumTestMinutes));
        var playerSearchScenario = Scenario
            .Create(
                "mixed_player_search",
                async _ =>
                {
                    var query = SPlayersQueries[Random.Shared.Next(SPlayersQueries.Length)];
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/players?query={query}&limit=10"
                        )
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(40)
            .WithLoadSimulations(simulation);
        var teamSearchScenario = Scenario
            .Create(
                "mixed_team_search",
                async _ =>
                {
                    var query = STeamSearchQueries[Random.Shared.Next(3)]; // Take first 3 values
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/teams?query={query}&limit=10"
                        )
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(30)
            .WithLoadSimulations(simulation);

        var coachSearchScenario = Scenario
            .Create(
                "mixed_coach_search",
                async _ =>
                {
                    var query = SCoachSearchQueries[Random.Shared.Next(3)]; // Take first 3 values
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/coaches?query={query}&limit=10"
                        )
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(20)
            .WithLoadSimulations(simulation);

        var matchSearchScenario = Scenario
            .Create(
                "mixed_match_search",
                async _ =>
                {
                    var query = SMatchSearchQueries[Random.Shared.Next(2)]; // Take first 2 values
                    var request = Http.CreateRequest(
                            "GET",
                            $"/api/search/matches?query={query}&limit=10"
                        )
                        .WithHeader("Accept", "application/json");
                    return await Http.Send(httpClient, request);
                }
            )
            .WithWeight(10)
            .WithLoadSimulations(simulation);

        NBomberRunner
            .RegisterScenarios(
                playerSearchScenario,
                teamSearchScenario,
                coachSearchScenario,
                matchSearchScenario
            )
            .Run();
    }
}
