using System.Net;
using System.Net.Http.Json;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Helpers;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Controllers;
[Collection("Database collection")]
public class MatchesControllerIntegrationTests(FootexWebApplicationFactory factory)
    : IClassFixture<FootexWebApplicationFactory>
{
    [Fact]
    public async Task GetAllMatches_ReturnsSuccessStatusCode()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/matches");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllMatchesQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Matches.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllMatches_WithQueryParameters_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            var match = TestData.CreateTestMatch(
                homeTeam.Id,
                awayTeam.Id,
                season.Id,
                true,
                user?.Id
            );
            context.Matches.Add(match);
        }

        await context.SaveChangesAsync();

        // Act
        var response = await httpClient.GetAsync("/api/matches?status=Scheduled&matchWeek=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllMatchesQueryResponse>();
        result.Should().NotBeNull();
        result?.Succeeded.Should().BeTrue();
        result?.Matches.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMatchById_WithValidId_ReturnsMatch()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            var match = TestData.CreateTestMatch(
                homeTeam.Id,
                awayTeam.Id,
                season.Id,
                true,
                user.Id
            );
            context.Matches.Add(match);
            await context.SaveChangesAsync();

            // Act
            var response = await httpClient.GetAsync($"/api/matches/{match.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<GetMatchByIdQueryResponse>();
            result.Should().NotBeNull();
            result?.Succeeded.Should().BeTrue();
            result?.Match.Should().NotBeNull();
            result?.Match?.Id.Should().Be(match.Id);
        }
    }

    [Fact]
    public async Task GetMatchById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await httpClient.GetAsync("/api/matches/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMatchByIdWithDetails_WithValidId_ReturnsDetailedMatch()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            if (user?.Id != null)
            {
                var match = TestData.CreateTestMatch(
                    homeTeam.Id,
                    awayTeam.Id,
                    season.Id,
                    true,
                    user.Id
                );
                context.Matches.Add(match);
                await context.SaveChangesAsync();

                // Act
                var response = await httpClient.GetAsync($"/api/matches/Details/{match.Id}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var result =
                    await response.Content.ReadFromJsonAsync<GetMatchByIdWithDetailsQueryResponse>();
                result.Should().NotBeNull();
                result?.Succeeded.Should().BeTrue();
                result?.Match.Should().NotBeNull();
            }
        }
    }

    [Fact]
    public async Task CreateMatch_WithValidData_ReturnsCreated()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            var createMatchDto = new CreateMatchDto
            {
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                HomeSeasonId = season.Id,
                AwaySeasonId = season.Id,
                ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
                MatchStatus = "Scheduled",
                CreatorId = user?.Id,
                HomeTeamInMatchName = $"{homeTeam.Name}_2025",
                AwayTeamInMatchName = $"{awayTeam.Name}_2025",
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/api/matches", createMatchDto);
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var result = await response.Content.ReadFromJsonAsync<CreateMatchCommandResponse>();
            result.Should().NotBeNull();
            result?.Succeeded.Should().BeTrue();
            result?.Id.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task UpdateMatch_WithValidData_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            var match = TestData.CreateTestMatch(
                homeTeam.Id,
                awayTeam.Id,
                season.Id,
                true,
                user.Id
            );
            context.Matches.Add(match);
            await context.SaveChangesAsync();
            var updateMatchDto = new UpdateMatchDto
            {
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(8),
                MatchWeek = 2,
                MatchStatus = "Postponed",
            };

            // Act
            var response = await httpClient.PutAsJsonAsync(
                $"/api/matches/{match.Id}",
                updateMatchDto
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UpdateMatchCommandResponse>();
            result.Should().NotBeNull();
            result?.Succeeded.Should().BeTrue();
        }
    }

    [Fact]
    public async Task DeleteMatch_WithValidId_ReturnsOk()
    {
        // Arrange
        var httpClient = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("testuser@example.com");
        var season = TestData.CreateTestDbSeason();
        context.Seasons.Add(season);
        var (homeTeam, awayTeam) = CreateDbHomeAndAwayTeams();
        if (homeTeam != null && awayTeam != null)
        {
            context.Teams.AddRange(homeTeam, awayTeam);
            await context.SaveChangesAsync();
            var match = TestData.CreateTestMatch(
                homeTeam.Id,
                awayTeam.Id,
                season.Id,
                true,
                user.Id
            );
            context.Matches.Add(match);
            await context.SaveChangesAsync();

            // Act
            var response = await httpClient.DeleteAsync($"/api/matches/{match.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteMatchCommandResponse>();
            result.Should().NotBeNull();
            result?.Succeeded.Should().BeTrue();
        }
    }

    private static List<Team> CreateDbHomeAndAwayTeams()
    {
        Team[] teams = [TestData.CreateTestDbTeam("Home"), TestData.CreateTestDbTeam("Away")];

        return teams.ToList();
    }
}
