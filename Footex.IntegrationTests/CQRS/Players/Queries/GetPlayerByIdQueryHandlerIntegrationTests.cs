using Application.CQRS.Players.Queries;
using Domain.Interfaces;
using Footex.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.CQRS.Players.Queries;

public class GetPlayerByIdQueryHandlerIntegrationTests : BaseIntegrationTest
{
    private readonly GetPlayerByIdQueryHandler _handler;
    private readonly IUnitOfWork _unitOfWork;

    public GetPlayerByIdQueryHandlerIntegrationTests(FootexWebApplicationFactory factory)
        : base(factory)
    {
        _handler = ServiceProvider.GetRequiredService<GetPlayerByIdQueryHandler>();
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    [Fact]
    public async Task Handle_ValidPlayerId_ReturnsPlayerSuccessfully()
    {
        // Arrange
        var team = TestData.CreateTeam("Test Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var player = TestData.CreatePlayer("John Doe", team.Id);
        await _unitOfWork.Players.AddAsync(player);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Player);
        Assert.Equal(player.Id, result.Player.Id);
        Assert.Equal(player.FullName, result.Player.FullName);
        Assert.Equal(player.ShirtNumber, result.Player.ShirtNumber);
        Assert.Equal(player.Position, result.Player.Position);
        Assert.Equal(team.Id, result.Player.TeamId);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Handle_NonExistentPlayerId_ReturnsNotFoundResponse()
    {
        // Arrange
        const int nonExistentId = -1;
        var query = new GetPlayerByIdQuery { Id = nonExistentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Player);
        Assert.Contains($"Player with ID {nonExistentId} not found", result.Error);
    }

    [Fact]
    public async Task Handle_PlayerWithDetailedInformation_ReturnsCompletePlayerDto()
    {
        // Arrange
        var team = TestData.CreateTeam("Barcelona");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var player = TestData.CreatePlayer("Lionel Messi", team.Id);
        player.Nationality = "Argentina";
        player.PreferredFoot = "Left";
        player.PhotoUrl = "https://example.com/messi.jpg";

        await _unitOfWork.Players.AddAsync(player);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Player);
        Assert.Equal(player.Nationality, result.Player.Nationality);
        Assert.Equal(player.PreferredFoot, result.Player.PreferredFoot);
        Assert.Equal(player.PhotoUrl, result.Player.PhotoUrl);
    }

    [Fact]
    public async Task Handle_DeletedPlayer_ReturnsNotFoundResponse()
    {
        // Arrange
        var team = TestData.CreateTeam("Test Team");
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var player = TestData.CreatePlayer("Deleted Player", team.Id);
        await _unitOfWork.Players.AddAsync(player);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.NotFound);
        Assert.Null(result.Player);
        Assert.Contains($"Player with ID {player.Id} not found", result.Error);
    }

    [Fact]
    public async Task Handle_PlayerWithTeamInformation_ReturnsPlayerWithTeamDetails()
    {
        // Arrange
        var team = TestData.CreateTeam("Manchester United");
        team.Logo = "https://example.com/man-utd-logo.png";
        await _unitOfWork.Teams.AddAsync(team);
        await _unitOfWork.SaveChangesAsync();

        var player = TestData.CreatePlayer("Marcus Rashford", team.Id);
        await _unitOfWork.Players.AddAsync(player);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Player);
        Assert.Equal(team.Id, result.Player.TeamId);
    }

    [Fact]
    public async Task Handle_MultiplePlayersInDatabase_ReturnsCorrectPlayer()
    {
        // Arrange
        var team1 = TestData.CreateTeam("Team 1");
        var team2 = TestData.CreateTeam("Team 2");
        await _unitOfWork.Teams.AddAsync(team1);
        await _unitOfWork.Teams.AddAsync(team2);
        await _unitOfWork.SaveChangesAsync();

        var player1 = TestData.CreatePlayer("Player 1", team1.Id);
        var player2 = TestData.CreatePlayer("Player 2", team2.Id);
        var player3 = TestData.CreatePlayer("Player 3", team1.Id);

        await _unitOfWork.Players.AddAsync(player1);
        await _unitOfWork.Players.AddAsync(player2);
        await _unitOfWork.Players.AddAsync(player3);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetPlayerByIdQuery { Id = player2.Id };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Player);
        Assert.Equal(player2.Id, result.Player.Id);
        Assert.Equal(player2.FullName, result.Player.FullName);
        Assert.Equal(team2.Id, result.Player.TeamId);
        Assert.NotEqual(player1.Id, result.Player.Id);
        Assert.NotEqual(player3.Id, result.Player.Id);
    }

    [Fact]
    public async Task Handle_DatabaseException_ReturnsErrorResponse()
    {
        // Arrange
        // Dispose the current context to simulate database error
        await DisposeContext();

        var query = new GetPlayerByIdQuery { Id = -1 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.Player);
    }

    private Task DisposeContext()
    {
        _unitOfWork.Dispose();
        return Task.CompletedTask;
    }
}
