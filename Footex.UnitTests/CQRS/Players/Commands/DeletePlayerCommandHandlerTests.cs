using Application.CQRS.Players.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Players.Commands;

public class DeletePlayerCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly DeletePlayerCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public DeletePlayerCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeletePlayerCommandHandler(_unitOfWorkMock.Object);

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
        var command = new DeletePlayerCommand { Id = 1 };

        var existingPlayer = new Player { Id = command.Id, FullName = "Lionel Messi" };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(command.Id)).ReturnsAsync(existingPlayer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Players.DeleteAsync(existingPlayer), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentPlayer_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new DeletePlayerCommand { Id = 999 };

        _unitOfWorkMock.Setup(x => x.Players.GetByIdAsync(command.Id)).ReturnsAsync((Player?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Player with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.Players.DeleteAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeletePlayerCommand { Id = 1 };

        _unitOfWorkMock
            .Setup(x => x.Players.GetByIdAsync(command.Id))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();

        _unitOfWorkMock.Verify(x => x.Players.DeleteAsync(It.IsAny<Player>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
