using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Match = Domain.Models.Match;

namespace Infrastructure.Services;

public class UnifiedSearchService(
    IServiceScopeFactory serviceScopeFactory,
    IUnitOfWork unitOfWork,
    ILogger<UnifiedSearchService> logger,
    IConfiguration configuration
) : IAdvancedSearchService
{
    private readonly bool _enablePostgresFts = configuration.GetValue(
        "Search:EnablePostgreSQLFullText",
        true
    );
    private readonly int _fuzzySearchThreshold = configuration.GetValue(
        "Search:FuzzySearchThreshold",
        3
    );

    public async Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        return await SearchWithStrategyAsync(query, SearchStrategy.Auto, page, pageSize);
    }

    public async Task<SearchResultDto> SearchAllAsync(
        string query,
        int page = 1,
        int pageSize = 10,
        bool enableFuzzySearch = false
    )
    {
        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.Auto;
        return await SearchWithStrategyAsync(query, strategy, page, pageSize);
    }

    public async Task<SearchResultDto> SearchWithStrategyAsync(
        string query,
        SearchStrategy strategy = SearchStrategy.Auto,
        int page = 1,
        int pageSize = 10
    )
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new SearchResultDto
            {
                TotalResults = 0,
                CurrentPage = page,
                TotalPages = 0,
                PageSize = pageSize,
                Items = [],
            };

        try
        {
            // Determine search strategy
            var selectedStrategy =
                strategy == SearchStrategy.Auto ? DetermineOptimalStrategy(query) : strategy;

            logger.LogDebug(
                "Using search strategy: {Strategy} for query: '{Query}'",
                selectedStrategy,
                query
            );

            var results = selectedStrategy switch
            {
                SearchStrategy.FullText => await ExecuteFullTextSearchAsync(query),
                SearchStrategy.Fuzzy => await ExecuteFuzzySearchAsync(query),
                SearchStrategy.Hybrid => await ExecuteHybridSearchAsync(query),
                _ => await ExecuteFullTextSearchAsync(query),
            };

            // Apply pagination
            var totalResults = results.Count;
            var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);
            var pagedResults = results.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new SearchResultDto
            {
                TotalResults = totalResults,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Items = pagedResults,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing search for '{Query}'", query);
            throw;
        }
    }

    public async Task<SearchResultDto> SearchWithFiltersAsync(SearchFiltersDto filters)
    {
        if (filters == null)
        {
            logger.LogWarning("SearchWithFiltersAsync called with null filters.");
            return new SearchResultDto
            {
                TotalResults = 0,
                Items = [],
                CurrentPage = 1,
                PageSize = 10,
                TotalPages = 0,
            };
        }

        logger.LogInformation("Executing search with filters: {@Filters}", filters);

        var allFilteredItems = new List<SearchItemDto>();

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

            var typesToSearch =
                filters.EntityTypes != null && filters.EntityTypes.Any()
                    ? filters.EntityTypes
                    : new List<string> { "Team", "Player", "Coach", "Stadium", "Match" }; // Default types

            var lowerQuery = string.IsNullOrWhiteSpace(filters.Query)
                ? null
                : filters.Query.ToLowerInvariant();

            // Note: The SearchStrategy from filters.Strategy and filters.EnableFuzzySearch
            // could be used here to determine how 'filters.Query' is matched (e.g., FTS, fuzzy, or simple Contains).
            // For this implementation, 'filters.Query' will use simple 'Contains' for brevity.
            // A more advanced version would integrate the strategy selection here.

            foreach (var entityType in typesToSearch)
                if (entityType.Equals("Team", StringComparison.OrdinalIgnoreCase))
                {
                    var teamQuery = context.Teams.AsQueryable();
                    if (lowerQuery != null)
                        teamQuery = teamQuery.Where(t =>
                            (t.Name != null && t.Name.ToLowerInvariant().Contains(lowerQuery))
                            || (t.City != null && t.City.ToLowerInvariant().Contains(lowerQuery))
                            || (
                                t.League != null && t.League.ToLowerInvariant().Contains(lowerQuery)
                            )
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Country))
                        teamQuery = teamQuery.Where(t =>
                            t.Country != null
                            && t.Country.Equals(filters.Country, StringComparison.OrdinalIgnoreCase)
                        );
                    if (!string.IsNullOrWhiteSpace(filters.League))
                        teamQuery = teamQuery.Where(t =>
                            t.League != null
                            && t.League.Equals(filters.League, StringComparison.OrdinalIgnoreCase)
                        );

                    var teamData = await teamQuery
                        .Select(t => new
                        {
                            t.Id,
                            t.Name,
                            t.League,
                            t.City,
                            t.Country,
                            t.Logo,
                        })
                        .ToListAsync();

                    allFilteredItems.AddRange(
                        teamData.Select(t => new SearchItemDto
                        {
                            Id = t.Id.ToString(),
                            Type = "Team",
                            Name = t.Name ?? "N/A",
                            Description =
                                $"League: {t.League ?? "N/A"}, City: {t.City ?? "N/A"}, Country: {t.Country ?? "N/A"}",
                            ThumbnailUrl = t.Logo,
                            Url = $"/teams/{t.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["League"] = t.League,
                                ["City"] = t.City,
                                ["Country"] = t.Country,
                            },
                        })
                    );
                }
                else if (entityType.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    var playerQuery = context.Players.Include(p => p.Team).AsQueryable();
                    if (lowerQuery != null)
                        playerQuery = playerQuery.Where(p =>
                            (
                                p.FullName != null
                                && p.FullName.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || (
                                p.KnownName != null
                                && p.KnownName.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || (
                                p.Position != null
                                && p.Position.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || // Assuming Player has Position
                            (
                                p.Nationality != null
                                && p.Nationality.ToLowerInvariant().Contains(lowerQuery)
                            )
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Country)) // Using general Country for Player Nationality
                        playerQuery = playerQuery.Where(p =>
                            p.Nationality != null
                            && p.Nationality.Equals(
                                filters.Country,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Position)) // Using filters.Position
                        playerQuery = playerQuery.Where(p =>
                            p.Position != null
                            && p.Position.Equals(
                                filters.Position,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                    // Assuming Player has DateOfBirth for age calculation if Min/MaxAge filters were in your DTO
                    // filters.MinPlayerAge / MaxPlayerAge are not in the user's DTO.
                    var playerData = await playerQuery
                        .Select(p => new
                        {
                            p.Id,
                            p.FullName,
                            p.KnownName,
                            p.Nationality,
                            p.Position,
                            TeamName = p.Team != null ? p.Team.Name : null,
                            p.PhotoUrl,
                        })
                        .ToListAsync();

                    allFilteredItems.AddRange(
                        playerData.Select(p => new SearchItemDto
                        {
                            Id = p.Id.ToString(),
                            Type = "Player",
                            Name = $"{p.FullName} {p.KnownName}".Trim(),
                            Description =
                                $"Nationality: {p.Nationality ?? "N/A"}, Position: {p.Position ?? "N/A"}, Team: {p.TeamName ?? "N/A"}",
                            ThumbnailUrl = p.PhotoUrl,
                            Url = $"/players/{p.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Nationality"] = p.Nationality,
                                ["Position"] = p.Position,
                                ["TeamName"] = p.TeamName,
                            },
                        })
                    );
                }
                else if (entityType.Equals("Coach", StringComparison.OrdinalIgnoreCase))
                {
                    var coachQuery = context.Coaches.Include(c => c.Team).AsQueryable();
                    if (lowerQuery != null)
                        coachQuery = coachQuery.Where(c =>
                            (
                                c.FirstName != null
                                && c.FirstName.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || (
                                c.LastName != null
                                && c.LastName.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || (
                                c.Nationality != null
                                && c.Nationality.ToLowerInvariant().Contains(lowerQuery)
                            )
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Country)) // Using general Country for Coach Nationality
                        coachQuery = coachQuery.Where(c =>
                            c.Nationality != null
                            && c.Nationality.Equals(
                                filters.Country,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Role)) // Assuming Coach has a 'Role' or similar field
                        coachQuery = coachQuery.Where(c =>
                            c.Role != null
                            && c.Role.Equals(filters.Role, StringComparison.OrdinalIgnoreCase)
                        ); // Example: c.Role

                    var coachData = await coachQuery
                        .Select(c => new
                        {
                            c.Id,
                            c.FirstName,
                            c.LastName,
                            c.Nationality,
                            c.Role,
                            TeamName = c.Team != null ? c.Team.Name : null,
                            c.PhotoUrl,
                        })
                        .ToListAsync();

                    allFilteredItems.AddRange(
                        coachData.Select(c => new SearchItemDto
                        {
                            Id = c.Id.ToString(),
                            Type = "Coach",
                            Name = $"{c.FirstName} {c.LastName}".Trim(),
                            Description =
                                $"Nationality: {c.Nationality ?? "N/A"}, Role: {c.Role ?? "N/A"}, Team: {c.TeamName ?? "N/A"}",
                            ThumbnailUrl = c.PhotoUrl,
                            Url = $"/coaches/{c.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Nationality"] = c.Nationality,
                                ["Role"] = c.Role,
                                ["TeamName"] = c.TeamName,
                            },
                        })
                    );
                }
                else if (entityType.Equals("Stadium", StringComparison.OrdinalIgnoreCase))
                {
                    var stadiumQuery = context.Stadiums.AsQueryable();
                    if (lowerQuery != null)
                        stadiumQuery = stadiumQuery.Where(s =>
                            (s.Name != null && s.Name.ToLowerInvariant().Contains(lowerQuery))
                            || (s.City != null && s.City.ToLowerInvariant().Contains(lowerQuery))
                        );
                    if (!string.IsNullOrWhiteSpace(filters.Country))
                        stadiumQuery = stadiumQuery.Where(s =>
                            s.Country != null
                            && s.Country.Equals(filters.Country, StringComparison.OrdinalIgnoreCase)
                        );
                    // Assuming Stadium has City, user's DTO doesn't have a specific StadiumCity
                    // Using filters.League for stadium's league context is unusual, typically stadiums aren't directly in leagues.
                    // If a stadium is tied to a team, you might filter by the team's league.
                    if (filters.MinCapacity.HasValue)
                        stadiumQuery = stadiumQuery.Where(s =>
                            s.Capacity >= filters.MinCapacity.Value
                        );
                    if (filters.MaxCapacity.HasValue)
                        stadiumQuery = stadiumQuery.Where(s =>
                            s.Capacity <= filters.MaxCapacity.Value
                        );

                    var stadiumData = await stadiumQuery
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            s.City,
                            s.Capacity,
                            s.Country,
                            s.ImageUrl,
                        })
                        .ToListAsync();

                    allFilteredItems.AddRange(
                        stadiumData.Select(s => new SearchItemDto
                        {
                            Id = s.Id.ToString(),
                            Type = "Stadium",
                            Name = s.Name ?? "N/A",
                            Description =
                                $"City: {s.City ?? "N/A"}, Capacity: {s.Capacity}, Country: {s.Country ?? "N/A"}",
                            ThumbnailUrl = s.ImageUrl,
                            Url = $"/stadiums/{s.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["City"] = s.City,
                                ["Capacity"] = s.Capacity.ToString(),
                                ["Country"] = s.Country,
                            },
                        })
                    );
                }
                else if (entityType.Equals("Match", StringComparison.OrdinalIgnoreCase))
                {
                    var matchQuery = context
                        .Matches.Include(m => m.HomeTeam)
                        .Include(m => m.AwayTeam)
                        .AsQueryable();
                    if (lowerQuery != null)
                        matchQuery = matchQuery.Where(m =>
                            (
                                m.HomeTeam != null
                                && m.HomeTeam.Name != null
                                && m.HomeTeam.Name.ToLowerInvariant().Contains(lowerQuery)
                            )
                            || (
                                m.AwayTeam != null
                                && m.AwayTeam.Name != null
                                && m.AwayTeam.Name.ToLowerInvariant().Contains(lowerQuery)
                            )
                        );
                    if (filters.FromDate.HasValue)
                        matchQuery = matchQuery.Where(m =>
                            m.ScheduledDateTimeUtc >= filters.FromDate.Value
                        );
                    if (filters.ToDate.HasValue)
                        matchQuery = matchQuery.Where(m =>
                            m.ScheduledDateTimeUtc <= filters.ToDate.Value
                        );
                    if (!string.IsNullOrWhiteSpace(filters.League)) // Match league
                        matchQuery = matchQuery.Where(m => m.Id > 0);

                    // filters.HomeTeamId / AwayTeamId are not in user's DTO.
                    var matchData = await matchQuery
                        .Select(m => new
                        {
                            m.Id,
                            HomeTeamName = m.HomeTeam != null ? m.HomeTeam.Name : null,
                            AwayTeamName = m.AwayTeam != null ? m.AwayTeam.Name : null,
                            m.ScheduledDateTimeUtc,
                            m.HomeTeamScore,
                            m.AwayTeamScore,
                        })
                        .ToListAsync();

                    allFilteredItems.AddRange(
                        matchData.Select(m => new SearchItemDto
                        {
                            Id = m.Id.ToString(),
                            Type = "Match",
                            Name = $"{m.HomeTeamName ?? "N/A"} vs {m.AwayTeamName ?? "N/A"}",
                            Description =
                                $"Date: {m.ScheduledDateTimeUtc.ToShortDateString()}, Score: {m.HomeTeamScore}-{m.AwayTeamScore}",
                            Url = $"/matches/{m.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Date"] = m.ScheduledDateTimeUtc.ToString("o"),
                                ["HomeTeamName"] = m.HomeTeamName,
                                ["AwayTeamName"] = m.AwayTeamName,
                                ["Score"] = $"{m.HomeTeamScore}-{m.AwayTeamScore}",
                            },
                        })
                    );
                }

            // Apply Sorting
            if (!string.IsNullOrWhiteSpace(filters.SortBy))
                // This is a simplified sorting. For robust dynamic sorting on different properties of SearchItemDto,
                // you might need a more complex solution or a switch statement.
                // Sorting on properties of the original entities before projection is more performant.
                // Here, we sort the collected SearchItemDto list.
                try
                {
                    var pi = typeof(SearchItemDto).GetProperty(
                        filters.SortBy,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
                    );
                    if (pi != null)
                    {
                        if (filters.SortDescending)
                            allFilteredItems = allFilteredItems
                                .OrderByDescending(x => pi.GetValue(x, null))
                                .ToList();
                        else
                            allFilteredItems = allFilteredItems
                                .OrderBy(x => pi.GetValue(x, null))
                                .ToList();
                    }
                    else
                    {
                        logger.LogWarning(
                            "SortBy property '{SortByProperty}' not found on SearchItemDto.",
                            filters.SortBy
                        );
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error applying sorting for SortBy '{SortByProperty}'.",
                        filters.SortBy
                    );
                }
            else
                // Default sort if no SortBy is specified, e.g., by Name or relevance if available
                allFilteredItems = allFilteredItems.OrderBy(item => item.Name).ToList();

            var totalResults = allFilteredItems.Count;
            var totalPages = (int)Math.Ceiling(totalResults / (double)filters.PageSize);
            var currentPage = Math.Max(1, Math.Min(filters.Page, totalPages == 0 ? 1 : totalPages));

            var pagedResults = allFilteredItems
                .Skip((currentPage - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToList();

            return new SearchResultDto
            {
                TotalResults = totalResults,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = filters.PageSize,
                Items = pagedResults,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred during SearchWithFiltersAsync for filters: {@Filters}",
                filters
            );
            return new SearchResultDto
            {
                TotalResults = 0,
                Items = [],
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalPages = 0,
                Error = "An error occurred during the search.",
            };
        }
    }

    public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(
        string query,
        int limit = 5
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var suggestions = new List<SearchSuggestionDto>();
            var normalizedQuery = NormalizeSearchQuery(query);

            // Get suggestions from different entity types
            var teamSuggestions = await GetTeamSuggestionsAsync(normalizedQuery, limit / 4);
            var playerSuggestions = await GetPlayerSuggestionsAsync(normalizedQuery, limit / 4);
            var coachSuggestions = await GetCoachSuggestionsAsync(normalizedQuery, limit / 4);
            var stadiumSuggestions = await GetStadiumSuggestionsAsync(normalizedQuery, limit / 4);

            suggestions.AddRange(teamSuggestions);
            suggestions.AddRange(playerSuggestions);
            suggestions.AddRange(coachSuggestions);
            suggestions.AddRange(stadiumSuggestions);

            return suggestions.OrderByDescending(s => s.Relevance).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting search suggestions for '{Query}'", query);
            return [];
        }
    }

    public async Task<List<Team>> SearchTeamsAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.FullText;
        return await SearchTeamsWithStrategyAsync(query, strategy, limit);
    }

    public async Task<List<Player>> SearchPlayersAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.FullText;
        return await SearchPlayersWithStrategyAsync(query, strategy, limit);
    }

    public async Task<List<Coach>> SearchCoachesAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.FullText;
        return await SearchCoachesWithStrategyAsync(query, strategy, limit);
    }

    public async Task<List<Stadium>> SearchStadiumsAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.FullText;
        return await SearchStadiumsWithStrategyAsync(query, strategy, limit);
    }

    public async Task<List<Season>> SearchSeasonsAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var strategy = enableFuzzySearch ? SearchStrategy.Hybrid : SearchStrategy.FullText;
        return await SearchSeasonsWithStrategyAsync(query, strategy, limit);
    }

    public async Task<List<Match>> SearchMatchesAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var normalizedQuery = NormalizeSearchQuery(query);

            // Search by team names first
            var teams = await unitOfWork.Teams.SearchAsync(normalizedQuery);
            var teamIds = teams.Select(t => t.Id).ToList();

            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
            var matches = await context
                .Matches.Where(m =>
                    teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)
                )
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsNoTracking()
                .Take(limit)
                .ToListAsync();

            return matches;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching matches for '{Query}'", query);
            return [];
        }
    }

    public async Task<List<Team>> SearchTeamsWithAdvancedRankingAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            return await SearchTeamsWithStrategyAsync(query, SearchStrategy.Hybrid, limit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in advanced team search for '{Query}'", query);
            return [];
        }
    }

    public async Task<List<Player>> SearchPlayersWithAdvancedRankingAsync(
        string query,
        int limit = 10
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            return await SearchPlayersWithStrategyAsync(query, SearchStrategy.Hybrid, limit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in advanced player search for '{Query}'", query);
            return [];
        }
    }

    public async Task<List<Coach>> SearchCoachesWithAdvancedRankingAsync(
        string query,
        int limit = 10
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            return await SearchCoachesWithStrategyAsync(query, SearchStrategy.Hybrid, limit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in advanced coach search for '{Query}'", query);
            return [];
        }
    }

    public async Task<List<Stadium>> SearchStadiumsWithAdvancedRankingAsync(
        string query,
        int limit = 10
    )
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            return await SearchStadiumsWithStrategyAsync(query, SearchStrategy.Hybrid, limit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in advanced stadium search for '{Query}'", query);
            return [];
        }
    }

    public async Task<SearchResultDto> UnifiedSearchAsync(
        string query,
        List<string>? entityTypes = null,
        int page = 1,
        int pageSize = 10
    )
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new SearchResultDto
            {
                TotalResults = 0,
                CurrentPage = page,
                TotalPages = 0,
                PageSize = pageSize,
                Items = [],
            };

        try
        {
            var results = new List<SearchItemDto>();
            var normalizedQuery = NormalizeSearchQuery(query);

            // If entity types are specified, only search those types
            var typesToSearch = entityTypes ?? ["Team", "Player", "Coach", "Match", "Stadium"];

            var searchTasks = new List<Task<List<SearchItemDto>>>();

            if (typesToSearch.Contains("Team"))
                searchTasks.Add(
                    Task.Run(async () =>
                    {
                        var teams = await SearchTeamsWithAdvancedRankingAsync(query, 20);
                        return teams
                            .Select(t => new SearchItemDto
                            {
                                Id = t.Id.ToString(),
                                Type = "Team",
                                Name = t.Name ?? "Unknown Team",
                                Description = $"{t.City} - {t.League}",
                                ThumbnailUrl = t.Logo,
                                Url = $"/teams/{t.Id}",
                                AdditionalData = new Dictionary<string, string?>
                                {
                                    ["League"] = t.League,
                                    ["City"] = t.City,
                                    ["Country"] = t.Country,
                                },
                            })
                            .ToList();
                    })
                );

            if (typesToSearch.Contains("Player"))
                searchTasks.Add(
                    Task.Run(async () =>
                    {
                        var players = await SearchPlayersWithAdvancedRankingAsync(query, 20);
                        return players
                            .Select(p => new SearchItemDto
                            {
                                Id = p.Id.ToString(),
                                Type = "Player",
                                Name = p.FullName ?? "Unknown Player",
                                Description = $"{p.Position} - {p.Team?.Name ?? "No Team"}",
                                ThumbnailUrl = p.PhotoUrl,
                                Url = $"/players/{p.Id}",
                                AdditionalData = new Dictionary<string, string?>
                                {
                                    ["Position"] = p.Position,
                                    ["Team"] = p.Team?.Name,
                                    ["Nationality"] = p.Nationality,
                                },
                            })
                            .ToList();
                    })
                );

            if (typesToSearch.Contains("Coach"))
                searchTasks.Add(
                    Task.Run(async () =>
                    {
                        var coaches = await SearchCoachesWithAdvancedRankingAsync(query, 20);
                        return coaches
                            .Select(c => new SearchItemDto
                            {
                                Id = c.Id.ToString(),
                                Type = "Coach",
                                Name = $"{c.FirstName} {c.LastName}",
                                Description = $"{c.Role} - {c.Team?.Name ?? "No Team"}",
                                ThumbnailUrl = c.PhotoUrl,
                                Url = $"/coaches/{c.Id}",
                                AdditionalData = new Dictionary<string, string?>
                                {
                                    ["Role"] = c.Role,
                                    ["Team"] = c.Team?.Name,
                                    ["Nationality"] = c.Nationality,
                                },
                            })
                            .ToList();
                    })
                );

            if (typesToSearch.Contains("Match"))
                searchTasks.Add(
                    Task.Run(async () =>
                    {
                        var matches = await SearchMatchesAsync(query, 20);
                        return matches
                            .Select(m => new SearchItemDto
                            {
                                Id = m.Id.ToString(),
                                Type = "Match",
                                Name = $"{m.HomeTeam?.Name} vs {m.AwayTeam?.Name}",
                                Description = $"{m.ScheduledDateTimeUtc:MMM dd, yyyy}",
                                Url = $"/matches/{m.Id}",
                                AdditionalData = new Dictionary<string, string?>
                                {
                                    ["Date"] = m.ScheduledDateTimeUtc.ToString("yyyy-MM-dd"),
                                    ["Status"] = m.MatchStatus,
                                },
                            })
                            .ToList();
                    })
                );

            if (typesToSearch.Contains("Stadium"))
                searchTasks.Add(
                    Task.Run(async () =>
                    {
                        var stadiums = await SearchStadiumsWithAdvancedRankingAsync(query, 20);
                        return stadiums
                            .Select(s => new SearchItemDto
                            {
                                Id = s.Id.ToString(),
                                Type = "Stadium",
                                Name = s.Name ?? "Unknown Stadium",
                                Description = $"{s.City}, {s.Country} - Capacity: {s.Capacity:N0}",
                                ThumbnailUrl = s.ImageUrl,
                                Url = $"/stadiums/{s.Id}",
                                AdditionalData = new Dictionary<string, string?>
                                {
                                    ["City"] = s.City,
                                    ["Country"] = s.Country,
                                    ["Capacity"] = s.Capacity.ToString("N0"),
                                },
                            })
                            .ToList();
                    })
                );

            var searchResults = await Task.WhenAll(searchTasks);
            foreach (var resultList in searchResults)
                results.AddRange(resultList);

            // Apply pagination
            var totalResults = results.Count;
            var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);
            var pagedResults = results
                .OrderBy(r => GetEntityTypePriority(r.Type ?? ""))
                .ThenBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new SearchResultDto
            {
                TotalResults = totalResults,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Items = pagedResults,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in unified search for '{Query}'", query);
            throw;
        }
    }

    public async Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchAnalyticsDto
            {
                Query = query,
                StrategyUsed = SearchStrategy.Auto,
                SearchDuration = TimeSpan.Zero,
                TotalResultsFound = 0,
                ResultsByEntityType = new Dictionary<string, int>(),
                AverageRelevanceScore = 0f,
                UsedFallbackSearch = false,
                SearchSuggestions = new List<string>(),
            };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var strategy = DetermineOptimalStrategy(query);
            var usedFallback = !_enablePostgresFts && strategy == SearchStrategy.FullText;
            // Perform search across all entity types
            var teamSearchTask = SearchTeamsWithAdvancedRankingAsync(query, 50);
            var playerSearchTask = SearchPlayersWithAdvancedRankingAsync(query, 50);
            var coachSearchTask = SearchCoachesWithAdvancedRankingAsync(query, 50);
            var stadiumSearchTask = SearchStadiumsWithAdvancedRankingAsync(query, 50);
            var matchSearchTask = SearchMatchesAsync(query, 50);

            var allTasks = new Task[]
            {
                teamSearchTask,
                playerSearchTask,
                coachSearchTask,
                stadiumSearchTask,
                matchSearchTask,
            };

            await Task.WhenAll(allTasks); // Wait for all tasks to complete
            stopwatch.Stop();

            var resultsByType = new Dictionary<string, int>
            {
                ["Team"] = teamSearchTask.Result.Count,
                ["Player"] = playerSearchTask.Result.Count,
                ["Coach"] = coachSearchTask.Result.Count,
                ["Stadium"] = stadiumSearchTask.Result.Count,
                ["Match"] = matchSearchTask.Result.Count,
            };

            var totalResults = resultsByType.Values.Sum();

            // Generate search suggestions
            var suggestions = await GetSearchSuggestionsAsync(query);
            var suggestionTexts = suggestions.Select(s => s.Text).ToList();

            // Calculate average relevance (simplified)
            var averageRelevance = suggestions.Any() ? suggestions.Average(s => s.Relevance) : 0f;

            // Convert internal SearchStrategy to interface SearchStrategy
            var interfaceStrategy = strategy switch
            {
                SearchStrategy.Auto => SearchStrategy.Auto,
                SearchStrategy.FullText => SearchStrategy.FullText,
                SearchStrategy.Fuzzy => SearchStrategy.Fuzzy,
                SearchStrategy.Hybrid => SearchStrategy.Hybrid,
                _ => SearchStrategy.Auto,
            };

            return new SearchAnalyticsDto
            {
                Query = query,
                StrategyUsed = interfaceStrategy,
                SearchDuration = stopwatch.Elapsed,
                TotalResultsFound = totalResults,
                ResultsByEntityType = resultsByType,
                AverageRelevanceScore = averageRelevance,
                UsedFallbackSearch = usedFallback,
                SearchSuggestions = suggestionTexts,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error generating search analytics for '{Query}'", query);

            return new SearchAnalyticsDto
            {
                Query = query,
                StrategyUsed = SearchStrategy.Auto,
                SearchDuration = stopwatch.Elapsed,
                TotalResultsFound = 0,
                ResultsByEntityType = new Dictionary<string, int>(),
                AverageRelevanceScore = 0f,
                UsedFallbackSearch = true,
                SearchSuggestions = new List<string>(),
            };
        }
    }

    // Private helper methods
    private SearchStrategy DetermineOptimalStrategy(string query)
    {
        // Smart strategy selection based on query characteristics
        if (!_enablePostgresFts)
            return SearchStrategy.Fuzzy;

        // For very short queries, prefer fuzzy search
        if (query.Length <= 3)
            return SearchStrategy.Fuzzy;

        // For queries with special characters or complex patterns, use hybrid
        if (Regex.IsMatch(query, @"[^a-zA-Z0-9\s]"))
            return SearchStrategy.Hybrid;

        // Default to full-text search for longer, clean queries
        return SearchStrategy.FullText;
    }

    private async Task<List<SearchItemDto>> ExecuteFullTextSearchAsync(string query)
    {
        var normalizedQuery = NormalizeSearchQuery(query);
        var phraseQuery = CreatePhraseQuery(query);
        var prefixQuery = CreatePrefixQuery(query);

        var searchTasks = new[]
        {
            SearchTeamsWithAdvancedRankingAsync(normalizedQuery, phraseQuery, prefixQuery),
            SearchPlayersWithAdvancedRankingAsync(normalizedQuery, phraseQuery, prefixQuery),
            SearchCoachesWithAdvancedRankingAsync(normalizedQuery, phraseQuery, prefixQuery),
            SearchMatchesWithAdvancedRankingAsync(normalizedQuery, phraseQuery, prefixQuery),
            SearchStadiumsWithAdvancedRankingAsync(normalizedQuery, phraseQuery, prefixQuery),
        };

        var results = await Task.WhenAll(searchTasks);
        var allResults = new List<(SearchItemDto Item, float Score)>();

        foreach (var resultList in results)
            allResults.AddRange(resultList);

        return allResults
            .OrderByDescending(r => r.Score)
            .ThenBy(r => GetEntityTypePriority(r.Item.Type ?? ""))
            .Select(r => r.Item)
            .ToList();
    }

    private async Task<List<SearchItemDto>> ExecuteFuzzySearchAsync(string query)
    {
        var searchTerm = query.ToLower().Trim();
        var searchTasks = new[]
        {
            SearchTeamsWithFuzzyAsync(searchTerm),
            SearchPlayersWithFuzzyAsync(searchTerm),
            SearchCoachesWithFuzzyAsync(searchTerm),
            SearchStadiumsWithFuzzyAsync(searchTerm),
        };

        var results = await Task.WhenAll(searchTasks);
        var allResults = new List<SearchItemDto>();

        foreach (var resultList in results)
            allResults.AddRange(resultList);

        return allResults.OrderBy(r => r.Name).ToList();
    }

    private async Task<List<SearchItemDto>> ExecuteHybridSearchAsync(string query)
    {
        // Combine both full-text and fuzzy search results
        var fullTextResults = await ExecuteFullTextSearchAsync(query);
        var fuzzyResults = await ExecuteFuzzySearchAsync(query);

        // Merge and deduplicate results
        var combinedResults = new Dictionary<string, SearchItemDto>();

        foreach (var result in fullTextResults)
            combinedResults[$"{result.Type}_{result.Id}"] = result;

        foreach (var result in fuzzyResults)
        {
            var key = $"{result.Type}_{result.Id}";
            if (!combinedResults.ContainsKey(key))
                combinedResults[key] = result;
        }

        return combinedResults.Values.ToList();
    }

    private string NormalizeSearchQuery(string query)
    {
        return Regex.Replace(query.Trim(), @"[^\w\s]", " ").Replace("'", "''").Trim();
    }

    private string CreatePhraseQuery(string query)
    {
        var normalized = NormalizeSearchQuery(query);
        return $"'{normalized}'";
    }

    private string CreatePrefixQuery(string query)
    {
        var words = NormalizeSearchQuery(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" & ", words.Select(w => $"{w}:*"));
    }

    private int GetEntityTypePriority(string entityType)
    {
        return entityType switch
        {
            "Team" => 1,
            "Player" => 2,
            "Coach" => 3,
            "Match" => 4,
            "Stadium" => 5,
            _ => 10,
        };
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (source == target)
            return 0;
        if (source.Length == 0)
            return target.Length;
        if (target.Length == 0)
            return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
            distance[i, 0] = i;
        for (var j = 0; j <= target.Length; j++)
            distance[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        for (var j = 1; j <= target.Length; j++)
        {
            var cost = target[j - 1] == source[i - 1] ? 0 : 1;
            distance[i, j] = Math.Min(
                Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                distance[i - 1, j - 1] + cost
            );
        }

        return distance[source.Length, target.Length];
    }

    // Implementation methods - PostgreSQL full-text search with ranking
    private async Task<List<(SearchItemDto Item, float Score)>> SearchTeamsWithAdvancedRankingAsync(
        string normalizedQuery,
        string phraseQuery,
        string prefixQuery
    )
    {
        if (!_enablePostgresFts)
        {
            // Fallback to basic search
            var teams = await unitOfWork.Teams.SearchAsync(normalizedQuery);
            return teams
                .Select(t =>
                    (
                        new SearchItemDto
                        {
                            Id = t.Id.ToString(),
                            Type = "Team",
                            Name = t.Name ?? "Unknown Team",
                            Description = $"{t.City} - {t.League}",
                            ThumbnailUrl = t.Logo,
                            Url = $"/teams/{t.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["League"] = t.League,
                                ["City"] = t.City,
                                ["Country"] = t.Country,
                            },
                        },
                        1.0f
                    )
                )
                .ToList();
        }

        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        // PostgreSQL full-text search with ranking
        var results = await context
            .Teams.FromSqlRaw(
                @"
                SELECT t.*, 
                       ts_rank(to_tsvector('english', COALESCE(t.""Name"", '') || ' ' || COALESCE(t.""League"", '') || ' ' || COALESCE(t.""City"", '')), 
                               plainto_tsquery('english', {0})) as rank
                FROM ""Teams"" t
                WHERE to_tsvector('english', COALESCE(t.""Name"", '') || ' ' || COALESCE(t.""League"", '') || ' ' || COALESCE(t.""City"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY rank DESC
                LIMIT 50",
                normalizedQuery
            )
            .AsNoTracking()
            .ToListAsync();

        return results
            .Select(t =>
                (
                    new SearchItemDto
                    {
                        Id = t.Id.ToString(),
                        Type = "Team",
                        Name = t.Name ?? "Unknown Team",
                        Description = $"{t.City} - {t.League}",
                        ThumbnailUrl = t.Logo,
                        Url = $"/teams/{t.Id}",
                        AdditionalData = new Dictionary<string, string?>
                        {
                            ["League"] = t.League,
                            ["City"] = t.City,
                            ["Country"] = t.Country,
                        },
                    },
                    0.8f // Base score for PostgreSQL FTS
                )
            )
            .ToList();
    }

    private async Task<
        List<(SearchItemDto Item, float Score)>
    > SearchPlayersWithAdvancedRankingAsync(
        string normalizedQuery,
        string phraseQuery,
        string prefixQuery
    )
    {
        if (!_enablePostgresFts)
        {
            // Fallback to basic search
            var players = await unitOfWork.Players.SearchAsync(normalizedQuery);
            return players
                .Select(p =>
                    (
                        new SearchItemDto
                        {
                            Id = p.Id.ToString(),
                            Type = "Player",
                            Name = p.FullName ?? "Unknown Player",
                            Description = $"{p.Position} - {p.Team?.Name ?? "No Team"}",
                            ThumbnailUrl = p.PhotoUrl,
                            Url = $"/players/{p.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Position"] = p.Position,
                                ["Team"] = p.Team?.Name,
                                ["Nationality"] = p.Nationality,
                            },
                        },
                        1.0f
                    )
                )
                .ToList();
        }

        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        // PostgreSQL full-text search with ranking
        var results = await context
            .Players.FromSqlRaw(
                @"
                SELECT p.*, t.""Name"" as TeamName,
                       ts_rank(to_tsvector('english', COALESCE(p.""FullName"", '') || ' ' || COALESCE(p.""KnownName"", '') || ' ' || COALESCE(p.""Position"", '')), 
                               plainto_tsquery('english', {0})) as rank
                FROM ""Players"" p
                LEFT JOIN ""Teams"" t ON p.""TeamId"" = t.""Id""
                WHERE to_tsvector('english', COALESCE(p.""FullName"", '') || ' ' || COALESCE(p.""KnownName"", '') || ' ' || COALESCE(p.""Position"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY rank DESC
                LIMIT 50",
                normalizedQuery
            )
            .Include(p => p.Team)
            .AsNoTracking()
            .ToListAsync();

        return results
            .Select(p =>
                (
                    new SearchItemDto
                    {
                        Id = p.Id.ToString(),
                        Type = "Player",
                        Name = p.FullName ?? "Unknown Player",
                        Description = $"{p.Position} - {p.Team?.Name ?? "No Team"}",
                        ThumbnailUrl = p.PhotoUrl,
                        Url = $"/players/{p.Id}",
                        AdditionalData = new Dictionary<string, string?>
                        {
                            ["Position"] = p.Position,
                            ["Team"] = p.Team?.Name,
                            ["Nationality"] = p.Nationality,
                        },
                    },
                    0.75f // Base score for PostgreSQL FTS
                )
            )
            .ToList();
    }

    private async Task<
        List<(SearchItemDto Item, float Score)>
    > SearchCoachesWithAdvancedRankingAsync(
        string normalizedQuery,
        string phraseQuery,
        string prefixQuery
    )
    {
        if (!_enablePostgresFts)
        {
            // Fallback to basic search
            var coaches = await unitOfWork.Coaches.SearchAsync(normalizedQuery);
            return coaches
                .Select(c =>
                    (
                        new SearchItemDto
                        {
                            Id = c.Id.ToString(),
                            Type = "Coach",
                            Name = $"{c.FirstName} {c.LastName}",
                            Description = $"{c.Role} - {c.Team?.Name ?? "No Team"}",
                            ThumbnailUrl = c.PhotoUrl,
                            Url = $"/coaches/{c.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Role"] = c.Role,
                                ["Team"] = c.Team?.Name,
                                ["Nationality"] = c.Nationality,
                            },
                        },
                        1.0f
                    )
                )
                .ToList();
        }

        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        // PostgreSQL full-text search with ranking
        var results = await context
            .Coaches.FromSqlRaw(
                @"
                SELECT c.*, t.""Name"" as TeamName,
                       ts_rank(to_tsvector('english', COALESCE(c.""FirstName"", '') || ' ' || COALESCE(c.""LastName"", '') || ' ' || COALESCE(c.""Role"", '')), 
                               plainto_tsquery('english', {0})) as rank
                FROM ""Coaches"" c
                LEFT JOIN ""Teams"" t ON c.""TeamId"" = t.""Id""
                WHERE to_tsvector('english', COALESCE(c.""FirstName"", '') || ' ' || COALESCE(c.""LastName"", '') || ' ' || COALESCE(c.""Role"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY rank DESC
                LIMIT 50",
                normalizedQuery
            )
            .Include(c => c.Team)
            .AsNoTracking()
            .ToListAsync();

        return results
            .Select(c =>
                (
                    new SearchItemDto
                    {
                        Id = c.Id.ToString(),
                        Type = "Coach",
                        Name = $"{c.FirstName} {c.LastName}",
                        Description = $"{c.Role} - {c.Team?.Name ?? "No Team"}",
                        ThumbnailUrl = c.PhotoUrl,
                        Url = $"/coaches/{c.Id}",
                        AdditionalData = new Dictionary<string, string?>
                        {
                            ["Role"] = c.Role,
                            ["Team"] = c.Team?.Name,
                            ["Nationality"] = c.Nationality,
                        },
                    },
                    0.7f // Base score for PostgreSQL FTS
                )
            )
            .ToList();
    }

    private async Task<
        List<(SearchItemDto Item, float Score)>
    > SearchMatchesWithAdvancedRankingAsync(
        string normalizedQuery,
        string phraseQuery,
        string prefixQuery
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        if (!_enablePostgresFts)
        {
            // Fallback to basic search using Teams
            var teams = await unitOfWork.Teams.SearchAsync(normalizedQuery);
            var teamIds = teams.Select(t => t.Id).ToList();

            var matches = await context
                .Matches.Where(m =>
                    teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)
                )
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsNoTracking()
                .Take(20)
                .ToListAsync();

            return matches
                .Select(m =>
                    (
                        new SearchItemDto
                        {
                            Id = m.Id.ToString(),
                            Type = "Match",
                            Name = $"{m.HomeTeam?.Name} vs {m.AwayTeam?.Name}",
                            Description = $"{m.ScheduledDateTimeUtc:MMM dd, yyyy} ",
                            Url = $"/matches/{m.Id}",
                            AdditionalData = new Dictionary<string, string?>
                            {
                                ["Date"] = m.ScheduledDateTimeUtc.ToString("yyyy-MM-dd"),
                                ["Status"] = m.MatchStatus,
                            },
                        },
                        1.0f
                    )
                )
                .ToList();
        }

        // PostgreSQL full-text search for matches
        var results = await context
            .Matches.FromSqlRaw(
                @"
                SELECT m.*,
                       ts_rank(to_tsvector('english', COALESCE(ht.""Name"", '') || ' ' || COALESCE(at.""Name"", '')), 
                               plainto_tsquery('english', {0})) as rank
                FROM ""Matches"" m
                LEFT JOIN ""Teams"" ht ON m.""HomeTeamId"" = ht.""Id""
                LEFT JOIN ""Teams"" at ON m.""AwayTeamId"" = at.""Id""
                WHERE to_tsvector('english', COALESCE(ht.""Name"", '') || ' ' || COALESCE(at.""Name"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY rank DESC
                LIMIT 30",
                normalizedQuery
            )
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .AsNoTracking()
            .ToListAsync();

        return results
            .Select(m =>
                (
                    new SearchItemDto
                    {
                        Id = m.Id.ToString(),
                        Type = "Match",
                        Name = $"{m.HomeTeam?.Name} vs {m.AwayTeam?.Name}",
                        Description = $"{m.ScheduledDateTimeUtc:MMM dd, yyyy} ",
                        Url = $"/matches/{m.Id}",
                        AdditionalData = new Dictionary<string, string?>
                        {
                            ["Date"] = m.ScheduledDateTimeUtc.ToString("yyyy-MM-dd"),
                            ["Status"] = m.MatchStatus,
                        },
                    },
                    0.6f // Base score for PostgreSQL FTS
                )
            )
            .ToList();
    }

    private async Task<
        List<(SearchItemDto Item, float Score)>
    > SearchStadiumsWithAdvancedRankingAsync(
        string normalizedQuery,
        string phraseQuery,
        string prefixQuery
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        // Stadium search implementation - basic search since stadiums might not have full-text indexes
        var stadiums = await context
            .Stadiums.Where(s =>
                s.Name!.ToLower().Contains(normalizedQuery.ToLower())
                || s.City!.ToLower().Contains(normalizedQuery.ToLower())
            )
            .AsNoTracking()
            .Take(20)
            .ToListAsync();

        return stadiums
            .Select(s =>
                (
                    new SearchItemDto
                    {
                        Id = s.Id.ToString(),
                        Type = "Stadium",
                        Name = s.Name ?? "Unknown Stadium",
                        Description = $"{s.City}, {s.Country} - Capacity: {s.Capacity:N0}",
                        ThumbnailUrl = s.ImageUrl,
                        Url = $"/stadiums/{s.Id}",
                        AdditionalData = new Dictionary<string, string?>
                        {
                            ["City"] = s.City,
                            ["Country"] = s.Country,
                            ["Capacity"] = s.Capacity.ToString("N0"),
                        },
                    },
                    0.5f // Base score
                )
            )
            .ToList();
    }

    // Fuzzy search implementations
    private async Task<List<SearchItemDto>> SearchTeamsWithFuzzyAsync(string searchTerm)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var teams = await context.Teams.Where(t => t.Name != null).AsNoTracking().ToListAsync();

        var fuzzyMatches = teams
            .Where(t =>
                CalculateLevenshteinDistance(searchTerm, t.Name!.ToLower()) <= _fuzzySearchThreshold
            )
            .Select(t => new SearchItemDto
            {
                Id = t.Id.ToString(),
                Type = "Team",
                Name = t.Name ?? "Unknown Team",
                Description = $"{t.City} - {t.League}",
                ThumbnailUrl = t.Logo,
                Url = $"/teams/{t.Id}",
                AdditionalData = new Dictionary<string, string?>
                {
                    ["League"] = t.League,
                    ["City"] = t.City,
                    ["Country"] = t.Country,
                },
            })
            .ToList();

        return fuzzyMatches;
    }

    private async Task<List<SearchItemDto>> SearchPlayersWithFuzzyAsync(string searchTerm)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var players = await context
            .Players.Where(p => p.FullName != null)
            .Include(p => p.Team)
            .AsNoTracking()
            .ToListAsync();

        var fuzzyMatches = players
            .Where(p =>
                CalculateLevenshteinDistance(searchTerm, p.FullName!.ToLower())
                    <= _fuzzySearchThreshold
                || (
                    p.KnownName != null
                    && CalculateLevenshteinDistance(searchTerm, p.KnownName.ToLower())
                        <= _fuzzySearchThreshold
                )
            )
            .Select(p => new SearchItemDto
            {
                Id = p.Id.ToString(),
                Type = "Player",
                Name = p.FullName ?? "Unknown Player",
                Description = $"{p.Position} - {p.Team?.Name ?? "No Team"}",
                ThumbnailUrl = p.PhotoUrl,
                Url = $"/players/{p.Id}",
                AdditionalData = new Dictionary<string, string?>
                {
                    ["Position"] = p.Position,
                    ["Team"] = p.Team?.Name,
                    ["Nationality"] = p.Nationality,
                },
            })
            .ToList();

        return fuzzyMatches;
    }

    private async Task<List<SearchItemDto>> SearchCoachesWithFuzzyAsync(string searchTerm)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var coaches = await context.Coaches.Include(c => c.Team).AsNoTracking().ToListAsync();

        var fuzzyMatches = coaches
            .Where(c =>
                (
                    c.FirstName != null
                    && CalculateLevenshteinDistance(searchTerm, c.FirstName.ToLower())
                        <= _fuzzySearchThreshold
                )
                || (
                    c.LastName != null
                    && CalculateLevenshteinDistance(searchTerm, c.LastName.ToLower())
                        <= _fuzzySearchThreshold
                )
            )
            .Select(c => new SearchItemDto
            {
                Id = c.Id.ToString(),
                Type = "Coach",
                Name = $"{c.FirstName} {c.LastName}",
                Description = $"{c.Role} - {c.Team?.Name ?? "No Team"}",
                ThumbnailUrl = c.PhotoUrl,
                Url = $"/coaches/{c.Id}",
                AdditionalData = new Dictionary<string, string?>
                {
                    ["Role"] = c.Role,
                    ["Team"] = c.Team?.Name,
                    ["Nationality"] = c.Nationality,
                },
            })
            .ToList();

        return fuzzyMatches;
    }

    private async Task<List<SearchItemDto>> SearchStadiumsWithFuzzyAsync(string searchTerm)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadiums = await context
            .Stadiums.Where(s => s.Name != null)
            .AsNoTracking()
            .ToListAsync();

        var fuzzyMatches = stadiums
            .Where(s =>
                CalculateLevenshteinDistance(searchTerm, s.Name!.ToLower()) <= _fuzzySearchThreshold
                || (
                    s.City != null
                    && CalculateLevenshteinDistance(searchTerm, s.City.ToLower())
                        <= _fuzzySearchThreshold
                )
            )
            .Select(s => new SearchItemDto
            {
                Id = s.Id.ToString(),
                Type = "Stadium",
                Name = s.Name ?? "Unknown Stadium",
                Description = $"{s.City}, {s.Country} - Capacity: {s.Capacity:N0}",
                ThumbnailUrl = s.ImageUrl,
                Url = $"/stadiums/{s.Id}",
                AdditionalData = new Dictionary<string, string?>
                {
                    ["City"] = s.City,
                    ["Country"] = s.Country,
                    ["Capacity"] = s.Capacity.ToString("N0"),
                },
            })
            .ToList();

        return fuzzyMatches;
    }

    // Strategy-specific search methods
    private async Task<List<Team>> SearchTeamsWithStrategyAsync(
        string query,
        SearchStrategy strategy,
        int limit
    )
    {
        return strategy switch
        {
            SearchStrategy.FullText when _enablePostgresFts => await SearchTeamsWithPostgreSqlAsync(
                query,
                limit
            ),
            SearchStrategy.Fuzzy => await SearchTeamsWithFuzzyStrategyAsync(query, limit),
            SearchStrategy.Hybrid => await SearchTeamsWithHybridStrategyAsync(query, limit),
            _ => (await unitOfWork.Teams.SearchAsync(query)).Take(limit).ToList(),
        };
    }

    private async Task<List<Player>> SearchPlayersWithStrategyAsync(
        string query,
        SearchStrategy strategy,
        int limit
    )
    {
        return strategy switch
        {
            SearchStrategy.FullText when _enablePostgresFts =>
                await SearchPlayersWithPostgreSqlAsync(query, limit),
            SearchStrategy.Fuzzy => await SearchPlayersWithFuzzyStrategyAsync(query, limit),
            SearchStrategy.Hybrid => await SearchPlayersWithHybridStrategyAsync(query, limit),
            _ => (await unitOfWork.Players.SearchAsync(query)).Take(limit).ToList(),
        };
    }

    private async Task<List<Coach>> SearchCoachesWithStrategyAsync(
        string query,
        SearchStrategy strategy,
        int limit
    )
    {
        return strategy switch
        {
            SearchStrategy.FullText when _enablePostgresFts =>
                await SearchCoachesWithPostgreSqlAsync(query, limit),
            SearchStrategy.Fuzzy => await SearchCoachesWithFuzzyStrategyAsync(query, limit),
            SearchStrategy.Hybrid => await SearchCoachesWithHybridStrategyAsync(query, limit),
            _ => (await unitOfWork.Coaches.SearchAsync(query)).Take(limit).ToList(),
        };
    }

    private async Task<List<Stadium>> SearchStadiumsWithStrategyAsync(
        string query,
        SearchStrategy strategy,
        int limit
    )
    {
        var normalizedQuery = query.ToLower().Trim();
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadiums = await context
            .Stadiums.Where(s =>
                s.Name!.ToLower().Contains(normalizedQuery)
                || s.City!.ToLower().Contains(normalizedQuery)
            )
            .AsNoTracking()
            .Take(limit)
            .ToListAsync();

        return stadiums;
    }

    private async Task<List<Season>> SearchSeasonsWithStrategyAsync(
        string query,
        SearchStrategy strategy,
        int limit
    )
    {
        var normalizedQuery = query.ToLower().Trim();
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var seasons = await context
            .Seasons.Where(s =>
                s.Name!.ToLower().Contains(normalizedQuery)
                || s.StartDate.ToString(CultureInfo.InvariantCulture).Contains(normalizedQuery)
                || s.EndDate.ToString(CultureInfo.InvariantCulture).Contains(normalizedQuery)
            )
            .AsNoTracking()
            .Take(limit)
            .ToListAsync();

        return seasons;
    }

    // Helper methods for specific strategies
    private async Task<List<Team>> SearchTeamsWithPostgreSqlAsync(string query, int limit)
    {
        var normalizedQuery = NormalizeSearchQuery(query);
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        return await context
            .Teams.FromSqlRaw(
                @"
                SELECT t.*
                FROM ""Teams"" t
                WHERE to_tsvector('english', COALESCE(t.""Name"", '') || ' ' || COALESCE(t.""League"", '') || ' ' || COALESCE(t.""City"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY ts_rank(to_tsvector('english', COALESCE(t.""Name"", '') || ' ' || COALESCE(t.""League"", '') || ' ' || COALESCE(t.""City"", '')), 
                                plainto_tsquery('english', {0})) DESC
                LIMIT {1}",
                normalizedQuery,
                limit
            )
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Player>> SearchPlayersWithPostgreSqlAsync(string query, int limit)
    {
        var normalizedQuery = NormalizeSearchQuery(query);
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        return await context
            .Players.FromSqlRaw(
                @"
                SELECT p.*
                FROM ""Players"" p
                WHERE to_tsvector('english', COALESCE(p.""FullName"", '') || ' ' || COALESCE(p.""KnownName"", '') || ' ' || COALESCE(p.""Position"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY ts_rank(to_tsvector('english', COALESCE(p.""FullName"", '') || ' ' || COALESCE(p.""KnownName"", '') || ' ' || COALESCE(p.""Position"", '')), 
                                plainto_tsquery('english', {0})) DESC
                LIMIT {1}",
                normalizedQuery,
                limit
            )
            .Include(p => p.Team)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Coach>> SearchCoachesWithPostgreSqlAsync(string query, int limit)
    {
        var normalizedQuery = NormalizeSearchQuery(query);
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        return await context
            .Coaches.FromSqlRaw(
                @"
                SELECT c.*
                FROM ""Coaches"" c
                WHERE to_tsvector('english', COALESCE(c.""FirstName"", '') || ' ' || COALESCE(c.""LastName"", '') || ' ' || COALESCE(c.""Role"", ''))
                      @@ plainto_tsquery('english', {0})
                ORDER BY ts_rank(to_tsvector('english', COALESCE(c.""FirstName"", '') || ' ' || COALESCE(c.""LastName"", '') || ' ' || COALESCE(c.""Role"", '')), 
                                plainto_tsquery('english', {0})) DESC
                LIMIT {1}",
                normalizedQuery,
                limit
            )
            .Include(c => c.Team)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Team>> SearchTeamsWithFuzzyStrategyAsync(string query, int limit)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var searchTerm = query.ToLower().Trim();
        var teams = await context.Teams.AsNoTracking().ToListAsync();

        return teams
            .Where(t =>
                t.Name != null
                && CalculateLevenshteinDistance(searchTerm, t.Name.ToLower())
                    <= _fuzzySearchThreshold
            )
            .Take(limit)
            .ToList();
    }

    private async Task<List<Player>> SearchPlayersWithFuzzyStrategyAsync(string query, int limit)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var searchTerm = query.ToLower().Trim();
        var players = await context.Players.Include(p => p.Team).AsNoTracking().ToListAsync();

        return players
            .Where(p =>
                (
                    p.FullName != null
                    && CalculateLevenshteinDistance(searchTerm, p.FullName.ToLower())
                        <= _fuzzySearchThreshold
                )
                || (
                    p.KnownName != null
                    && CalculateLevenshteinDistance(searchTerm, p.KnownName.ToLower())
                        <= _fuzzySearchThreshold
                )
            )
            .Take(limit)
            .ToList();
    }

    private async Task<List<Coach>> SearchCoachesWithFuzzyStrategyAsync(string query, int limit)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var searchTerm = query.ToLower().Trim();
        var coaches = await context.Coaches.Include(c => c.Team).AsNoTracking().ToListAsync();

        return coaches
            .Where(c =>
                (
                    c.FirstName != null
                    && CalculateLevenshteinDistance(searchTerm, c.FirstName.ToLower())
                        <= _fuzzySearchThreshold
                )
                || (
                    c.LastName != null
                    && CalculateLevenshteinDistance(searchTerm, c.LastName.ToLower())
                        <= _fuzzySearchThreshold
                )
            )
            .Take(limit)
            .ToList();
    }

    private async Task<List<Team>> SearchTeamsWithHybridStrategyAsync(string query, int limit)
    {
        var ftResults = await SearchTeamsWithPostgreSqlAsync(query, limit / 2);
        var fuzzyResults = await SearchTeamsWithFuzzyStrategyAsync(query, limit / 2);

        // Combine and deduplicate
        var combined = ftResults
            .Concat(fuzzyResults)
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return combined;
    }

    private async Task<List<Player>> SearchPlayersWithHybridStrategyAsync(string query, int limit)
    {
        var ftResults = await SearchPlayersWithPostgreSqlAsync(query, limit / 2);
        var fuzzyResults = await SearchPlayersWithFuzzyStrategyAsync(query, limit / 2);

        // Combine and deduplicate
        var combined = ftResults
            .Concat(fuzzyResults)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return combined;
    }

    private async Task<List<Coach>> SearchCoachesWithHybridStrategyAsync(string query, int limit)
    {
        var ftResults = await SearchCoachesWithPostgreSqlAsync(query, limit / 2);
        var fuzzyResults = await SearchCoachesWithFuzzyStrategyAsync(query, limit / 2);

        // Combine and deduplicate
        var combined = ftResults
            .Concat(fuzzyResults)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return combined;
    }

    // Search suggestion methods
    private async Task<List<SearchSuggestionDto>> GetTeamSuggestionsAsync(
        string normalizedQuery,
        int limit
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var teams = await context
            .Teams.Where(t => t.Name!.ToLower().Contains(normalizedQuery.ToLower()))
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return teams
            .Select(t => new SearchSuggestionDto
            {
                Text = t.Name ?? "",
                Type = "Team",
                Description = $"{t.City} - {t.League}",
                Relevance = CalculateRelevanceScore(normalizedQuery, t.Name ?? ""),
            })
            .ToList();
    }

    private async Task<List<SearchSuggestionDto>> GetPlayerSuggestionsAsync(
        string normalizedQuery,
        int limit
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var players = await context
            .Players.Where(p =>
                p.FullName!.ToLower().Contains(normalizedQuery.ToLower())
                || p.KnownName!.ToLower().Contains(normalizedQuery.ToLower())
            )
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return players
            .Select(p => new SearchSuggestionDto
            {
                Text = p.FullName ?? "",
                Type = "Player",
                Description = p.Position,
                Relevance = CalculateRelevanceScore(normalizedQuery, p.FullName ?? ""),
            })
            .ToList();
    }

    private async Task<List<SearchSuggestionDto>> GetCoachSuggestionsAsync(
        string normalizedQuery,
        int limit
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var coaches = await context
            .Coaches.Where(c =>
                c.FirstName!.ToLower().Contains(normalizedQuery.ToLower())
                || c.LastName!.ToLower().Contains(normalizedQuery.ToLower())
            )
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return coaches
            .Select(c => new SearchSuggestionDto
            {
                Text = $"{c.FirstName} {c.LastName}",
                Type = "Coach",
                Description = c.Role,
                Relevance = CalculateRelevanceScore(normalizedQuery, $"{c.FirstName} {c.LastName}"),
            })
            .ToList();
    }

    private async Task<List<SearchSuggestionDto>> GetStadiumSuggestionsAsync(
        string normalizedQuery,
        int limit
    )
    {
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var stadiums = await context
            .Stadiums.Where(s =>
                s.Name!.ToLower().Contains(normalizedQuery.ToLower())
                || s.City!.ToLower().Contains(normalizedQuery.ToLower())
            )
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return stadiums
            .Select(s => new SearchSuggestionDto
            {
                Text = s.Name ?? "",
                Type = "Stadium",
                Description = $"{s.City}, {s.Country}",
                Relevance = CalculateRelevanceScore(normalizedQuery, s.Name ?? ""),
            })
            .ToList();
    }

    private float CalculateRelevanceScore(string query, string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        var queryLower = query.ToLower();
        var textLower = text.ToLower();

        // Exact match gets highest score
        if (textLower == queryLower)
            return 1.0f;

        // Starts with query gets high score
        if (textLower.StartsWith(queryLower))
            return 0.8f;

        // Contains query gets medium score
        if (textLower.Contains(queryLower))
            return 0.6f;

        // Calculate based on Levenshtein distance
        var distance = CalculateLevenshteinDistance(queryLower, textLower);
        var maxLength = Math.Max(queryLower.Length, textLower.Length);

        return Math.Max(0f, 1f - (float)distance / maxLength);
    }
}
