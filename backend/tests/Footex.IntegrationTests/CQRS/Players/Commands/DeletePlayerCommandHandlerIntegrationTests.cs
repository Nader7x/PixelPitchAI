using Application.CQRS.Players.Commands;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Commands;

public class DeletePlayerCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidId_DeletesPlayer()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        await Context.Teams.AddAsync(team);
        await Context.SaveChangesAsync();

        var player = TestData.CreateTestDbPlayer(team.Id);
        await Context.Players.AddAsync(player);
        await Context.SaveChangesAsync();

        var command = new DeletePlayerCommand { Id = player.Id };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        (await Context.Players.FindAsync(player.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeletePlayerCommand { Id = 999999 };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
