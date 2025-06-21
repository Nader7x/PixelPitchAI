using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Interfaces;
using Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Footex.UnitTests.CQRS.Stadiums.Queries;

public class GetStadiumByIdQueryHandlerTests
{
    private readonly Fixture _fixture;
    private readonly GetStadiumByIdQueryHandler _handler;
    private readonly Mock<IStadiumMapper> _iStadiumMapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public GetStadiumByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _iStadiumMapperMock = new Mock<IStadiumMapper>();
        _handler = new GetStadiumByIdQueryHandler(_unitOfWorkMock.Object, _iStadiumMapperMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task Handle_WithValidId_ReturnsSuccessResponse()
    {
        // Arrange
        var stadiumId = 1;
        var query = new GetStadiumByIdQuery { Id = stadiumId };
        var stadium = CreateValidStadium(stadiumId);
        var expectedStadiumDto = new StadiumDto
        {
            Id = stadiumId,
            Name = stadium.Name,
            City = stadium.City,
            Country = stadium.Country,
            Capacity = stadium.Capacity
        };

        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(stadiumId))
            .ReturnsAsync(stadium);
        _iStadiumMapperMock.Setup(x => x.ToDto(stadium))
            .Returns(expectedStadiumDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.NotFound.Should().BeFalse();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Id.Should().Be(stadiumId);
        result.Error.Should().BeNull();

        _unitOfWorkMock.Verify(x => x.Stadiums.GetByIdAsync(stadiumId), Times.Once);
        _iStadiumMapperMock.Verify(x => x.ToDto(stadium), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsNotFoundResponse()
    {
        // Arrange
        var stadiumId = 999;
        var query = new GetStadiumByIdQuery { Id = stadiumId };

        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(stadiumId))
            .ReturnsAsync((Stadium?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Stadium.Should().BeNull();
        result.Error.Should().Be($"Stadium with ID {stadiumId} not found");

        _unitOfWorkMock.Verify(x => x.Stadiums.GetByIdAsync(stadiumId), Times.Once);
        _iStadiumMapperMock.Verify(x => x.ToDto(It.IsAny<Stadium>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ReturnsFailureResponse()
    {
        // Arrange
        var stadiumId = 1;
        var query = new GetStadiumByIdQuery { Id = stadiumId };
        var exceptionMessage = "Database connection failed";

        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(stadiumId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeFalse();
        result.Stadium.Should().BeNull();
        result.Error.Should().Be(exceptionMessage);

        _unitOfWorkMock.Verify(x => x.Stadiums.GetByIdAsync(stadiumId), Times.Once);
        _iStadiumMapperMock.Verify(x => x.ToDto(It.IsAny<Stadium>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_WithInvalidId_ReturnsNotFoundResponse(int invalidId)
    {
        // Arrange
        var query = new GetStadiumByIdQuery { Id = invalidId };

        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(invalidId))
            .ReturnsAsync((Stadium?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.NotFound.Should().BeTrue();
        result.Stadium.Should().BeNull();
        result.Error.Should().Be($"Stadium with ID {invalidId} not found");

        _unitOfWorkMock.Verify(x => x.Stadiums.GetByIdAsync(invalidId), Times.Once);
        _iStadiumMapperMock.Verify(x => x.ToDto(It.IsAny<Stadium>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidStadium_MapsCorrectly()
    {
        // Arrange
        var stadiumId = 1;
        var query = new GetStadiumByIdQuery { Id = stadiumId };
        var stadium = CreateValidStadium(stadiumId);
        var expectedStadiumDto = new StadiumDto
        {
            Id = stadiumId,
            Name = stadium.Name,
            City = stadium.City,
            Country = stadium.Country,
            Capacity = stadium.Capacity,
            SurfaceType = stadium.SurfaceType,
            Address = stadium.Address,
            Latitude = stadium.Latitude,
            Longitude = stadium.Longitude,
            Description = stadium.Description,
            Facilities = stadium.Facilities,
            BuiltDate = stadium.BuiltDate
        };

        _unitOfWorkMock.Setup(x => x.Stadiums.GetByIdAsync(stadiumId))
            .ReturnsAsync(stadium);
        _iStadiumMapperMock.Setup(x => x.ToDto(stadium))
            .Returns(expectedStadiumDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Stadium.Should().NotBeNull();
        result.Stadium!.Id.Should().Be(expectedStadiumDto.Id);
        result.Stadium.Name.Should().Be(expectedStadiumDto.Name);
        result.Stadium.City.Should().Be(expectedStadiumDto.City);
        result.Stadium.Country.Should().Be(expectedStadiumDto.Country);
        result.Stadium.Capacity.Should().Be(expectedStadiumDto.Capacity);
        result.Stadium.SurfaceType.Should().Be(expectedStadiumDto.SurfaceType);
        result.Stadium.Address.Should().Be(expectedStadiumDto.Address);
        result.Stadium.Latitude.Should().Be(expectedStadiumDto.Latitude);
        result.Stadium.Longitude.Should().Be(expectedStadiumDto.Longitude);
        result.Stadium.Description.Should().Be(expectedStadiumDto.Description);
        result.Stadium.Facilities.Should().Be(expectedStadiumDto.Facilities);
        result.Stadium.BuiltDate.Should().Be(expectedStadiumDto.BuiltDate);
    }

    private Stadium CreateValidStadium(int id)
    {
        var stadium = _fixture.Create<Stadium>();
        stadium.Id = id;
        stadium.Name = "Emirates Stadium";
        stadium.City = "London";
        stadium.Country = "England";
        stadium.Capacity = 60260;
        stadium.SurfaceType = "Grass";
        stadium.Address = "Hornsey Rd, London N7 7AJ";
        stadium.Latitude = 51.5549m;
        stadium.Longitude = -0.1084m;
        stadium.Description = "Home stadium of Arsenal FC";
        stadium.Facilities = "VIP boxes, restaurants, shops";
        stadium.BuiltDate = new DateTime(2006, 7, 22);
        return stadium;
    }
}
