using Application.CQRS.Matches.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Matches.Commands;

public class CreateMatchCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly CreateMatchCommandHandler _handler;
    private readonly Mock<IMatchMapper> _iMatchMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreateMatchCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iMatchMapperMock = new Mock<IMatchMapper>();
        _handler = new CreateMatchCommandHandler(_unitOfWorkMock.Object, _iMatchMapperMock.Object);

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
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        var homeSeason = TestDataBuilder.CreateValidSeason(1);
        var awaySeason = TestDataBuilder.CreateValidSeason(2);
        var homeTeam = TestDataBuilder.CreateValidTeam(1, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(2, "Chelsea");
        var match = TestDataBuilder.CreateValidMatch(1);

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeTeamId, command.AwayTeamId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam, awayTeam]);

        _iMatchMapperMock.Setup(x => x.ToMatchFromCreate(command)).Returns(match);

        _unitOfWorkMock
            .Setup(x => x.Matches.AddAsync(It.IsAny<Match>()))
            .Returns(Task.FromResult<EntityEntry<Match>>(null!));
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(match.Id);
        result.HomeTeamName.Should().Be(homeTeam.Name);
        result.AwayTeamName.Should().Be(awayTeam.Name);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidHomeSeason_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId })
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
        result.Error.Should().Contain($"Season with ID {command.HomeSeasonId} not found");

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAwaySeason_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        var homeSeason = TestDataBuilder.CreateValidSeason(1);

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Season with ID {command.AwaySeasonId} not found");

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidHomeTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        var homeSeason = TestDataBuilder.CreateValidSeason(1);
        var awaySeason = TestDataBuilder.CreateValidSeason(2);

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { command.HomeTeamId })),
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

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAwayTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        var homeSeason = TestDataBuilder.CreateValidSeason(1);
        var awaySeason = TestDataBuilder.CreateValidSeason(2);
        var homeTeam = TestDataBuilder.CreateValidTeam(1, "Arsenal");

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeTeamId, command.AwayTeamId })
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

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidStadium_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        command.StadiumId = 999; // Invalid stadium ID
        var homeSeason = TestDataBuilder.CreateValidSeason(1);
        var awaySeason = TestDataBuilder.CreateValidSeason(2);
        var homeTeam = TestDataBuilder.CreateValidTeam(1, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(2, "Chelsea");

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeTeamId, command.AwayTeamId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam, awayTeam]);
        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetByIdAsync(command.StadiumId.Value, CancellationToken.None))
            .ReturnsAsync((Stadium?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Stadium with ID {command.StadiumId} not found");

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), CancellationToken.None)
            )
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Matches.AddAsync(It.IsAny<Match>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCoaches_ReturnsSuccessResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidCreateMatchCommand();
        command.HomeCoachId = 1;
        command.AwayCoachId = 2;

        var homeSeason = TestDataBuilder.CreateValidSeason(1);
        var awaySeason = TestDataBuilder.CreateValidSeason(2);
        var homeTeam = TestDataBuilder.CreateValidTeam(1, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(2, "Chelsea");
        var homeCoach = _fixture.Create<Coach>();
        var awayCoach = _fixture.Create<Coach>();
        var match = TestDataBuilder.CreateValidMatch(1);

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeSeasonId, command.AwaySeasonId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeSeason, awaySeason]);
        _unitOfWorkMock
            .Setup(x =>
                x.Teams.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(new[] { command.HomeTeamId, command.AwayTeamId })
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeTeam, awayTeam]);
        _unitOfWorkMock
            .Setup(x =>
                x.Coaches.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeCoachId.Value, command.AwayCoachId.Value }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([homeCoach, awayCoach]);

        _iMatchMapperMock.Setup(x => x.ToMatchFromCreate(command)).Returns(match);

        _unitOfWorkMock
            .Setup(x => x.Matches.AddAsync(It.IsAny<Match>()))
            .Returns(Task.FromResult<EntityEntry<Match>>(null!));
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(match.Id);

        _unitOfWorkMock.Verify(
            x =>
                x.Coaches.GetByIdsAsync(
                    It.Is<IEnumerable<int>>(ids =>
                        ids.SequenceEqual(
                            new[] { command.HomeCoachId.Value, command.AwayCoachId.Value }
                        )
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
