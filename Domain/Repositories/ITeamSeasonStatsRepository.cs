using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamSeasonStatsRepository
{
    Task<TeamSeasonStats?> GetByIdAsync(int id);
    Task<TeamSeasonStats?> GetTeamStatsBySeasonAsync(int teamId, int seasonId);
    Task<IReadOnlyList<TeamSeasonStats>> GetAllAsync();
    Task<IReadOnlyList<TeamSeasonStats>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<TeamSeasonStats>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<TeamSeasonStats>> GetLeagueTableAsync(int seasonId);
    
    Task<TeamSeasonStats> AddAsync(TeamSeasonStats stats);
    void Update(TeamSeasonStats stats);
    void Remove(TeamSeasonStats stats);
}
