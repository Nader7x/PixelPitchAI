using Application.CQRS.Seasons.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

public class GetAllSeasonsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ReturnsAllSeasons()
    {
        // Arrange
        var season1 = TestData.CreateTestDbSeason();
        var season2 = TestData.CreateTestDbSeason();
        Context.Seasons.AddRange(season1, season2);
        await Context.SaveChangesAsync();

        var query = new GetAllSeasonsQuery();

        // Act
        var response = await Mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().Contain(s => s.Id == season1.Id);
        response.Seasons.Should().Contain(s => s.Id == season2.Id);
    }
}
