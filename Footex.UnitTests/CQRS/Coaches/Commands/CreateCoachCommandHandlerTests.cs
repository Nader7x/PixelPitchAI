using System.Linq.Expressions;
using Application.CQRS.Coaches.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Coaches.Commands;

public class CreateCoachCommandHandlerTests
{
    private readonly Mock<ICoachMapper> _coachMapperMock;
    private readonly NoRecursionFixture _fixture;
    private readonly CreateCoachCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreateCoachCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _coachMapperMock = new Mock<ICoachMapper>();
        _handler = new CreateCoachCommandHandler(_unitOfWorkMock.Object, _coachMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture.Customizations.Add(new IFormFileSpecimenBuilder());
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
        var command = new CreateCoachCommand
        {
            FirstName = "Jose",
            LastName = "Mourinho",
            DateOfBirth = new DateTime(1963, 1, 26),
            Nationality = "Portuguese",
            Role = "Head Coach",
            PreferredFormation = "4-3-3",
            CoachingStyle = "Defensive",
            Biography = "Experienced coach",
            YearsOfExperience = 25,
        };

        var coach = new Coach
        {
            Id = 1,
            FirstName = command.FirstName,
            LastName = command.LastName,
            DateOfBirth = command.DateOfBirth,
            Nationality = command.Nationality,
            Role = command.Role,
            PreferredFormation = command.PreferredFormation,
            CoachingStyle = command.CoachingStyle,
            Biography = command.Biography,
            YearsOfExperience = command.YearsOfExperience,
        };

        _unitOfWorkMock
            .Setup(x =>
                x.Coaches.FindAsync(
                    It.IsAny<Expression<Func<Coach, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Coach?)null);

        _coachMapperMock.Setup(x => x.ToCoachFromCreate(command)).Returns(coach);

        _unitOfWorkMock
            .Setup(x => x.Coaches.AddAsync(It.IsAny<Coach>()))
            .Returns(Task.FromResult<EntityEntry<Coach>>(null!));
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Id.Should().Be(coach.Id);
        result.FullName.Should().Be($"{command.FirstName} {command.LastName}");
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Coaches.AddAsync(It.IsAny<Coach>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateCoachName_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreateCoachCommand { FirstName = "Jose", LastName = "Mourinho" };

        var existingCoach = new Coach
        {
            Id = 1,
            FirstName = command.FirstName,
            LastName = command.LastName,
        };

        _unitOfWorkMock
            .Setup(x =>
                x.Coaches.FindAsync(
                    It.IsAny<Expression<Func<Coach, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existingCoach);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result
            .Error.Should()
            .Contain($"Coach with name '{command.FirstName} {command.LastName}' already exists");

        _unitOfWorkMock.Verify(x => x.Coaches.AddAsync(It.IsAny<Coach>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new CreateCoachCommand { FirstName = "Jose", LastName = "Mourinho" };

        _unitOfWorkMock
            .Setup(x =>
                x.Coaches.FindAsync(
                    It.IsAny<Expression<Func<Coach, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");

        _unitOfWorkMock.Verify(x => x.Coaches.AddAsync(It.IsAny<Coach>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
