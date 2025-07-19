using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

[Collection("Database")]
public class GetLiveMatchQueryHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetLiveMatchQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidUserId_ReturnsLiveMatchOrNotFound()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        _context.Users.Add(user);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        _context.TeamSeasons.AddRange(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var match = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, false, user.Id);
        match.MatchStatus = "Live";
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var query = new GetLiveMatchQuery { UserId = user.Id };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.HasLiveMatch.Should().BeTrue();
        response.LiveMatch.Should().NotBeNull();
        response.LiveMatch!.Id.Should().Be(match.Id);
    }
}
