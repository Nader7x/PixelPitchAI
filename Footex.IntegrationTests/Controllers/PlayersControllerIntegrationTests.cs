using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Application.Helpers;
using Domain.Enums;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Footex.IntegrationTests.Controllers;

public class PlayersControllerIntegrationTests(
    FootexWebApplicationFactory factory,
    ITestOutputHelper testOutputHelper
) : IClassFixture<FootexWebApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task GetAllPlayers_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/players");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllPlayersQueryResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllPlayers_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var player = TestData.CreateTestDbPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync(
            "/api/players?nationality=England&preferredFoot=Right&pageNumber=1&pageSize=10"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllPlayersQueryResponse>();
        _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Players.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPlayerById_WithValidId_ReturnsPlayer()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var player = TestData.CreateTestDbPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/players/{player.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPlayerByIdQueryResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player.Id.Should().Be(player.Id);
    }

    [Fact]
    public async Task GetPlayerById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/players/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePlayer_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var createPlayerDto = new CreatePlayerDto
        {
            FullName = "John Doe",
            KnownName = "John",
            Position = "Forward",
            Nationality = "England",
            PreferredFoot = "Right",
            TeamId = team.Id,
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/players",
            createPlayerDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreatePlayerCommandResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreatePlayer_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var createPlayerDto = new CreatePlayerDto
        {
            FullName = "", // Invalid: empty name
            KnownName = "Doe",
            Position = "Forward",
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/players", createPlayerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePlayer_WithValidData_ReturnsOk()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var player = TestData.CreateTestDbPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();
        var updatePlayerDto = new UpdatePlayerDto
        {
            FullName = "Updated Player",
            KnownName = "Updated",
            Position = nameof(PlayerPosition.CentralMidfielder),
            Nationality = "Spain",
            PreferredFoot = "Left",
            ShirtNumber = 10,
        };

        var response = await httpClient.PutAsJsonAsync(
            $"/api/players/{player.Id}",
            updatePlayerDto.ToMultipartFormDataContent()
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdatePlayerCommandResponse>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePlayer_WithInvalidId_ReturnsNotFound()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        var updatePlayerDto = new UpdatePlayerDto
        {
            FullName = "Updated Player",
            KnownName = "Updated",
            Position = nameof(PlayerPosition.CentralMidfielder),
        };
        var formData = new MultipartFormDataContent
        {
            { new StringContent(updatePlayerDto.FullName), "FullName" },
            { new StringContent(updatePlayerDto.KnownName), "KnownName" },
            { new StringContent(updatePlayerDto.Position), "Position" },
        };

        var response = await httpClient.PutAsync("/api/players/999999", formData);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePlayer_WithValidId_ReturnsOk()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var player = TestData.CreateTestDbPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();

        var response = await httpClient.DeleteAsync($"/api/players/{player.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeletePlayerCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePlayer_WithInvalidId_ReturnsNotFound()
    {
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        var response = await httpClient.DeleteAsync("/api/players/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
