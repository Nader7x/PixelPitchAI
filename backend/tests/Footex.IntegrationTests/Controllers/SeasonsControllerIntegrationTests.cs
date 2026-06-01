using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Seasons.Commands;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class SeasonsControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllSeasons_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/seasons");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSeasonsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSeasons_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync(
            "/api/seasons?leagueName=Premier League&country=England"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSeasonsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Seasons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSeasonById_WithValidId_ReturnsSeason()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/seasons/{season.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSeasonByIdQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Season.Should().NotBeNull();
        result?.Season?.Id.Should().Be(season.Id);
    }

    [Fact]
    public async Task GetSeasonById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/seasons/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSeasonTeams_WithValidId_ReturnsTeams()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var team = TestData.CreateTestDbTeam();
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var teamSeason = new TeamSeason { TeamId = team.Id, SeasonId = season.Id };
        context.TeamSeasons.Add(teamSeason);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync($"/api/seasons/seasonteams/{season.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSeasonTeamsQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.TeamSeasons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSeason_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var competition = TestData.CreateTestCompetition();
        await context.Competitions.AddAsync(competition);
        await context.SaveChangesAsync();
        var createSeasonDto = new CreateSeasonDto
        {
            Name = "2024/25 Premier League",
            LeagueName = "Premier League",
            Country = "England",
            StartDate = new DateTime(2024, 8, 1),
            EndDate = new DateTime(2025, 5, 31),
            IsActive = true,
            CompetitionId = competition.Id,
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/seasons", createSeasonDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateSeasonCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSeason_WithValidData_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();
        var updateSeasonDto = new UpdateSeasonDto
        {
            Name = "Updated Test Season",
            LeagueName = "La Liga",
            Country = "Spain",
            IsActive = false,
        };

        // Act
        var response = await httpClient.PutAsJsonAsync(
            $"/api/seasons/{season.Id}",
            updateSeasonDto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateSeasonCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSeason_WithValidId_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.DeleteAsync($"/api/seasons/{season.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeleteSeasonCommandResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
    }
}
