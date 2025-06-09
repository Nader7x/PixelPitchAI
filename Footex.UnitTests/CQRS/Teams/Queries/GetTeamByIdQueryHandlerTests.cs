using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Application.Mappers;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Teams.Queries;

public class GetTeamByIdQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetTeamByIdQueryHandler _handler;
    private readonly Mock<TeamMapper> _teamMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetTeamByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _teamMapperMock = new Mock<TeamMapper>();
        _handler = new GetTeamByIdQueryHandler(_unitOfWorkMock.Object, _teamMapperMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var teamId = 1;
        var query = new GetTeamByIdQuery { Id = teamId };
        var team = CreateValidTeam(teamId);
        var expectedTeamDto = new TeamDto
        {
            Id = teamId,
            Name = team.Name,
            ShortName = team.ShortName,
            Country = team.Country,
            City = team.City,
            League = team.League
        };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
            .ReturnsAsync(team);
        _teamMapperMock.Setup(x => x.ToTeamDto(team))
            .Returns(expectedTeamDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Team.Should().NotBeNull();
        result.Team!.Id.Should().Be(teamId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsyncWithStadium(teamId), Times.Once);
        _teamMapperMock.Verify(x => x.ToTeamDto(team), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var teamId = 999;
        var query = new GetTeamByIdQuery { Id = teamId };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
            .ReturnsAsync((Team?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Team.Should().BeNull();
        result.Error.Should().Be("Team not found");

        _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsyncWithStadium(teamId), Times.Once);
        _teamMapperMock.Verify(x => x.ToTeamDto(It.IsAny<Team>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetTeamByIdQuery { Id = invalidId };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(invalidId))
            .ReturnsAsync((Team?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Team.Should().BeNull();
        result.Error.Should().Be("Team not found");

        _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsyncWithStadium(invalidId), Times.Once);
        _teamMapperMock.Verify(x => x.ToTeamDto(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidTeam_MapsCorrectly()
    {
        // Arrange
        var teamId = 1;
        var query = new GetTeamByIdQuery { Id = teamId };
        var team = CreateValidTeam(teamId);
        var expectedTeamDto = new TeamDto
        {
            Id = teamId,
            Name = team.Name,
            ShortName = team.ShortName,
            Logo = team.Logo,
            Country = team.Country,
            City = team.City,
            League = team.League,
            PrimaryColor = team.PrimaryColor,
            SecondaryColor = team.SecondaryColor,
            FoundationDate = team.FoundationDate
        };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
            .ReturnsAsync(team);
        _teamMapperMock.Setup(x => x.ToTeamDto(team))
            .Returns(expectedTeamDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Team.Should().NotBeNull();
        result.Team!.Id.Should().Be(expectedTeamDto.Id);
        result.Team.Name.Should().Be(expectedTeamDto.Name);
        result.Team.ShortName.Should().Be(expectedTeamDto.ShortName);
        result.Team.Logo.Should().Be(expectedTeamDto.Logo);
        result.Team.Country.Should().Be(expectedTeamDto.Country);
        result.Team.City.Should().Be(expectedTeamDto.City);
        result.Team.League.Should().Be(expectedTeamDto.League);
        result.Team.PrimaryColor.Should().Be(expectedTeamDto.PrimaryColor);
        result.Team.SecondaryColor.Should().Be(expectedTeamDto.SecondaryColor);
        result.Team.FoundationDate.Should().Be(expectedTeamDto.FoundationDate);
    }

    [Fact]
    public async Task Handle_VerifiesRepositoryCallWithCorrectMethod()
    {
        // Arrange
        var teamId = 5;
        var query = new GetTeamByIdQuery { Id = teamId };
        var team = CreateValidTeam(teamId);
        var teamDto = new TeamDto { Id = teamId, Name = team.Name };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
            .ReturnsAsync(team);
        _teamMapperMock.Setup(x => x.ToTeamDto(team))
            .Returns(teamDto);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsyncWithStadium(teamId), Times.Once);
        _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _teamMapperMock.Verify(x => x.ToTeamDto(team), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullTeam_DoesNotCallMapper()
    {
        // Arrange
        var teamId = 1;
        var query = new GetTeamByIdQuery { Id = teamId };

        _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
            .ReturnsAsync((Team?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Team.Should().BeNull();

        _teamMapperMock.Verify(x => x.ToTeamDto(It.IsAny<Team>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentTeamIds_CallsRepositoryWithCorrectId()
    {
        // Arrange
        var teamIds = new[] { 1, 10, 25, 100 };

        foreach (var teamId in teamIds)
        {
            var query = new GetTeamByIdQuery { Id = teamId };
            var team = CreateValidTeam(teamId);
            var teamDto = new TeamDto { Id = teamId, Name = $"Team {teamId}" };

            _unitOfWorkMock.Setup(x => x.Teams.GetByIdAsyncWithStadium(teamId))
                .ReturnsAsync(team);
            _teamMapperMock.Setup(x => x.ToTeamDto(team))
                .Returns(teamDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Team!.Id.Should().Be(teamId);

            _unitOfWorkMock.Verify(x => x.Teams.GetByIdAsyncWithStadium(teamId), Times.Once);
        }
    }

    private Team CreateValidTeam(int id)
    {
        var team = _fixture.Create<Team>();
        team.Id = id;
        team.Name = "Arsenal FC";
        team.ShortName = "ARS";
        team.Logo = "https://example.com/arsenal-logo.png";
        team.Country = "England";
        team.City = "London";
        team.League = "Premier League";
        team.PrimaryColor = "#EF0107";
        team.SecondaryColor = "#023474";
        team.FoundationDate = new DateTime(1886, 10, 1);
        team.StadiumId = 1;
        return team;
    }
}