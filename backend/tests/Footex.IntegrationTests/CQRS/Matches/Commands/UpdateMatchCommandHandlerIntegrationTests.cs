using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

public class UpdateMatchCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_UpdatesMatchInDatabase()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();

        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        Context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.SeasonId;
        match.AwayTeamSeasonId = awaySeasonTeam.SeasonId;
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.SeasonId,
            AwaySeasonId = awaySeasonTeam.SeasonId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(14),
            MatchWeek = 2,
            HomeTeamScore = 2,
            AwayTeamScore = 1,
            MatchStatus = "Completed",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify match was updated in database
        var updatedMatch = await Context.Matches.FindAsync(match.Id);
        updatedMatch.Should().NotBeNull();
        updatedMatch!.ScheduledDateTimeUtc.Should().Be(command.ScheduledDateTimeUtc);
        updatedMatch.MatchWeek.Should().Be(command.MatchWeek);
        updatedMatch.HomeTeamScore.Should().Be(command.HomeTeamScore);
        updatedMatch.AwayTeamScore.Should().Be(command.AwayTeamScore);
        updatedMatch.MatchStatus.Should().Be(command.MatchStatus);
    }

    [Fact]
    public async Task Handle_WithInvalidMatchId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdateMatchCommand
        {
            Id = 999999, // Invalid match ID
            HomeTeamId = 1,
            AwayTeamId = 2,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithSameHomeAndAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddAsync(team);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var seasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, team.Id);
        await Context.TeamSeasons.AddAsync(seasonTeam);

        var match = TestData.CreateTestMatch(team.Id, team.Id, season.Id, true, user.Id);
        match.HomeTeamSeasonId = seasonTeam.SeasonId;
        match.AwayTeamSeasonId = seasonTeam.SeasonId;
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();
        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = seasonTeam.SeasonId,
            AwaySeasonId = seasonTeam.SeasonId,
            HomeTeamId = team.Id,
            AwayTeamId = team.Id, // Same as home team
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response
            .Error.Should()
            .Contain(
                "Home team and away team must be either different Teams or Same Teams With a different Seasons"
            );
    }

    [Fact]
    public async Task Handle_UpdatesWinningAndLosingTeams()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();

        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.SeasonId;
        match.AwayTeamSeasonId = awaySeasonTeam.SeasonId;
        await Context.Matches.AddAsync(match);
        await Context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.SeasonId,
            AwaySeasonId = awaySeasonTeam.SeasonId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            HomeTeamScore = 3,
            AwayTeamScore = 1,
            WinningTeamId = homeTeam.Id,
            LosingTeamId = awayTeam.Id,
            IsDraw = false,
            MatchStatus = "Completed",
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify winning/losing teams were updated
        var updatedMatch = await Context.Matches.FindAsync(match.Id);
        updatedMatch.Should().NotBeNull();
        updatedMatch!.WinningTeamId.Should().Be(homeTeam.Id);
        updatedMatch.LosingTeamId.Should().Be(awayTeam.Id);
        updatedMatch.IsDraw.Should().BeFalse();
    }
}
