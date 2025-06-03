using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces;

public interface IAdvancedSearchService : ISearchService
{
    /// <summary>
    /// Enhanced search with configurable search strategies
    /// </summary>
    Task<SearchResultDto> SearchWithStrategyAsync(string query, SearchStrategy strategy = SearchStrategy.Auto, int page = 1, int pageSize = 10);
    
    /// <summary>
    /// Get search suggestions with relevance ranking
    /// </summary>
    Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit = 5);
    
    /// <summary>
    /// Search with advanced filters and sorting
    /// </summary>
    Task<SearchResultDto> SearchWithFiltersAsync(SearchFiltersDto filters);

    /// <summary>
    /// Search for matches with advanced ranking
    /// </summary>
    Task<List<Match>> SearchMatchesAsync(string query, int limit = 10, bool enableFuzzySearch = false);

    /// <summary>
    /// Advanced team search with ranking and scoring
    /// </summary>
    Task<List<Team>> SearchTeamsWithAdvancedRankingAsync(string query, int limit = 10);

    /// <summary>
    /// Advanced player search with detailed filtering
    /// </summary>
    Task<List<Player>> SearchPlayersWithAdvancedRankingAsync(string query, int limit = 10);

    /// <summary>
    /// Advanced coach search with relevance scoring
    /// </summary>
    Task<List<Coach>> SearchCoachesWithAdvancedRankingAsync(string query, int limit = 10);

    /// <summary>
    /// Advanced stadium search with location-based ranking
    /// </summary>
    Task<List<Stadium>> SearchStadiumsWithAdvancedRankingAsync(string query, int limit = 10);

    /// <summary>
    /// Multi-entity search with unified ranking
    /// </summary>
    Task<SearchResultDto> UnifiedSearchAsync(string query, List<string>? entityTypes = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Get search analytics and statistics
    /// </summary>
    Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(string query);
}

public enum SearchStrategy
{
    Auto,           // Automatically choose based on query and data
    FullText,       // PostgreSQL full-text search
    Fuzzy,          // Levenshtein distance-based fuzzy search
    Hybrid          // Combination of both strategies
}

public class SearchFiltersDto
{
    public string Query { get; set; } = "";
    public List<string>? EntityTypes { get; set; }
    public string? Country { get; set; }
    public string? League { get; set; }
    public string? Position { get; set; }
    public string? Role { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public bool EnableFuzzySearch { get; set; } = false;
    public SearchStrategy Strategy { get; set; } = SearchStrategy.Auto;
}

public class SearchSuggestionDto
{
    public string Text { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public float Relevance { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Dictionary<string, string?> AdditionalData { get; set; } = new();
}

public class SearchAnalyticsDto
{
    public string Query { get; set; } = "";
    public SearchStrategy StrategyUsed { get; set; }
    public TimeSpan SearchDuration { get; set; }
    public int TotalResultsFound { get; set; }
    public Dictionary<string, int> ResultsByEntityType { get; set; } = new();
    public float AverageRelevanceScore { get; set; }
    public bool UsedFallbackSearch { get; set; }
    public List<string> SearchSuggestions { get; set; } = new();
}

