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

public class PlayersControllerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FootexWebApplicationFactory _factory;

    public PlayersControllerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllPlayers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/players");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllPlayers_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var response =
            await _client.GetAsync("/api/players?nationality=England&preferredFoot=Right&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("players", out var players);
        players.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetPlayerById_WithValidId_ReturnsPlayer()
    {
        // Arrange
        var playerId = await SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync($"/api/players/{playerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("player", out var player);
        player.ValueKind.Should().Be(JsonValueKind.Object);

        player.TryGetProperty("id", out var id);
        id.GetInt32().Should().Be(playerId);
    }

    [Fact]
    public async Task GetPlayerById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/players/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPlayerById_ReturnsCacheHeaders()
    {
        // Arrange
        var playerId = await SeedTestDataAsync();

        // Act - First call
        var response1 = await _client.GetAsync($"/api/players/{playerId}");

        // Act - Second call (should be cached)
        var response2 = await _client.GetAsync($"/api/players/{playerId}");

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
    public async Task CreatePlayer_WithValidData_ReturnsCreated()
    {
        // Arrange
        var teamId = await SeedTestTeamAsync();
        var createPlayerDto = new CreatePlayerDto
        {
            FullName = "John",
            KnownName = "Doe",
            Position = "Forward",
            Nationality = "England",
            PreferredFoot = "Right",
            TeamId = teamId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/players", createPlayerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("playerId", out var playerId);
        playerId.GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreatePlayer_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createPlayerDto = new CreatePlayerDto
        {
            FullName = "", // Invalid: empty name
            KnownName = "Doe",
            Position = "Forward"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/players", createPlayerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePlayer_WithValidData_ReturnsOk()
    {
        // Arrange
        var playerId = await SeedTestDataAsync();
        var updatePlayerDto = new UpdatePlayerDto
        {
            FullName = "Updated",
            KnownName = "Player",
            Position = "Midfielder",
            Nationality = "Spain"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/players/{playerId}", updatePlayerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePlayer_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updatePlayerDto = new UpdatePlayerDto
        {
            FullName = "Updated",
            KnownName = "Player",
            Position = "Midfielder"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/players/999999", updatePlayerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePlayer_WithValidId_ReturnsOk()
    {
        // Arrange
        var playerId = await SeedTestDataAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/players/{playerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeletePlayer_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/players/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllPlayers_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        await SeedMultiplePlayersAsync(15); // Seed more than one page

        // Act
        var response = await _client.GetAsync("/api/players?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        jsonDoc.RootElement.TryGetProperty("succeeded", out var succeeded);
        succeeded.GetBoolean().Should().BeTrue();

        jsonDoc.RootElement.TryGetProperty("players", out var players);
        players.ValueKind.Should().Be(JsonValueKind.Array);
        players.GetArrayLength().Should().BeLessOrEqualTo(5);
    }

    private async Task<int> SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var team = TestData.CreateTestTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var player = TestData.CreateTestPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();

        return player.Id;
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

    private async Task SeedMultiplePlayersAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        var team = TestData.CreateTestTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        for (var i = 0; i < count; i++)
        {
            var player = TestData.CreateTestPlayer(team.Id);
            player.FullName = $"Player{i}";
            context.Players.Add(player);
        }

        await context.SaveChangesAsync();
    }
}