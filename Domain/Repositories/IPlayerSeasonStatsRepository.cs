using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerSeasonStatsRepository
{
    Task<PlayerSeasonStats?> GetByIdAsync(int id);
    Task<PlayerSeasonStats?> GetPlayerStatsBySeasonAsync(int playerId, int seasonId, int? teamId = null);
    Task<IReadOnlyList<PlayerSeasonStats>> GetAllAsync();
    Task<IReadOnlyList<PlayerSeasonStats>> GetByPlayerIdAsync(int playerId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<PlayerSeasonStats>> GetTopScorersAsync(int seasonId, int count);
    Task<IReadOnlyList<PlayerSeasonStats>> GetTopAssistsAsync(int seasonId, int count);
    
    Task<PlayerSeasonStats> AddAsync(PlayerSeasonStats stats);
    void Update(PlayerSeasonStats stats);
    void Remove(PlayerSeasonStats stats);
}
