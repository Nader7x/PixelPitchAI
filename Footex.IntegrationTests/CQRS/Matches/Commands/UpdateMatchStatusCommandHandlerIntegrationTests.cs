using Application.CQRS.Matches.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Commands;

[Collection("Database")]
public class UpdateMatchStatusCommandHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public UpdateMatchStatusCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidMatchId_UpdatesStatus()
    {
        // Arrange
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        var user = TestData.CreateTestUser(true);
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        match.MatchStatus = "Scheduled";
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var command = new UpdateMatchStatusCommand { MatchId = match.Id, NewStatus = "Completed" };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Status.Should().Be("Completed");
        var updatedMatch = await _context.Matches.FindAsync(match.Id);
        updatedMatch!.MatchStatus.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_WithInvalidMatchId_ReturnsNotFound()
    {
        // Arrange
        var command = new UpdateMatchStatusCommand { MatchId = 999999, NewStatus = "Completed" };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.NotFound.Should().BeTrue();
        response.Status.Should().BeNull();
    }
}
