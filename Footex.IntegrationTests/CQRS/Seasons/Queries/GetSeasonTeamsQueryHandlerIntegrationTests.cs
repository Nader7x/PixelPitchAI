using Application.CQRS.Seasons.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

public class GetSeasonTeamsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidSeasonId_ReturnsTeams()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        Context.Seasons.Add(season);
        Context.Teams.AddRange(team1, team2);
        await Context.SaveChangesAsync();

        // Assuming TeamSeason is the join entity
        var teamSeason1 = TestData.CreateTestDbSeasonTeam(season.Id, team1.Id);
        var teamSeason2 = TestData.CreateTestDbSeasonTeam(season.Id, team2.Id);
        Context.TeamSeasons.AddRange(teamSeason1, teamSeason2);
        await Context.SaveChangesAsync();

        var query = new GetSeasonTeamsQuery { SeasonId = season.Id };

        // Act
        var response = await Mediator.Send(query);

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
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamSeasons.Should().NotBeNull();
        response.TeamSeasons.Should().BeEmpty();
    }
}
