using System.Security.Claims;
using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
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

public class PlayersControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly PlayersController _controller;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IPlayerMapper> _playerMapperMock;
    private readonly TestFixtureBase _testFixtureBase;

    public PlayersControllerTests(TestFixtureBase testFixtureBase)
    {
        _testFixtureBase = testFixtureBase;
        _mediatorMock = new Mock<IMediator>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _playerMapperMock = new Mock<IPlayerMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();
        _fixture.Customizations.Add(new IFormFileSpecimenBuilder());
        _fixture
            .Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _controller = new PlayersController(
            _mediatorMock.Object,
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

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllPlayersQuery>(), default))
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
        var result = await _controller.GetAllPlayers("England", "Right", 1, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllPlayersQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllPlayersQuery>(), default), Times.Once);
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

        // Act
        var result = await _controller.GetAllPlayers("England", "Right", 1, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllPlayersQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllPlayersQuery>(), default), Times.Never);
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

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default))
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
        var result = await _controller.GetPlayerById(playerId);

        // Assert
        result.Should().BeOfType<ActionResult<GetPlayerByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default),
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

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreatePlayerCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreatePlayer(playerDto);

        // Assert
        result.Should().BeOfType<ActionResult<CreatePlayerCommandResponse>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<CreatePlayerCommand>(), default), Times.Once);
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

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default))
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

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdatePlayerCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdatePlayer(playerId, playerDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<UpdatePlayerCommand>(), default), Times.Once);
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

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default))
            .ReturnsAsync(getPlayerResponse);

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeletePlayerCommand>(c => c.Id == playerId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeletePlayer(playerId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(
            x => x.Send(It.Is<DeletePlayerCommand>(c => c.Id == playerId), default),
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

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default))
            .ReturnsAsync(getPlayerResponse);

        // Act
        var result = await _controller.DeletePlayer(playerId);

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

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetPlayerByIdQuery>(q => q.Id == playerId), default))
            .ReturnsAsync(getPlayerResponse);

        _mediatorMock
            .Setup(x => x.Send(It.Is<DeletePlayerCommand>(c => c.Id == playerId), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeletePlayer(playerId);

        // Assert
        result.Should().BeOfType<ActionResult<DeletePlayerCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
