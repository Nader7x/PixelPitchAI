using Application.CQRS.Players.Queries;
using Domain.Interfaces;
using FluentAssertions;
using Footex.IntegrationTests.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Queries;

public class GetPlayerByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ValidPlayerId_ReturnsPlayerSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var player = TestData.CreateTestDbPlayer(team.Id);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player!.Id.Should().Be(player.Id);
        result.Player.FullName.Should().Be(player.FullName);
        result.Player.ShirtNumber.Should().Be(player.ShirtNumber);
        result.Player.Position.Should().Be(player.Position);
        result.Player.TeamId.Should().Be(team.Id);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NonExistentPlayerId_ReturnsNotFoundResponse()
    {
        // Arrange
        const int nonExistentId = -1;
        var query = new GetPlayerByIdQuery { Id = nonExistentId };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Player.Should().BeNull();
        result.Error.Should().Contain($"Player with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_PlayerWithDetailedInformation_ReturnsCompletePlayerDto()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam("Barcelona");
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var player = TestData.CreateTestDbPlayer(team.Id);
        player.Nationality = "Argentina";
        player.PreferredFoot = "Left";
        player.PhotoUrl = "https://example.com/messi.jpg";

        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player!.Nationality.Should().Be(player.Nationality);
        result.Player.PreferredFoot.Should().Be(player.PreferredFoot);
        result.Player.PhotoUrl.Should().Be(player.PhotoUrl);
    }

    [Fact]
    public async Task Handle_DeletedPlayer_ReturnsNotFoundResponse()
    {
        // Arrange
        var player = TestData.CreateTestDbPlayer();
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();
        // Simulate deletion by removing the player
        UnitOfWork.Players.Delete(player);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Player.Should().BeNull();
        result.Error.Should().Contain($"Player with ID {player.Id} not found");
    }

    [Fact]
    public async Task Handle_PlayerWithTeamInformation_ReturnsPlayerWithTeamDetails()
    {
        // Arrange
        var team = TestData.CreateTestDbTeam();
        team.Logo = "https://example.com/man-utd-logo.png";
        await UnitOfWork.Teams.AddAsync(team);
        await UnitOfWork.SaveChangesAsync();

        var player = TestData.CreateTestDbPlayer(team.Id);
        await UnitOfWork.Players.AddAsync(player);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player!.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task Handle_MultiplePlayersInDatabase_ReturnsCorrectPlayer()
    {
        // Arrange
        var team1 = TestData.CreateTestDbTeam();
        var team2 = TestData.CreateTestDbTeam();
        await UnitOfWork.Teams.AddAsync(team1);
        await UnitOfWork.Teams.AddAsync(team2);
        await UnitOfWork.SaveChangesAsync();

        var player1 = TestData.CreateTestDbPlayer(team1.Id);
        var player2 = TestData.CreateTestDbPlayer(team2.Id);
        var player3 = TestData.CreateTestDbPlayer(team1.Id);

        await UnitOfWork.Players.AddAsync(player1);
        await UnitOfWork.Players.AddAsync(player2);
        await UnitOfWork.Players.AddAsync(player3);
        await UnitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player2.Id };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Player.Should().NotBeNull();
        result.Player!.Id.Should().Be(player2.Id);
        result.Player.FullName.Should().Be(player2.FullName);
        result.Player.TeamId.Should().Be(team2.Id);
        result.Player.Id.Should().NotBe(player1.Id);
        result.Player.Id.Should().NotBe(player3.Id);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        // Dispose the current context to simulate database error
        await DisposeContext();

        var query = new GetPlayerByIdQuery { Id = -1 };

        // Act
        var result = await Mediator.Send(query, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Player.Should().BeNull();
    }

    private Task DisposeContext()
    {
        UnitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
