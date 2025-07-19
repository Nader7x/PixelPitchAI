using Application.CQRS.Players.Commands;
using Domain.Enums;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Commands;

[Collection("Database")]
public class UpdatePlayerCommandHandlerIntegrationTests : IClassFixture<FootexWebApplicationFactory>
{
    private readonly FootballDbContext _context;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;

    public UpdatePlayerCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _context = _scope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesPlayerInDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var player = TestData.CreateTestDbPlayer(team.Id);
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var command = new UpdatePlayerCommand
        {
            Id = player.Id,
            FullName = "Jane Smith",
            KnownName = "Janie",
            Nationality = "USA",
            PreferredFoot = "Left",
            PhotoUrl = "http://example.com/photo2.jpg",
            Position = nameof(PlayerPosition.CenterBack),
            TeamId = team.Id,
            ShirtNumber = 5,
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();

        // Verify player was updated in database
        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        updatedPlayer.Should().NotBeNull();
        updatedPlayer!.FullName.Should().Be(command.FullName);
        updatedPlayer.KnownName.Should().Be(command.KnownName);
        updatedPlayer.Nationality.Should().Be(command.Nationality);
        updatedPlayer.PreferredFoot.Should().Be(command.PreferredFoot);
        updatedPlayer.Position.Should().Be(command.Position);
        updatedPlayer.ShirtNumber.Should().Be(command.ShirtNumber);
    }

    [Fact]
    public async Task Handle_WithInvalidPlayerId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 999999,
            FullName = "Jane Smith",
            KnownName = "Janie",
            Nationality = "USA",
            PreferredFoot = "Left",
            PhotoUrl = "http://example.com/photo2.jpg",
            Position = nameof(PlayerPosition.CenterBack),
            TeamId = 1,
            ShirtNumber = 5,
        };

        // Act
        var response = await _mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
    }
}
