using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.Logging;
using Domain.Repositories;

namespace Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ICoachRepository _coachRepository;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        ITeamRepository teamRepository,
        IMatchRepository matchRepository,
        IPlayerRepository playerRepository,
        ICoachRepository coachRepository,
        ILogger<SearchService> logger)
    {
        _teamRepository = teamRepository;
        _matchRepository = matchRepository;
        _playerRepository = playerRepository;
        _coachRepository = coachRepository;
        _logger = logger;
    }

    public async Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new SearchResultDto
            {
                TotalResults = 0,
                CurrentPage = page,
                TotalPages = 0,
                PageSize = pageSize,
                Items = new List<SearchItemDto>()
            };
        }

        try
        {
            // Normalize the query
            query = query.ToLower().Trim();

            // Get results from each repository
            var teamResults = await SearchTeamsAsync(query);
            var matchResults = await SearchMatchesAsync(query);
            var playerResults = await SearchPlayersAsync(query);
            var coachResults = await SearchCoachesAsync(query);

            // Combine all results
            var allResults = new List<SearchItemDto>();
            allResults.AddRange(teamResults);
            allResults.AddRange(matchResults);
            allResults.AddRange(playerResults);
            allResults.AddRange(coachResults);

            // Calculate pagination
            var totalResults = allResults.Count;
            var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

            // Apply pagination
            var pagedResults = allResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new SearchResultDto
            {
                TotalResults = totalResults,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Items = pagedResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching for '{Query}'", query);
            throw;
        }
    }

    private async Task<List<SearchItemDto>> SearchTeamsAsync(string query)
    {
        var teams = await _teamRepository.SearchAsync(query);
        return teams.Select(team => new SearchItemDto
        {
            Id = team.Id.ToString(),
            Type = "Team",
            Name = team.Name,
            Description = $"{team.City} - Founded in {team.FoundationDate}",
            ThumbnailUrl = team.Logo,
            Url = $"/teams/{team.Id}",
            AdditionalData = new Dictionary<string, string?>
            {
                { "Location", team.City },
                { "League", team.League }
            }
        }).ToList();
    }

    private async Task<List<SearchItemDto>> SearchMatchesAsync(string query)
    {
        var matches = await _matchRepository.SearchAsync(query);
        return matches.Select(match => new SearchItemDto
        {
            Id = match.Id.ToString(),
            Type = "Match",
            Name = $"{match.HomeTeam?.Name} vs {match.AwayTeam?.Name}",
            Description = $"Match on {match.ScheduledDateTimeUTC:d} at {match.Stadium.Name}",
            ThumbnailUrl = "/images/default-match.png", // Default image
            Url = $"/matches/{match.Id}",
            AdditionalData = new Dictionary<string, string?>
            {
                { "Date", match.ScheduledDateTimeUTC.ToString("yyyy-MM-dd") },
                { "Venue", match.Stadium.Name },
                { "Status", match.MatchStatus.ToString() }
            }
        }).ToList();
    }

    private async Task<List<SearchItemDto>> SearchPlayersAsync(string query)
    {
        var players = await _playerRepository.SearchAsync(query);
        return players.Select(player => new SearchItemDto
        {
            Id = player.Id.ToString(),
            Type = "Player",
            Name = player.FullName,
            Description = $"{player.ShirtNumber} - {player.Team?.Name ?? "Free Agent"}",
            ThumbnailUrl = player.PhotoUrl ?? "/images/default-player.png",
            Url = $"/players/{player.Id}",
            AdditionalData = new Dictionary<string, string?>
            {
                { "Position", player.ShirtNumber.ToString() },
                { "Nationality", player.Nationality },
            }
        }).ToList();
    }

    private async Task<List<SearchItemDto>> SearchCoachesAsync(string query)
    {
        var coaches = await _coachRepository.SearchAsync(query);
        return coaches.Select(coach => new SearchItemDto
        {
            Id = coach.Id.ToString(),
            Type = "Coach",
            Name = coach.FullName,
            Description = $"{coach.Role} - {coach.Team?.Name ?? "Unattached"}",
            ThumbnailUrl = coach.ProfileImageUrl ?? "/images/default-coach.png",
            Url = $"/coaches/{coach.Id}",
            AdditionalData = new Dictionary<string, string>
            {
                { "Role", coach.Role },
                { "Nationality", coach.Nationality },
                { "Experience", $"{coach.YearsOfExperience} years" }
            }
        }).ToList();
    }

    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}