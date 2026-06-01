using Application.CQRS.Teams.Queries;
using Domain.Models;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Teams.Queries;

public class GetAllTeamsQueryHandlerIntegrationTests
    : BaseIntegrationTest
{
    public GetAllTeamsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory) : base(factory)
    {
        FreeDbAsync(Context.Matches,Context.Coaches,Context.Players,Context.Teams).Wait();
    }
    [Fact]
    public async Task Handle_ReturnsAllTeams()
    {
        // Arrange
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
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

        // Act
        var response = await Mediator.Send(new GetAllTeamsQuery());

        // Assert
        response.Should().NotBeNull();
        response.Teams.Should().BeEmpty();
    }
}
