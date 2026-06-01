using System.Security.Claims;
using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class StadiumsControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly StadiumsController _controller;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IStadiumMapper> _stadiumMapperMock;
    private readonly TestFixtureBase _testFixtureBase;

    public StadiumsControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _mediatorMock = new Mock<IMediator>();
        _stadiumMapperMock = new Mock<IStadiumMapper>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();
        _fixture.Customizations.Add(new IFormFileSpecimenBuilder());
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new StadiumsController(
            _mediatorMock.Object,
            _stadiumMapperMock.Object,
            _fileStorageServiceMock.Object,
            _cacheServiceMock.Object
        );

        // Setup controller context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Fact]
    public async Task GetAllStadiums_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllStadiumsQueryResponse
        {
            Succeeded = true,
            Stadiums = new List<StadiumDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Emirates Stadium",
                    Country = "England",
                    City = "London",
                    Capacity = 60000,
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllStadiumsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllStadiumsQueryResponse?)null);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllStadiumsQuery>(), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllStadiumsQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllStadiums("England", "London");

        // Assert
        result.Should().BeOfType<ActionResult<GetAllStadiumsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllStadiumsQuery>(), default), Times.Once);
    }

    [Fact]
    public async Task GetAllStadiums_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllStadiumsQueryResponse
        {
            Succeeded = true,
            Stadiums = new List<StadiumDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Emirates Stadium",
                    Country = "England",
                    City = "London",
                    Capacity = 60000,
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllStadiumsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetAllStadiums("England", "London");

        // Assert
        result.Should().BeOfType<ActionResult<GetAllStadiumsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllStadiumsQuery>(), default), Times.Never);
    }

    [Fact]
    public async Task GetStadiumById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var stadiumId = 1;
        var expectedResponse = new GetStadiumByIdQueryResponse
        {
            Succeeded = true,
            Stadium = new StadiumDto
            {
                Id = stadiumId,
                Name = "Emirates Stadium",
                Country = "England",
                City = "London",
                Capacity = 60000,
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetStadiumByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetStadiumByIdQueryResponse?)null);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetStadiumByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetStadiumById(stadiumId);

        // Assert
        result.Should().BeOfType<ActionResult<GetStadiumByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateStadium_WithValidStadium_ReturnsCreatedResult()
    {
        // Arrange
        var stadiumDto = new CreateStadiumDto
        {
            Name = "Emirates Stadium",
            Country = "England",
            City = "London",
            Capacity = 60000,
        };

        var createCommand = new CreateStadiumCommand
        {
            Name = stadiumDto.Name,
            Country = stadiumDto.Country,
            City = stadiumDto.City,
            Capacity = stadiumDto.Capacity,
        };

        var expectedResponse = new CreateStadiumCommandResponse
        {
            Succeeded = true,
            Id = 1,
            Name = stadiumDto.Name,
        };

        _stadiumMapperMock
            .Setup(x => x.ToCreateCommand(It.IsAny<CreateStadiumDto>()))
            .Returns(createCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateStadiumCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateStadium(stadiumDto);

        // Assert
        result.Should().BeOfType<ActionResult<CreateStadiumCommandResponse>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<CreateStadiumCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateStadium_WithValidStadium_ReturnsOkResult()
    {
        // Arrange
        var stadiumId = 1;
        var stadiumDto = _fixture.Create<UpdateStadiumDto>();

        var getStadiumResponse = new GetStadiumByIdQueryResponse
        {
            Succeeded = true,
            Stadium = new StadiumDto
            {
                Id = stadiumId,
                ImageUrl = "test.jpg",
                Name = "Test Stadium",
                City = "Test City",
                Country = "Test Country",
                Capacity = 10000,
            },
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(getStadiumResponse);

        var updateCommand = new UpdateStadiumCommand
        {
            Id = stadiumId,
            Name = stadiumDto.Name,
            Country = stadiumDto.Country,
            City = stadiumDto.City,
            Capacity = stadiumDto.Capacity,
        };

        var expectedResponse = new UpdateStadiumCommandResponse
        {
            Succeeded = true,
            Id = stadiumId,
            Name = stadiumDto.Name,
        };

        _stadiumMapperMock
            .Setup(x => x.ToUpdateCommand(It.IsAny<UpdateStadiumDto>()))
            .Returns(updateCommand);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateStadiumCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateStadium(stadiumId, stadiumDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdateStadiumCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task DeleteStadium_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var stadiumId = 1;
        var expectedResponse = new DeleteStadiumCommandResponse { Succeeded = true };

        var getStadiumResponse = new GetStadiumByIdQueryResponse
        {
            Succeeded = true,
            Stadium = new StadiumDto
            {
                Id = stadiumId,
                ImageUrl = "test.jpg",
                Name = "Test Stadium",
                City = "Test City",
                Country = "Test Country",
                Capacity = 10000,
            },
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(getStadiumResponse);

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeleteStadiumCommand>(c => c.Id == stadiumId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteStadium(stadiumId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<DeleteStadiumCommand>(c => c.Id == stadiumId), default),
            Times.Once
        );
    }

    [Fact]
    public async Task GetStadiumById_WithNonExistentId_ReturnsNotFoundResult()
    {
        // Arrange
        var stadiumId = 99;
        var expectedResponse = new GetStadiumByIdQueryResponse
        {
            Succeeded = false,
            NotFound = true,
            Error = "Stadium not found",
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetStadiumByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetStadiumByIdQueryResponse?)null);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetStadiumById(stadiumId);

        // Assert
        result.Should().BeOfType<ActionResult<GetStadiumByIdQueryResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateStadium_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var stadiumDto = _fixture.Create<CreateStadiumDto>();
        var command = _fixture.Create<CreateStadiumCommand>();
        var expectedResponse = new CreateStadiumCommandResponse
        {
            Succeeded = false,
            Error = "Invalid data",
        };

        _stadiumMapperMock.Setup(m => m.ToCreateCommand(stadiumDto)).Returns(command);
        _mediatorMock.Setup(x => x.Send(command, default)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateStadium(stadiumDto);

        // Assert
        result.Should().BeOfType<ActionResult<CreateStadiumCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateStadium_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var stadiumId = 99;
        var stadiumDto = _fixture.Create<UpdateStadiumDto>();
        var getResponse = new GetStadiumByIdQueryResponse { Succeeded = false, NotFound = true };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(getResponse);

        // Act
        var result = await _controller.UpdateStadium(stadiumId, stadiumDto);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateStadiumCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(getResponse);
    }

    [Fact]
    public async Task DeleteStadium_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var stadiumId = 99;
        var getResponse = new GetStadiumByIdQueryResponse { Succeeded = false, NotFound = true };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(getResponse);

        // Act
        var result = await _controller.DeleteStadium(stadiumId);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteStadiumCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(getResponse);
    }

    [Fact]
    public async Task DeleteStadium_WhenDeletionFails_ReturnsBadRequest()
    {
        // Arrange
        var stadiumId = 1;
        var getResponse = new GetStadiumByIdQueryResponse
        {
            Succeeded = true,
            Stadium = new StadiumDto { Id = stadiumId, ImageUrl = "test.jpg" },
        };
        var deleteResponse = new DeleteStadiumCommandResponse
        {
            Succeeded = false,
            Error = "Deletion failed",
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetStadiumByIdQuery>(q => q.Id == stadiumId), default))
            .ReturnsAsync(getResponse);

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeleteStadiumCommand>(c => c.Id == stadiumId), default))
            .ReturnsAsync(deleteResponse);

        // Act
        var result = await _controller.DeleteStadium(stadiumId);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteStadiumCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(deleteResponse);
    }
}
