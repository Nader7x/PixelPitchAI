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

public class CoachesControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public CoachesControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllCoaches_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/coaches/filter");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllCoaches_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/coaches/filter?nationality=England");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("coaches", out var coaches);
        coaches.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetCoachById_WithValidId_ReturnsCoach()
    {
        // Arrange
        var coachId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/coaches/{coachId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("coach", out var coach);
        coach.ValueKind.Should().Be(JsonValueKind.Object);

        coach.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(coachId);
    }

    [Fact]
    public async Task GetCoachById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/coaches/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCoachById_ReturnsCacheHeaders()
    {
        // Arrange
        var coachId = await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync($"/api/coaches/{coachId}");

        // Act - Second call (should be cached)
        var response2 = await _client.GetAsync($"/api/coaches/{coachId}");

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
    public async Task CreateCoach_WithValidData_ReturnsCreated()
    {
        // Arrange
        var teamId = await SeedTestTeamAsync();
        var createCoachDto = new CreateCoachDto
        {
            FirstName = "John",
            LastName = "Smith",
            DateOfBirth = new DateTime(1970, 3, 15),
            Nationality = "England",
            YearsOfExperience = 10,
            TeamId = teamId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/coaches", createCoachDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("coachId", out var coachId);
        coachId.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCoach_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createCoachDto = new CreateCoachDto
        {
            FirstName = "", // Invalid: empty name
            LastName = "Smith",
            DateOfBirth = new DateTime(1970, 3, 15)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/coaches", createCoachDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCoach_WithValidData_ReturnsOk()
    {
        // Arrange
        var coachId = await SeedTestDataAsync();
        var updateCoachDto = new UpdateCoachDto
        {
            FirstName = "Updated",
            LastName = "Coach",
            Nationality = "Spain",
            YearsOfExperience = 15
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/coaches/{coachId}", updateCoachDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCoach_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateCoachDto = new UpdateCoachDto
        {
            FirstName = "Updated",
            LastName = "Coach",
            Nationality = "Spain"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/coaches/999999", updateCoachDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCoach_WithValidId_ReturnsOk()
    {
        // Arrange
        var coachId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/coaches/{coachId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCoach_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/coaches/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllCoaches_WithTeamFilter_ReturnsFilteredResults()
    {
        // Arrange
        var teamId = await SeedTestTeamAsync();
        await SeedTestDataWithTeamAsync(teamId);

        // Act
        var response = await _client.GetAsync($"/api/coaches/filter?teamId={teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("coaches", out var coaches);
        coaches.ValueKind.Should().Be(JsonValueKind.Array);
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var team = TestData.CreateTestTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var coach = TestData.CreateTestCoach(team.Id);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();

        return coach.Id;
    }

    private async Task<int> SeedTestTeamAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var team = TestData.CreateTestTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        return team.Id;
    }

    private async Task<int> SeedTestDataWithTeamAsync(int teamId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var coach = TestData.CreateTestCoach(teamId);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();

        return coach.Id;
    }
}