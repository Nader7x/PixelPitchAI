using System.Runtime.CompilerServices;
using Application.Dtos.V2;
using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.Protos.V2;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventGrpcStreamConsumerV2Tests
{
    private readonly Mock<ISimulationGrpcClientV2> _mockGrpcClient = new();
    private readonly Mock<IMatchEventBroadcaster> _mockBroadcaster = new();
    private readonly Mock<INotificationsBroadcaster> _mockNotifications = new();
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory = new();
    private readonly Mock<IServiceScope> _mockScope = new();
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IMatchRepository> _mockMatchRepo = new();
    private readonly Mock<IConnectionMultiplexer> _mockRedis = new();
    private readonly MatchEventGrpcStreamConsumerV2 _consumer;

    public MatchEventGrpcStreamConsumerV2Tests()
    {
        _mockUnitOfWork.Setup(u => u.Matches).Returns(_mockMatchRepo.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockRedis.Setup(r => r.IsConnected).Returns(false);

        _consumer = new MatchEventGrpcStreamConsumerV2(
            _mockGrpcClient.Object,
            _mockBroadcaster.Object,
            _mockNotifications.Object,
            _mockScopeFactory.Object,
            _mockRedis.Object,
            NullLogger<MatchEventGrpcStreamConsumerV2>.Instance
        );
    }

    [Fact]
    public async Task StartConsumingMatchStreamV2Async_BroadcastsAllFrameKindsAndPersistsCompletedMatch()
    {
        var match = new Match { Id = 42, CreatorId = "user-42", IsLive = true, MatchStatus = "SimulatingInProgress" };
        _mockMatchRepo.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(match);

        var request = new SimulateMatchV2Request { MatchId = "42" };

        async IAsyncEnumerable<MatchStreamFrameV2> GenerateFrames([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new MatchStreamFrameV2
            {
                SequenceNo = 1,
                MatchClockSeconds = 120,
                Period = 1,
                Kind = FrameKind.Event,
                Event = new StructuredEventV2
                {
                    EventIndex = 1,
                    PossessionPhaseId = 1,
                    PossessingTeamSide = 1,
                    ActorSlot = 10,
                    RecipientSlot = 9,
                    PlayerId = 1001,
                    PlayerName = "Pedri",
                    ActionType = "shot",
                    SubType = "OpenPlay",
                    Outcome = "Goal",
                    HomeScore = 1,
                    AwayScore = 0,
                    Xg = 0.42f,
                },
            };

            yield return new MatchStreamFrameV2
            {
                SequenceNo = 2,
                MatchClockSeconds = 121,
                Period = 1,
                Kind = FrameKind.Spatial,
                Spatial = new PitchSpatialFrameV2
                {
                    SimTimestampMs = 121000,
                    BallX = 95f,
                    BallY = 34f,
                    BallZ = 1.2f,
                    HomeVoronoiControl = 0.58f,
                    Players = { new PlayerSpatialStateV2 { Slot = 10, PlayerId = 1001, X = 90f, Y = 33f, HasBall = true } },
                },
            };

            yield return new MatchStreamFrameV2
            {
                SequenceNo = 3,
                MatchClockSeconds = 122,
                Period = 1,
                Kind = FrameKind.Telemetry,
                Telemetry = new TelemetrySnapshotV2
                {
                    HomeXg = 0.42f,
                    AwayXg = 0.11f,
                    HomeXt = 0.65f,
                    AwayXt = 0.21f,
                    HomePossessionPct = 60f,
                    HomeShots = 4,
                    AwayShots = 2,
                },
            };

            yield return new MatchStreamFrameV2
            {
                SequenceNo = 4,
                MatchClockSeconds = 123,
                Period = 1,
                Kind = FrameKind.Commentary,
                Commentary = new CommentaryCueV2
                {
                    AnchorEventIndex = 1,
                    Tier = "goal",
                    Headline = "Goal!",
                    Body = "Clinical finish!",
                    Language = "en",
                    VerifierPassed = true,
                },
            };

            yield return new MatchStreamFrameV2
            {
                SequenceNo = 5,
                MatchClockSeconds = 5400,
                Period = 2,
                Kind = FrameKind.Lifecycle,
                Lifecycle = new MatchLifecycleV2
                {
                    Status = "match_end",
                    HomeScore = 1,
                    AwayScore = 0,
                    ExecutionTier = "local_cuda",
                    SpatialTrackObjectKey = "matches/42/spatial.zst",
                    SpatialTrackCodec = "zstd-f32-v2",
                    Message = "Full time",
                },
            };

            await Task.CompletedTask;
        }

        _mockGrpcClient
            .Setup(c => c.StreamMatchV2Async(request, It.IsAny<CancellationToken>()))
            .Returns(GenerateFrames());

        await _consumer.StartConsumingMatchStreamV2Async(request, "user-42", CancellationToken.None);

        _mockBroadcaster.Verify(
            b => b.BroadcastStructuredEventAsync("42", It.Is<StructuredEventV2Dto>(e => e.PlayerName == "Pedri" && e.HomeScore == 1), 1, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockBroadcaster.Verify(
            b => b.BroadcastSpatialFrameAsync("42", It.Is<SpatialFrameV2Dto>(s => s.Players.Count == 1), 2, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockBroadcaster.Verify(
            b => b.BroadcastTelemetryAsync("42", It.Is<TelemetrySnapshotV2Dto>(t => t.HomeXg == 0.42f), 3, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockBroadcaster.Verify(
            b => b.BroadcastCommentaryAsync("42", It.Is<CommentaryCueV2Dto>(c => c.Headline == "Goal!"), 4, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockBroadcaster.Verify(
            b => b.BroadcastLifecycleAsync("42", It.Is<MatchLifecycleV2Dto>(l => l.Status == "match_end"), 5, It.IsAny<CancellationToken>()),
            Times.Once
        );

        match.MatchStatus.Should().Be("Completed");
        match.IsLive.Should().BeFalse();
        match.HomeTeamScore.Should().Be(1);
        match.AwayTeamScore.Should().Be(0);
        match.SpatialTrackObjectKey.Should().Be("matches/42/spatial.zst");
        match.SpatialTrackCodec.Should().Be("zstd-f32-v2");
        match.ModelVersion.Should().Be("PixelPitch-EventTF-112M/local_cuda");
        match.MatchEvents.Should().NotBeNull();
        match.MatchEvents!.TotalEvents.Should().Be(1);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifications.Verify(
            n => n.BroadcastNotificationAsync("user-42", It.Is<NotificationV2Dto>(dto => dto.Type == "match_completed" && dto.MatchId == "42"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task StartConsumingMatchStreamV2Async_OnStreamError_PersistsFailedMatchAndNotifiesUser()
    {
        var match = new Match { Id = 99, CreatorId = "user-99", IsLive = true, MatchStatus = "SimulatingInProgress" };
        _mockMatchRepo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync(match);

        var request = new SimulateMatchV2Request { MatchId = "99" };

        async IAsyncEnumerable<MatchStreamFrameV2> FailingStream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new MatchStreamFrameV2
            {
                SequenceNo = 1,
                MatchClockSeconds = 10,
                Period = 1,
                Kind = FrameKind.Lifecycle,
                Lifecycle = new MatchLifecycleV2 { Status = "match_start", HomeScore = 0, AwayScore = 0 },
            };
            await Task.CompletedTask;
            throw new InvalidOperationException("gRPC stream disconnected");
        }

        _mockGrpcClient
            .Setup(c => c.StreamMatchV2Async(request, It.IsAny<CancellationToken>()))
            .Returns(FailingStream());

        await _consumer.StartConsumingMatchStreamV2Async(request, "user-99", CancellationToken.None);

        match.MatchStatus.Should().Be("Failed");
        match.IsLive.Should().BeFalse();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifications.Verify(
            n => n.BroadcastNotificationAsync("user-99", It.Is<NotificationV2Dto>(dto => dto.Type == "notification" && dto.Title == "Match Simulation Failed"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
