using System.Security.Claims;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using AutoFixture;
using Domain.Interfaces;
using FluentAssertions;
using Footex.Configuration;
using Footex.Controllers;
using Footex.UnitTests.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Controllers;

public class MatchesControllerTests : IClassFixture<TestFixtureBase>
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly MatchesController _controller;
    private readonly Fixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IHubContext<NotificationService, INotificationService>> _hubContextMock;
    private readonly Mock<ILogger<MatchesController>> _loggerMock;
    private readonly Mock<MatchMapper> _matchMapperMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IOptions<SimulationServiceOptions>> _simulationOptionsMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public MatchesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _matchMapperMock = new Mock<MatchMapper>();
        _simulationOptionsMock = new Mock<IOptions<SimulationServiceOptions>>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<MatchesController>>();
        _hubContextMock = new Mock<IHubContext<NotificationService, INotificationService>>();
        _cacheServiceMock = new Mock<ICacheService>();

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Setup simulation options
        var simulationOptions = new SimulationServiceOptions
        {
            BaseUrl = "http://localhost:5000",
            ApiKey = "test-key"
        };
        _simulationOptionsMock.Setup(x => x.Value).Returns(simulationOptions);

        _controller = new MatchesController(
            _mediatorMock.Object,
            _httpClientFactoryMock.Object,
            _matchMapperMock.Object,
            _simulationOptionsMock.Object,
            _serviceScopeFactoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object,
            _cacheServiceMock.Object);

        // Setup controller context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetAllMatches_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new GetAllMatchesQueryResponse
        {
            Succeeded = true,
            Matches = new List<MatchDto>
            {
                new() { Id = 1, HomeTeamName = "Arsenal", AwayTeamName = "Chelsea" }
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<GetAllMatchesQueryResponse>(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync((GetAllMatchesQueryResponse?)null);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetAllMatchesQuery>(), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<GetAllMatchesQueryResponse>(),
                It.IsAny<TimeSpan>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetAllMatches(1, 1, "Scheduled", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllMatchesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<GetAllMatchesQuery>(q =>
            q.HomeSeasonId == 1 &&
            q.TeamId == 1 &&
            q.Status == "Scheduled"), default), Times.Once);
    }

    [Fact]
    public async Task GetAllMatches_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new GetAllMatchesQueryResponse
        {
            Succeeded = true,
            Matches = new List<MatchDto>
            {
                new() { Id = 1, HomeTeamName = "Arsenal", AwayTeamName = "Chelsea" }
            }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<GetAllMatchesQueryResponse>(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _controller.GetAllMatches(1, 1, "Scheduled", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 1);

        // Assert
        result.Should().BeOfType<ActionResult<GetAllMatchesQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(cachedResponse);

        // Verify mediator was not called since we used cache
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetAllMatchesQuery>(), CancellationToken.None), Times.Never);

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
            Match = new MatchDto { Id = matchId, HomeTeamName = "Arsenal", AwayTeamName = "Chelsea" }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<GetMatchByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync((GetMatchByIdQueryResponse?)null);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMatchByIdQuery>(), CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<GetMatchByIdQueryResponse>(),
                It.IsAny<TimeSpan>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetMatchById(matchId);

        // Assert
        result.Should().BeOfType<ActionResult<GetMatchByIdQueryResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.Is<GetMatchByIdQuery>(q => q.Id == matchId), CancellationToken.None),
            Times.Once);
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
            Error = "Match not found"
        };

        _cacheServiceMock.Setup(x => x.GetAsync<GetMatchByIdQueryResponse>(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync((GetMatchByIdQueryResponse?)null);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMatchByIdQuery>(), CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMatchById(matchId);

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
            Id = _fixture.Create<int>()
        };

        _matchMapperMock.Setup(x => x.ToCreateCommand(createMatchDto))
            .Returns(createCommand);

        _mediatorMock.Setup(x => x.Send(createCommand, CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateMatch(createMatchDto);

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
        var createCommand = new CreateMatchCommand
        {
            CreatorId = ""
        };
        var expectedResponse = new CreateMatchCommandResponse
        {
            Succeeded = false,
            Error = "Validation failed"
        };

        _matchMapperMock.Setup(x => x.ToCreateCommand(createMatchDto))
            .Returns(createCommand);

        _mediatorMock.Setup(x => x.Send(createCommand, default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateMatch(createMatchDto);

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
            ScheduledDateTime = updateMatchDto.ScheduledDateTimeUTC
        };

        _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateMatchCommand>(), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateMatch(matchId, updateMatchDto);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateMatchCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateMatch_WithMismatchedIds_ReturnsBadRequest()
    {
        // Arrange
        const int urlId = 1;
        const int bodyId = 2;
        var updateMatchDto = TestDataBuilder.CreateValidUpdateMatchDto(bodyId);

        // Act
        var result = await _controller.UpdateMatch(urlId, updateMatchDto);

        // Assert
        result.Should().BeOfType<ActionResult<UpdateMatchCommandResponse>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "ID in URL does not match ID in request body" });
    }

    [Fact]
    public async Task DeleteMatch_WithValidId_ReturnsOkResult()
    {
        // Arrange
        const int matchId = 1;
        var expectedResponse = new DeleteMatchCommandResponse
        {
            Succeeded = true
        };

        _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteMatchCommand>(), default))
            .ReturnsAsync(expectedResponse);

        _cacheServiceMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteMatch(matchId);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteMatchCommandResponse>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);

        // Verify cache invalidation was called
        _cacheServiceMock.Verify(x => x.RemoveAsync($"match_{matchId}", CancellationToken.None), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync($"match_details_{matchId}", CancellationToken.None), Times.Once);
        _cacheServiceMock.Verify(x => x.RemoveAsync("matches_all_*", CancellationToken.None), Times.Once);
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
            Error = "Match not found"
        };

        _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteMatchCommand>(), default))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteMatch(matchId);

        // Assert
        result.Should().BeOfType<ActionResult<DeleteMatchCommandResponse>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }
}