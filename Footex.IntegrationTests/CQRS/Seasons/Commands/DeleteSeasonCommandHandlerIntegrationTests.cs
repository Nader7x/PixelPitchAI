using Application.CQRS.Seasons.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Seasons.Commands;

public class DeleteSeasonCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public DeleteSeasonCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidId_DeletesSeason()
    {
        // Arrange
        var season = TestData.CreateTestDbSeason();
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var command = new DeleteSeasonCommand { Id = season.Id };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        _context.Seasons.Find(season.Id).Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteSeasonCommand { Id = 999999 };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
