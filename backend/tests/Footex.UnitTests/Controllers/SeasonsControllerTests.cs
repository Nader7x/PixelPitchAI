using System.Security.Claims;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Application.Interfaces;
using AutoFixture;
using Domain.Models;
using FluentAssertions;
using Footex.Controllers;
using Footex.UnitTests.Common;
using Application.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class SeasonsControllerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly SeasonsController _controller;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<ISeasonMapper> _seasonMapperMock;

    public SeasonsControllerTests()
    {
        _seasonMapperMock = new Mock<ISeasonMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new NoRecursionFixture();

        _controller = new SeasonsController(
            _seasonMapperMock.Object,
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
    public async Task GetAllSeasons_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllSeasonsQueryResponse
        {
            Succeeded = true,
            Seasons = new List<SeasonDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Premier League 2023-24",
                    LeagueName = "Premier League",
                    Country = "England",
                    IsActive = true,
                },
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllSeasonsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllSeasonsQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetAllSeasonsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllSeasonsQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllSeasons("Premier League", "England", true, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllSeasonsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllSeasonsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllSeasons_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllSeasonsQueryResponse
        {
            Succeeded = true,
            Seasons = new List<SeasonDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Premier League 2023-24",
                    LeagueName = "Premier League",
                    Country = "England",
                    IsActive = true,
                },
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllSeasonsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetAllSeasons("Premier League", "England", true, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllSeasonsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetAllSeasonsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSeasonById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var seasonId = 1;
        var expectedResponse = new GetSeasonByIdQueryResponse
        {
            Succeeded = true,
            Season = new SeasonDto
            {
                Id = seasonId,
                Name = "Premier League 2023-24",
                LeagueName = "Premier League",
                Country = "England",
                IsActive = true,
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonByIdQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetSeasonByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetSeasonById(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSeasonById_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var seasonId = 1;
        var cachedResponse = new GetSeasonByIdQueryResponse
        {
            Succeeded = true,
            Season = new SeasonDto
            {
                Id = seasonId,
                Name = "Cached Premier League 2023-24",
                LeagueName = "Premier League",
                Country = "England",
                IsActive = true,
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetSeasonById(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetSeasonByIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSeasonById_WithInvalidId_ReturnsNotFoundResult()
    {
        // Arrange
        var seasonId = 99;
        var expectedResponse = new GetSeasonByIdQueryResponse
        {
            Succeeded = false,
            NotFound = true,
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonByIdQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSeasonById(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonByIdQueryResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSeasonById_WithMediatorFailure_ReturnsBadRequest()
    {
        // Arrange
        var seasonId = 1;
        var expectedResponse = new GetSeasonByIdQueryResponse { Succeeded = false };

        var handlerMock = new Mock<IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonByIdQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSeasonById(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonByIdQueryResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonByIdQuery>(q => q.Id == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSeasonTeams_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var seasonId = 1;
        var expectedResponse = new GetSeasonTeamsQueryResponse
        {
            Succeeded = true,
            TeamSeasons = new List<TeamSeason>
            {
                new()
                {
                    Team = new Team { Id = 1, Name = "Team A" },
                },
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonTeamsQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSeasonTeams(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonTeamsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as GetSeasonTeamsQueryResponse;
        response.Should().NotBeNull();
        response!.Succeeded.Should().BeTrue();
        response.TeamSeasons.Should().NotBeEmpty();
        response.TeamSeasons.Should().HaveCount(1);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSeasonTeams_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var seasonId = 1;
        var cachedResponse = new GetSeasonTeamsQueryResponse
        {
            Succeeded = true,
            TeamSeasons = new List<TeamSeason>
            {
                new()
                {
                    Team = new Team { Id = 1, Name = "Team A" },
                },
            },
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetSeasonTeams(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonTeamsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        handlerMock.Verify(x => x.Handle(It.IsAny<GetSeasonTeamsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSeasonTeams_WithInvalidId_ReturnsNotFoundResult()
    {
        // Arrange
        var seasonId = 99;
        var expectedResponse = new GetSeasonTeamsQueryResponse
        {
            Succeeded = false,
            Error = "Not Found",
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonTeamsQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSeasonTeams(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonTeamsQueryResponse>>();
        var notFoundResult = result.Result as BadRequestObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetSeasonTeams_WhenMediatorFails_ReturnsBadRequest()
    {
        // Arrange
        var seasonId = 1;
        var expectedResponse = new GetSeasonTeamsQueryResponse
        {
            Succeeded = false,
            Error = "Mediator failure",
        };

        var handlerMock = new Mock<IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>>();

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetSeasonTeamsQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetSeasonTeamsQueryResponse?)null);

        handlerMock
            .Setup(x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetSeasonTeams(seasonId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetSeasonTeamsQueryResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetSeasonTeamsQuery>(q => q.SeasonId == seasonId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
