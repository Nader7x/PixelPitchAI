
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IMatchRepository : IRepository<Match>
{
    Task<IReadOnlyList<Match>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<Match>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<Match>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(int count);
    Task<IReadOnlyList<Match>> GetRecentMatchesAsync(int count);
    Task<IReadOnlyList<Match>> GetByStatusAsync(string status);
    Task<IReadOnlyList<Match>> GetAllWithDetailsAsync();

    Task<Match?> GetByIdWithDetailsAsync(int matchId);
}
