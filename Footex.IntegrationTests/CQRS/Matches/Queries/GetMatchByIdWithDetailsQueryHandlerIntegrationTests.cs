using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

[Collection("Database")]
public class GetMatchByIdWithDetailsQueryHandlerIntegrationTests
    : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetMatchByIdWithDetailsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsMatchDetails()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var query = new GetMatchByIdWithDetailsQuery { MatchId = match.Id };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Match.Should().NotBeNull();
        response.Match!.Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var query = new GetMatchByIdWithDetailsQuery { MatchId = 999999 };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.NotFound.Should().BeTrue();
        response.Match.Should().BeNull();
    }
}
