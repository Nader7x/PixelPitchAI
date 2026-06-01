using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Application.Helpers;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class TeamsControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllTeams_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllTeamsQueryResponse>();
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllTeams_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        team.Country = "England";
        team.League = "Premier League";
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync(
            "/api/teams?country=England&league=Premier League"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllTeamsQueryResponse>();
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTeamById_WithValidId_ReturnsTeam()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/teams/{team.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTeamByIdQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Team.Should().NotBeNull();
        result?.Team?.Id.Should().Be(team.Id);
    }

    [Fact]
    public async Task GetTeamById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/teams/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTeam_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        // Ensure stadium exists for the test
        var stadium = TestData.CreateTestDbStadium();
        context.Stadiums.Add(stadium);
        await context.SaveChangesAsync();
        var createTeamDto = new CreateTeamDto
        {
            Name = "Test Team FC",
            Country = "England",
            League = "Premier League",
            StadiumId = stadium.Id,
            FoundationDate = new DateTime(2000, 1, 1),
        };

        // Act
        var response = await httpClient.PostAsync(
            "/api/teams",
            createTeamDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateTeamCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateTeam_WithValidData_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var updateTeamDto = new UpdateTeamDto
        {
            Id = team.Id,
            Name = "Updated Team FC",
            Country = "Spain",
            League = "La Liga",
        };

        // Act
        var response = await httpClient.PutAsync(
            $"/api/teams/{team.Id}",
            updateTeamDto.ToMultipartFormDataContent()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateTeamCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTeam_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/teams/{team.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetTeamSeasons_WithValidId_ReturnsSeasons()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();
        var teamSeason = new TeamSeason { TeamId = team.Id, SeasonId = season.Id };
        context.TeamSeasons.Add(teamSeason);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/teams/Seasons/{team.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTeamSeasonsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.TeamId.Should().Be(team.Id);
        result?.Seasons.Should().NotBeEmpty();
    }
}
