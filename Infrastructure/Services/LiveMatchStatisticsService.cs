using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
///     Service for managing live match statistics with in-memory caching for optimized performance.
///     Reduces database calls during real-time event processing.
/// </summary>
public class LiveMatchStatisticsService : ILiveMatchStatisticsService
{
    private readonly IEventAnalysisService _eventAnalysisService;

    // Thread-safe cache for live matches
    private readonly ConcurrentDictionary<string, Match> _liveMatchesCache = new();
    private readonly ILogger<LiveMatchStatisticsService> _logger;
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LiveMatchStatisticsService(
        ILogger<LiveMatchStatisticsService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService,
        IEventAnalysisService eventAnalysisService)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _performanceMonitoringService = performanceMonitoringService;
        _eventAnalysisService = eventAnalysisService;
        _serviceScopeFactory = serviceScopeFactory;
        _performanceMonitoringService = performanceMonitoringService;
    }

    /// <inheritdoc />
    public async Task<Match?> PreloadMatchForLiveStatistics(string matchId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork =
                scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>(); // Record database call for performance monitoring
            _performanceMonitoringService.RecordDatabaseCall("PreloadMatch", stopwatch.Elapsed.TotalMilliseconds);

            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
            if (match == null)
            {
                _logger.LogWarning("Match with ID {MatchId} not found for preloading", matchId);
                return null;
            }

            // Cache the match for fast access during live events
            _liveMatchesCache.AddOrUpdate(matchId, match, (_, _) => match);

            _logger.LogInformation("Successfully preloaded match {MatchId} for live statistics", matchId);
            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading match {MatchId} for live statistics", matchId);
            return null;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
    public Task<Match> AddMatchToLiveStatistics(Match match)
    {

        // Add the match to the live cache
        _liveMatchesCache.AddOrUpdate(match.Id.ToString(), match, (_, _) => match);
        _logger.LogInformation("Added match {MatchId} to live statistics cache", match.Id);

        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public async Task<int> PreloadMultipleMatchesForLiveStatistics(IEnumerable<string> matchIds)
    {
        var loadedCount = 0;
        var idsList = matchIds.ToList();
        var tasks = idsList.Select(async matchId =>
        {
            var match = await PreloadMatchForLiveStatistics(matchId);
            if (match != null) Interlocked.Increment(ref loadedCount);
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Preloaded {LoadedCount} out of {RequestedCount} matches for live statistics",
            loadedCount, idsList.Count);

        return loadedCount;
    }

    /// <inheritdoc />
    public Match? GetCachedLiveMatch(string matchId)
    {
        if (_liveMatchesCache.TryGetValue(matchId, out var cachedMatch))
        {
            _performanceMonitoringService.RecordCacheHit("GetLiveMatch");
            _logger.LogDebug("Cache hit for match {MatchId}", matchId);
            return cachedMatch;
        }

        _performanceMonitoringService.RecordCacheMiss("GetLiveMatch");
        _logger.LogDebug("Cache miss for match {MatchId}", matchId);
        return null;
    }

    /// <inheritdoc />
    public Dictionary<string, Match> GetAllLiveMatches()
    {
        var liveMatches = _liveMatchesCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        _logger.LogDebug("Retrieved {Count} live matches from cache", liveMatches.Count);
        return liveMatches;
    }

    /// <summary>
    ///     Updates cached match statistics in real-time during event processing.
    ///     This method should be called when processing live events to keep cache synchronized.
    /// </summary>
    /// <param name="matchId">The match ID to update</param>
    /// <param name="updatedMatch">The updated match object with latest statistics</param>
    public void UpdateCachedMatch(string matchId, Match updatedMatch)
    {
        _liveMatchesCache.AddOrUpdate(matchId, updatedMatch, (_, _) => updatedMatch);
        _logger.LogDebug("Updated cached statistics for match {MatchId}", matchId);
    }

    /// <summary>
    ///     Removes a match from the live cache when it's no longer active.
    ///     Should be called when a match ends or is no longer being tracked.
    /// </summary>
    /// <param name="matchId">The match ID to remove from cache</param>
    public bool RemoveFromLiveCache(string matchId)
    {
        var removed = _liveMatchesCache.TryRemove(matchId, out _);
        if (removed) _logger.LogInformation("Removed match {MatchId} from live cache", matchId);

        return removed;
    }

    /// <summary>
    ///     Gets the current cache status for monitoring purposes.
    /// </summary>
    /// <returns>Cache status information</returns>
    public object GetCacheStatus()
    {
        return new
        {
            TotalCachedMatches = _liveMatchesCache.Count,
            CachedMatchIds = _liveMatchesCache.Keys.ToList(),
            MemoryEfficient = true,
            LastRefresh = DateTime.UtcNow
        };
    }

    public void UpdateCachedMatch(FootballMatchEvent matchEvent, Match updatedMatch)
    {
        // Update the match statistics based on the event
        _eventAnalysisService.UpdateMatchStatistics(matchEvent, updatedMatch);

        // Update the cache with the new statistics
        _liveMatchesCache.AddOrUpdate(updatedMatch.Id.ToString(), updatedMatch, (_, _) => updatedMatch);

        _logger.LogDebug("Updated cached statistics for match {MatchId} with event {EventId}",
            updatedMatch.Id, matchEvent.event_index);
    }
}