using System.Security.Claims;
using Application.CQRS;
using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class PlayersControllerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly PlayersController _controller;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<IPlayerMapper> _playerMapperMock;

    public PlayersControllerTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _playerMapperMock = new Mock<IPlayerMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();

        _controller = new PlayersController(
            _fileStorageServiceMock.Object,
            _playerMapperMock.Object,
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
    public async Task GetAllPlayers_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllPlayersQueryResponse
        {
            Succeeded = true,
            Players = new List<PlayerDto>
            {
                new()
                {
                    Id = 1,
                    FullName = "Bukayo Saka",
                    KnownName = "Saka",
                    Position = "Winger",
                    Nationality = "England",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllPlayersQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllPlayersQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetAllPlayersQuery, GetAllPlayersQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetAllPlayersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllPlayersQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllPlayers("England", "Right", 1, 1, 10, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllPlayersQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllPlayersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllPlayers_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllPlayersQueryResponse
        {
            Succeeded = true,
            Players = new List<PlayerDto>
            {
                new()
                {
                    Id = 1,
                    FullName = "Bukayo Saka",
                    KnownName = "Saka",
                    Position = "Winger",
                    Nationality = "England",
                },
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllPlayersQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        var handlerMock = new Mock<IRequestHandler<GetAllPlayersQuery, GetAllPlayersQueryResponse>>();

        // Act
        var result = await _controller.GetAllPlayers("England", "Right", 1, 1, 10, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllPlayersQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllPlayersQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPlayerById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var playerId = 1;
        var expectedResponse = new GetPlayerByIdQueryResponse
        {
            Succeeded = true,
            Player = new PlayerDto
            {
                Id = playerId,
                FullName = "Bukayo Saka",
                KnownName = "Saka",
                Position = "Winger",
                Nationality = "England",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetPlayerByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetPlayerByIdQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetPlayerByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetPlayerById(playerId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetPlayerByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreatePlayer_WithValidPlayer_ReturnsCreatedResult()
    {
        // Arrange
        var playerDto = new CreatePlayerDto
        {
            FullName = "Bukayo Saka",
            KnownName = "Saka",
            Position = "Winger",
            Nationality = "England",
            PreferredFoot = "Left",
        };

        var createCommand = new CreatePlayerCommand
        {
            FullName = playerDto.FullName,
            KnownName = playerDto.KnownName,
            Position = playerDto.Position,
            Nationality = playerDto.Nationality,
            PreferredFoot = playerDto.PreferredFoot,
        };

        var expectedResponse = new CreatePlayerCommandResponse
        {
            Succeeded = true,
            Id = 1,
            FullName = playerDto.FullName,
        };

        _playerMapperMock
            .Setup(x => x.ToCreateCommand(It.IsAny<CreatePlayerDto>()))
            .Returns(createCommand);

        var handlerMock = new Mock<IRequestHandler<CreatePlayerCommand, CreatePlayerCommandResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<CreatePlayerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreatePlayer(playerDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<CreatePlayerCommandResponse>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<CreatePlayerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayer_WithValidPlayer_ReturnsOkResult()
    {
        // Arrange
        var playerId = 1;
        var playerDto = _fixture.Create<UpdatePlayerDto>();

        var getPlayerResponse = new GetPlayerByIdQueryResponse
        {
            Succeeded = true,
            Player = new PlayerDto { Id = playerId, PhotoUrl = "test.jpg" },
        };

        var getHandlerMock = new Mock<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getPlayerResponse);

        var updateCommand = new UpdatePlayerCommand
        {
            Id = playerId,
            FullName = playerDto.FullName,
            KnownName = playerDto.KnownName,
            Position = playerDto.Position,
            Nationality = playerDto.Nationality,
        };

        var expectedResponse = new UpdatePlayerCommandResponse
        {
            Succeeded = true,
            Id = playerId,
            FullName = playerDto.FullName,
        };

        _playerMapperMock
            .Setup(x => x.ToUpdateCommand(It.IsAny<UpdatePlayerDto>()))
            .Returns(updateCommand);

        var updateHandlerMock = new Mock<IRequestHandler<UpdatePlayerCommand, UpdatePlayerCommandResponse>>();
        updateHandlerMock
            .Setup(x => x.Handle(It.IsAny<UpdatePlayerCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdatePlayer(playerId, playerDto, getHandlerMock.Object, updateHandlerMock.Object);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        updateHandlerMock.Verify(x => x.Handle(It.IsAny<UpdatePlayerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePlayer_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var playerId = 1;
        var expectedResponse = new DeletePlayerCommandResponse { Succeeded = true };

        var getPlayerResponse = new GetPlayerByIdQueryResponse
        {
            Succeeded = true,
            Player = new PlayerDto { Id = playerId, PhotoUrl = "test.jpg" },
        };

        var getHandlerMock = new Mock<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getPlayerResponse);

        var deleteHandlerMock = new Mock<IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>>();
        deleteHandlerMock
            .Setup(x => x.Handle(It.Is<DeletePlayerCommand>(c => c.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeletePlayer(playerId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        deleteHandlerMock.Verify(
            x => x.Handle(It.Is<DeletePlayerCommand>(c => c.Id == playerId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeletePlayer_WhenPlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var playerId = 1;
        var getPlayerResponse = new GetPlayerByIdQueryResponse
        {
            Succeeded = false,
            NotFound = true,
        };

        var getHandlerMock = new Mock<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getPlayerResponse);

        var deleteHandlerMock = new Mock<IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>>();

        // Act
        var result = await _controller.DeletePlayer(playerId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeletePlayerCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(getPlayerResponse);
    }

    [Fact]
    public async Task DeletePlayer_WhenMediatorFails_ReturnsBadRequest()
    {
        // Arrange
        var playerId = 1;
        var getPlayerResponse = new GetPlayerByIdQueryResponse
        {
            Succeeded = true,
            Player = new PlayerDto
            {
                Id = playerId,
                PhotoUrl = "test.jpg",
                FullName = "test",
                KnownName = "test",
                Nationality = "test",
                Position = "test",
            },
        };
        var expectedResponse = new DeletePlayerCommandResponse { Succeeded = false };

        var getHandlerMock = new Mock<IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>>();
        getHandlerMock
            .Setup(x => x.Handle(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getPlayerResponse);

        var deleteHandlerMock = new Mock<IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>>();
        deleteHandlerMock
            .Setup(x => x.Handle(It.Is<DeletePlayerCommand>(c => c.Id == playerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeletePlayer(playerId, getHandlerMock.Object, deleteHandlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeletePlayerCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
