using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

public class CreateMatchCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootexWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly FootballDbContext _context;

    public CreateMatchCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesMatchInDatabase()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var awayTeam = TestData.CreateTestTeam();
        awayTeam.Name = "Away Team";
        awayTeam.ShortName = "AWT";
        
        var season = TestData.CreateTestSeason();
        var stadium = TestData.CreateTestStadium();

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        _context.Stadiums.Add(stadium);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            StadiumId = stadium.Id,
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID,
            MatchStatus = "Scheduled" // Assuming a default match status
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify match was created in database
        var createdMatch = await _context.Matches.FindAsync(response.Id);
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
        var team = TestData.CreateTestTeam();
        var season = TestData.CreateTestSeason();
        
        _context.Teams.Add(team);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var seasonTeam = TestData.CreateTestSeasonTeam(season.Id, team.Id);
        _context.TeamSeasons.Add(seasonTeam);
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = seasonTeam.Id,
            AwaySeasonId = seasonTeam.Id,
            HomeTeamId = team.Id,
            AwayTeamId = team.Id, // Same as home team
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("must be different");
    }

    [Fact]
    public async Task Handle_WithInvalidHomeTeam_ReturnsFailureResponse()
    {
        // Arrange
        var awayTeam = TestData.CreateTestTeam();
        var season = TestData.CreateTestSeason();
        
        _context.Teams.Add(awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.Add(awaySeasonTeam);
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = 999,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = 999999, // Invalid team ID
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Home Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithInvalidAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var homeTeam = TestData.CreateTestTeam();
        var season = TestData.CreateTestSeason();
        
        _context.Teams.Add(homeTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        _context.TeamSeasons.Add(homeSeasonTeam);
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = 999,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = 999999, // Invalid team ID
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Away Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithInvalidStadium_ReturnsFailureResponse()
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
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            StadiumId = 999999, // Invalid stadium ID
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Stadium").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithCoaches_CreatesMatchWithCoaches()
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

        var homeCoach = TestData.CreateTestCoach(homeTeam.Id);
        var awayCoach = TestData.CreateTestCoach(awayTeam.Id);
        awayCoach.FirstName = "Away";
        awayCoach.LastName = "Coach";

        _context.Coaches.AddRange(homeCoach, awayCoach);

        var homeSeasonTeam = TestData.CreateTestSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var command = new CreateMatchCommand
        {
            HomeSeasonId = homeSeasonTeam.Id,
            AwaySeasonId = awaySeasonTeam.Id,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            HomeCoachId = homeCoach.Id,
            AwayCoachId = awayCoach.Id,
            MatchWeek = 1,
            CreatorId = "test-user", // Assuming a test user ID
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify match was created with coaches
        var createdMatch = await _context.Matches.FindAsync(response.Id);
        createdMatch.Should().NotBeNull();
        createdMatch!.HomeCoachId.Should().Be(homeCoach.Id);
        createdMatch.AwayCoachId.Should().Be(awayCoach.Id);
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
