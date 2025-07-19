using Application.CQRS.Matches.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Matches.Queries;

[Collection("Database")]
public class GetAllMatchesQueryHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetAllMatchesQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_ReturnsAllMatches()
    {
        // Arrange
        var user = TestData.CreateTestUser(true);
        var homeTeam = TestData.CreateTestDbTeam();
        var awayTeam = TestData.CreateTestDbTeam();
        var season = TestData.CreateTestDbSeason();
        await _context.Users.AddAsync(user);
        await _context.Teams.AddRangeAsync(homeTeam, awayTeam);
        await _context.Seasons.AddAsync(season);
        await _context.SaveChangesAsync();

        var homeSeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, homeTeam.Id);
        var awaySeasonTeam = TestData.CreateTestDbSeasonTeam(season.Id, awayTeam.Id);
        await _context.TeamSeasons.AddRangeAsync(homeSeasonTeam, awaySeasonTeam);
        await _context.SaveChangesAsync();

        var match1 = TestData.CreateTestMatch(homeTeam.Id, awayTeam.Id, season.Id, true, user.Id);
        var match2 = TestData.CreateTestMatch(awayTeam.Id, homeTeam.Id, season.Id, true, user.Id);
        await _context.Matches.AddRangeAsync(match1, match2);
        await _context.SaveChangesAsync();

        var query = new GetAllMatchesQuery();

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Matches.Should().NotBeNull();
        response.Matches.Should().Contain(m => m!.Id == match1.Id);
        response.Matches.Should().Contain(m => m!.Id == match2.Id);
    }
}
