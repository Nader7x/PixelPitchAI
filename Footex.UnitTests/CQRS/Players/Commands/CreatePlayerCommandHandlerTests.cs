using Application.CQRS.Players.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Players.Commands;

public class CreatePlayerCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly CreatePlayerCommandHandler _handler;
    private readonly Mock<IPlayerMapper> _playerMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreatePlayerCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _playerMapperMock = new Mock<IPlayerMapper>();
        _handler = new CreatePlayerCommandHandler(_unitOfWorkMock.Object, _playerMapperMock.Object);

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
        var command = new CreatePlayerCommand
        {
            FullName = "Lionel Messi",
            KnownName = "Messi",
            Nationality = "Argentine",
            Position = "Forward",
            ShirtNumber = 10,
        };

        var player = new Player
        {
            Id = 1,
            FullName = command.FullName,
            KnownName = command.KnownName,
            Nationality = command.Nationality,
            Position = command.Position,
            ShirtNumber = command.ShirtNumber,
        };

        _unitOfWorkMock
            .Setup(x =>
                x.Players.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Player, bool>>>()
                )
            )
            .ReturnsAsync((Player?)null);

        _playerMapperMock.Setup(x => x.ToPlayerFromCreate(command)).Returns(player);

        _unitOfWorkMock
            .Setup(x => x.Players.AddAsync(It.IsAny<Player>()))
            .Returns(Task.FromResult<EntityEntry<Player>>(null!));
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(player.Id);
        result.FullName.Should().Be(command.FullName);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.AddAsync(It.IsAny<Player>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicatePlayerName_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreatePlayerCommand { FullName = "Lionel Messi" };

        var existingPlayer = new Player { Id = 1, FullName = command.FullName };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByFullNameAsync(command.FullName))
            .ReturnsAsync(existingPlayer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be($"Player with name '{command.FullName}' already exists");

        _unitOfWorkMock.Verify(x => x.Players.AddAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreatePlayerCommand { FullName = "Lionel Messi" };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByFullNameAsync(command.FullName))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");

        _unitOfWorkMock.Verify(x => x.Players.AddAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
