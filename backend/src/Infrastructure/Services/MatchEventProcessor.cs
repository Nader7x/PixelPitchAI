using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IMatchEventProcessor
{
    Task<Match?> GetOrLoadMatchEntityAsync(string matchId);
    (int Home, int Away) GetOrInitMatchScore(string matchId, Match? matchEntity);
    void UpdateMatchScore(string matchId, int homeScore, int awayScore);
    Task ProcessEventAsync(FootballMatchEvent matchEvent, Match matchEntity, CancellationToken cancellationToken = default);
    Task FlushAndPersistMatchAsync(string matchId, bool isNormalEnd = true, CancellationToken cancellationToken = default);
    IReadOnlyList<FootballMatchEvent> GetCachedEvents(string matchId);
    void ClearMatch(string matchId);
}

/// <summary>
///     Unified match event processing layer shared between Pipeline A (RabbitMQ) and Pipeline B (direct gRPC).
///     Encapsulates entity caching, score tracking, statistics calculations, SSE/SignalR broadcasting,
///     and batch database persistence.
/// </summary>
public sealed class MatchEventProcessor : IMatchEventProcessor
{
    private readonly ConcurrentDictionary<string, Match> _loadedMatches = new();
    private readonly ConcurrentDictionary<string, (int Home, int Away)> _matchScores = new();
    private readonly ConcurrentDictionary<string, List<FootballMatchEvent>> _matchEventsCache = new();
    private readonly object _matchEventsCacheLock = new();

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly ILiveMatchStatisticsService _liveMatchStatisticsService;
    private readonly IMatchEventBroadcaster? _broadcaster;
    private readonly IHubContext<MatchHub, IMatchHub>? _hubContext;
    private readonly ILogger<MatchEventProcessor> _logger;

    public MatchEventProcessor(
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService,
        ILiveMatchStatisticsService liveMatchStatisticsService,
        ILogger<MatchEventProcessor> logger,
        IMatchEventBroadcaster? broadcaster = null,
        IHubContext<MatchHub, IMatchHub>? hubContext = null)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _performanceMonitoringService = performanceMonitoringService;
        _liveMatchStatisticsService = liveMatchStatisticsService;
        _logger = logger;
        _broadcaster = broadcaster;
        _hubContext = hubContext;
    }

    public async Task<Match?> GetOrLoadMatchEntityAsync(string matchId)
    {
        if (_loadedMatches.TryGetValue(matchId, out var cachedMatch))
            return cachedMatch;

        if (!int.TryParse(matchId, out var parsedId) || parsedId <= 0)
            return null;

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stopwatch = Stopwatch.StartNew();
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(parsedId);
            stopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "LoadMatchEntity",
                stopwatch.Elapsed.TotalMilliseconds
            );

            if (match != null)
            {
                _loadedMatches.TryAdd(matchId, match);
                return match;
            }

            _logger.LogWarning("Match with ID {Id} not found", matchId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading match entity for match {Id}", matchId);
            return null;
        }
    }

    public (int Home, int Away) GetOrInitMatchScore(string matchId, Match? matchEntity)
    {
        return _matchScores.GetOrAdd(
            matchId,
            _ => (matchEntity?.HomeTeamScore ?? 0, matchEntity?.AwayTeamScore ?? 0)
        );
    }

    public void UpdateMatchScore(string matchId, int homeScore, int awayScore)
    {
        _matchScores[matchId] = (homeScore, awayScore);
    }

    public async Task ProcessEventAsync(FootballMatchEvent matchEvent, Match matchEntity, CancellationToken cancellationToken = default)
    {
        if (matchEvent.match_id == null)
            return;

        if (matchEvent.Score != null)
        {
            matchEntity.HomeTeamScore = matchEvent.Score.Home;
            matchEntity.AwayTeamScore = matchEvent.Score.Away;
            UpdateMatchScore(matchEvent.match_id, matchEvent.Score.Home, matchEvent.Score.Away);
        }

        // 1. Scoped match statistics update
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();
            var matchEventsEntity = matchEntity.MatchEvents ??= new MatchEvents
            {
                MatchId = int.Parse(matchEvent.match_id),
                EventsJson = "[]",
                LastUpdated = DateTime.UtcNow,
                TotalEvents = 0,
            };

            await eventAnalysis.UpdateMatchStatistics(
                matchEvent,
                matchEventsEntity,
                matchEntity,
                false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing match event {Index} for match {Id}",
                matchEvent.event_index,
                matchEvent.match_id
            );
        }

        // 2. Cache event in memory for batch persistence
        lock (_matchEventsCacheLock)
        {
            if (!_matchEventsCache.TryGetValue(matchEvent.match_id, out var eventsList))
            {
                eventsList = [];
                _matchEventsCache[matchEvent.match_id] = eventsList;
            }
            eventsList.Add(matchEvent);
        }

        // 3. Broadcast event to SSE and SignalR
        try
        {
            if (_broadcaster != null)
                await _broadcaster.BroadcastEventAsync(matchEvent.match_id, matchEvent, cancellationToken);

            if (_hubContext != null && int.TryParse(matchEvent.match_id, out var parsedId))
            {
                await _hubContext
                    .Clients.Group(matchEvent.match_id)
                    .SendMatchEventAsync("match_event", parsedId, matchEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting event for match {Id}", matchEvent.match_id);
        }

        // 4. Broadcast statistics if significant event
        if (IsSignificantEvent(matchEvent))
        {
            await BroadcastMatchStatisticsAsync(matchEvent, matchEntity, cancellationToken);
            await _liveMatchStatisticsService.AddMatchToLiveStatistics(matchEntity);
        }
    }

    public async Task FlushAndPersistMatchAsync(string matchId, bool isNormalEnd = true, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            List<FootballMatchEvent>? events;
            lock (_matchEventsCacheLock)
            {
                _matchEventsCache.Remove(matchId, out events);
            }

            if (!int.TryParse(matchId, out var parsedMatchId))
            {
                ClearMatch(matchId);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();

            var dbStopwatch = Stopwatch.StartNew();
            var matchToUpdate = await unitOfWork.Matches.GetByIdWithDetailsAsync(parsedMatchId);
            dbStopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "GetMatchWithDetails_SaveEvents",
                dbStopwatch.Elapsed.TotalMilliseconds
            );

            if (matchToUpdate == null)
            {
                _logger.LogWarning("Match {Id} not found for saving events.", matchId);
                ClearMatch(matchId);
                return;
            }

            matchToUpdate.IsLive = false;
            matchToUpdate.MatchStatus = isNormalEnd ? "Completed" : "Truncated";

            if (events != null && events.Count > 0)
            {
                var matchEventsEntity = matchToUpdate.MatchEvents;
                if (matchEventsEntity == null)
                {
                    matchEventsEntity = new MatchEvents
                    {
                        MatchId = parsedMatchId,
                        EventsJson = "[]",
                        LastUpdated = DateTime.UtcNow,
                    };
                    await unitOfWork.MatchEvents.AddAsync(matchEventsEntity);
                    matchToUpdate.MatchEvents = matchEventsEntity;
                }

                foreach (var ev in events.OrderBy(e => e.time_seconds))
                {
                    await eventAnalysis.UpdateMatchStatistics(ev, matchEventsEntity, matchToUpdate);
                }

                var lastScoreEvent = events.LastOrDefault(e => e.Score != null);
                if (lastScoreEvent?.Score != null)
                {
                    matchToUpdate.HomeTeamScore = lastScoreEvent.Score.Home;
                    matchToUpdate.AwayTeamScore = lastScoreEvent.Score.Away;
                }

                matchEventsEntity.SetEvents(events);
                matchEventsEntity.TotalEvents = events.Count;
                matchEventsEntity.LastUpdated = DateTime.UtcNow;
            }

            var saveStopwatch = Stopwatch.StartNew();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            saveStopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "SaveChanges_MatchEvents",
                saveStopwatch.Elapsed.TotalMilliseconds
            );

            _logger.LogInformation(
                "Saved {Count} events for match {Id} to DB in {Ms}ms (Status: {Status})",
                events?.Count ?? 0,
                matchId,
                stopwatch.Elapsed.TotalMilliseconds,
                matchToUpdate.MatchStatus
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving events for match {Id}", matchId);
        }
        finally
        {
            stopwatch.Stop();
            ClearMatch(matchId);
            _broadcaster?.ClearReplayBuffer(matchId);
        }
    }

    public IReadOnlyList<FootballMatchEvent> GetCachedEvents(string matchId)
    {
        lock (_matchEventsCacheLock)
        {
            return _matchEventsCache.TryGetValue(matchId, out var list)
                ? [.. list]
                : [];
        }
    }

    public void ClearMatch(string matchId)
    {
        _loadedMatches.TryRemove(matchId, out _);
        _matchScores.TryRemove(matchId, out _);
        lock (_matchEventsCacheLock)
        {
            _matchEventsCache.TryRemove(matchId, out _);
        }
    }

    private async Task BroadcastMatchStatisticsAsync(FootballMatchEvent matchEvent, Match matchEntity, CancellationToken cancellationToken)
    {
        try
        {
            if (matchEntity.MatchStatistics == null)
                return;

            var matchStatistics = new
            {
                matchId = matchEntity.Id,
                timeStamp = matchEvent.timestamp,
                homeTeam = new
                {
                    name = matchEntity.HomeTeam?.Name ?? matchEntity.HomeTeamInMatchName,
                    score = matchEntity.HomeTeamScore ?? 0,
                    shots = matchEntity.MatchStatistics.HomeTeamShots ?? 0,
                    shotsOnTarget = matchEntity.MatchStatistics.HomeTeamShotsOnTarget ?? 0,
                    possession = matchEntity.MatchStatistics.HomeTeamPossession ?? 0,
                    passes = matchEntity.MatchStatistics.HomeTeamPasses ?? 0,
                    passAccuracy = matchEntity.MatchStatistics.HomeTeamPassAccuracy ?? 0,
                    corners = matchEntity.MatchStatistics.HomeTeamCorners ?? 0,
                    fouls = matchEntity.MatchStatistics.HomeTeamFouls ?? 0,
                    yellowCards = matchEntity.MatchStatistics.HomeTeamYellowCards ?? 0,
                    redCards = matchEntity.MatchStatistics.HomeTeamRedCards ?? 0,
                    offsides = matchEntity.MatchStatistics.HomeTeamOffsides ?? 0,
                },
                awayTeam = new
                {
                    name = matchEntity.AwayTeam?.Name ?? matchEntity.AwayTeamInMatchName,
                    score = matchEntity.AwayTeamScore ?? 0,
                    shots = matchEntity.MatchStatistics.AwayTeamShots ?? 0,
                    shotsOnTarget = matchEntity.MatchStatistics.AwayTeamShotsOnTarget ?? 0,
                    possession = matchEntity.MatchStatistics.AwayTeamPossession ?? 0,
                    passes = matchEntity.MatchStatistics.AwayTeamPasses ?? 0,
                    passAccuracy = matchEntity.MatchStatistics.AwayTeamPassAccuracy ?? 0,
                    corners = matchEntity.MatchStatistics.AwayTeamCorners ?? 0,
                    fouls = matchEntity.MatchStatistics.AwayTeamFouls ?? 0,
                    yellowCards = matchEntity.MatchStatistics.AwayTeamYellowCards ?? 0,
                    redCards = matchEntity.MatchStatistics.AwayTeamRedCards ?? 0,
                    offsides = matchEntity.MatchStatistics.AwayTeamOffsides ?? 0,
                },
                matchInfo = new
                {
                    status = matchEntity.MatchStatus,
                    isLive = matchEntity.IsLive,
                    currentMinute = matchEvent.minute,
                    lastEventTime = matchEvent.time_seconds,
                    eventType = matchEvent.action,
                    eventTeam = matchEvent.team,
                },
                lastUpdated = DateTime.UtcNow,
            };

            if (_broadcaster != null)
                await _broadcaster.BroadcastStatisticsAsync(matchEntity.Id.ToString(), matchStatistics, cancellationToken);

            if (_hubContext != null)
            {
                await _hubContext
                    .Clients.Group($"MatchStatistics-{matchEntity.Id}")
                    .SendMatchStatisticsAsync(
                        "match_statistics_update",
                        matchEntity.Id,
                        matchStatistics
                    );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting match stats for match {Id}", matchEntity.Id);
        }
    }

    private static bool IsSignificantEvent(FootballMatchEvent? matchEvent)
    {
        if (matchEvent is null)
            return false;
        var action = matchEvent.action.ToLowerInvariant();
        var eventType = matchEvent.event_type?.ToLowerInvariant();
        var type = matchEvent.type?.ToLowerInvariant();

        if (type is "kick off" or "free kick" or "corner")
            return true;

        if (
            action.Contains("card")
            || action.Contains("yellow")
            || action.Contains("red")
            || eventType == "card"
            || eventType == "yellow_card"
            || eventType == "red_card"
        )
            return true;
        if (matchEvent is { long_pass: not null and true })
            return true;

        if (
            action.Contains("substitution")
            || action.Contains("sub")
            || eventType == "substitution"
        )
            return true;

        if (
            action
            is "match_start"
                or "match_end"
                or "first_half_end"
                or "second_half_start"
                or "stoppage_time_start"
        )
            return true;

        if (
            action.Contains("penalty")
            || eventType == "penalty"
            || matchEvent.outcome?.ToLowerInvariant() == "penalty"
        )
            return true;

        if (action.Contains("own") && action.Contains("goal"))
            return true;

        return eventType switch
        {
            "shot"
            or "duel"
            or "foul committed"
            or "foul won"
            or "dribble"
            or "interception"
            or "clearance"
            or "block"
            or "carry"
            or "ball_recovery"
            or "ball recovery" => true,
            _ => false,
        };
    }
}
