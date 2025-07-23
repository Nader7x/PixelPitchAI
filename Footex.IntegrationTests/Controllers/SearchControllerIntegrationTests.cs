using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class SearchControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task SearchTeams_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        const string query = "Test Team";

        // Act
        var response = await httpClient.GetAsync($"/api/search/teams?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Team>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchPlayers_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var player = TestData.CreateTestPlayer(team.Id);
        context.Players.Add(player);
        await context.SaveChangesAsync();
        const string query = "Test Player";

        // Act
        var response = await httpClient.GetAsync(
            $"/api/search/players?query={query}&limit=10&enableFuzzySearch=true"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Player>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchCoaches_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var coach = TestData.CreateTestCoach(team.Id);
        context.Coaches.Add(coach);
        await context.SaveChangesAsync();
        const string query = "Test Coach";

        // Act
        var response = await httpClient.GetAsync(
            $"/api/search/coaches?query={query}&limit=10&advanced=true"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Coach>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchStadiums_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();
        const string query = "Test Stadium";

        // Act
        var response = await httpClient.GetAsync($"/api/search/stadiums?query={query}&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Stadium>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UnifiedSearch_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        const string query = "Test";

        // Act
        var response = await httpClient.GetAsync(
            $"/api/search/unified?query={query}&page=1&pageSize=10"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultDto>();
        result.Should().NotBeNull();
        result?.Items.Should().NotBeEmpty();
    }
}
