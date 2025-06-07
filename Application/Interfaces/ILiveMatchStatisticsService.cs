using Domain.Models;

namespace Application.Interfaces;

/// <summary>
///     Service interface for managing live match statistics and real-time match data caching.
/// </summary>
public interface ILiveMatchStatisticsService
{
    /// <summary>
    ///     Preloads match data into the live statistics cache to optimize subsequent event processing.
    ///     This should be called when a match starts or when the first event is received.
    /// </summary>
    /// <param name="matchId">The match ID to preload</param>
    /// <returns>The loaded match object, or null if not found</returns>
    Task<Match?> PreloadMatchForLiveStatistics(string matchId);

    /// <summary>
    ///     Preloads multiple matches for live statistics tracking.
    ///     Useful for preparing matches that are about to start.
    /// </summary>
    /// <param name="matchIds">Collection of match IDs to preload</param>
    /// <returns>Number of successfully preloaded matches</returns>
    Task<int> PreloadMultipleMatchesForLiveStatistics(IEnumerable<string> matchIds);

    /// <summary>
    ///     Gets cached live match statistics without triggering database calls.
    ///     Returns null if the match is not currently cached.
    /// </summary>
    /// <param name="matchId">The match ID to retrieve</param>
    /// <returns>Cached match object or null</returns>
    Match? GetCachedLiveMatch(string matchId);

    /// <summary>
    ///     Gets all currently live matches being tracked for real-time statistics.
    /// </summary>
    /// <returns>Dictionary of live matches with their current statistics</returns>
    Dictionary<string, Match> GetAllLiveMatches();

    /// <summary>
    ///     Updates cached match statistics in real-time during event processing.
    ///     This method should be called when processing live events to keep cache synchronized.
    /// </summary>
    /// <param name="matchId">The match ID to update</param>
    /// <param name="updatedMatch">The updated match object with latest statistics</param>
    void UpdateCachedMatch(string matchId, Match updatedMatch);

    /// <summary>
    ///     Updates the cached match statistics for a specific match based on a match event.
    ///     This method should be called when processing live events to keep the cache synchronized.
    /// </summary>
    /// <param name="matchEvent">The match event that triggered the update</param>
    /// <param name="updatedMatch">The updated match object with latest statistics</param>
    void UpdateCachedMatch(FootballMatchEvent matchEvent, Match updatedMatch);

    /// <summary>
    ///     Removes a match from the live cache when it's no longer active.
    ///     Should be called when a match ends or is no longer being tracked.
    /// </summary>
    /// <param name="matchId">The match ID to remove from cache</param>
    bool RemoveFromLiveCache(string matchId);

    /// <summary>
    ///     Gets the current cache status for monitoring purposes.
    /// </summary>
    /// <returns>Cache status information</returns>
    object GetCacheStatus();
}