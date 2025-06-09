using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

public class UpdateMatchCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly FootexWebApplicationFactory _factory;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public UpdateMatchCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesMatchInDatabase()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        awayTeam.ShortName = "AWT";

        var season = TestData.CreateTestSeason();

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.Id;
        match.AwayTeamSeasonId = awaySeasonTeam.Id;
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(14),
            MatchWeek = 2,
            HomeTeamScore = 2,
            AwayTeamScore = 1,
            MatchStatus = "Finished",
            HomeTeamPossession = 60,
            AwayTeamPossession = 40
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify match was updated in database
        var updatedMatch = await _context.Matches.FindAsync(match.Id);
        updatedMatch.Should().NotBeNull();
        updatedMatch!.ScheduledDateTimeUtc.Should().Be(command.ScheduledDateTimeUtc);
        updatedMatch.MatchWeek.Should().Be(command.MatchWeek);
        updatedMatch.HomeTeamScore.Should().Be(command.HomeTeamScore);
        updatedMatch.AwayTeamScore.Should().Be(command.AwayTeamScore);
        updatedMatch.MatchStatus.Should().Be(command.MatchStatus);
        updatedMatch.HomeTeamPossession.Should().Be(command.HomeTeamPossession);
        updatedMatch.AwayTeamPossession.Should().Be(command.AwayTeamPossession);
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
            MatchWeek = 1
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithSameHomeAndAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        var season = TestData.CreateTestSeason();

        _context.Teams.Add(team);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var seasonTeam = TestData.CreateTestSeasonTeam(season.Id, team.Id);
        _context.TeamSeasons.Add(seasonTeam);

        var match = TestData.CreateTestMatch(team.Id, team.Id);
        match.HomeTeamSeasonId = seasonTeam.Id;
        match.AwayTeamSeasonId = seasonTeam.Id;
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = seasonTeam.Id,
            AwaySeasonId = seasonTeam.Id,
            HomeTeamId = team.Id,
            AwayTeamId = team.Id, // Same as home team
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("must be different");
    }

    [Fact]
    public async Task Handle_WithInvalidPossessionSum_ReturnsFailureResponse()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        awayTeam.ShortName = "AWT";

        var season = TestData.CreateTestSeason();

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.Id;
        match.AwayTeamSeasonId = awaySeasonTeam.Id;
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            HomeTeamPossession = 60,
            AwayTeamPossession = 50 // Should sum to 100
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("possession").And.Contain("100");
    }

    [Fact]
    public async Task Handle_UpdatesWinningAndLosingTeams()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        awayTeam.ShortName = "AWT";

        var season = TestData.CreateTestSeason();

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.Id;
        match.AwayTeamSeasonId = awaySeasonTeam.Id;
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            HomeTeamScore = 3,
            AwayTeamScore = 1,
            WinningTeamId = homeTeam.Id,
            LosingTeamId = awayTeam.Id,
            IsDraw = false,
            MatchStatus = "Finished"
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify winning/losing teams were updated
        var updatedMatch = await _context.Matches.FindAsync(match.Id);
        updatedMatch.Should().NotBeNull();
        updatedMatch!.WinningTeamId.Should().Be(homeTeam.Id);
        updatedMatch.LosingTeamId.Should().Be(awayTeam.Id);
        updatedMatch.IsDraw.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdatesMatchStatistics()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        awayTeam.ShortName = "AWT";

        var season = TestData.CreateTestSeason();

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id);
        match.HomeTeamSeasonId = homeSeasonTeam.Id;
        match.AwayTeamSeasonId = awaySeasonTeam.Id;
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchCommand
        {
            Id = match.Id,
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            HomeTeamShots = 15,
            AwayTeamShots = 8,
            HomeTeamShotsOnTarget = 7,
            AwayTeamShotsOnTarget = 3,
            HomeTeamCorners = 6,
            AwayTeamCorners = 2,
            HomeTeamFouls = 12,
            AwayTeamFouls = 18,
            HomeTeamYellowCards = 2,
            AwayTeamYellowCards = 4,
            HomeTeamRedCards = 0,
            AwayTeamRedCards = 1
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify statistics were updated
        var updatedMatch = await _context.Matches.FindAsync(match.Id);
        updatedMatch.Should().NotBeNull();
        updatedMatch!.HomeTeamShots.Should().Be(command.HomeTeamShots);
        updatedMatch.AwayTeamShots.Should().Be(command.AwayTeamShots);
        updatedMatch.HomeTeamShotsOnTarget.Should().Be(command.HomeTeamShotsOnTarget);
        updatedMatch.AwayTeamShotsOnTarget.Should().Be(command.AwayTeamShotsOnTarget);
        updatedMatch.HomeTeamCorners.Should().Be(command.HomeTeamCorners);
        updatedMatch.AwayTeamCorners.Should().Be(command.AwayTeamCorners);
        updatedMatch.HomeTeamFouls.Should().Be(command.HomeTeamFouls);
        updatedMatch.AwayTeamFouls.Should().Be(command.AwayTeamFouls);
        updatedMatch.HomeTeamYellowCards.Should().Be(command.HomeTeamYellowCards);
        updatedMatch.AwayTeamYellowCards.Should().Be(command.AwayTeamYellowCards);
        updatedMatch.HomeTeamRedCards.Should().Be(command.HomeTeamRedCards);
        updatedMatch.AwayTeamRedCards.Should().Be(command.AwayTeamRedCards);
    }
}