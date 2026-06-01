using Application.CQRS.Coaches.Commands;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Coaches.Commands;

public class UpdateCoachCommandHandlerTests
{
    private readonly Mock<ICoachMapper> _coachMapperMock;
    private readonly NoRecursionFixture _fixture;
    private readonly UpdateCoachCommandHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public UpdateCoachCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _coachMapperMock = new Mock<ICoachMapper>();
        _handler = new UpdateCoachCommandHandler(_unitOfWorkMock.Object, _coachMapperMock.Object);

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
        var command = new UpdateCoachCommand
        {
            Id = 1,
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

        var existingCoach = new Coach
        {
            Id = command.Id,
            FirstName = "Jose",
            LastName = "Mourinho",
            DateOfBirth = new DateTime(1963, 1, 26),
            Nationality = "Portuguese",
            Role = "Head Coach",
        };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCoach);

        _coachMapperMock
            .Setup(x => x.ToCoachFromUpdate(command, existingCoach))
            .Callback<UpdateCoachCommand, Coach>(
                (coachCommand, coach) =>
                {
                    coach.Id = coachCommand.Id;
                    if (coachCommand.FirstName != null)
                        coach.FirstName = coachCommand.FirstName;
                    if (coachCommand.LastName != null)
                        coach.LastName = coachCommand.LastName;
                    if (coachCommand.DateOfBirth != null)
                        coach.DateOfBirth = coachCommand.DateOfBirth.Value;
                    if (coachCommand.Nationality != null)
                        coach.Nationality = coachCommand.Nationality;
                    if (coachCommand.Role != null)
                        coach.Role = coachCommand.Role;
                    if (coachCommand.YearsOfExperience != null)
                        coach.YearsOfExperience = coachCommand.YearsOfExperience.Value;
                    if (coachCommand.PhotoUrl != null)
                        coach.PhotoUrl = coachCommand.PhotoUrl;
                    if (coachCommand.Biography != null)
                        coach.Biography = coachCommand.Biography;
                    if (coachCommand.PreferredFormation != null)
                        coach.PreferredFormation = coachCommand.PreferredFormation;
                    if (coachCommand.CoachingStyle != null)
                        coach.CoachingStyle = coachCommand.CoachingStyle;
                    if (coachCommand.TeamId != null)
                        coach.TeamId = coachCommand.TeamId.Value;
                }
            );

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Id.Should().Be(command.Id);
        result.FullName.Should().Be($"{command.FirstName} {command.LastName}");
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCoach_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = new UpdateCoachCommand
        {
            Id = 999,
            FirstName = "Jose",
            LastName = "Mourinho",
        };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coach?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Error.Should().Contain($"Coach with ID {command.Id} not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new UpdateCoachCommand
        {
            Id = 1,
            FirstName = "Jose",
            LastName = "Mourinho",
        };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Database error");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
