using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Dtos;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;

public class SeasonsControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public SeasonsControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllSeasons_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/seasons");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSeasons_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/seasons?leagueName=Premier League&country=England&isActive=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("seasons", out var seasons);
        seasons.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetSeasonById_WithValidId_ReturnsSeason()
    {
        // Arrange
        var seasonId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/seasons/{seasonId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("season", out var season);
        season.ValueKind.Should().Be(JsonValueKind.Object);

        season.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(seasonId);
    }

    [Fact]
    public async Task GetSeasonById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/seasons/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSeasonById_ReturnsCacheHeaders()
    {
        // Arrange
        var seasonId = await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync($"/api/seasons/{seasonId}");
        
        // Act - Second call (should be cached)
        var response2 = await _client.GetAsync($"/api/seasons/{seasonId}");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check cache headers
        response1.Headers.Should().ContainKey("X-Cache-Hit");
        response1.Headers.GetValues("X-Cache-Hit").First().Should().Be("false");
        
        response2.Headers.Should().ContainKey("X-Cache-Hit");
        response2.Headers.GetValues("X-Cache-Hit").First().Should().Be("true");
    }

    [Fact]
    public async Task GetTeamSeasons_WithValidId_ReturnsTeams()
    {
        // Arrange
        var seasonId = await SeedTestDataWithTeamsAsync();

        // Act
        var response = await _client.GetAsync($"/api/seasons/TeamSeasons/{seasonId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("teams", out var teams);
        teams.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetTeamSeasons_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/seasons/TeamSeasons/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSeason_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createSeasonDto = new CreateSeasonDto
        {
            Name = "2024/25 Premier League",
            LeagueName = "Premier League",
            Country = "England",
            StartDate = new DateTime(2024, 8, 1),
            EndDate = new DateTime(2025, 5, 31),
            IsActive = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/seasons", createSeasonDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("seasonId", out var seasonId);
        seasonId.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSeason_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createSeasonDto = new CreateSeasonDto
        {
            Name = "", // Invalid: empty name
            LeagueName = "Premier League",
            Country = "England"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/seasons", createSeasonDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSeason_WithValidData_ReturnsOk()
    {
        // Arrange
        var seasonId = await SeedTestDataAsync();
        var updateSeasonDto = new UpdateSeasonDto
        {
            Name = "Updated Season",
            LeagueName = "La Liga",
            Country = "Spain",
            IsActive = false
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/seasons/{seasonId}", updateSeasonDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSeason_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateSeasonDto = new UpdateSeasonDto
        {
            Name = "Updated Season",
            LeagueName = "La Liga",
            Country = "Spain"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/seasons/999999", updateSeasonDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSeason_WithValidId_ReturnsOk()
    {
        // Arrange
        var seasonId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/seasons/{seasonId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSeason_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/seasons/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllSeasons_CachesBehavior_WorksCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync("/api/seasons?leagueName=Premier League");
        
        // Act - Second call with same parameters (should be cached)
        var response2 = await _client.GetAsync("/api/seasons?leagueName=Premier League");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check cache headers
        response1.Headers.Should().ContainKey("X-Cache-Hit");
        response1.Headers.GetValues("X-Cache-Hit").First().Should().Be("false");
        
        response2.Headers.Should().ContainKey("X-Cache-Hit");
        response2.Headers.GetValues("X-Cache-Hit").First().Should().Be("true");
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var season = TestData.CreateTestSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();

        return season.Id;
    }

    private async Task<int> SeedTestDataWithTeamsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var season = TestData.CreateTestSeason();
        context.Seasons.Add(season);

        var team1 = TestData.CreateTestTeam();
        var team2 = TestData.CreateTestTeam();
        team2.Name = "Team 2";
        
        context.Teams.Add(team1);
        context.Teams.Add(team2);
        
        await context.SaveChangesAsync();

        // Create season teams relationship
        var seasonTeam1 = TestData.CreateTestSeasonTeam(season.Id, team1.Id);
        var seasonTeam2 = TestData.CreateTestSeasonTeam(season.Id, team2.Id);
        
        context.TeamSeasons.Add(seasonTeam1);
        context.TeamSeasons.Add(seasonTeam2);
        
        await context.SaveChangesAsync();

        return season.Id;
    }
}
