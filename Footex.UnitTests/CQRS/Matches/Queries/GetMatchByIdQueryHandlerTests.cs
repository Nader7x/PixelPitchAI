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

public class GetMatchByIdQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetMatchByIdQueryHandler _handler;
    private readonly Mock<IMatchMapper> _IMatchMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetMatchByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _IMatchMapperMock = new Mock<IMatchMapper>();
        _handler = new GetMatchByIdQueryHandler(_unitOfWorkMock.Object, _IMatchMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdQuery { Id = matchId };
        var match = TestDataBuilder.CreateValidMatch(matchId);
        var matchDto = new MatchDto { Id = matchId };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId)).ReturnsAsync(match);
        _IMatchMapperMock.Setup(x => x.ToDto(match)).Returns(matchDto);

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
        _IMatchMapperMock.Verify(x => x.ToDto(match), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var matchId = 999;
        var query = new GetMatchByIdQuery { Id = matchId };

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId))
            .ReturnsAsync((Func<Match?>)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Match.Should().BeNull();
        result.Error.Should().Contain($"Match with ID {matchId} not found");

        _unitOfWorkMock.Verify(x => x.Matches.GetByIdWithDetailsAsync(matchId), Times.Once);
        _IMatchMapperMock.Verify(x => x.ToDto(It.IsAny<Match>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdQuery { Id = matchId };
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
        _IMatchMapperMock.Verify(x => x.ToDto(It.IsAny<Match>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetMatchByIdQuery { Id = invalidId };

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdWithDetailsAsync(invalidId))
            .ReturnsAsync((Match?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Match.Should().BeNull();
        result.Error.Should().Contain($"Match with ID {invalidId} not found");
    }

    [Fact]
    public async Task Handle_WithValidMatch_MapsCorrectly()
    {
        // Arrange
        var matchId = 1;
        var query = new GetMatchByIdQuery { Id = matchId };
        var match = TestDataBuilder.CreateValidMatch(matchId);
        var expectedMatchDto = new MatchDto
        {
            Id = matchId,
            HomeTeamScore = match.HomeTeamScore,
            AwayTeamScore = match.AwayTeamScore,
            MatchStatus = match.MatchStatus,
        };

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdWithDetailsAsync(matchId)).ReturnsAsync(match);
        _IMatchMapperMock.Setup(x => x.ToDto(match)).Returns(expectedMatchDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Match.Should().NotBeNull();
        result.Match!.Id.Should().Be(expectedMatchDto.Id);
        result.Match.HomeTeamScore.Should().Be(expectedMatchDto.HomeTeamScore);
        result.Match.AwayTeamScore.Should().Be(expectedMatchDto.AwayTeamScore);
        result.Match.MatchStatus.Should().Be(expectedMatchDto.MatchStatus);
    }
}
