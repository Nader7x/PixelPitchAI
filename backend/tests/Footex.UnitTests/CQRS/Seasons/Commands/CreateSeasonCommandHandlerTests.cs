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

public class CreateSeasonCommandHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly CreateSeasonCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreateSeasonCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateSeasonCommandHandler(_unitOfWorkMock.Object);

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
        var command = _fixture
            .Build<CreateSeasonCommand>()
            .With(c => c.StartDate, DateTime.UtcNow)
            .With(c => c.EndDate, DateTime.UtcNow.AddDays(1))
            .Create();

        _unitOfWorkMock
            .Setup(x => x.Seasons.FindAsync(It.IsAny<Expression<Func<Season, bool>>>()))
            .ReturnsAsync((Season?)null);

        _unitOfWorkMock
            .Setup(x => x.Seasons.AddAsync(It.IsAny<Season>()))
            .Callback<Season>(s => s.Id = _fixture.Create<int>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().NotBe(0);
        result.Name.Should().Be(command.Name);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Seasons.AddAsync(It.IsAny<Season>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEndDateBeforeStartDate_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture
            .Build<CreateSeasonCommand>()
            .With(c => c.StartDate, DateTime.UtcNow)
            .With(c => c.EndDate, DateTime.UtcNow.AddDays(-1))
            .Create();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("End date must be after start date");
    }

    [Fact]
    public async Task Handle_WithExistingSeasonName_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture
            .Build<CreateSeasonCommand>()
            .With(c => c.StartDate, DateTime.UtcNow)
            .With(c => c.EndDate, DateTime.UtcNow.AddDays(1))
            .Create();
        var existingSeason = _fixture.Create<Season>();

        _unitOfWorkMock
            .Setup(x =>
                x.Seasons.FindAsync(
                    s => s.Name.ToLower() == command.Name.ToLower(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existingSeason);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be($"Season with name '{command.Name}' already exists");
    }

    [Fact]
    public async Task Handle_WithActiveSeasonInSameLeague_ReturnsErrorResponse()
    {
        // Arrange
        var command = _fixture
            .Build<CreateSeasonCommand>()
            .With(c => c.IsActive, true)
            .With(c => c.StartDate, DateTime.UtcNow)
            .With(c => c.EndDate, DateTime.UtcNow.AddDays(1))
            .Create();
        var activeSeason = _fixture.Create<Season>();

        _unitOfWorkMock
            .SetupSequence(x =>
                x.Seasons.FindAsync(
                    s =>
                        s.LeagueName == command.LeagueName
                        && s.Country == command.Country
                        && s.IsActive,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(activeSeason);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result
            .Error.Should()
            .Be($"An active season for {command.LeagueName} in {command.Country} already exists");
    }
}
