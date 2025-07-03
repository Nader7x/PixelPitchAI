using Application.Interfaces;

namespace Footex.Helpers;

/// <summary>
///     Helper class for managing Redis cache operations consistently across the application
/// </summary>
public class CacheHelper
{
    private readonly ICacheService _cacheService;

    public CacheHelper(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    ///     Invalidates match-related caches when match data is updated
    /// </summary>
    /// <param name="matchId">The ID of the match being updated</param>
    public async Task InvalidateMatchCaches(int matchId)
    {
        // Remove specific match caches
        await _cacheService.RemoveAsync($"match_{matchId}");
        await _cacheService.RemoveAsync($"match_details_{matchId}");

        // Pattern-based invalidation for listings that might include this match
        await _cacheService.RemoveAsync("matches_all_*");

        // Also invalidate any user matches that might include this match
        // Using a wildcard pattern for user match lists
        await _cacheService.RemoveAsync("user_matches_*");
    }

    /// <summary>
    ///     Invalidates all team-related caches
    /// </summary>
    /// <param name="teamId">The ID of the team being updated</param>
    public async Task InvalidateTeamCaches(int teamId)
    {
        await _cacheService.RemoveAsync($"team_{teamId}");
        await _cacheService.RemoveAsync("teams_all_*");
        await _cacheService.RemoveAsync($"team_seasons_{teamId}");
    }

    /// <summary>
    ///     Invalidates all player-related caches
    /// </summary>
    /// <param name="playerId">The ID of the player being updated</param>
    public async Task InvalidatePlayerCaches(int playerId)
    {
        await _cacheService.RemoveAsync($"player_{playerId}");
        await _cacheService.RemoveAsync("players_all_*");
    }

    /// <summary>
    ///     Invalidates all season-related caches
    /// </summary>
    /// <param name="seasonId">The ID of the season being updated</param>
    public async Task InvalidateSeasonCaches(int seasonId)
    {
        await _cacheService.RemoveAsync($"season_{seasonId}");
        await _cacheService.RemoveAsync($"season_teams_{seasonId}");
        await _cacheService.RemoveAsync("seasons_all_*");
    }

    /// <summary>
    ///     Invalidates all stadium-related caches
    /// </summary>
    /// <param name="stadiumId">The ID of the stadium being updated</param>
    public async Task InvalidateStadiumCaches(int stadiumId)
    {
        await _cacheService.RemoveAsync($"stadium_{stadiumId}");
        await _cacheService.RemoveAsync("stadiums_all_*");
    }

    /// <summary>
    ///     Invalidates all coach-related caches
    /// </summary>
    /// <param name="coachId">The ID of the coach being updated</param>
    public async Task InvalidateCoachCaches(int coachId)
    {
        await _cacheService.RemoveAsync($"coach_{coachId}");
        await _cacheService.RemoveAsync("coaches_all_*");
    }
}
