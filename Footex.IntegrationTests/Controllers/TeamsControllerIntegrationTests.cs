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

public class TeamsControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public TeamsControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllTeams_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllTeams_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/teams?country=England&league=Premier League");

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
    public async Task GetTeamById_WithValidId_ReturnsTeam()
    {
        // Arrange
        var teamId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("team", out var team);
        team.ValueKind.Should().Be(JsonValueKind.Object);

        team.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(teamId);
    }

    [Fact]
    public async Task GetTeamById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/teams/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTeam_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createTeamDto = new CreateTeamDto
        {
            Name = "Test Team FC",
            Country = "England",
            League = "Premier League",
            StadiumId = 1,
            FoundationDate = new DateTime(2000, 1, 1),
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams", createTeamDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("teamId", out var teamId);
        teamId.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTeam_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createTeamDto = new CreateTeamDto
        {
            Name = "", // Invalid: empty name
            Country = "England",
            League = "Premier League",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/teams", createTeamDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTeam_WithValidData_ReturnsOk()
    {
        // Arrange
        var teamId = await SeedTestDataAsync();
        var updateTeamDto = new UpdateTeamDto
        {
            Name = "Updated Team FC",
            Country = "Spain",
            League = "La Liga",
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/teams/{teamId}", updateTeamDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTeam_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateTeamDto = new UpdateTeamDto
        {
            Name = "Updated Team FC",
            Country = "Spain",
            League = "La Liga",
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/teams/999999", updateTeamDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTeam_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var teamId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTeam_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/teams/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeamSeasons_WithValidId_ReturnsSeasons()
    {
        // Arrange
        var teamId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/teams/Seasons/{teamId}");

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
    public async Task GetTeamSeasons_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/teams/Seasons/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var team = TestData.CreateTestTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        return team.Id;
    }
}
