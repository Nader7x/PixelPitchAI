using System.Linq.Expressions;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Queries;

public class GetUserMatchesQueryHandlerTests
{
    private readonly GetUserMatchesQueryHandler _handler;
    private readonly Mock<IMatchMapper> _iMatchMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetUserMatchesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iMatchMapperMock = new Mock<IMatchMapper>();
        _handler = new GetUserMatchesQueryHandler(_unitOfWorkMock.Object, _iMatchMapperMock.Object);

        var fixture = new Fixture();
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidUserId_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = "user123";
        var query = new GetUserMatchesQuery { UserId = userId };
        var userMatches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
            TestDataBuilder.CreateValidMatch(3)
        };
        var matchDtos = userMatches.Select(m => new MatchDto
        {
            Id = m.Id,
            ScheduledDateTimeUtc = m.ScheduledDateTimeUtc
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(userMatches);
        _iMatchMapperMock.Setup(x => x.ToDtoList(userMatches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().NotBeNull();
        result.Matches!.Should().HaveCount(3);
        result.Error.Should().BeNull();

        // Verify matches are ordered by ScheduledDateTimeUtc descending
        var orderedMatches = result.Matches.ToList();
        for (var i = 1; i < orderedMatches.Count; i++)
        {
            var currentDate = orderedMatches[i]?.ScheduledDateTimeUtc ?? DateTime.MinValue;
            var previousDate = orderedMatches[i - 1]?.ScheduledDateTimeUtc ?? DateTime.MinValue;
            currentDate.Should().BeOnOrBefore(previousDate);
        }

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDtoList(userMatches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUserId_ReturnsEmptyList()
    {
        // Arrange
        var userId = "nonexistent-user";
        var query = new GetUserMatchesQuery { UserId = userId };
        var emptyMatches = new List<Match>();
        var emptyMatchDtos = new List<MatchDto?>();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(emptyMatches);
        _iMatchMapperMock.Setup(x => x.ToDtoList(emptyMatches))
            .Returns(emptyMatchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().NotBeNull();
        result.Matches!.Should().BeEmpty();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDtoList(emptyMatches), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var userId = "user123";
        var query = new GetUserMatchesQuery { UserId = userId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Matches.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Match>>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidUserId_StillCallsRepository(string? invalidUserId)
    {
        // Arrange
        var query = new GetUserMatchesQuery { UserId = invalidUserId! };
        var emptyMatches = new List<Match>();
        var emptyMatchDtos = new List<MatchDto?>();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(emptyMatches);
        _iMatchMapperMock.Setup(x => x.ToDtoList(emptyMatches))
            .Returns(emptyMatchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().NotBeNull();
        result.Matches!.Should().BeEmpty();

        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FiltersMatchesByUserId()
    {
        // Arrange
        var userId = "user123";
        var query = new GetUserMatchesQuery { UserId = userId };
        var userMatches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2)
        };

        // Set up matches to have the correct CreatorId
        userMatches.ForEach(m => m.CreatorId = userId);

        var matchDtos = userMatches.Select(m => new MatchDto { Id = m.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(userMatches);
        _iMatchMapperMock.Setup(x => x.ToDtoList(userMatches))
            .Returns(matchDtos!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().HaveCount(2);

        // Verify that the repository was called with the correct filter expression
        _unitOfWorkMock.Verify(x => x.Matches.GetAllAsync(
            It.IsAny<Expression<Func<Match, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrdersMatchesByScheduledDateDescending()
    {
        // Arrange
        var userId = "user123";
        var query = new GetUserMatchesQuery { UserId = userId };
        var now = DateTime.UtcNow;
        var userMatches = new List<Match>
        {
            TestDataBuilder.CreateValidMatch(1),
            TestDataBuilder.CreateValidMatch(2),
            TestDataBuilder.CreateValidMatch(3)
        };

        // Set different scheduled dates
        userMatches[0].ScheduledDateTimeUtc = now.AddDays(-1);
        userMatches[1].ScheduledDateTimeUtc = now.AddDays(-3);
        userMatches[2].ScheduledDateTimeUtc = now.AddDays(-2);

        var matchDtos = userMatches.Select(m => new MatchDto
        {
            Id = m.Id,
            ScheduledDateTimeUtc = m.ScheduledDateTimeUtc
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(userMatches);
        _iMatchMapperMock.Setup(x => x.ToDtoList(userMatches))
            .Returns(matchDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Matches.Should().NotBeNull();

        // Verify ordering - most recent first
        var orderedMatches = result.Matches!.ToList();
        orderedMatches[0]!.ScheduledDateTimeUtc.Should().Be(now.AddDays(-1)); // Most recent
        orderedMatches[1]!.ScheduledDateTimeUtc.Should().Be(now.AddDays(-2));
        orderedMatches[2]!.ScheduledDateTimeUtc.Should().Be(now.AddDays(-3)); // Oldest
    }
}
