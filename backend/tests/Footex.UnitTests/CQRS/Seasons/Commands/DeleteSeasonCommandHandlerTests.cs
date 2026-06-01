using System.Linq.Expressions;
using Application.CQRS.Seasons.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.CQRS.Seasons.Commands;

public class DeleteSeasonCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly DeleteSeasonCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public DeleteSeasonCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteSeasonCommandHandler(_unitOfWorkMock.Object);

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
        var command = _fixture.Create<DeleteSeasonCommand>();
        var season = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .With(s => s.IsActive, false)
            .Without(s => s.Matches)
            .Without(s => s.SeasonTeams)
            .Create();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);

        _unitOfWorkMock
            .Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(new List<Match>());
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(new List<TeamSeason>());

        _unitOfWorkMock.Setup(x => x.Seasons.Delete(season));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Seasons.Delete(season), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSeason_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture.Create<DeleteSeasonCommand>();

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.Id)).ReturnsAsync((Season?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be($"Season with ID {command.Id} not found");
    }

    [Fact]
    public async Task Handle_WithAssociatedMatches_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture.Create<DeleteSeasonCommand>();
        var season = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .With(
                s => s.Matches,
                new List<Match>
                {
                    new() { Id = 0, CreatorId = "null" },
                }
            )
            .Without(s => s.SeasonTeams)
            .Create();
        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        _unitOfWorkMock
            .Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(season.Matches!);
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(new List<TeamSeason>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Cannot delete season as it has associated matches");
    }

    [Fact]
    public async Task Handle_WithAssociatedTeamStatistics_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture.Create<DeleteSeasonCommand>();
        var season = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .Without(s => s.Matches)
            .With(s => s.SeasonTeams, new List<TeamSeason> { new() })
            .Create();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);

        _unitOfWorkMock
            .Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(new List<Match>());
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(season.SeasonTeams!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Cannot delete season as it has associated team seasons");
    }

    [Fact]
    public async Task Handle_WhenSeasonIsActive_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture.Create<DeleteSeasonCommand>();
        var season = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .With(s => s.IsActive, true)
            .Without(s => s.Matches)
            .Without(s => s.SeasonTeams)
            .Create();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        _unitOfWorkMock
            .Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(new List<Match>());
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(new List<TeamSeason>());
        _unitOfWorkMock
            .Setup(x => x.Seasons.GetAllAsync(It.IsAny<Expression<Func<Season, bool>>>()))
            .ReturnsAsync(new List<Season> { season });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Cannot delete the only active season for this league");
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<DeleteSeasonCommand>();
        var season = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .With(s => s.IsActive, false)
            .Without(s => s.Matches)
            .Without(s => s.SeasonTeams)
            .Create();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);

        _unitOfWorkMock
            .Setup(x => x.Matches.GetAllAsync(It.IsAny<Expression<Func<Match, bool>>>()))
            .ReturnsAsync(new List<Match>());
        _unitOfWorkMock
            .Setup(x => x.TeamSeasons.GetAllAsync(It.IsAny<Expression<Func<TeamSeason, bool>>>()))
            .ReturnsAsync(new List<TeamSeason>());

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetAllAsync(It.IsAny<Expression<Func<Season, bool>>>()))
            .ReturnsAsync(new List<Season> { season });

        _unitOfWorkMock.Setup(x => x.Seasons.Delete(season));

        var expectedErrorMessage = "Database error";
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(expectedErrorMessage)); // This mock setup is correct for throwing

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse(); // <-- Assert that Succeeded is FALSE
        result.Error.Should().Be(expectedErrorMessage); // <-- Assert on the error message
        result.NotFound.Should().BeFalse(); // Ensure not found is false
        // (Unless the mock setup of GetByIdAsync would return null,
        // which is not the case here).

        // Verify that Delete was called, but SaveChangesAsync was attempted
        _unitOfWorkMock.Verify(x => x.Seasons.Delete(season), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
