using Application.CQRS.Coaches.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Coaches.Queries;

public class GetAllCoachesQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WhenCoachesExist_ReturnsAllCoaches()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();
        var coach1 = TestData.CreateTestDbCoach(team.Id, "Coach", "One");
        var coach2 = TestData.CreateTestDbCoach(team.Id, "Coach", "Two");
        await UnitOfWork.Coaches.AddRangeAsync(CancellationToken.None, coach1, coach2);
        await UnitOfWork.SaveChangesAsync();
        var query = new GetAllCoachesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Should().NotBeNull();
        result.Coaches.Should().NotBeNull();
        result.Coaches.Count.Should().BeGreaterOrEqualTo(2);
        result.Coaches.Should().Contain(c => c.FirstName == "Coach" && c.LastName == "One");
        result.Coaches.Should().Contain(c => c.FirstName == "Coach" && c.LastName == "Two");
    }

    [Fact]
    public async Task Handle_WhenNoCoachesExist_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllCoachesQuery();

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches.Should().BeEmpty();
    }
}
