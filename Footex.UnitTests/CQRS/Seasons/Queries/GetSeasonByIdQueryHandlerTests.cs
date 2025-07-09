using System.Linq.Expressions;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Seasons.Queries;

public class GetSeasonByIdQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetSeasonByIdQueryHandler _handler;
    private readonly Mock<ISeasonMapper> _iSeasonMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetSeasonByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iSeasonMapperMock = new Mock<ISeasonMapper>();
        _handler = new GetSeasonByIdQueryHandler(_unitOfWorkMock.Object, _iSeasonMapperMock.Object);

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
        var seasonId = 1;
        var query = new GetSeasonByIdQuery { Id = seasonId };
        var season = CreateValidSeason(seasonId);
        var teamStats = new List<TeamSeason>();
        var expectedSeasonDto = new SeasonDto
        {
            Id = seasonId,
            Name = season.Name,
            LeagueName = season.LeagueName,
            Country = season.Country,
            IsActive = season.IsActive,
        };

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(seasonId)).ReturnsAsync(season);
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(teamStats);
        _iSeasonMapperMock.Setup(x => x.ToDto(season)).Returns(expectedSeasonDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Season.Should().NotBeNull();
        result.Season!.Id.Should().Be(seasonId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Seasons.GetByIdAsync(seasonId), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()),
            Times.Once
        );
        _iSeasonMapperMock.Verify(x => x.ToDto(season), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var seasonId = 999;
        var query = new GetSeasonByIdQuery { Id = seasonId };

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(seasonId)).ReturnsAsync((Season?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Season.Should().BeNull();
        result.Error.Should().Be($"Season with ID {seasonId} not found");

        _unitOfWorkMock.Verify(x => x.Seasons.GetByIdAsync(seasonId), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()),
            Times.Never
        );
        _iSeasonMapperMock.Verify(x => x.ToDto(It.IsAny<Season>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var seasonId = 1;
        var query = new GetSeasonByIdQuery { Id = seasonId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(seasonId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Season.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Seasons.GetByIdAsync(seasonId), Times.Once);
        _iSeasonMapperMock.Verify(x => x.ToDto(It.IsAny<Season>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetSeasonByIdQuery { Id = invalidId };

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(invalidId)).ReturnsAsync((Season?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Season.Should().BeNull();
        result.Error.Should().Be($"Season with ID {invalidId} not found");

        _unitOfWorkMock.Verify(x => x.Seasons.GetByIdAsync(invalidId), Times.Once);
        _iSeasonMapperMock.Verify(x => x.ToDto(It.IsAny<Season>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidSeason_MapsCorrectly()
    {
        // Arrange
        var seasonId = 1;
        var query = new GetSeasonByIdQuery { Id = seasonId };
        var season = CreateValidSeason(seasonId);
        var teamStats = new List<TeamSeason>();
        var expectedSeasonDto = new SeasonDto
        {
            Id = seasonId,
            Name = season.Name,
            LeagueName = season.LeagueName,
            Country = season.Country,
            CurrentRound = season.CurrentRound.Value,
            TotalRounds = season.TotalRounds.Value,
            IsActive = season.IsActive,
            IsCompleted = season.IsCompleted.Value,
            StartDate = season.StartDate,
            EndDate = season.EndDate,
        };

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(seasonId)).ReturnsAsync(season);
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(teamStats);
        _iSeasonMapperMock.Setup(x => x.ToDto(season)).Returns(expectedSeasonDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Season.Should().NotBeNull();
        result.Season!.Id.Should().Be(expectedSeasonDto.Id);
        result.Season.Name.Should().Be(expectedSeasonDto.Name);
        result.Season.LeagueName.Should().Be(expectedSeasonDto.LeagueName);
        result.Season.Country.Should().Be(expectedSeasonDto.Country);
        result.Season.CurrentRound.Should().Be(expectedSeasonDto.CurrentRound);
        result.Season.TotalRounds.Should().Be(expectedSeasonDto.TotalRounds);
        result.Season.IsActive.Should().Be(expectedSeasonDto.IsActive);
        result.Season.IsCompleted.Should().Be(expectedSeasonDto.IsCompleted);
        result.Season.StartDate.Should().Be(expectedSeasonDto.StartDate);
        result.Season.EndDate.Should().Be(expectedSeasonDto.EndDate);
    }

    [Fact]
    public async Task Handle_WithValidSeason_FetchesTeamStatsCorrectly()
    {
        // Arrange
        var seasonId = 1;
        var query = new GetSeasonByIdQuery { Id = seasonId };
        var season = CreateValidSeason(seasonId);
        var teamStats = new List<TeamSeason>
        {
            new() { SeasonId = seasonId, TeamId = 1 },
            new() { SeasonId = seasonId, TeamId = 2 },
        };
        var expectedSeasonDto = new SeasonDto { Id = seasonId, Name = season.Name };

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(seasonId)).ReturnsAsync(season);
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(teamStats);
        _iSeasonMapperMock.Setup(x => x.ToDto(season)).Returns(expectedSeasonDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x =>
                x.TeamSeasons.GetAllAsync(
                    It.Is<Expression<Func<TeamSeason, bool>>>(expr => expr != null)
                ),
            Times.Once
        );
    }

    private Season CreateValidSeason(int id)
    {
        var season = _fixture.Create<Season>();
        season.Id = id;
        season.Name = "2023/24";
        season.LeagueName = "Premier League";
        season.Country = "England";
        season.CurrentRound = 20;
        season.TotalRounds = 38;
        season.IsActive = true;
        season.IsCompleted = false;
        season.StartDate = new DateTime(2023, 8, 12);
        season.EndDate = new DateTime(2024, 5, 19);
        return season;
    }
}
