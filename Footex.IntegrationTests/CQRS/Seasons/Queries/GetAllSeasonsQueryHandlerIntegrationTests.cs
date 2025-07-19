using Application.CQRS.Seasons.Queries;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Queries;

[Collection("Database")]
public class GetAllSeasonsQueryHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public GetAllSeasonsQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_ReturnsAllSeasons()
    {
        // Arrange
        var season1 = TestData.CreateTestDbSeason();
        var season2 = TestData.CreateTestDbSeason();
        _context.Seasons.AddRange(season1, season2);
        await _context.SaveChangesAsync();

        var query = new GetAllSeasonsQuery();

        // Act
        var response = await _mediator.Send(query);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Seasons.Should().NotBeNull();
        response.Seasons.Should().Contain(s => s.Id == season1.Id);
        response.Seasons.Should().Contain(s => s.Id == season2.Id);
    }
}
