using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Dtos;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class MatchesControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public MatchesControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllMatches_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/matches");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllMatches_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/matches?status=Scheduled&matchWeek=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("matches", out var matches);
        matches.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetMatchById_WithValidId_ReturnsMatch()
    {
        // Arrange
        var matchId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/matches/{matchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("match", out var match);
        match.ValueKind.Should().Be(JsonValueKind.Object);

        match.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(matchId);
    }

    [Fact]
    public async Task GetMatchById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/matches/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMatchByIdWithDetails_WithValidId_ReturnsDetailedMatch()
    {
        // Arrange
        var matchId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/matches/Details/{matchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("match", out var match);
        match.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task CreateMatch_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var createMatchDto = new CreateMatchDto
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeSeasonId = 1,
            AwaySeasonId = 1,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            CreatorId = Guid.Empty.ToString()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/matches", createMatchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMatch_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var matchId = await SeedTestDataAsync();
        var updateMatchDto = new UpdateMatchDto
        {
            Id = matchId,
            HomeTeamId = 1,
            AwayTeamId = 2,
            SeasonId = 1,
            ScheduledDateTimeUTC = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            MatchStatus = "Scheduled"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/matches/{matchId}", updateMatchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMatch_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var matchId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/matches/{matchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task SimulateMatch_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var simulateMatchDto = new SimulateMatchDto
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamName = "Arsenal",
            AwayTeamName = "Chelsea",
            HomeTeamSeason = "2023/24",
            AwayTeamSeason = "2023/24",
            HomeSeasonId = 7,
            AwaySeasonId = 7
        };

        // Act
        var response = await _client.PostAsJsonAsync("/simulateMatch/test-user", simulateMatchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // Ensure database is created and migrated
        await context.Database.EnsureCreatedAsync();

        // Create test season
        var season = new Season
        {
            Name = "2023/24",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            LeagueName = "Premier League",
            Country = "England"
        };
        context.Seasons.Add(season);
        await context.SaveChangesAsync();

        // Create test teams
        var homeTeam = new Team
        {
            Name = "Arsenal",
            FoundationDate = DateTime.UtcNow.AddYears(-100),
            Country = "England",
            City = "London",
            Logo = "https://example.com/arsenal-logo.png"
        };

        var awayTeam = new Team
        {
            Name = "Chelsea",
            FoundationDate = DateTime.UtcNow.AddYears(-100),
            Country = "England",
            City = "London",
            Logo = "https://example.com/chelsea-logo.png"
        };

        context.Teams.AddRange(homeTeam, awayTeam);
        await context.SaveChangesAsync();

        // Create test match
        var match = new Match
        {
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            HomeTeamSeasonId = season.Id,
            AwayTeamSeasonId = season.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            MatchWeek = 1,
            CreatorId = "test-user-id",
            HomeTeamInMatchName = "Arsenal_2024",
            AwayTeamInMatchName = "Chelsea_2024"
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        return match.Id;
    }
}