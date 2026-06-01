using System.Linq.Expressions;
using Application.CQRS.Teams.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Teams.Commands;

public class DeleteTeamCommandHandlerTests
{
    private readonly DeleteTeamCommandHandler _handler;
    private readonly Mock<ILogger<DeleteTeamCommandHandler>> _loggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public DeleteTeamCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteTeamCommandHandler>>();
        _handler = new DeleteTeamCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);

        var fixture = new NoRecursionFixture();
        fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessResponse()
    {
        var command = new DeleteTeamCommand { Id = 1 };
        var existingTeam = new Team { Id = command.Id, Name = "Real Madrid" };

        var mockDbContextTransaction = new Mock<IDbContextTransaction>();

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.Id)).ReturnsAsync(existingTeam);

        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mockDbContextTransaction.Object);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ReturnsAsync(new List<Coach>());

        _unitOfWorkMock.Setup(x => x.Teams.Delete(existingTeam));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Never);

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(x => x.Teams.Delete(existingTeam), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_WithNonExistentTeam_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new DeleteTeamCommand { Id = 999 };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.Id)).ReturnsAsync((Team?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Team with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.Teams.Delete(It.IsAny<Team>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
