using System.Security.Claims;
using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using Application.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class CoachesControllerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICoachMapper> _coachMapperMock;
    private readonly CoachesController _controller;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly NoRecursionFixture _fixture;

    public CoachesControllerTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _coachMapperMock = new Mock<ICoachMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();

        _controller = new CoachesController(
            _fileStorageServiceMock.Object,
            _coachMapperMock.Object,
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
    public async Task GetAllCoaches_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllCoachesQueryResponse
        {
            Succeeded = true,
            Coaches = new List<CoachDto>
            {
                new()
                {
                    Id = 1,
                    FirstName = "Mikel",
                    LastName = "Arteta",
                    Nationality = "Spain",
                    TeamId = 1,
                    TeamName = "Arsenal",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllCoachesQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllCoachesQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetAllCoachesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllCoachesQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllCoaches("Spain", 1, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllCoachesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllCoachesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCoaches_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllCoachesQueryResponse
        {
            Succeeded = true,
            Coaches = new List<CoachDto>
            {
                new()
                {
                    Id = 1,
                    FirstName = "Mikel",
                    LastName = "Arteta",
                    Nationality = "Spain",
                    TeamId = 1,
                    TeamName = "Arsenal",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllCoachesQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        var handlerMock = new Mock<IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse>>();

        // Act
        var result = await _controller.GetAllCoaches("Spain", 1, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllCoachesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllCoachesQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCoachById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var coachId = 1;
        var expectedResponse = new GetCoachByIdQueryResponse
        {
            Succeeded = true,
            Coach = new CoachDto
            {
                Id = coachId,
                FirstName = "Mikel",
                LastName = "Arteta",
                Nationality = "Spain",
                TeamId = 1,
                TeamName = "Arsenal",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetCoachByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetCoachByIdQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetCoachByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetCoachById(coachId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetCoachByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCoachById_WithNonExistentId_ReturnsNotFoundResult()
    {
        // Arrange
        var coachId = 99;
        var expectedResponse = new GetCoachByIdQueryResponse
        {
            Succeeded = false,
            NotFound = true,
            Error = "Coach not found",
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetCoachByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetCoachByIdQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetCoachById(coachId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetCoachByIdQueryResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateCoach_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var createCoachDto = _fixture.Create<CreateCoachDto>();
        var command = _fixture.Create<CreateCoachCommand>();
        var expectedResponse = new CreateCoachCommandResponse
        {
            Succeeded = true,
            Id = 1,
            FullName = $"{createCoachDto.FirstName} {createCoachDto.LastName}",
        };

        _coachMapperMock.Setup(m => m.ToCreateCommand(createCoachDto)).Returns(command);
        
        var handlerMock = new Mock<IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>>();
        handlerMock.Setup(x => x.Handle(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);
        
        _fileStorageServiceMock
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("http://photo.url/new.jpg");

        // Act
        var result = await _controller.CreateCoach(createCoachDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<CreateCoachCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateCoach_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createCoachDto = _fixture.Create<CreateCoachDto>();
        var command = _fixture.Create<CreateCoachCommand>();
        var expectedResponse = new CreateCoachCommandResponse
        {
            Succeeded = false,
            Error = "Invalid data.",
        };

        _coachMapperMock.Setup(m => m.ToCreateCommand(createCoachDto)).Returns(command);
        
        var handlerMock = new Mock<IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>>();
        handlerMock.Setup(x => x.Handle(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateCoach(createCoachDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<CreateCoachCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateCoach_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var coachId = 1;
        var updateCoachDto = _fixture.Create<UpdateCoachDto>();
        var command = _fixture.Build<UpdateCoachCommand>().With(c => c.Id, coachId).Create();
        var getQueryResponse = new GetCoachByIdQueryResponse
        {
            Succeeded = true,
            Coach = new CoachDto
            {
                Id = coachId,
                FirstName = "Old",
                LastName = "Name",
            },
        };
        var expectedResponse = new UpdateCoachCommandResponse
        {
            Succeeded = true,
            Id = coachId,
            FullName = $"{updateCoachDto.FirstName} {updateCoachDto.LastName}",
        };

        var getHandlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getQueryResponse);
            
        _coachMapperMock.Setup(m => m.ToUpdateCommand(updateCoachDto)).Returns(command);
        
        var updateHandlerMock = new Mock<IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>>();
        updateHandlerMock.Setup(x => x.Handle(command, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateCoach(coachId, updateCoachDto, getHandlerMock.Object, updateHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateCoachCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateCoach_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var coachId = 99;
        var updateCoachDto = _fixture.Create<UpdateCoachDto>();
        var expectedResponse = new GetCoachByIdQueryResponse { Succeeded = false, NotFound = true };

        var getHandlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
            
        var updateHandlerMock = new Mock<IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>>();

        // Act
        var result = await _controller.UpdateCoach(coachId, updateCoachDto, getHandlerMock.Object, updateHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateCoachCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task DeleteCoach_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var coachId = 1;
        var getQueryResponse = new GetCoachByIdQueryResponse
        {
            Succeeded = true,
            Coach = new CoachDto { Id = coachId, PhotoUrl = "http://photo.url/test.jpg" },
        };
        var expectedResponse = new DeleteCoachCommandResponse { Succeeded = true };

        var getHandlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getQueryResponse);
            
        var deleteHandlerMock = new Mock<IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse>>();
        deleteHandlerMock
            .Setup(x => x.Handle(It.Is<DeleteCoachCommand>(c => c.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteCoach(coachId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteCoachCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task DeleteCoach_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var coachId = 99;
        var expectedResponse = new GetCoachByIdQueryResponse { Succeeded = false, NotFound = true };

        var getHandlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
            
        var deleteHandlerMock = new Mock<IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse>>();

        // Act
        var result = await _controller.DeleteCoach(coachId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteCoachCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task DeleteCoach_WhenDeletionFails_ReturnsBadRequest()
    {
        // Arrange
        var coachId = 1;
        var getQueryResponse = new GetCoachByIdQueryResponse
        {
            Succeeded = true,
            Coach = new CoachDto { Id = coachId },
        };
        var expectedResponse = new DeleteCoachCommandResponse
        {
            Succeeded = false,
            Error = "Error deleting coach.",
        };

        var getHandlerMock = new Mock<IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetCoachByIdQuery>(q => q.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getQueryResponse);
            
        var deleteHandlerMock = new Mock<IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse>>();
        deleteHandlerMock
            .Setup(x => x.Handle(It.Is<DeleteCoachCommand>(c => c.Id == coachId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteCoach(coachId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteCoachCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
