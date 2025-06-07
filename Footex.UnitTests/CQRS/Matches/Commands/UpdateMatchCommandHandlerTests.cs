using Application.CQRS.Matches.Commands;
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
    private readonly Fixture _fixture;
    private readonly UpdateMatchCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdateMatchCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateMatchCommandHandler(_unitOfWorkMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
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
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(command.AwayTeamId, "Chelsea");

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.HomeTeamId))
            .ReturnsAsync(homeTeam);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.AwayTeamId))
            .ReturnsAsync(awayTeam);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(command.Id);
        result.HomeTeamName.Should().Be(homeTeam.Name);
        result.AwayTeamName.Should().Be(awayTeam.Name);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Matches.UpdateAsync(existingMatch), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentMatch_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
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

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync((Season?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain($"Season with ID {command.HomeSeasonId} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidHomeTeam_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.HomeTeamId))
            .ReturnsAsync((Team?)null);

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
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.HomeTeamId))
            .ReturnsAsync(homeTeam);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.AwayTeamId))
            .ReturnsAsync((Team?)null);

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

        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var team = TestDataBuilder.CreateValidTeam(1, "Arsenal");

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(1))
            .ReturnsAsync(team);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Home team and away team must be different");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidPossessionSum_ReturnsFailureResponse()
    {
        // Arrange
        var command = TestDataBuilder.CreateValidUpdateMatchCommand();
        command.HomeTeamPossession = 60;
        command.AwayTeamPossession = 50; // Sum = 110, not 100

        var existingMatch = TestDataBuilder.CreateValidMatch(command.Id);
        var homeSeason = TestDataBuilder.CreateValidSeason(command.HomeSeasonId);
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(command.AwayTeamId, "Chelsea");

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.HomeTeamId))
            .ReturnsAsync(homeTeam);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.AwayTeamId))
            .ReturnsAsync(awayTeam);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Home team and away team possession must sum to 100%");

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
        var homeTeam = TestDataBuilder.CreateValidTeam(command.HomeTeamId, "Arsenal");
        var awayTeam = TestDataBuilder.CreateValidTeam(command.AwayTeamId, "Chelsea");

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(command.Id))
            .ReturnsAsync(existingMatch);
        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.HomeSeasonId))
            .ReturnsAsync(homeSeason);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.HomeTeamId))
            .ReturnsAsync(homeTeam);
        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsync(command.AwayTeamId))
            .ReturnsAsync(awayTeam);
        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(command.StadiumId.Value))
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

        _unitOfWorkMock.Setup(x => x.Matches.GetByIdAsync(It.IsAny<int>()))
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