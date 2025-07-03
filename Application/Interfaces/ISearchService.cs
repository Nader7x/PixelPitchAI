using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 10);
    Task<SearchResultDto> SearchAllAsync(
        string query,
        int page = 1,
        int pageSize = 10,
        bool enableFuzzySearch = false
    );
    Task<List<Team>> SearchTeamsAsync(string query, int limit = 10, bool enableFuzzySearch = false);
    Task<List<Player>> SearchPlayersAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    );
    Task<List<Coach>> SearchCoachesAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    );
    Task<List<Stadium>> SearchStadiumsAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    );
    Task<List<Season>> SearchSeasonsAsync(
        string query,
        int limit = 10,
        bool enableFuzzySearch = false
    );
}
