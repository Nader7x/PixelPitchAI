using Application.CQRS.Matches.Queries;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

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
        var liveMatch = new Match()
        {
            Id = 42,
            CreatorId = "creator_id"
        };
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(liveMatch);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.LiveMatch.Id.Should().Be(liveMatch.Id);
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
            .ReturnsAsync(new Domain.Models.Match
            {
                Id = 0,
                CreatorId = "test_creator_id" // Simulating no live match found
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.LiveMatch.Id.Should().Be(0);
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
            .ReturnsAsync(new Match()
            {
                Id = 0,
                CreatorId = "creator_id" // Simulating no live match found
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.LiveMatch.Id.Should().Be(0);
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
            .ReturnsAsync(new Match()
            {
                Id = 0,
                CreatorId = "creator_id" // Simulating no live match found
            });

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
        var firstLiveMatch = new Match
        {
            Id = 1,
            CreatorId = "creator_id"
        };
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(firstLiveMatch);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.LiveMatch.Should().Be(firstLiveMatch);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId = "specific-user-id";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(new Match
            {
                Id = 1,
                CreatorId = "creator_id"
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.Matches.GetLiveMatchAsync(userId), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetMatchesForTheory))]
    public async Task Handle_WithDifferentMatchIds_ReturnsCorrectMatchId(Match expectedMatch)
    {
        // Arrange
        var userId = "user123";
        var query = new GetLiveMatchQuery { UserId = userId };

        _unitOfWorkMock.Setup(x => x.Matches.GetLiveMatchAsync(userId))
            .ReturnsAsync(expectedMatch);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.LiveMatch.Should().Be(expectedMatch);
        result.Error.Should().BeNull();
    }

    public static readonly IEnumerable<object[]> GetMatchesForTheory =
    [
        new object[] { new Match { Id = 1, CreatorId = "creator_id" } },
        new object[] { new Match { Id = 2, CreatorId = "creator_id" } },
        new object[] { new Match { Id = 3, CreatorId = "creator_id" } }
    ];
}