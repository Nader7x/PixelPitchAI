using Application.CQRS.Players.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Players.Commands;

public class UpdatePlayerCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly UpdatePlayerCommandHandler _handler;
    private readonly Mock<IPlayerMapper> _playerMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdatePlayerCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _playerMapperMock = new Mock<IPlayerMapper>();
        _handler = new UpdatePlayerCommandHandler(_unitOfWorkMock.Object, _playerMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new UpdatePlayerCommand { Id = 1, FullName = "Lionel Messi" };

        var existingPlayer = new Player
        {
            Id = command.Id,
            FullName = "Lionel Messi",
            Nationality = "Argentine",
            Position = nameof(PlayerPosition.CenterForward),
        };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlayer);

        _playerMapperMock
            .Setup(x => x.ToPlayerFromUpdate(It.IsAny<UpdatePlayerCommand>(), It.IsAny<Player>()))
            .Callback<UpdatePlayerCommand, Player>(
                (playerCommand, player) =>
                {
                    player.Id = playerCommand.Id;
                    if (playerCommand.FullName != null)
                        player.FullName = playerCommand.FullName;
                    if (playerCommand.KnownName != null)
                        player.KnownName = playerCommand.KnownName;
                    if (playerCommand.Nationality != null)
                        player.Nationality = playerCommand.Nationality;
                    if (playerCommand.ShirtNumber != null)
                        player.ShirtNumber = playerCommand.ShirtNumber.Value;
                    if (playerCommand.PreferredFoot != null)
                        player.PreferredFoot = playerCommand.PreferredFoot;
                    if (playerCommand.TeamId != null)
                        player.TeamId = playerCommand.TeamId.Value;
                    if (playerCommand.PhotoUrl != null)
                        player.PhotoUrl = playerCommand.PhotoUrl;
                    if (playerCommand.Position != null)
                        player.Position = playerCommand.Position;
                }
            );
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Id.Should().Be(command.Id);
        result.FullName.Should().Be(command.FullName);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentPlayer_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new UpdatePlayerCommand { Id = 999, FullName = "Lionel Messi" };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Player with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdatePlayerCommand { Id = 1, FullName = "Lionel Messi" };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
