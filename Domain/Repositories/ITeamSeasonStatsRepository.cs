
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamSeasonStatsRepository : IRepository<TeamSeasonStats>
{
    Task<TeamSeasonStats?> GetTeamStatsBySeasonAsync(int teamId, int seasonId);
    Task<IReadOnlyList<TeamSeasonStats>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<TeamSeasonStats>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<TeamSeasonStats>> GetLeagueTableAsync(int seasonId);
    Task<IReadOnlyList<TeamSeasonStats>> GetAllAsyncWithTeamsAndSeasons();


}
