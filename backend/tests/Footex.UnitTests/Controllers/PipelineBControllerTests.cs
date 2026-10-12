using System.Linq.Expressions;
using Application.CQRS;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.CQRS.Notifications.Commands;
using Application.Dtos;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Footex.Configuration;
using Footex.Controllers;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Controllers;

public class PipelineBControllerTests : IDisposable
{
    private readonly Match _match = new() { Id = 42, CreatorId = "user", IsLive = true, MatchStatus = "Queued", SimulationId = "grpc_42" };
    private readonly Mock<IMatchRepository> _matches = new();
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<IHttpClientFactory> _http = new();
    private readonly Mock<IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>> _create = new();
    private readonly Mock<IRequestHandler<UpdateMatchStatusCommand, UpdateMatchStatusCommandResponse>> _update = new();
    private readonly Mock<IRequestHandler<CreateNotificationCommand, CreateNotificationCommandResponse>> _notification = new();
    private readonly Mock<IRequestHandler<GetLiveMatchQuery, GetLiveMatchQueryResponse>> _live = new();
    private readonly MatchStreamingQueue _queue = new(1);
    private readonly ServiceProvider _provider;
    private readonly MatchesController _controller;
    private CreateMatchCommand? _createdCommand;

    public PipelineBControllerTests()
    {
        _unit.SetupGet(u => u.Matches).Returns(_matches.Object);
        _matches.Setup(m => m.GetByIdAsync(42)).ReturnsAsync(_match);
        _matches.Setup(m => m.GetByIdWithDetailsAsync(42)).ReturnsAsync(_match);
        _matches.Setup(m => m.FindAsync(It.IsAny<Expression<Func<Match, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_match);
        _matches.Setup(m => m.UpdateSimulationIdAsync(42, "grpc_42", It.IsAny<CancellationToken>())).ReturnsAsync(_match);
        _create.Setup(h => h.Handle(It.IsAny<CreateMatchCommand>(), It.IsAny<CancellationToken>()))
            .Callback<CreateMatchCommand, CancellationToken>((c, _) => _createdCommand = c)
            .ReturnsAsync(new CreateMatchCommandResponse { Succeeded = true, Id = 42 });
        _live.Setup(h => h.Handle(It.IsAny<GetLiveMatchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetLiveMatchQueryResponse { HasLiveMatch = false });
        _notification.Setup(h => h.Handle(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateNotificationCommandResponse { Succeeded = true });
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork>(_ => _unit.Object);
        _provider = services.BuildServiceProvider();
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var grpc = new Mock<ISimulationGrpcClient>();
        grpc.Setup(g => g.GetHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            new global::Infrastructure.Protos.HealthResponse { Status = true, ModelLoaded = true, XgboostLoaded = true });
        _controller = new MatchesController(_http.Object, Mock.Of<IMatchMapper>(),
            Options.Create(new SimulationServiceOptions()), scopeFactory, _unit.Object,
            Mock.Of<IPerformanceMonitoringService>(), Mock.Of<ILiveMatchStatisticsService>(),
            NullLogger<MatchesController>.Instance, Mock.Of<IHubContext<NotificationService, INotificationService>>(),
            Mock.Of<ICacheService>(), Options.Create(new EventIngestionOptions { Mode = "GrpcStream" }), _queue, grpc.Object);
    }

    private static SimulateMatchDto Request(string homeSeason = "2019/2020") => new()
    {
        HomeTeamId = 1, AwayTeamId = 2, HomeTeamName = "Home", AwayTeamName = "Away",
        HomeTeamSeason = homeSeason, AwayTeamSeason = "2020/2021", HomeSeasonId = 1, AwaySeasonId = 2
    };

    private Task<ActionResult<CreateMatchCommandResponse>> Start(SimulateMatchDto? request = null, CancellationToken token = default) =>
        _controller.SimulateMatch("user", request ?? Request(), token, _create.Object, _update.Object, _notification.Object, _live.Object);

    [Fact]
    public async Task StartUsesCorrectSeasonsAndQueuesWithoutMarkingCompleted()
    {
        Assert.IsType<OkObjectResult>((await Start()).Result);
        Assert.Equal("Home_2020", _createdCommand!.HomeTeamInMatchName);
        Assert.Equal("Away_2021", _createdCommand.AwayTeamInMatchName);
        Assert.Equal("Queued", _createdCommand.MatchStatus);
        Assert.Null(_createdCommand.ModelSimulationStartTimeUtc);
        _update.Verify(h => h.Handle(It.IsAny<UpdateMatchStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("2020")]
    [InlineData("abcd/2021")]
    [InlineData("")]
    public async Task MalformedSeasonFailsBeforeCreatingMatch(string season)
    {
        Assert.IsType<BadRequestObjectResult>((await Start(Request(season))).Result);
        _create.Verify(h => h.Handle(It.IsAny<CreateMatchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FullQueueCompensatesCreatedMatch()
    {
        _queue.TryEnqueue(new global::Infrastructure.Protos.SimulateMatchRequest { MatchId = "1" });
        var result = Assert.IsType<ObjectResult>((await Start()).Result);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("Failed", _match.MatchStatus);
        Assert.False(_match.IsLive);
    }

    [Fact]
    public async Task CancellationAfterCreationUsesIndependentCompensationToken()
    {
        using var cancel = new CancellationTokenSource();
        _matches.Setup(m => m.UpdateSimulationIdAsync(42, "grpc_42", It.IsAny<CancellationToken>()))
            .Returns(() => { cancel.Cancel(); throw new OperationCanceledException(cancel.Token); });
        Assert.Equal(503, Assert.IsType<ObjectResult>((await Start(token: cancel.Token)).Result).StatusCode);
        Assert.False(_match.IsLive);
        _unit.Verify(u => u.SaveChangesAsync(It.Is<CancellationToken>(t => !t.IsCancellationRequested)), Times.Once);
    }

    [Fact]
    public async Task NotificationExceptionDoesNotFailAnAcceptedSimulation()
    {
        _notification.Setup(h => h.Handle(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Notification unavailable"));
        Assert.IsType<OkObjectResult>((await Start()).Result);
        Assert.True(_match.IsLive);
        Assert.True(_queue.TryRead(out var request));
        Assert.Equal("42", request!.MatchId);
    }

    [Theory]
    [InlineData("Queued", "pending", 202)]
    [InlineData("In Progress", "running", 202)]
    [InlineData("Half Time", "running", 202)]
    [InlineData("Completed", "completed", 200)]
    [InlineData("Truncated", "failed", 200)]
    [InlineData("Failed", "failed", 200)]
    public async Task LocalStatusAndResultsUseBackendRecordsWithoutHttp(string state, string status, int resultCode)
    {
        _match.MatchStatus = state;
        _match.HomeTeamScore = 2;
        _match.AwayTeamScore = 1;
        _match.MatchEvents = new MatchEvents { TotalEvents = 8 };
        _match.ModelSimulationStartTimeUtc = DateTime.UtcNow.AddSeconds(-10);
        _match.UpdatedAt = DateTime.UtcNow;
        var statusResult = Assert.IsType<OkObjectResult>((await _controller.GetSimulationStatus("grpc_42", default, _update.Object)).Result);
        Assert.Equal(status, Assert.IsType<SimulationStatusResponse>(statusResult.Value).Status);
        var result = Assert.IsType<ObjectResult>((await _controller.GetSimulationResult("grpc_42", default,
            _notification.Object, _update.Object, Mock.Of<IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>>())).Result);
        Assert.Equal(resultCode, result.StatusCode);
        var response = Assert.IsType<SimulationResultResponse>(result.Value);
        Assert.Equal(status, response.Status);
        Assert.Equal(8, response.EventsCount);
        Assert.Equal(2, response.HomeTeamScore);
        _http.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        _unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notification.Verify(h => h.Handle(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownSimulationReturnsNotFound()
    {
        _matches.Setup(m => m.FindAsync(It.IsAny<Expression<Func<Match, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Match?)null);
        Assert.IsType<NotFoundObjectResult>((await _controller.GetSimulationStatus("grpc_999", default, _update.Object)).Result);
    }

    public void Dispose() => _provider.Dispose();
}
