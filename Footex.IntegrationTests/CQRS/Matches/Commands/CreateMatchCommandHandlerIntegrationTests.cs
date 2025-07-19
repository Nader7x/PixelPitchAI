using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

public class CreateMatchCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesMatchInDatabase()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();

        var season = TestData.CreateTestDbSeason();
        var stadium = TestData.CreateTestDbStadium();

        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Users.AddAsync(user);
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Stadiums.AddAsync(stadium);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.SeasonId,
            AwaySeasonId = awaySeasonTeam.SeasonId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            HomeTeamInMatchName = homeTeam.ShortName,
            AwayTeamInMatchName = awayTeam.ShortName,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            StadiumId = stadium.Id,
            MatchWeek = 1,
            CreatorId = user.Id,
            MatchStatus = "Scheduled", // Assuming a default match status
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify a match was created in a database
        var createdMatch = await Context.Matches.FindAsync(response.Id);
        createdMatch.Should().NotBeNull();
        createdMatch!.HomeTeamId.Should().Be(command.HomeTeamId);
        createdMatch.AwayTeamId.Should().Be(command.AwayTeamId);
        createdMatch.ScheduledDateTimeUtc.Should().Be(command.ScheduledDateTimeUtc);
        createdMatch.StadiumId.Should().Be(command.StadiumId);
        createdMatch.MatchWeek.Should().Be(command.MatchWeek);
        createdMatch.MatchStatus.Should().Be(command.MatchStatus);
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
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = seasonTeam.SeasonId,
            AwaySeasonId = seasonTeam.SeasonId,
            HomeTeamId = team.Id,
            AwayTeamId = team.Id, // Same as home team
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = user.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Home and Away teams Must be different");
    }

    [Fact]
    public async Task Handle_WithInvalidHomeTeam_ReturnsFailureResponse()
    {
        // Arrange
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddAsync(awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddAsync(awaySeasonTeam);
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = awaySeasonTeam.SeasonId,
            AwaySeasonId = awaySeasonTeam.SeasonId,
            HomeTeamId = 999999, // Invalid team ID
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = user.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Home Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithInvalidAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);

        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddAsync(homeTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        await Context.TeamSeasons.AddAsync(homeSeasonTeam);
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.SeasonId,
            AwaySeasonId = homeSeasonTeam.SeasonId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = 999999, // Invalid team ID
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = user.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Away Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithInvalidStadium_ReturnsFailureResponse()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();

        var season = TestData.CreateTestSeason();
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
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.SeasonId,
            AwaySeasonId = awaySeasonTeam.SeasonId,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            StadiumId = 999999, // Invalid stadium ID
            MatchWeek = 1,
            CreatorId = user.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Stadium").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithCoaches_CreatesMatchWithCoaches()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        homeTeam.Name = "Home TeamC";
        homeTeam.ShortName = "HMTC";
        var awayTeam = TestData.CreateTestDbTeam();
        awayTeam.Name = "Away TeamC";
        awayTeam.ShortName = "AWTC";

        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        await Context.Database.BeginTransactionAsync();
        await Context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await Context.Seasons.AddAsync(season);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        await Context.Database.CommitTransactionAsync();

        var homeCoach = TestData.CreateTestDbCoach(homeTeam.Id);
        homeCoach.FirstName = "HomeC";
        homeCoach.LastName = "Coach";
        var awayCoach = TestData.CreateTestDbCoach(awayTeam.Id);
        awayCoach.FirstName = "AwayC";
        awayCoach.LastName = "Coach";

        await Context.Coaches.AddRangeAsync(homeCoach, awayCoach);

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await Context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await Context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = season.Id,
            AwaySeasonId = season.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            HomeTeamInMatchName = homeTeam.ShortName,
            AwayTeamInMatchName = awayTeam.ShortName,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            HomeCoachId = homeCoach.Id,
            AwayCoachId = awayCoach.Id,
            MatchWeek = 1,
            CreatorId = user.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify match was created with coaches
        var createdMatch = await Context.Matches.FindAsync(response.Id);
        createdMatch.Should().NotBeNull();
        createdMatch!.HomeCoachId.Should().Be(homeCoach.Id);
        createdMatch.AwayCoachId.Should().Be(awayCoach.Id);
    }
}
