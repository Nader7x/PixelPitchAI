using Application.CQRS.Teams.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetAllTeamsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ReturnsAllTeams()
    {
        // Arrange
        Context.Teams.RemoveRange(Context.Teams);
        await Context.SaveChangesAsync();
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        team2.Name = "Second Team";
        team2.ShortName = "ST2";
        await Context.Teams.AddRangeAsync(team1, team2);
        await Context.SaveChangesAsync();

        // Act
        var response = await Mediator.Send(new GetAllTeamsQuery());

        // Assert
        response.Should().NotBeNull();
        response.Teams.Should().HaveCount(2);
        response.Teams?.Select(t => t.Name).Should().Contain([team1.Name, team2.Name]);
    }

    [Fact]
    public async Task Handle_WhenNoTeamsExist_ReturnsEmptyList()
    {
        // Arrange
        Context.Teams.RemoveRange(Context.Teams);
        await Context.SaveChangesAsync();

        // Act
        var response = await Mediator.Send(new GetAllTeamsQuery());

        // Assert
        response.Should().NotBeNull();
        response.Teams.Should().BeEmpty();
    }
}
