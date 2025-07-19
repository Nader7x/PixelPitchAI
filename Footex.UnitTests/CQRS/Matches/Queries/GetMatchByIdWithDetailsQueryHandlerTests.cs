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

public class GetMatchByIdWithDetailsQueryHandlerTests
{
    private readonly GetMatchByIdWithDetailsQueryHandler _handler;
    private readonly Mock<IMatchMapper> _iMatchMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetMatchByIdWithDetailsQueryHandlerTests()
    {
        var liveMatchStatisticsServiceMock = new Mock<ILiveMatchStatisticsService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iMatchMapperMock = new Mock<IMatchMapper>();
        _handler = new GetMatchByIdWithDetailsQueryHandler(
            _iMatchMapperMock.Object,
            _unitOfWorkMock.Object,
            liveMatchStatisticsServiceMock.Object
        );

        var fixture = new Fixture();
        fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidMatchId_ReturnsSuccessResponse()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
        var match = TestDataBuilder.CreateValidMatch(matchId);
        var matchDetailsDto = new MatchDetailsDto { Id = matchId };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId)).ReturnsAsync(match);
        _iMatchMapperMock.Setup(x => x.ToDetailsFromMatch(match)).Returns(matchDetailsDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Match.Should().NotBeNull();
        result.Match!.Id.Should().Be(matchId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.GetByIdWithDetailsAsync(matchId), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDetailsFromMatch(match), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentMatchId_ReturnsNotFoundResponse()
    {
        // Arrange
        var matchId = 999;
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId))
            .ReturnsAsync((Match?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Match.Should().BeNull();
        result.Error.Should().Contain($"Match with ID {matchId} not found");

        _unitOfWorkMock.Verify(x => x.Matches.GetByIdWithDetailsAsync(matchId), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDetailsFromMatch(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Match.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Matches.GetByIdWithDetailsAsync(matchId), Times.Once);
        _iMatchMapperMock.Verify(x => x.ToDetailsFromMatch(It.IsAny<Match>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidMatchId_ReturnsNotFoundResponse(int invalidMatchId)
    {
        // Arrange
        var query = new GetMatchByIdWithDetailsQuery { MatchId = invalidMatchId };

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdWithDetailsAsync(invalidMatchId))
            .ReturnsAsync((Match?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Match.Should().BeNull();
        result.Error.Should().Contain($"Match with ID {invalidMatchId} not found");
    }

    [Fact]
    public async Task Handle_WithValidMatchDetails_MapsCorrectly()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
        var match = TestDataBuilder.CreateValidMatch(matchId);
        var expectedDetailsDto = new MatchDetailsDto
        {
            Id = matchId,
            HomeTeamScore = match.HomeTeamScore,
            AwayTeamScore = match.AwayTeamScore,
            MatchStatus = match.MatchStatus,
            ScheduledDateTimeUtc = match.ScheduledDateTimeUtc,
        };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId)).ReturnsAsync(match);
        _iMatchMapperMock.Setup(x => x.ToDetailsFromMatch(match)).Returns(expectedDetailsDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Match.Should().NotBeNull();
        result.Match!.Id.Should().Be(expectedDetailsDto.Id);
        result.Match.HomeTeamScore.Should().Be(expectedDetailsDto.HomeTeamScore);
        result.Match.AwayTeamScore.Should().Be(expectedDetailsDto.AwayTeamScore);
        result.Match.MatchStatus.Should().Be(expectedDetailsDto.MatchStatus);
        result.Match.ScheduledDateTimeUtc.Should().Be(expectedDetailsDto.ScheduledDateTimeUtc);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectMatchId()
    {
        // Arrange
        var matchId = 42;
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
        var match = TestDataBuilder.CreateValidMatch(matchId);
        var matchDetailsDto = new MatchDetailsDto { Id = matchId };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId)).ReturnsAsync(match);
        _iMatchMapperMock.Setup(x => x.ToDetailsFromMatch(match)).Returns(matchDetailsDto);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.Matches.GetByIdWithDetailsAsync(matchId), Times.Once);
    }
}
