using Application.CQRS.Matches.Commands;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Commands;

public class DeleteMatchCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly DeleteMatchCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public DeleteMatchCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteMatchCommandHandler(_unitOfWorkMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidScheduledMatch_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 1 };
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        existingMatch.MatchStatus = "Scheduled";

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id)).ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(existingMatch), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentMatch_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 999 };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id)).ReturnsAsync((Match?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Match with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedMatch_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 1 };
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        existingMatch.MatchStatus = "Completed";

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id)).ReturnsAsync(existingMatch);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Cannot delete a match that is Completed");

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInProgressMatch_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 1 };
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        existingMatch.MatchStatus = "InProgress";

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id)).ReturnsAsync(existingMatch);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Cannot delete a match that is InProgress");

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Postponed")]
    [InlineData("Cancelled")]
    public async Task Handle_WithDeletableMatchStatuses_ReturnsSuccessResponse(string matchStatus)
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 1 };
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        existingMatch.MatchStatus = matchStatus;

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id)).ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(existingMatch), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = new DeleteMatchCommand { Id = 1 };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Matches.DeleteAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
