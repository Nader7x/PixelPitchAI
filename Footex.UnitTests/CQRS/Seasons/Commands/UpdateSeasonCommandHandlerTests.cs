using System.Linq.Expressions;
using Application.CQRS.Seasons.Commands;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Seasons.Commands;

public class UpdateSeasonCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly UpdateSeasonCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdateSeasonCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateSeasonCommandHandler(_unitOfWorkMock.Object);

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
        var command = _fixture
            .Build<UpdateSeasonCommand>()
            .With(c => c.StartDate, DateTime.UtcNow.AddDays(10))
            .With(c => c.EndDate, DateTime.UtcNow.AddDays(20))
            .With(c => c.TotalRounds, 10)
            .With(c => c.CurrentRound, 5)
            .With(c => c.IsActive, false)
            .Create();

        var existingSeason = _fixture
            .Build<Season>()
            .With(s => s.Id, command.Id)
            .With(s => s.Name, command.Name)
            .With(s => s.IsActive, false)
            .Create();

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.Id)).ReturnsAsync(existingSeason);

        _unitOfWorkMock
            .Setup(x => x.Seasons.FindAsync(It.IsAny<Expression<Func<Season, bool>>>()))
            .ReturnsAsync((Season)null);

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetAllAsync(It.IsAny<Expression<Func<Season, bool>>>()))
            .ReturnsAsync(new List<Season>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(x => x.Seasons.UpdateAsync(It.IsAny<Season>()));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(command.Id);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();
        result.NotFound.Should().BeFalse();

        _unitOfWorkMock.Verify(x => x.Seasons.UpdateAsync(existingSeason), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentSeason_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = _fixture.Create<UpdateSeasonCommand>();

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.Id)).ReturnsAsync((Season)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Be($"Season with ID {command.Id} not found");
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<UpdateSeasonCommand>();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetByIdAsync(command.Id))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenSaveChangesFails_ReturnsFailureResponse()
    {
        // Arrange
        var command = _fixture.Create<UpdateSeasonCommand>();
        command.EndDate = DateTime.MaxValue;
        command.StartDate = DateTime.MinValue;
        command.CurrentRound = 1;
        var existingSeason = _fixture.Create<Season>();
        existingSeason.TotalRounds = 2;

        _unitOfWorkMock.Setup(x => x.Seasons.GetByIdAsync(command.Id)).ReturnsAsync(existingSeason);

        var expectedErrorMessage = "Database error";
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(expectedErrorMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(expectedErrorMessage);
    }
}
