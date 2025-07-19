using System.Linq.Expressions;
using Application.CQRS.Coaches.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Coaches.Commands;

public class DeleteCoachCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly DeleteCoachCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public DeleteCoachCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteCoachCommandHandler(_unitOfWorkMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture.Customizations.Add(new IFormFileSpecimenBuilder());
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
        var command = new DeleteCoachCommand { Id = 1 };

        var existingCoach = new Coach
        {
            Id = command.Id,
            FirstName = "Jose",
            LastName = "Mourinho",
            DateOfBirth = new DateTime(1963, 1, 26),
            Nationality = "Portuguese",
            Role = "Head Coach",
            TeamId = null, // Ensure the coach is not assigned to any team
        };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCoach);
        _unitOfWorkMock
            .Setup(x =>
                x.Matches.FindAsync(
                    It.IsAny<Expression<Func<Match, bool>>>(), // Use Expression for predicate
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Match?)null);
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

        _unitOfWorkMock.Verify(x => x.Coaches.Delete(existingCoach), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCoach_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new DeleteCoachCommand { Id = 999 };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coach?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Coach with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.Coaches.Delete(It.IsAny<Coach>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteCoachCommand { Id = 1 };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");

        _unitOfWorkMock.Verify(x => x.Coaches.Delete(It.IsAny<Coach>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
