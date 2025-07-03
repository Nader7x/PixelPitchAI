using System.Linq.Expressions;
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

public class GetAllCoachesQueryHandlerTests
{
    private readonly Mock<ICoachMapper> _iCoachMapperMock;
    private readonly NoRecursionFixture _fixture;
    private readonly GetAllCoachesQueryHandler _handler;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllCoachesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iCoachMapperMock = new Mock<ICoachMapper>();
        _handler = new GetAllCoachesQueryHandler(_unitOfWorkMock.Object, _iCoachMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllCoaches()
    {
        // Arrange
        var query = new GetAllCoachesQuery();
        var coaches = new List<Coach>
        {
            CreateValidCoach(1),
            CreateValidCoach(2),
            CreateValidCoach(3),
        };
        var expectedCoachDtos = coaches
            .Select(c => new CoachDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Nationality = c.Nationality,
                YearsOfExperience = c.YearsOfExperience,
            })
            .ToList();

        _unitOfWorkMock.Setup(x => x.Coaches.GetAllAsync()).ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches!.Count.Should().Be(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Coaches.GetAllAsync(), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNationalityFilter_ReturnsFilteredCoaches()
    {
        // Arrange
        var nationality = "England";
        var query = new GetAllCoachesQuery { Nationality = nationality };
        var coaches = new List<Coach> { CreateValidCoach(1), CreateValidCoach(2) };
        coaches.ForEach(c => c.Nationality = nationality);
        var expectedCoachDtos = coaches
            .Select(c => new CoachDto { Id = c.Id, Nationality = c.Nationality })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches!.Count.Should().Be(2);
        result.Coaches.All(c => c.Nationality == nationality).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTeamIdFilter_ReturnsFilteredCoaches()
    {
        // Arrange
        var teamId = 5;
        var query = new GetAllCoachesQuery { TeamId = teamId };
        var coaches = new List<Coach> { CreateValidCoach(1), CreateValidCoach(2) };
        coaches.ForEach(c => c.TeamId = teamId);
        var expectedCoachDtos = coaches
            .Select(c => new CoachDto { Id = c.Id, TeamId = c.TeamId })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches!.Count.Should().Be(2);
        result.Coaches.All(c => c.TeamId == teamId).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBothFilters_ReturnsFilteredCoaches()
    {
        // Arrange
        var nationality = "Spain";
        var teamId = 3;
        var query = new GetAllCoachesQuery { Nationality = nationality, TeamId = teamId };
        var coaches = new List<Coach> { CreateValidCoach(1) };
        coaches[0].Nationality = nationality;
        coaches[0].TeamId = teamId;
        var expectedCoachDtos = coaches
            .Select(c => new CoachDto
            {
                Id = c.Id,
                Nationality = c.Nationality,
                TeamId = c.TeamId,
            })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches!.Count.Should().Be(1);
        result.Coaches.First().Nationality.Should().Be(nationality);
        result.Coaches.First().TeamId.Should().Be(teamId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllCoachesQuery();
        var coaches = new List<Coach>();
        var expectedCoachDtos = new List<CoachDto>();

        _unitOfWorkMock.Setup(x => x.Coaches.GetAllAsync()).ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches.Should().NotBeNull();
        result.Coaches!.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Coaches.GetAllAsync(), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllCoachesQuery();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Coaches.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Coaches.GetAllAsync(), Times.Once);
        _iCoachMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Coach>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFilterException_ReturnsFailureResponse()
    {
        // Arrange
        var nationality = "Brazil";
        var query = new GetAllCoachesQuery { Nationality = nationality };
        var exceptionMessage = "Filter operation failed";

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Coaches.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
        _iCoachMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Coach>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        var query = new GetAllCoachesQuery { Nationality = "", TeamId = null };
        var coaches = new List<Coach> { CreateValidCoach(1) };
        var expectedCoachDtos = new List<CoachDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Coaches.GetAllAsync()).ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();

        // Verify GetAllAsync without filter was called
        _unitOfWorkMock.Verify(x => x.Coaches.GetAllAsync(), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_NationalityFilterIgnoresCase()
    {
        // Arrange
        var nationality = "ENGLAND"; // uppercase input
        var query = new GetAllCoachesQuery { Nationality = nationality };
        var coaches = new List<Coach> { CreateValidCoach(1) };
        coaches[0].Nationality = "england"; // lowercase in database
        var expectedCoachDtos = new List<CoachDto>
        {
            new() { Id = 1, Nationality = "england" },
        };

        _unitOfWorkMock
            .Setup(x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()))
            .ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches!.Count.Should().Be(1);

        _unitOfWorkMock.Verify(
            x => x.Coaches.GetAllAsync(It.IsAny<Expression<Func<Coach, bool>>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_MapperCorrectlyTransformsCoaches()
    {
        // Arrange
        var query = new GetAllCoachesQuery();
        var coaches = new List<Coach> { CreateValidCoach(1) };
        var expectedCoachDtos = new List<CoachDto>
        {
            new()
            {
                Id = 1,
                FirstName = "Test Coach",
                LastName = "Coach",
                Nationality = "England",
                YearsOfExperience = 5,
                TeamId = 1,
                DateOfBirth = new DateTime(1980, 1, 1),
                PreferredFormation = "4-4-2",
            },
        };

        _unitOfWorkMock.Setup(x => x.Coaches.GetAllAsync()).ReturnsAsync(coaches);
        _iCoachMapperMock.Setup(x => x.ToDtoList(coaches)).Returns(expectedCoachDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Coaches!.Count.Should().Be(1);

        var coachDto = result.Coaches.First();
        coachDto.Id.Should().Be(1);
        coachDto.FirstName.Should().Be("Test Coach");
        coachDto.LastName.Should().Be("Coach");
        coachDto.Nationality.Should().Be("England");
        coachDto.YearsOfExperience.Should().Be(5);
        coachDto.TeamId.Should().Be(1);
        coachDto.DateOfBirth.Should().Be(new DateTime(1980, 1, 1));
        coachDto.PreferredFormation.Should().Be("4-4-2");

        _iCoachMapperMock.Verify(x => x.ToDtoList(coaches), Times.Once);
    }

    private Coach CreateValidCoach(int id)
    {
        var coach = _fixture.Create<Coach>();
        coach.Id = id;
        coach.FirstName = "John Smith";
        coach.LastName = "Smith";
        coach.Nationality = "England";
        coach.YearsOfExperience = 10;
        coach.TeamId = 1;
        coach.DateOfBirth = new DateTime(1975, 5, 15);
        coach.PreferredFormation = "4-3-3";
        return coach;
    }
}
