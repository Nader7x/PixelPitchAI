using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Coaches.Queries;

public class GetCoachByIdQueryHandlerTests
{
    private readonly Mock<ICoachMapper> _iCoachMapperMock;
    private readonly NoRecursionFixture _fixture;
    private readonly GetCoachByIdQueryHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetCoachByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iCoachMapperMock = new Mock<ICoachMapper>();
        _handler = new GetCoachByIdQueryHandler(_unitOfWorkMock.Object, _iCoachMapperMock.Object);

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
        var coachId = 1;
        var query = new GetCoachByIdQuery { Id = coachId };
        var coach = CreateValidCoach(coachId);
        var expectedCoachDto = new CoachDto
        {
            Id = coachId,
            FirstName = coach.FirstName,
            LastName = coach.LastName,
            Nationality = coach.Nationality,
            Role = coach.Role,
        };

        _unitOfWorkMock.Setup(x => x.Coaches.GetByIdAsync(coachId)).ReturnsAsync(coach);
        _iCoachMapperMock.Setup(x => x.ToDto(coach)).Returns(expectedCoachDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Coach.Should().NotBeNull();
        result.Coach!.Id.Should().Be(coachId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Coaches.GetByIdAsync(coachId), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDto(coach), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var coachId = 999;
        var query = new GetCoachByIdQuery { Id = coachId };

        _unitOfWorkMock.Setup(x => x.Coaches.GetByIdAsync(coachId)).ReturnsAsync((Coach?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Coach.Should().BeNull();
        result.Error.Should().Be("Coach not found");

        _unitOfWorkMock.Verify(x => x.Coaches.GetByIdAsync(coachId), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDto(It.IsAny<Coach>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var coachId = 1;
        var query = new GetCoachByIdQuery { Id = coachId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetByIdAsync(coachId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Coach.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Coaches.GetByIdAsync(coachId), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDto(It.IsAny<Coach>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetCoachByIdQuery { Id = invalidId };

        _unitOfWorkMock.Setup(x => x.Coaches.GetByIdAsync(invalidId)).ReturnsAsync((Coach?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Coach.Should().BeNull();
        result.Error.Should().Be("Coach not found");

        _unitOfWorkMock.Verify(x => x.Coaches.GetByIdAsync(invalidId), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDto(It.IsAny<Coach>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCoach_MapsCorrectly()
    {
        // Arrange
        var coachId = 1;
        var query = new GetCoachByIdQuery { Id = coachId };
        var coach = CreateValidCoach(coachId);
        var expectedCoachDto = new CoachDto
        {
            Id = coachId,
            FirstName = coach.FirstName,
            LastName = coach.LastName,
            DateOfBirth = coach.DateOfBirth,
            Nationality = coach.Nationality,
            Role = coach.Role,
            TeamId = coach.TeamId,
            PreferredFormation = coach.PreferredFormation,
            CoachingStyle = coach.CoachingStyle,
            YearsOfExperience = coach.YearsOfExperience,
        };

        _unitOfWorkMock.Setup(x => x.Coaches.GetByIdAsync(coachId)).ReturnsAsync(coach);
        _iCoachMapperMock.Setup(x => x.ToDto(coach)).Returns(expectedCoachDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coach.Should().NotBeNull();
        result.Coach!.Id.Should().Be(expectedCoachDto.Id);
        result.Coach.FirstName.Should().Be(expectedCoachDto.FirstName);
        result.Coach.LastName.Should().Be(expectedCoachDto.LastName);
        result.Coach.DateOfBirth.Should().Be(expectedCoachDto.DateOfBirth);
        result.Coach.Nationality.Should().Be(expectedCoachDto.Nationality);
        result.Coach.Role.Should().Be(expectedCoachDto.Role);
        result.Coach.TeamId.Should().Be(expectedCoachDto.TeamId);
        result.Coach.PreferredFormation.Should().Be(expectedCoachDto.PreferredFormation);
        result.Coach.CoachingStyle.Should().Be(expectedCoachDto.CoachingStyle);
        result.Coach.YearsOfExperience.Should().Be(expectedCoachDto.YearsOfExperience);
    }

    private Coach CreateValidCoach(int id)
    {
        var coach = _fixture.Create<Coach>();
        coach.Id = id;
        coach.FirstName = "Pep";
        coach.LastName = "Guardiola";
        coach.DateOfBirth = new DateTime(1971, 1, 18);
        coach.Nationality = "Spain";
        coach.Role = "Head Coach";
        coach.TeamId = 1;
        coach.PreferredFormation = "4-3-3";
        coach.CoachingStyle = "Possession-based";
        coach.YearsOfExperience = 15;
        return coach;
    }
}
