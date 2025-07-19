using Application.CQRS.Teams.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetTeamSeasonsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidTeamId_ReturnsSeasons()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var season = TestData.CreateTestDbSeason();
        Context.Seasons.Add(season);
        await Context.SaveChangesAsync();

        var teamSeason = TestData.CreateTestDbSeasonTeam(season.Id, team.Id);
        Context.TeamSeasons.Add(teamSeason);
        await Context.SaveChangesAsync();

        var query = new GetTeamSeasonsQuery { TeamId = team.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamId.Should().Be(team.Id);
        response.TeamName.Should().Be(team.Name);
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().ContainSingle();
        response.Seasons![0].SeasonId.Should().Be(season.Id);
        response.Seasons[0].SeasonName.Should().Be(season.Name);
    }

    [Fact]
    public async Task Handle_WithInvalidTeamId_ReturnsNotFound()
    {
        // Arrange
        var query = new GetTeamSeasonsQuery { TeamId = 999999 };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithNoSeasons_ReturnsEmptySeasonsList()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var query = new GetTeamSeasonsQuery { TeamId = team.Id };

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.TeamId.Should().Be(team.Id);
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().BeEmpty();
    }
}
