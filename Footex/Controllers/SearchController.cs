using Application.DTOs;
using Application.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController(IAdvancedSearchService advancedSearchService) : ControllerBase
{
    /// <summary>
    ///     Global search across all entities with ranking and relevance
    /// </summary>
    /// <param name="query">Search term (minimum 2 characters)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page (1-50)</param>
    /// <returns>Search results with pagination information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Search query must be at least 2 characters long");

        if (page < 1) page = 1;

        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var results = await advancedSearchService.SearchAsync(query, page, pageSize);
        return Ok(results);
    }

    /// <summary>
    ///     Advanced search with strategy selection and fuzzy matching
    /// </summary>
    /// <param name="query">Search term (minimum 2 characters)</param>
    /// <param name="strategy">Search strategy to use</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page (1-50)</param>
    /// <returns>Search results with pagination information</returns>
    [HttpGet("strategy")]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchWithStrategy(
        [FromQuery] string query,
        [FromQuery] SearchStrategy strategy = SearchStrategy.Auto,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Search query must be at least 2 characters long");

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var results = await advancedSearchService.SearchWithStrategyAsync(query, strategy, page, pageSize);
        return Ok(results);
    }

    /// <summary>
    ///     Advanced search with comprehensive filtering capabilities
    /// </summary>
    /// <param name="filters">Search filters and criteria</param>
    /// <returns>Filtered search results</returns>
    [HttpPost("filtered")]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchWithFilters([FromBody] SearchFiltersDto filters)
    {
        if (string.IsNullOrWhiteSpace(filters.Query) || filters.Query.Length < 2)
            return BadRequest("Search query must be at least 2 characters long");

        if (filters.Page < 1) filters.Page = 1;
        if (filters.PageSize < 1 || filters.PageSize > 50) filters.PageSize = 10;

        var results = await advancedSearchService.SearchWithFiltersAsync(filters);
        return Ok(results);
    }

    /// <summary>
    ///     Unified search across multiple entity types with ranking
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="entityTypes">Entity types to search (comma-separated)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Results per page</param>
    /// <returns>Unified search results</returns>
    [HttpGet("unified")]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UnifiedSearch(
        [FromQuery] string query,
        [FromQuery] string? entityTypes = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Search query must be at least 2 characters long");

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var entityTypesList = string.IsNullOrWhiteSpace(entityTypes)
            ? null
            : entityTypes.Split(',').Select(t => t.Trim()).ToList();

        var results = await advancedSearchService.UnifiedSearchAsync(query, entityTypesList, page, pageSize);
        return Ok(results);
    }

    /// <summary>
    ///     Advanced search with fuzzy matching capabilities across all entities
    /// </summary>
    /// <param name="query">Search term (minimum 2 characters)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <returns>Search results with pagination information</returns>
    [HttpGet("all")]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchAll(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool enableFuzzySearch = false)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Search query must be at least 2 characters long");

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var results = await advancedSearchService.SearchAllAsync(query, page, pageSize, enableFuzzySearch);
        return Ok(results);
    }

    /// <summary>
    ///     Search for teams with advanced ranking and relevance scoring
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <param name="advanced">Use advanced ranking algorithm</param>
    /// <returns>List of matching teams</returns>
    [HttpGet("teams")]
    [ProducesResponseType(typeof(List<Team>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchTeams(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false,
        [FromQuery] bool advanced = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var teams = advanced
            ? await advancedSearchService.SearchTeamsWithAdvancedRankingAsync(query, limit)
            : await advancedSearchService.SearchTeamsAsync(query, limit, enableFuzzySearch);

        return Ok(teams);
    }

    /// <summary>
    ///     Search for players with advanced ranking and detailed filtering
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <param name="advanced">Use advanced ranking algorithm</param>
    /// <returns>List of matching players</returns>
    [HttpGet("players")]
    [ProducesResponseType(typeof(List<Player>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchPlayers(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false,
        [FromQuery] bool advanced = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var players = advanced
            ? await advancedSearchService.SearchPlayersWithAdvancedRankingAsync(query, limit)
            : await advancedSearchService.SearchPlayersAsync(query, limit, enableFuzzySearch);

        return Ok(players);
    }

    /// <summary>
    ///     Search for coaches with advanced ranking and relevance scoring
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <param name="advanced">Use advanced ranking algorithm</param>
    /// <returns>List of matching coaches</returns>
    [HttpGet("coaches")]
    [ProducesResponseType(typeof(List<Coach>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchCoaches(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false,
        [FromQuery] bool advanced = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var coaches = advanced
            ? await advancedSearchService.SearchCoachesWithAdvancedRankingAsync(query, limit)
            : await advancedSearchService.SearchCoachesAsync(query, limit, enableFuzzySearch);

        return Ok(coaches);
    }

    /// <summary>
    ///     Search for stadiums with location-based ranking
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <param name="advanced">Use advanced ranking algorithm</param>
    /// <returns>List of matching stadiums</returns>
    [HttpGet("stadiums")]
    [ProducesResponseType(typeof(List<Stadium>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchStadiums(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false,
        [FromQuery] bool advanced = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var stadiums = advanced
            ? await advancedSearchService.SearchStadiumsWithAdvancedRankingAsync(query, limit)
            : await advancedSearchService.SearchStadiumsAsync(query, limit, enableFuzzySearch);

        return Ok(stadiums);
    }

    /// <summary>
    ///     Search for seasons with comprehensive filtering
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <returns>List of matching seasons</returns>
    [HttpGet("seasons")]
    [ProducesResponseType(typeof(List<Season>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchSeasons(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var seasons = await advancedSearchService.SearchSeasonsAsync(query, limit, enableFuzzySearch);
        return Ok(seasons);
    }

    /// <summary>
    ///     Search for matches with advanced filtering
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="limit">Maximum number of results (1-50)</param>
    /// <param name="enableFuzzySearch">Enable fuzzy/approximate matching</param>
    /// <returns>List of matching matches</returns>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(List<Match>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchMatches(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] bool enableFuzzySearch = false)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 50) limit = 10;

        var matches = await advancedSearchService.SearchMatchesAsync(query, limit, enableFuzzySearch);
        return Ok(matches);
    }

    /// <summary>
    ///     Get search suggestions/autocomplete with enhanced relevance scoring
    /// </summary>
    /// <param name="query">Partial search term (minimum 1 character)</param>
    /// <param name="limit">Maximum number of suggestions (1-20)</param>
    /// <returns>List of search suggestions with relevance scores</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<SearchSuggestionDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetSearchSuggestions(
        [FromQuery] string query,
        [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        if (limit < 1 || limit > 20) limit = 5;

        try
        {
            var suggestions = await advancedSearchService.GetSearchSuggestionsAsync(query, limit);
            return Ok(suggestions);
        }
        catch (Exception)
        {
            // Return empty list on error rather than failing
            return Ok(new List<SearchSuggestionDto>());
        }
    }

    /// <summary>
    ///     Get search analytics and performance metrics
    /// </summary>
    /// <param name="query">Search term to analyze</param>
    /// <returns>Search analytics and performance data</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(SearchAnalyticsDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetSearchAnalytics([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Search query cannot be empty");

        try
        {
            var analytics = await advancedSearchService.GetSearchAnalyticsAsync(query);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating search analytics: {ex.Message}");
        }
    }

    /// <summary>
    ///     Bulk search across multiple queries
    /// </summary>
    /// <param name="queries">List of search queries</param>
    /// <param name="pageSize">Results per query</param>
    /// <returns>Bulk search results</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(Dictionary<string, SearchResultDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkSearch([FromBody] List<string> queries, [FromQuery] int pageSize = 10)
    {
        if (queries == null || !queries.Any()) return BadRequest("At least one search query is required");

        if (queries.Count > 10) return BadRequest("Maximum 10 queries allowed per bulk search");

        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var results = new Dictionary<string, SearchResultDto>();

        foreach (var query in queries.Where(q => !string.IsNullOrWhiteSpace(q)))
            try
            {
                var result = await advancedSearchService.SearchAsync(query, 1, pageSize);
                results[query] = result;
            }
            catch (Exception)
            {
                // Add empty result for failed queries
                results[query] = new SearchResultDto
                {
                    TotalResults = 0,
                    CurrentPage = 1,
                    TotalPages = 0,
                    PageSize = pageSize,
                    Items = []
                };
            }

        return Ok(results);
    }
}