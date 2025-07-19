using Application.CQRS.Seasons.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

[Collection("Database")]
public class GetSeasonTeamsQueryHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetSeasonTeamsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidSeasonId_ReturnsTeams()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        _context.Seasons.Add(season);
        _context.Teams.AddRange(team1, team2);
        await _context.SaveChangesAsync();

        // Assuming TeamSeason is the join entity
        var teamSeason1 = TestData.CreateTestDbSeasonTeam(season.Id, team1.Id);
        var teamSeason2 = TestData.CreateTestDbSeasonTeam(season.Id, team2.Id);
        _context.TeamSeasons.AddRange(teamSeason1, teamSeason2);
        await _context.SaveChangesAsync();

        var query = new GetSeasonTeamsQuery { SeasonId = season.Id };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamSeasons.Should().NotBeNull();
        response.TeamSeasons.Should().Contain(ts => ts.TeamId == team1.Id);
        response.TeamSeasons.Should().Contain(ts => ts.TeamId == team2.Id);
    }

    [Fact]
    public async Task Handle_WithInvalidSeasonId_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetSeasonTeamsQuery { SeasonId = 999999 };

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamSeasons.Should().NotBeNull();
        response.TeamSeasons.Should().BeEmpty();
    }
}
