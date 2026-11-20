using System.Text.Json;
using Application.Dtos.V2;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Protos.V2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services;

public interface IMatchEventGrpcStreamConsumerV2
{
    Task StartConsumingMatchStreamV2Async(
        SimulateMatchV2Request request,
        string userId,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
///     Exclusive live match streaming consumer over gRPC SimulationServiceV2.
///     Routes typed Protobuf frames directly to SSE broadcasters, Redis hot state,
///     and post-match PostgreSQL persistence with zero reflection.
/// </summary>
public sealed class MatchEventGrpcStreamConsumerV2 : IMatchEventGrpcStreamConsumerV2
{
    private readonly ISimulationGrpcClientV2 _grpcClient;
    private readonly IMatchEventBroadcaster _broadcaster;
    private readonly INotificationsBroadcaster _notificationsBroadcaster;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<MatchEventGrpcStreamConsumerV2> _logger;

    public MatchEventGrpcStreamConsumerV2(
        ISimulationGrpcClientV2 grpcClient,
        IMatchEventBroadcaster broadcaster,
        INotificationsBroadcaster notificationsBroadcaster,
        IServiceScopeFactory serviceScopeFactory,
        IConnectionMultiplexer redis,
        ILogger<MatchEventGrpcStreamConsumerV2> logger
    )
    {
        _grpcClient = grpcClient;
        _broadcaster = broadcaster;
        _notificationsBroadcaster = notificationsBroadcaster;
        _serviceScopeFactory = serviceScopeFactory;
        _redis = redis;
        _logger = logger;
    }

    public async Task StartConsumingMatchStreamV2Async(
        SimulateMatchV2Request request,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var matchId = request.MatchId;
        var persistedEvents = new List<StructuredEventV2Dto>();
        TelemetrySnapshotV2Dto? lastTelemetry = null;
        string? spatialTrackKey = null;
        string? spatialTrackCodec = null;
        string? executionTier = null;
        uint homeScore = 0;
        uint awayScore = 0;
        var isNormalEnd = false;

        try
        {
            await foreach (var frame in _grpcClient.StreamMatchV2Async(request, cancellationToken))
            {
                var seq = frame.SequenceNo;
                switch (frame.Kind)
                {
                    case FrameKind.Event when frame.Event != null:
                    {
                        var evt = MapEvent(frame.Event, frame.MatchClockSeconds, frame.Period);
                        homeScore = evt.HomeScore;
                        awayScore = evt.AwayScore;
                        persistedEvents.Add(evt);
                        await _broadcaster.BroadcastStructuredEventAsync(matchId, evt, seq, cancellationToken);
                        break;
                    }
                    case FrameKind.Spatial when frame.Spatial != null:
                    {
                        var spatial = MapSpatial(frame.Spatial);
                        await _broadcaster.BroadcastSpatialFrameAsync(matchId, spatial, seq, cancellationToken);
                        await CacheSpatialFrameAsync(matchId, spatial);
                        break;
                    }
                    case FrameKind.Telemetry when frame.Telemetry != null:
                    {
                        lastTelemetry = MapTelemetry(frame.Telemetry);
                        await _broadcaster.BroadcastTelemetryAsync(matchId, lastTelemetry, seq, cancellationToken);
                        await CacheTelemetryAsync(matchId, lastTelemetry);
                        break;
                    }
                    case FrameKind.Commentary when frame.Commentary != null:
                    {
                        var cue = MapCommentary(frame.Commentary);
                        await _broadcaster.BroadcastCommentaryAsync(matchId, cue, seq, cancellationToken);
                        break;
                    }
                    case FrameKind.Lifecycle when frame.Lifecycle != null:
                    {
                        var lc = frame.Lifecycle;
                        homeScore = lc.HomeScore;
                        awayScore = lc.AwayScore;
                        if (!string.IsNullOrWhiteSpace(lc.ExecutionTier))
                            executionTier = lc.ExecutionTier;
                        if (!string.IsNullOrWhiteSpace(lc.SpatialTrackObjectKey))
                            spatialTrackKey = lc.SpatialTrackObjectKey;
                        if (!string.IsNullOrWhiteSpace(lc.SpatialTrackCodec))
                            spatialTrackCodec = lc.SpatialTrackCodec;

                        var lifecycleDto = new MatchLifecycleV2Dto(
                            matchId,
                            lc.Status,
                            frame.MatchClockSeconds,
                            frame.Period,
                            lc.HomeScore,
                            lc.AwayScore,
                            lc.Message
                        );
                        await _broadcaster.BroadcastLifecycleAsync(matchId, lifecycleDto, seq, cancellationToken);

                        if (lc.Status == "match_end")
                            isNormalEnd = true;
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("gRPC V2 stream canceled for match {MatchId}", matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming gRPC V2 stream for match {MatchId}", matchId);
        }
        finally
        {
            await PersistMatchV2Async(
                matchId,
                userId,
                homeScore,
                awayScore,
                persistedEvents,
                lastTelemetry,
                spatialTrackKey,
                spatialTrackCodec,
                executionTier,
                isNormalEnd
            );
        }
    }

    private async Task CacheTelemetryAsync(string matchId, TelemetrySnapshotV2Dto telemetry)
    {
        try
        {
            if (!_redis.IsConnected)
                return;
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(telemetry, MatchEventJsonContext.Default.TelemetrySnapshotV2Dto);
            await db.StringSetAsync($"match:v2:{matchId}:telemetry", json, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to cache V2 telemetry for match {MatchId}", matchId);
        }
    }

    private async Task CacheSpatialFrameAsync(string matchId, SpatialFrameV2Dto frame)
    {
        try
        {
            if (!_redis.IsConnected)
                return;
            var db = _redis.GetDatabase();
            var key = $"match:v2:{matchId}:spatial_ring";
            var json = JsonSerializer.Serialize(frame, MatchEventJsonContext.Default.SpatialFrameV2Dto);
            await db.ListLeftPushAsync(key, json);
            await db.ListTrimAsync(key, 0, 29);
            await db.KeyExpireAsync(key, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to cache V2 spatial ring for match {MatchId}", matchId);
        }
    }

    private async Task PersistMatchV2Async(
        string matchId,
        string userId,
        uint homeScore,
        uint awayScore,
        List<StructuredEventV2Dto> events,
        TelemetrySnapshotV2Dto? telemetry,
        string? spatialTrackKey,
        string? spatialTrackCodec,
        string? executionTier,
        bool isNormalEnd
    )
    {
        if (!int.TryParse(matchId, out var numericMatchId))
            return;

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(numericMatchId);
            if (match == null)
                return;

            match.HomeTeamScore = (int)homeScore;
            match.AwayTeamScore = (int)awayScore;
            match.IsLive = false;
            match.MatchStatus = isNormalEnd ? "Completed" : "Failed";
            match.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(spatialTrackKey))
                match.SpatialTrackObjectKey = spatialTrackKey;
            if (!string.IsNullOrWhiteSpace(spatialTrackCodec))
                match.SpatialTrackCodec = spatialTrackCodec;
            if (!string.IsNullOrWhiteSpace(executionTier))
                match.ModelVersion = $"PixelPitch-EventTF-112M/{executionTier}";

            if (telemetry != null)
            {
                match.HomeXG = telemetry.HomeXg;
                match.AwayXG = telemetry.AwayXg;
                match.HomeXT = telemetry.HomeXt;
                match.AwayXT = telemetry.AwayXt;

                var stats = match.MatchStatistics ??= new MatchStatistics { MatchId = numericMatchId };
                stats.HomeTeamXG = telemetry.HomeXg;
                stats.AwayTeamXG = telemetry.AwayXg;
                stats.HomeTeamPSXG = telemetry.HomePsxg;
                stats.AwayTeamPSXG = telemetry.AwayPsxg;
                stats.HomeTeamXT = telemetry.HomeXt;
                stats.AwayTeamXT = telemetry.AwayXt;
                stats.HomeTeamPPDA = telemetry.HomePpda;
                stats.AwayTeamPPDA = telemetry.AwayPpda;
                stats.HomeTeamFieldTilt = telemetry.HomeFieldTiltPct;
                stats.AwayTeamFieldTilt = 100.0 - telemetry.HomeFieldTiltPct;
                stats.HomeTeamShots = (int)telemetry.HomeShots;
                stats.AwayTeamShots = (int)telemetry.AwayShots;
                stats.HomeTeamShotsOnTarget = (int)telemetry.HomeShotsOnTarget;
                stats.AwayTeamShotsOnTarget = (int)telemetry.AwayShotsOnTarget;
                stats.HomeTeamPasses = (int)telemetry.HomePasses;
                stats.AwayTeamPasses = (int)telemetry.AwayPasses;
                stats.HomeTeamPassAccuracy = telemetry.HomePassAccuracy;
                stats.AwayTeamPassAccuracy = telemetry.AwayPassAccuracy;
                stats.HomeTeamPossession = (int)Math.Round(telemetry.HomePossessionPct);
                stats.AwayTeamPossession = 100 - stats.HomeTeamPossession;
            }

            if (events.Count > 0)
            {
                var eventsEntity = match.MatchEvents ??= new MatchEvents { MatchId = numericMatchId };
                eventsEntity.EventsJson = JsonSerializer.Serialize(
                    events,
                    MatchEventJsonContext.Default.ListStructuredEventV2Dto
                );
                eventsEntity.TotalEvents = events.Count;
                eventsEntity.GoalsHomeTeam = (int)homeScore;
                eventsEntity.GoalsAwayTeam = (int)awayScore;
                eventsEntity.LastUpdated = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _notificationsBroadcaster.BroadcastNotificationAsync(
                    userId,
                    new NotificationV2Dto(
                        Guid.NewGuid().ToString(),
                        userId,
                        isNormalEnd ? "match_completed" : "notification",
                        isNormalEnd ? "Match Simulation Completed" : "Match Simulation Failed",
                        isNormalEnd
                            ? $"Match #{matchId} simulation finished ({homeScore}-{awayScore})."
                            : $"Match #{matchId} simulation did not complete cleanly.",
                        DateTime.UtcNow,
                        MatchId: matchId
                    )
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist V2 match {MatchId}", matchId);
        }
    }

    private static StructuredEventV2Dto MapEvent(StructuredEventV2 e, uint clockSeconds, uint period) =>
        new(
            e.EventIndex,
            e.PossessionPhaseId,
            e.PossessingTeamSide,
            e.ActorSlot,
            e.RecipientSlot,
            e.PlayerId,
            e.PlayerName,
            e.ActionType,
            e.SubType,
            e.Outcome,
            e.StartX,
            e.StartY,
            e.EndX,
            e.EndY,
            e.EndZ,
            e.UnderPressure,
            e.BodyPart,
            e.PassHeight,
            e.Card,
            e.HomeScore,
            e.AwayScore,
            e.Xg,
            e.PostShotXg,
            e.Xpass,
            e.XtDelta,
            e.VaepValue,
            e.TurnoverRisk,
            clockSeconds,
            period
        );

    private static SpatialFrameV2Dto MapSpatial(PitchSpatialFrameV2 s)
    {
        var players = new List<PlayerSpatialStateV2Dto>(s.Players.Count);
        foreach (var p in s.Players)
        {
            players.Add(
                new PlayerSpatialStateV2Dto(
                    p.Slot,
                    p.PlayerId,
                    p.X,
                    p.Y,
                    p.Vx,
                    p.Vy,
                    p.FacingRad,
                    p.Stamina,
                    p.HasBall,
                    p.IsSentOff
                )
            );
        }
        return new SpatialFrameV2Dto(
            s.SimTimestampMs,
            s.BallX,
            s.BallY,
            s.BallZ,
            s.HomeVoronoiControl,
            players
        );
    }

    private static TelemetrySnapshotV2Dto MapTelemetry(TelemetrySnapshotV2 t) =>
        new(
            t.HomeXg,
            t.AwayXg,
            t.HomePsxg,
            t.AwayPsxg,
            t.HomeXt,
            t.AwayXt,
            t.HomePossessionPct,
            t.HomePpda,
            t.AwayPpda,
            t.HomeFieldTiltPct,
            t.MomentumIndex,
            t.HomeShots,
            t.AwayShots,
            t.HomeShotsOnTarget,
            t.AwayShotsOnTarget,
            t.HomePasses,
            t.AwayPasses,
            t.HomePassAccuracy,
            t.AwayPassAccuracy,
            t.HomeAvgXpass,
            t.AwayAvgXpass,
            t.HomeHighTurnovers,
            t.AwayHighTurnovers
        );

    private static CommentaryCueV2Dto MapCommentary(CommentaryCueV2 c) =>
        new(
            c.AnchorEventIndex,
            c.Tier,
            c.Headline,
            c.Body,
            c.SalienceScore,
            c.Language,
            c.ProviderId,
            c.ModelVersion,
            c.LatencyMs,
            c.VerifierPassed,
            c.FallbackUsed
        );
}
