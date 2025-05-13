
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerSeasonStatsRepository : IRepository<PlayerSeasonStats>
{
    Task<PlayerSeasonStats?> GetPlayerStatsBySeasonAsync(int playerId, int seasonId, int? teamId = null);
    Task<IReadOnlyList<PlayerSeasonStats>> GetByPlayerIdAsync(int playerId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetTopScorersAsync(int seasonId, int count);
    Task<IReadOnlyList<PlayerSeasonStats>> GetTopAssistsAsync(int seasonId, int count);

}
