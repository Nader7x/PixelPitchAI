using System.Linq.Expressions;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Seasons.Queries;

public class GetAllSeasonsQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetAllSeasonsQueryHandler _handler;
    private readonly Mock<ISeasonMapper> _iSeasonMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllSeasonsQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iSeasonMapperMock = new Mock<ISeasonMapper>();
        _handler = new GetAllSeasonsQueryHandler(_unitOfWorkMock.Object, _iSeasonMapperMock.Object);

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllSeasons()
    {
        // Arrange
        var query = new GetAllSeasonsQuery();
        var seasons = new List<Season>
        {
            CreateValidSeason(1),
            CreateValidSeason(2),
            CreateValidSeason(3),
        };
        var expectedSeasonDtoS = seasons
            .Select(s => new SeasonDto
            {
                Id = s.Id,
                Name = s.Name,
                LeagueName = s.LeagueName,
                Country = s.Country,
                IsActive = s.IsActive,
            })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons?.Count.Should().Be(3);
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLeagueNameFilter_ReturnsFilteredSeasons()
    {
        // Arrange
        const string leagueName = "Premier League";
        var query = new GetAllSeasonsQuery { LeagueName = leagueName };
        var seasons = new List<Season> { CreateValidSeason(1), CreateValidSeason(2) };
        seasons.ForEach(s => s.LeagueName = leagueName);
        var expectedSeasonDtoS = seasons
            .Select(s => new SeasonDto { Id = s.Id, LeagueName = s.LeagueName })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons?.Count.Should().Be(2);
        result.Seasons?.All(s => s.LeagueName == leagueName).Should().BeTrue();
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCountryFilter_ReturnsFilteredSeasons()
    {
        // Arrange
        const string country = "England";
        var query = new GetAllSeasonsQuery { Country = country };
        var seasons = new List<Season> { CreateValidSeason(1), CreateValidSeason(2) };
        seasons.ForEach(s => s.Country = country);
        var expectedSeasonDtoS = seasons
            .Select(s => new SeasonDto { Id = s.Id, Country = s.Country })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons?.Count.Should().Be(2);
        result.Seasons?.All(s => s.Country == country).Should().BeTrue();
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_ReturnsFilteredSeasons()
    {
        // Arrange
        const bool isActive = true;
        var query = new GetAllSeasonsQuery { IsActive = isActive };
        var seasons = new List<Season> { CreateValidSeason(1), CreateValidSeason(2) };
        seasons.ForEach(s => s.IsActive = isActive);
        var expectedSeasonDtoS = seasons
            .Select(s => new SeasonDto { Id = s.Id, IsActive = s.IsActive })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons.Count.Should().Be(2);
        result.Seasons.All(s => s.IsActive == isActive).Should().BeTrue();
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAllFilters_ReturnsFilteredSeasons()
    {
        // Arrange
        const string leagueName = "La Liga";
        const string country = "Spain";
        const bool isActive = true;
        var query = new GetAllSeasonsQuery
        {
            LeagueName = leagueName,
            Country = country,
            IsActive = isActive,
        };
        var seasons = new List<Season> { CreateValidSeason(1) };
        seasons[0].LeagueName = leagueName;
        seasons[0].Country = country;
        seasons[0].IsActive = isActive;
        var expectedSeasonDtoS = seasons
            .Select(s => new SeasonDto
            {
                Id = s.Id,
                LeagueName = s.LeagueName,
                Country = s.Country,
                IsActive = s.IsActive,
            })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons.Count.Should().Be(1);
        result.Seasons.First().LeagueName.Should().Be(leagueName);
        result.Seasons.First().Country.Should().Be(country);
        result.Seasons.First().IsActive.Should().Be(isActive);
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLeagueNameAndIsActiveFilters_ReturnsFilteredSeasons()
    {
        // Arrange
        const string leagueName = "Bundesliga";
        const bool isActive = false;
        var query = new GetAllSeasonsQuery { LeagueName = leagueName, IsActive = isActive };
        var season = CreateValidSeason(1);
        season.LeagueName = leagueName;
        season.IsActive = isActive;
        var seasons = new List<Season> { season };
        var expectedSeasonDtoS = new List<SeasonDto> { new() { Id = 1 } };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithCountryAndIsActiveFilters_ReturnsFilteredSeasons()
    {
        // Arrange
        const string country = "Italy";
        const bool isActive = true;
        var query = new GetAllSeasonsQuery { Country = country, IsActive = isActive };
        var season = CreateValidSeason(1);
        season.Country = country;
        season.IsActive = isActive;
        var seasons = new List<Season> { season };
        var expectedSeasonDtoS = new List<SeasonDto> { new() { Id = 1 } };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithLeagueNameAndCountryFilters_ReturnsFilteredSeasons()
    {
        // Arrange
        const string leagueName = "Serie A";
        const string country = "Italy";
        var query = new GetAllSeasonsQuery { LeagueName = leagueName, Country = country };
        var season = CreateValidSeason(1);
        season.LeagueName = leagueName;
        season.Country = country;
        var seasons = new List<Season> { season };
        var expectedSeasonDtos = new List<SeasonDto> { new() { Id = 1 } };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllSeasonsQuery();
        var seasons = new List<Season>();
        var expectedSeasonDtos = new List<SeasonDto>();

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());

        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Should().NotBeNull();
        result.Seasons.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllSeasonsQuery();
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Seasons.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _iSeasonMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Season>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFilterException_ReturnsFailureResponse()
    {
        // Arrange
        var leagueName = "Premier League";
        var query = new GetAllSeasonsQuery { LeagueName = leagueName };
        var exceptionMessage = "Filter operation failed";

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Seasons.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _iSeasonMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Season>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        var query = new GetAllSeasonsQuery
        {
            LeagueName = "",
            Country = "   ", // whitespace only
        };
        var seasons = new List<Season> { CreateValidSeason(1) };
        var expectedSeasonDtoS = new List<SeasonDto> { new() { Id = 1 } };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());
        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LeagueNameFilterIgnoresCase()
    {
        // Arrange
        var leagueName = "PREMIER LEAGUE"; // uppercase input
        var query = new GetAllSeasonsQuery { LeagueName = leagueName };
        var seasons = new List<Season> { CreateValidSeason(1) };
        seasons[0].LeagueName = "premier league"; // lowercase in database
        var expectedSeasonDtos = new List<SeasonDto>
        {
            new() { Id = 1, LeagueName = "premier league" },
        };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());
        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CountryFilterIgnoresCase()
    {
        // Arrange
        var country = "ENGLAND"; // uppercase input
        var query = new GetAllSeasonsQuery { Country = country };
        var seasons = new List<Season> { CreateValidSeason(1) };
        seasons[0].Country = "england"; // lowercase in database
        var expectedSeasonDtos = new List<SeasonDto>
        {
            new() { Id = 1, Country = "england" },
        };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());
        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MapperCorrectlyTransformsSeasons()
    {
        // Arrange
        var query = new GetAllSeasonsQuery();
        var seasons = new List<Season> { CreateValidSeason(1) };
        var expectedSeasonDtos = new List<SeasonDto>
        {
            new()
            {
                Id = 1,
                Name = "2023-24 Season",
                LeagueName = "Premier League",
                Country = "England",
                StartDate = new DateTime(2023, 8, 1),
                EndDate = new DateTime(2024, 5, 31),
                IsActive = true,
                TotalRounds = 38,
            },
        };

        _unitOfWorkMock
            .Setup(x => x.Seasons.GetQueryable())
            .Returns(seasons.AsQueryable().BuildMock());
        _iSeasonMapperMock.Setup(x => x.ToDtoList(seasons)).Returns(expectedSeasonDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Seasons.Count.Should().Be(1);

        var seasonDto = result.Seasons.First();
        seasonDto.Id.Should().Be(1);
        seasonDto.Name.Should().Be("2023-24 Season");
        seasonDto.LeagueName.Should().Be("Premier League");
        seasonDto.Country.Should().Be("England");
        seasonDto.StartDate.Should().Be(new DateTime(2023, 8, 1));
        seasonDto.EndDate.Should().Be(new DateTime(2024, 5, 31));
        seasonDto.IsActive.Should().BeTrue();
        seasonDto.TotalRounds.Should().Be(38);

        _iSeasonMapperMock.Verify(x => x.ToDtoList(seasons), Times.Once);
    }

    private Season CreateValidSeason(int id)
    {
        var season = _fixture.Create<Season>();
        season.Id = id;
        season.Name = "2023-24 Season";
        season.LeagueName = "Premier League";
        season.Country = "England";
        season.StartDate = new DateTime(2023, 8, 1);
        season.EndDate = new DateTime(2024, 5, 31);
        season.IsActive = true;
        season.TotalRounds = 38;
        return season;
    }
}
