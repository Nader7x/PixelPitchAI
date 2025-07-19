using Application.CQRS.Players.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Commands;

[Collection("Database")]
public class DeletePlayerCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public DeletePlayerCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidId_DeletesPlayer()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var player = TestData.CreatePlayer("John Doe", team.Id);
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var command = new DeletePlayerCommand { Id = player.Id };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        _context.Players.Find(player.Id).Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeletePlayerCommand { Id = 999999 };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
