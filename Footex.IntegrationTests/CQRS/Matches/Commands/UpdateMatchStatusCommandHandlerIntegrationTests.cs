using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

public class UpdateMatchStatusCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidMatchId_UpdatesStatus()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        Context.Teams.AddRange(homeTeam, awayTeam);
        Context.Seasons.Add(season);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        Context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        match.MatchStatus = "Scheduled";
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        var command = new UpdateMatchStatusCommand { MatchId = match.Id, NewStatus = "Completed" };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Status.Should().Be("Completed");
        var updatedMatch = await Context.Matches.FindAsync(match.Id);
        updatedMatch!.MatchStatus.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_WithInvalidMatchId_ReturnsNotFound()
    {
        // Arrange
        var command = new UpdateMatchStatusCommand { MatchId = 999999, NewStatus = "Completed" };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.NotFound.Should().BeTrue();
        response.Status.Should().BeNull();
    }
}
