using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Teams.Queries;

public class GetAllTeamsQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetAllTeamsQueryHandler _handler;
    private readonly Mock<ITeamMapper> _iTeamMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;    public GetAllTeamsQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iTeamMapperMock = new Mock<ITeamMapper>();
        _handler = new GetAllTeamsQueryHandler(_unitOfWorkMock.Object, _iTeamMapperMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllTeams()
    {
        // Arrange
        var query = new GetAllTeamsQuery();
        var teams = new List<Team>
        {
            CreateValidTeam(1),
            CreateValidTeam(2),
            CreateValidTeam(3)
        };
        var expectedTeamDtos = teams.Select(t => new TeamDto
        {
            Id = t.Id,
            Name = t.Name,
            Country = t.Country,
            City = t.City,
            League = t.League
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(teams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(teams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(3);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(teams), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCountryFilter_ReturnsFilteredTeams()
    {
        // Arrange
        var country = "England";
        var query = new GetAllTeamsQuery { Country = country };
        var allTeams = new List<Team>
        {
            CreateValidTeam(1),
            CreateValidTeam(2),
            CreateValidTeam(3)
        };
        allTeams[0].Country = country;
        allTeams[1].Country = country;
        allTeams[2].Country = "Spain"; // This should be filtered out

        var filteredTeams = allTeams.Where(t => t.Country == country).ToList();
        var expectedTeamDtos = filteredTeams.Select(t => new TeamDto
        {
            Id = t.Id,
            Country = t.Country
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(2);
        result.Teams.All(t => t.Country == country).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Team>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLeagueFilter_ReturnsFilteredTeams()
    {
        // Arrange
        var league = "Premier League";
        var query = new GetAllTeamsQuery { League = league };
        var allTeams = new List<Team>
        {
            CreateValidTeam(1),
            CreateValidTeam(2),
            CreateValidTeam(3)
        };
        allTeams[0].League = league;
        allTeams[1].League = league;
        allTeams[2].League = "La Liga"; // This should be filtered out

        var filteredTeams = allTeams.Where(t => t.League == league).ToList();
        var expectedTeamDtos = filteredTeams.Select(t => new TeamDto
        {
            Id = t.Id,
            League = t.League
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(2);
        result.Teams.All(t => t.League == league).Should().BeTrue();
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Team>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBothFilters_ReturnsFilteredTeams()
    {
        // Arrange
        var country = "Spain";
        var league = "La Liga";
        var query = new GetAllTeamsQuery { Country = country, League = league };
        var allTeams = new List<Team>
        {
            CreateValidTeam(1),
            CreateValidTeam(2),
            CreateValidTeam(3),
            CreateValidTeam(4)
        };
        allTeams[0].Country = country;
        allTeams[0].League = league;
        allTeams[1].Country = country;
        allTeams[1].League = "Copa del Rey"; // Wrong league
        allTeams[2].Country = "England"; // Wrong country
        allTeams[2].League = league;
        allTeams[3].Country = "France"; // Wrong country and league
        allTeams[3].League = "Ligue 1";

        var filteredTeams = allTeams.Where(t => t.Country == country && t.League == league).ToList();
        var expectedTeamDtos = filteredTeams.Select(t => new TeamDto
        {
            Id = t.Id,
            Country = t.Country,
            League = t.League
        }).ToList();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(1);
        result.Teams.First().Country.Should().Be(country);
        result.Teams.First().League.Should().Be(league);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Team>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllTeamsQuery();
        var teams = new List<Team>();
        var expectedTeamDtos = new List<TeamDto>();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(teams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(teams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(teams), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoMatchingFilters_ReturnsEmptyList()
    {
        // Arrange
        var country = "Australia"; // Country not in test data
        var query = new GetAllTeamsQuery { Country = country };
        var allTeams = new List<Team>
        {
            CreateValidTeam(1),
            CreateValidTeam(2)
        };
        allTeams.ForEach(t => t.Country = "England"); // Different country

        var filteredTeams = allTeams.Where(t => t.Country == country).ToList();
        var expectedTeamDtos = new List<TeamDto>();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Should().NotBeNull();
        result.Teams.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Team>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        var query = new GetAllTeamsQuery
        {
            Country = "",
            League = "   " // whitespace only
        };
        var teams = new List<Team> { CreateValidTeam(1), CreateValidTeam(2) };
        var expectedTeamDtos = teams.Select(t => new TeamDto { Id = t.Id }).ToList();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(teams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(teams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams!.Count.Should().Be(2);

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
        _iTeamMapperMock.Verify(x => x.ToDtoList(teams), Times.Once);
    }

    [Fact]
    public async Task Handle_FiltersAreCaseSensitive()
    {
        // Arrange
        var country = "ENGLAND"; // uppercase
        var query = new GetAllTeamsQuery { Country = country };
        var allTeams = new List<Team> { CreateValidTeam(1) };
        allTeams[0].Country = "England"; // different case

        var filteredTeams = allTeams.Where(t => t.Country == country).ToList();
        var expectedTeamDtos = new List<TeamDto>();

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams!.Count.Should().Be(0); // No match due to case sensitivity

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_MapperCorrectlyTransformsTeams()
    {
        // Arrange
        var query = new GetAllTeamsQuery();
        var teams = new List<Team> { CreateValidTeam(1) };
        var expectedTeamDtos = new List<TeamDto>
        {
            new()
            {
                Id = 1,
                Name = "Manchester United",
                ShortName = "Man Utd",
                Logo = "logo.png",
                Country = "England",
                City = "Manchester",
                League = "Premier League",
                PrimaryColor = "#FF0000",
                SecondaryColor = "#FFFFFF",
                FoundationDate = new DateTime(1878, 1, 1)
            }
        };

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(teams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(teams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams.Count.Should().Be(1);

        var teamDto = result.Teams.First();
        teamDto.Id.Should().Be(1);
        teamDto.Name.Should().Be("Manchester United");
        teamDto.ShortName.Should().Be("Man Utd");
        teamDto.Logo.Should().Be("logo.png");
        teamDto.Country.Should().Be("England");
        teamDto.City.Should().Be("Manchester");
        teamDto.League.Should().Be("Premier League");
        teamDto.PrimaryColor.Should().Be("#FF0000");
        teamDto.SecondaryColor.Should().Be("#FFFFFF");
        teamDto.FoundationDate.Should().Be(new DateTime(1878, 1, 1));

        _iTeamMapperMock.Verify(x => x.ToDtoList(teams), Times.Once);
    }

    [Fact]
    public async Task Handle_SucceededAlwaysTrue_NoExceptionHandling()
    {
        // Arrange
        var query = new GetAllTeamsQuery();
        var teams = new List<Team> { CreateValidTeam(1) };
        var expectedTeamDtos = new List<TeamDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(teams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(teams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue(); // Always true in this handler
        result.Error.Should().BeNull();

        // Note: This handler doesn't implement exception handling
        // so Succeeded is always true in the current implementation
    }

    [Fact]
    public async Task Handle_FilteringLogic_WorksWithMixedData()
    {
        // Arrange
        var country = "Italy";
        var league = "Serie A";
        var query = new GetAllTeamsQuery { Country = country, League = league };

        var allTeams = new List<Team>
        {
            CreateValidTeam(1), // Italy, Serie A - should match
            CreateValidTeam(2), // Italy, Coppa Italia - country match only
            CreateValidTeam(3), // Spain, Serie A - league match only (hypothetical)
            CreateValidTeam(4) // England, Premier League - no match
        };

        allTeams[0].Country = "Italy";
        allTeams[0].League = "Serie A";
        allTeams[1].Country = "Italy";
        allTeams[1].League = "Coppa Italia";
        allTeams[2].Country = "Spain";
        allTeams[2].League = "Serie A";
        allTeams[3].Country = "England";
        allTeams[3].League = "Premier League";

        var filteredTeams = allTeams.Where(t => t.Country == country && t.League == league).ToList();
        var expectedTeamDtos = new List<TeamDto> { new() { Id = 1 } };

        _unitOfWorkMock.Setup(x => x.Teams.GetAllAsync())
            .ReturnsAsync(allTeams);
        _iTeamMapperMock.Setup(x => x.ToDtoList(filteredTeams))
            .Returns(expectedTeamDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Teams!.Count.Should().Be(1);
        result.Teams.First().Id.Should().Be(1);

        _unitOfWorkMock.Verify(x => x.Teams.GetAllAsync(), Times.Once);
    }

    private Team CreateValidTeam(int id)
    {
        var team = _fixture.Create<Team>();
        team.Id = id;
        team.Name = "Manchester United";
        team.ShortName = "Man Utd";
        team.Logo = "logo.png";
        team.Country = "England";
        team.City = "Manchester";
        team.League = "Premier League";
        team.PrimaryColor = "#FF0000";
        team.SecondaryColor = "#FFFFFF";
        team.FoundationDate = new DateTime(1878, 1, 1);
        return team;
    }
}
