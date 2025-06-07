using Application.CQRS.Matches.Queries;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Matches.Queries;

public class GetLiveMatchQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetLiveMatchQueryHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetLiveMatchQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetLiveMatchQueryHandler(_unitOfWorkMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidLiveMatch_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = "user123";
        var liveMatchId = 42;
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(liveMatchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.MatchId.Should().Be(liveMatchId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoLiveMatch_ReturnsFailureResponse()
    {
        // Arrange
        var userId = "user123";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.MatchId.Should().Be(0);
        result.Error.Should().Be("No live match found for the user.");

        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullMatchResult_ReturnsFailureResponse()
    {
        // Arrange
        var userId = "user123";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(null as Func<int>);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.MatchId.Should().Be(0);
        result.Error.Should().Be("No live match found for the user.");

        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(userId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidUserId_StillCallsRepository(string invalidUserId)
    {
        // Arrange
        var query = new GetLiveMatchQuery { UserId = invalidUserId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(invalidUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("No live match found for the user.");

        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(invalidUserId), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleLiveMatches_ReturnsFirstMatch()
    {
        // Arrange
        var userId = "user123";
        var firstLiveMatchId = 1;
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(firstLiveMatchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.MatchId.Should().Be(firstLiveMatchId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = "specific-user-id";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(userId), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999)]
    public async Task Handle_WithDifferentMatchIds_ReturnsCorrectMatchId(int expectedMatchId)
    {
        // Arrange
        var userId = "user123";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(expectedMatchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.MatchId.Should().Be(expectedMatchId);
        result.Error.Should().BeNull();
    }
}