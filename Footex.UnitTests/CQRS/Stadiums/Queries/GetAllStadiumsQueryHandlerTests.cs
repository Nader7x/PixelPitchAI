using System.Linq.Expressions;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Footex.UnitTests.Common;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Footex.UnitTests.CQRS.Stadiums.Queries;

public class GetAllStadiumsQueryHandlerTests
{
    private readonly NoRecursionFixture _fixture;
    private readonly GetAllStadiumsQueryHandler _handler;
    private readonly Mock<IStadiumMapper> _iStadiumMapperMock;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetAllStadiumsQueryHandlerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iStadiumMapperMock = new Mock<IStadiumMapper>();
        _handler = new GetAllStadiumsQueryHandler(
            _unitOfWorkMock.Object,
            _iStadiumMapperMock.Object
        );

        _fixture = new NoRecursionFixture();
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllStadiums()
    {
        // Arrange
        var query = new GetAllStadiumsQuery();
        var stadiums = new List<Stadium>
        {
            CreateValidStadium(1),
            CreateValidStadium(2),
            CreateValidStadium(3),
        };
        var expectedStadiumDtoS = stadiums
            .Select(s => new StadiumDto
            {
                Id = s.Id,
                Name = s.Name,
                Country = s.Country,
                City = s.City,
                Capacity = s.Capacity,
            })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(3);
        result.Error.Should().BeNull();

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCountryFilter_ReturnsFilteredStadiums()
    {
        // Arrange
        const string country = "England";
        var query = new GetAllStadiumsQuery { Country = country };
        var stadiums = new List<Stadium> { CreateValidStadium(1), CreateValidStadium(2) };
        stadiums.ForEach(s => s.Country = country);
        var expectedStadiumDtoS = stadiums
            .Select(s => new StadiumDto { Id = s.Id, Country = s.Country })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(2);
        (result.Stadiums != null && result.Stadiums.All(s => s.Country == country))
            .Should()
            .BeTrue();
        result.Error.Should().BeNull();

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCityFilter_ReturnsFilteredStadiums()
    {
        // Arrange
        const string city = "London";
        var query = new GetAllStadiumsQuery { City = city };
        var stadiums = new List<Stadium> { CreateValidStadium(1), CreateValidStadium(2) };
        stadiums.ForEach(s => s.City = city);
        var expectedStadiumDtoS = stadiums
            .Select(s => new StadiumDto { Id = s.Id, City = s.City })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(2);
        (result.Stadiums != null && result.Stadiums.All(s => s.City == city)).Should().BeTrue();
        result.Error.Should().BeNull();

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBothFilters_ReturnsFilteredStadiums()
    {
        // Arrange
        const string country = "Spain";
        const string city = "Madrid";
        var query = new GetAllStadiumsQuery { Country = country, City = city };
        var stadiums = new List<Stadium> { CreateValidStadium(1) };
        stadiums[0].Country = country;
        stadiums[0].City = city;
        var expectedStadiumDtoS = stadiums
            .Select(s => new StadiumDto
            {
                Id = s.Id,
                Country = s.Country,
                City = s.City,
            })
            .ToList();

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(1);
        if (result.Stadiums != null)
        {
            result.Stadiums.First().Country.Should().Be(country);
            result.Stadiums.First().City.Should().Be(city);
        }

        result.Error.Should().BeNull();

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAllStadiumsQuery();
        var stadiums = new List<Stadium>();
        var expectedStadiumDtoS = new List<StadiumDto>();

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(0);
        result.Error.Should().BeNull();

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var query = new GetAllStadiumsQuery();
        const string exceptionMessage = "Database connection failed";

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(0);
        result.Error.Should().Be(exceptionMessage);

        _iStadiumMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Stadium>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFilterException_ReturnsFailureResponse()
    {
        // Arrange
        const string country = "Brazil";
        var query = new GetAllStadiumsQuery { Country = country };
        const string exceptionMessage = "Filter operation failed";

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Stadiums.Should().NotBeNull();
        result.Stadiums?.Count.Should().Be(0);
        result.Error.Should().Be(exceptionMessage);

        _iStadiumMapperMock.Verify(x => x.ToDtoList(It.IsAny<IEnumerable<Stadium>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyStringFilters_TreatedAsNoFilter()
    {
        // Arrange
        var query = new GetAllStadiumsQuery
        {
            Country = "",
            City = "   ", // whitespace only
        };
        var stadiums = new List<Stadium> { CreateValidStadium(1) };
        var expectedStadiumDtoS = new List<StadiumDto> { new() { Id = 1 } };

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CountryFilterIgnoresCase()
    {
        // Arrange
        const string country = "ENGLAND"; // uppercase input
        var query = new GetAllStadiumsQuery { Country = country };
        var stadiums = new List<Stadium> { CreateValidStadium(1) };
        stadiums[0].Country = "england"; // lowercase in database
        var expectedStadiumDtoS = new List<StadiumDto>
        {
            new() { Id = 1, Country = "england" },
        };

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums?.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CityFilterIgnoresCase()
    {
        // Arrange
        const string city = "LONDON"; // uppercase input
        var query = new GetAllStadiumsQuery { City = city };
        var stadiums = new List<Stadium> { CreateValidStadium(1) };
        stadiums[0].City = "london"; // lowercase in database
        var expectedStadiumDtoS = new List<StadiumDto>
        {
            new() { Id = 1, City = "london" },
        };

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums?.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MapperCorrectlyTransformsStadiums()
    {
        // Arrange
        var query = new GetAllStadiumsQuery();
        var stadiums = new List<Stadium> { CreateValidStadium(1) };
        var expectedStadiumDtoS = new List<StadiumDto>
        {
            new()
            {
                Id = 1,
                Name = "Test Stadium",
                Country = "England",
                City = "London",
                Capacity = 50000,
                BuiltDate = new DateTime(2007, 4, 28),
                SurfaceType = "Grass",
                Description = "A test stadium",
            },
        };

        _unitOfWorkMock
            .Setup(x => x.Stadiums.GetQueryable())
            .Returns(stadiums.BuildMock());

        _iStadiumMapperMock.Setup(x => x.ToDtoList(stadiums)).Returns(expectedStadiumDtoS);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadiums?.Count.Should().Be(1);

        if (result.Stadiums != null)
        {
            var stadiumDto = result.Stadiums.First();
            stadiumDto.Id.Should().Be(1);
            stadiumDto.Name.Should().Be("Test Stadium");
            stadiumDto.Country.Should().Be("England");
            stadiumDto.City.Should().Be("London");
            stadiumDto.Capacity.Should().Be(50000);
            stadiumDto.BuiltDate.Should().Be(new DateTime(2007, 4, 28));
            stadiumDto.SurfaceType.Should().Be("Grass");
            stadiumDto.Description.Should().Be("A test stadium");
        }

        _iStadiumMapperMock.Verify(x => x.ToDtoList(stadiums), Times.Once);
    }

    private Stadium CreateValidStadium(int id)
    {
        var stadium = _fixture.Create<Stadium>();
        stadium.Id = id;
        stadium.Name = "Wembley Stadium";
        stadium.Country = "England";
        stadium.City = "London";
        stadium.Capacity = 90000;
        stadium.BuiltDate = new DateTime(2007, 4, 28);
        stadium.SurfaceType = "Grass";
        stadium.Description = "The home of English football";
        return stadium;
    }
}
