using System.Security.Claims;
using Application.CQRS;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Services;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.Configuration;
using Footex.Controllers;
using Footex.UnitTests.Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class MatchesControllerTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly MatchesController _controller;
    private readonly NoRecursionFixture _fixture;
    private readonly Mock<IMatchMapper> _iMatchMapperMock;

    public MatchesControllerTests()
    {
        var liveMatchService = new Mock<ILiveMatchStatisticsService>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _iMatchMapperMock = new Mock<IMatchMapper>();
        var simulationOptionsMock = new Mock<IOptions<SimulationServiceOptions>>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<MatchesController>>();
        var hubContextMock = new Mock<IHubContext<NotificationService, INotificationService>>();
        _cacheServiceMock = new Mock<ICacheService>();
        var performanceMonitoring = new Mock<IPerformanceMonitoringService>();

        _fixture = new NoRecursionFixture();

        // Setup simulation options
        var simulationOptions = new SimulationServiceOptions
        {
            BaseUrl = "http://localhost:8000",
            ApiKey = "test-key",
        };
        simulationOptionsMock.Setup(x => x.Value).Returns(simulationOptions);

        _controller = new MatchesController(
            httpClientFactoryMock.Object,
            _iMatchMapperMock.Object,
            simulationOptionsMock.Object,
            serviceScopeFactoryMock.Object,
            unitOfWorkMock.Object,
            performanceMonitoring.Object,
            liveMatchService.Object,
            loggerMock.Object,
            hubContextMock.Object,
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
    public async Task GetAllMatches_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllMatchesQueryResponse
        {
            Succeeded = true,
            Matches =
            [
                new MatchDto
                {
                    Id = 1,
                    HomeTeamName = "Arsenal",
                    AwayTeamName = "Chelsea",
                },
            ],
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllMatchesQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetAllMatchesQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetAllMatchesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetAllMatchesQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllMatches(
            1,
            1,
            "Scheduled",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            1,
            handlerMock.Object
        );

        // Assert
        result.Should().BeOfType<ActionResult<GetAllMatchesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x =>
                x.Handle(
                    It.Is<GetAllMatchesQuery>(q =>
                        q.HomeSeasonId == 1 && q.TeamId == 1 && q.Status == "Scheduled"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllMatches_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllMatchesQueryResponse
        {
            Succeeded = true,
            Matches =
            [
                new MatchDto
                {
                    Id = 1,
                    HomeTeamName = "Arsenal",
                    AwayTeamName = "Chelsea",
                },
            ],
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetAllMatchesQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync(cachedResponse);

        var handlerMock = new Mock<IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>>();

        // Act
        var result = await _controller.GetAllMatches(
            1,
            1,
            "Scheduled",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            1,
            handlerMock.Object
        );

        // Assert
        result.Should().BeOfType<ActionResult<GetAllMatchesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        // Verify mediator was not called since we used cache
        handlerMock.Verify(
            x => x.Handle(It.IsAny<GetAllMatchesQuery>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Verify cache hit header was set
        _controller.Response.Headers.Should().ContainKey("X-Cache-Hit");
    }

    [Fact]
    public async Task GetMatchById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const int matchId = 1;
        var expectedResponse = new GetMatchByIdQueryResponse
        {
            Succeeded = true,
            Match = new MatchDto
            {
                Id = matchId,
                HomeTeamName = "Arsenal",
                AwayTeamName = "Chelsea",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetMatchByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetMatchByIdQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetMatchByIdQuery, GetMatchByIdQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetMatchByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<GetMatchByIdQueryResponse>(),
                    It.IsAny<TimeSpan>(),
                    CancellationToken.None
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetMatchById(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        handlerMock.Verify(
            x => x.Handle(It.Is<GetMatchByIdQuery>(q => q.Id == matchId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetMatchById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        const int matchId = 999;
        var expectedResponse = new GetMatchByIdQueryResponse
        {
            Succeeded = false,
            NotFound = true,
            Error = "Match not found",
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetMatchByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None)
            )
            .ReturnsAsync((GetMatchByIdQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetMatchByIdQuery, GetMatchByIdQueryResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<GetMatchByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMatchById(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdQueryResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateMatch_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createMatchDto = TestDataBuilder.CreateValidCreateMatchDto();
        var createCommand = new CreateMatchCommand { CreatorId = "" };
        var expectedResponse = new CreateMatchCommandResponse
        {
            Succeeded = true,
            Id = _fixture.Create<int>(),
        };

        _iMatchMapperMock.Setup(x => x.ToCreateCommand(createMatchDto)).Returns(createCommand);

        var handlerMock = new Mock<IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>>();
        handlerMock
            .Setup(x => x.Handle(createCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateMatch(createMatchDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<CreateMatchCommandResponse>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(expectedResponse);
        createdResult.ActionName.Should().Be(nameof(MatchesController.GetMatchById));
    }

    [Fact]
    public async Task CreateMatch_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createMatchDto = TestDataBuilder.CreateValidCreateMatchDto();
        var createCommand = new CreateMatchCommand { CreatorId = "" };
        var expectedResponse = new CreateMatchCommandResponse
        {
            Succeeded = false,
            Error = "Validation failed",
        };

        _iMatchMapperMock.Setup(x => x.ToCreateCommand(createMatchDto)).Returns(createCommand);

        var handlerMock = new Mock<IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>>();
        handlerMock.Setup(x => x.Handle(createCommand, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateMatch(createMatchDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<CreateMatchCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateMatch_WithValidData_ReturnsOkResult()
    {
        // Arrange
        const int matchId = 1;
        var updateMatchDto = TestDataBuilder.CreateValidUpdateMatchDto(matchId);
        var expectedResponse = new UpdateMatchCommandResponse
        {
            Succeeded = true,
            Id = matchId,
            ScheduledDateTime = updateMatchDto.ScheduledDateTimeUtc.GetValueOrDefault(),
        };
        _iMatchMapperMock
            .Setup(x => x.ToUpdateCommand(updateMatchDto))
            .Returns(
                new UpdateMatchCommand
                {
                    Id = matchId,
                    HomeTeamId = 1,
                    AwayTeamId = 2,
                    MatchWeek = 1,
                    ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
                    MatchStatus = "Scheduled",
                }
            );

        var handlerMock = new Mock<IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<UpdateMatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateMatch(matchId, updateMatchDto, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateMatchCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task DeleteMatch_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const int matchId = 1;
        var expectedResponse = new DeleteMatchCommandResponse { Succeeded = true };

        var handlerMock = new Mock<IRequestHandler<DeleteMatchCommand, DeleteMatchCommandResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<DeleteMatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock
            .Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteMatch(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteMatchCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        // Verify cache invalidation was called
        _cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("matches_all_*", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteMatch_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        const int matchId = 999;
        var expectedResponse = new DeleteMatchCommandResponse
        {
            Succeeded = false,
            NotFound = true,
            Error = "Match not found",
        };

        var handlerMock = new Mock<IRequestHandler<DeleteMatchCommand, DeleteMatchCommandResponse>>();
        handlerMock
            .Setup(x => x.Handle(It.IsAny<DeleteMatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteMatch(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteMatchCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMatchByIdWithDetails_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const int matchId = 1;
        var expectedResponse = new GetMatchByIdWithDetailsQueryResponse
        {
            Succeeded = true,
            Match = new MatchDetailsDto
            {
                Id = matchId,
                HomeTeamInMatchName = "Arsenal",
                AwayTeamInMatchName = "Chelsea",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetMatchByIdWithDetailsQueryResponse>(
                    It.IsAny<string>(),
                    CancellationToken.None
                )
            )
            .ReturnsAsync((GetMatchByIdWithDetailsQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>>();
        handlerMock
            .Setup(x =>
                x.Handle(It.Is<GetMatchByIdWithDetailsQuery>(q => q.MatchId == matchId), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMatchByIdWithDetails(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdWithDetailsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetMatchByIdWithDetails_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        const int matchId = 1;
        var cachedResponse = new GetMatchByIdWithDetailsQueryResponse
        {
            Succeeded = true,
            Match = new MatchDetailsDto
            {
                Id = matchId,
                HomeTeamInMatchName = "Cached Arsenal",
                AwayTeamInMatchName = "Cached Chelsea",
            },
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetMatchByIdWithDetailsQueryResponse>(
                    It.IsAny<string>(),
                    CancellationToken.None
                )
            )
            .ReturnsAsync(cachedResponse);

        var handlerMock = new Mock<IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>>();

        // Act
        var result = await _controller.GetMatchByIdWithDetails(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdWithDetailsQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);
        
        handlerMock.Verify(
            x => x.Handle(It.IsAny<GetMatchByIdWithDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetMatchByIdWithDetails_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        const int matchId = 999;
        var expectedResponse = new GetMatchByIdWithDetailsQueryResponse
        {
            Succeeded = false,
            NotFound = true,
            Error = "Match not found",
        };

        _cacheServiceMock
            .Setup(x =>
                x.GetAsync<GetMatchByIdWithDetailsQueryResponse>(
                    It.IsAny<string>(),
                    CancellationToken.None
                )
            )
            .ReturnsAsync((GetMatchByIdWithDetailsQueryResponse?)null);

        var handlerMock = new Mock<IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>>();
        handlerMock
            .Setup(x =>
                x.Handle(It.Is<GetMatchByIdWithDetailsQuery>(q => q.MatchId == matchId), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMatchByIdWithDetails(matchId, handlerMock.Object);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdWithDetailsQueryResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
