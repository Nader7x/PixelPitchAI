using Application.CQRS.Matches.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Commands;

public class UpdateMatchCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly UpdateMatchCommandHandler _handler;
    private readonly Mock<IMatchMapper> _mockMatchMapper;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdateMatchCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mockMatchMapper = new Mock<IMatchMapper>();
        _handler = new UpdateMatchCommandHandler(_unitOfWorkMock.Object, _mockMatchMapper.Object);

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
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var awaySeason = TestDataBuilder.CreateValidSeason(command.AwaySeasonId);
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(command.AwayTeamId, "Chelsea");

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeSeasonId.GetValueOrDefault(), command.AwaySeasonId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeTeamId.GetValueOrDefault(), command.AwayTeamId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam, awayTeam]);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(command.Id);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentMatch_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Match with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidSeason_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeSeasonId.GetValueOrDefault(), command.AwaySeasonId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("The Team Seasons or one of them Does not exist");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidHomeTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var awaySeason = TestDataBuilder.CreateValidSeason(command.AwaySeasonId);

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeSeasonId.GetValueOrDefault(), command.AwaySeasonId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeTeamId.GetValueOrDefault(), command.AwayTeamId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Home Team with ID {command.HomeTeamId} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var awaySeason = TestDataBuilder.CreateValidSeason(command.AwaySeasonId);
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeSeasonId.GetValueOrDefault(), command.AwaySeasonId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeTeamId.GetValueOrDefault(), command.AwayTeamId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Away Team with ID {command.AwayTeamId} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSameHomeAndAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        command.HomeTeamId = 1;
        command.AwayTeamId = 1; // Same team for both
        command.HomeSeasonId = 1;
        command.AwaySeasonId = 1; // Same season for both

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result
            .Error.Should()
            .Contain(
                "Home team and away team must be either different Teams or Same Teams With a different Seasons"
            );

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidStadium_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        command.StadiumId = 999; // Invalid stadium ID

        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var awaySeason = TestDataBuilder.CreateValidSeason(command.AwaySeasonId);
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(command.AwayTeamId, "Chelsea");

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeSeasonId.GetValueOrDefault(), command.AwaySeasonId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeTeamId.GetValueOrDefault(), command.AwayTeamId.GetValueOrDefault() }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam, awayTeam]);
        _unitOfWorkMock
            .Setup(x =>
                x.Stadiums.GetByIdAsync(command.StadiumId.GetValueOrDefault(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Stadium?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Stadium with ID {command.StadiumId} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Matches.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
