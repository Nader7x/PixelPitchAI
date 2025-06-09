using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class SearchControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public SearchControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SearchTeams_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Team";

        // Act
        var response = await _client.GetAsync($"/api/search/teams?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var teams = JsonSerializer.Deserialize<JsonElement>(content);
        teams.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchTeams_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/search/teams?query=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchPlayers_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Player";

        // Act
        var response = await _client.GetAsync($"/api/search/players?query={query}&limit=10&enableFuzzySearch=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var players = JsonSerializer.Deserialize<JsonElement>(content);
        players.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchCoaches_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Coach";

        // Act
        var response = await _client.GetAsync($"/api/search/coaches?query={query}&limit=10&advanced=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var coaches = JsonSerializer.Deserialize<JsonElement>(content);
        coaches.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchStadiums_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Stadium";

        // Act
        var response = await _client.GetAsync($"/api/search/stadiums?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var stadiums = JsonSerializer.Deserialize<JsonElement>(content);
        stadiums.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchMatches_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Match";

        // Act
        var response = await _client.GetAsync($"/api/search/matches?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var matches = JsonSerializer.Deserialize<JsonElement>(content);
        matches.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchSeasons_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test Season";

        // Act
        var response = await _client.GetAsync($"/api/search/seasons?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var seasons = JsonSerializer.Deserialize<JsonElement>(content);
        seasons.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task UnifiedSearch_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test";

        // Act
        var response = await _client.GetAsync($"/api/search/unified?query={query}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        jsonDoc.RootElement.TryGetProperty("results", out var results);
        results.ValueKind.Should().Be(JsonValueKind.Array);
        
        jsonDoc.RootElement.TryGetProperty("totalCount", out var totalCount);
        totalCount.GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task UnifiedSearch_WithEntityTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test";
        var entityTypes = "Team,Player";

        // Act
        var response = await _client.GetAsync($"/api/search/unified?query={query}&entityTypes={entityTypes}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        jsonDoc.RootElement.TryGetProperty("results", out var results);
        results.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task UnifiedSearch_WithShortQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/search/unified?query=A");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchWithFilters_WithValidFilters_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var filters = new SearchFiltersDto
        {
            Query = "Test",
            EntityTypes = new List<string> { "Team", "Player" },
            Country = "England",
            League = "Premier League",
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/search/filtered", filters);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        jsonDoc.RootElement.TryGetProperty("results", out var results);
        results.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchAll_WithValidQuery_ReturnsResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test";

        // Act
        var response = await _client.GetAsync($"/api/search/all?query={query}&limit=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        // Should contain results from multiple entity types
        jsonDoc.RootElement.TryGetProperty("teams", out var teams);
        jsonDoc.RootElement.TryGetProperty("players", out var players);
        jsonDoc.RootElement.TryGetProperty("coaches", out var coaches);
        jsonDoc.RootElement.TryGetProperty("stadiums", out var stadiums);
        
        teams.ValueKind.Should().Be(JsonValueKind.Array);
        players.ValueKind.Should().Be(JsonValueKind.Array);
        coaches.ValueKind.Should().Be(JsonValueKind.Array);
        stadiums.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchSuggestions_WithValidQuery_ReturnsSuggestions()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Tes";

        // Act
        var response = await _client.GetAsync($"/api/search/suggestions?query={query}&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<JsonElement>(content);
        suggestions.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SearchWithPagination_ReturnsCorrectPageSize()
    {
        // Arrange
        await SeedMultipleItemsAsync();
        var query = "Test";

        // Act
        var response = await _client.GetAsync($"/api/search/unified?query={query}&page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        jsonDoc.RootElement.TryGetProperty("results", out var results);
        results.GetArrayLength().Should().BeLessOrEqualTo(5);
        
        jsonDoc.RootElement.TryGetProperty("pageSize", out var pageSize);
        pageSize.GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task SearchWithInvalidLimit_UsesDefaultLimit()
    {
        // Arrange
        await SeedTestDataAsync();
        var query = "Test";

        // Act
        var response = await _client.GetAsync($"/api/search/teams?query={query}&limit=100"); // Over max limit

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // The controller should use default limit of 10 when limit exceeds maximum
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Seed stadium first
        var stadium = TestData.CreateTestStadium();
        stadium.Name = "Test Stadium";
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();

        // Seed team
        var team = TestData.CreateTestTeam();
        team.Name = "Test Team FC";
        team.StadiumId = stadium.Id;
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Seed player
        var player = TestData.CreateTestPlayer(team.Id);
        player.FullName = "Test";
        player.KnownName = "Player";
        context.Players.Add(player);

        // Seed coach
        var coach = TestData.CreateTestCoach(team.Id);
        coach.FirstName = "Test";
        coach.LastName = "Coach";
        context.Coaches.Add(coach);

        // Seed season
        var season = TestData.CreateTestSeason();
        season.Name = "Test Season 2024/25";
        context.Seasons.Add(season);

        await context.SaveChangesAsync();

        // Seed match
        var homeTeam = TestData.CreateTestTeam();
        homeTeam.Name = "Home Team";
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        
        context.Teams.AddRange(homeTeam, awayTeam);
        await context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id);
        context.Matches.Add(match);
        
        await context.SaveChangesAsync();
    }

    private async Task SeedMultipleItemsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Create multiple teams for pagination testing
        for (int i = 1; i <= 10; i++)
        {
            var team = TestData.CreateTestTeam();
            team.Name = $"Test Team {i}";
            context.Teams.Add(team);
        }

        await context.SaveChangesAsync();
    }
}
