using Application.CQRS.Players.Commands;
using Domain.Enums;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Commands;

public class CreatePlayerCommandHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesPlayerInDatabase()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "John John Doe",
            KnownName = "Johnny",
            Nationality = "England",
            PreferredFoot = "Right",
            PhotoUrl = "http://example.com/photo.jpg",
            Position = nameof(PlayerPosition.AttackingMidfielder),
            TeamId = team.Id,
            ShirtNumber = 10,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify player was created in database
        var createdPlayer = await Context.Players.FindAsync(response.Id);
        createdPlayer.Should().NotBeNull();
        createdPlayer!.FullName.Should().Be(command.FullName);
        createdPlayer.KnownName.Should().Be(command.KnownName);
        createdPlayer.Nationality.Should().Be(command.Nationality);
        createdPlayer.Position.Should().Be(command.Position);
        createdPlayer.ShirtNumber.Should().Be(command.ShirtNumber);
        createdPlayer.TeamId.Should().Be(command.TeamId);
    }

    [Fact]
    public async Task Handle_WithInvalidTeamId_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreatePlayerCommand
        {
            FullName = "John",
            KnownName = "Doe",
            Nationality = "England",
            Position = nameof(PlayerPosition.AttackingMidfielder),
            TeamId = 999999, // Invalid team ID
            ShirtNumber = 10,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Team").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WithDuplicateShirtNumber_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var existingPlayer = TestData.CreateTestPlayer(team.Id);
        existingPlayer.ShirtNumber = 10;
        Context.Players.Add(existingPlayer);
        await Context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "John",
            KnownName = "Doe",
            Nationality = "England",
            Position = nameof(PlayerPosition.CentralMidfielder),
            ShirtNumber = 10, // Duplicate jersey number
            TeamId = team.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Jersey number").And.Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithMinimalData_CreatesPlayerSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "Jane",
            KnownName = "Smith",
            Nationality = "Spain",
            Position = nameof(PlayerPosition.CenterForward),
            ShirtNumber = 9,
            TeamId = team.Id,
            // Optional fields not provided
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeTrue();
        response.Id.Should().BeGreaterThan(0);

        // Verify in database
        var createdPlayer = await Context.Players.FindAsync(response.Id);
        createdPlayer.Should().NotBeNull();
        createdPlayer!.FullName.Should().Be(command.FullName);
    }

    [Fact]
    public async Task Handle_WithAllPositions_CreatesPlayersSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var positions = Enum.GetValues<PlayerPosition>();
        var jerseyNumber = 1;

        foreach (var position in positions)
        {
            var command = new CreatePlayerCommand
            {
                FullName = $"Player{jerseyNumber}",
                KnownName = $"Position{position.ToString()}",
                Nationality = "England",
                Position = nameof(position),
                ShirtNumber = jerseyNumber,
                TeamId = team.Id,
            };

            // Act
            var response = await Mediator.Send(command);

            // Assert
            response.Should().NotBeNull();
            response.Succeeded.Should().BeTrue();
            response.Id.Should().BeGreaterThan(0);

            jerseyNumber++;
        }
    }

    [Fact]
    public async Task Handle_WithInvalidShirtNumber_ReturnsFailureResponse()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        Context.Teams.Add(team);
        await Context.SaveChangesAsync();

        var command = new CreatePlayerCommand
        {
            FullName = "Johnny Doe",
            KnownName = "JD",
            Nationality = "England",
            Position = nameof(PlayerPosition.CentralMidfielder),
            ShirtNumber = 0, // Invalid jersey number
            TeamId = team.Id,
        };

        // Act
        var response = await Mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Succeeded.Should().BeFalse();
        response.Error.Should().Contain("Shirt number");
    }
}
