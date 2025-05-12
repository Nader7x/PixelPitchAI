using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlayerSeasonStatsRepository(FootballDbContext context) : IPlayerSeasonStatsRepository
{
    public async Task<PlayerSeasonStats?> GetByIdAsync(int id)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlayerSeasonStats?> GetPlayerStatsBySeasonAsync(int playerId, int seasonId, int? teamId = null)
    {
        var query = context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.PlayerId == playerId && p.SeasonId == seasonId);

        if (teamId.HasValue)
        {
            query = query.Where(p => p.TeamId == teamId.Value);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetAllAsync()
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetByPlayerIdAsync(int playerId)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.PlayerId == playerId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetBySeasonIdAsync(int seasonId)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetByTeamIdAsync(int teamId)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.TeamId == teamId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetTopScorersAsync(int seasonId, int count)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .OrderByDescending(p => p.Goals)
            .ThenByDescending(p => p.Goals/90)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetTopAssistsAsync(int seasonId, int count)
    {
        return await context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .OrderByDescending(p => p.Assists)
            .ThenByDescending(p => p.Assists/90)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PlayerSeasonStats> AddAsync(PlayerSeasonStats stats)
    {
        await context.PlayerSeasonStats.AddAsync(stats);
        return stats;
    }

    public void Update(PlayerSeasonStats stats)
    {
        context.PlayerSeasonStats.Attach(stats);
        context.Entry(stats).State = EntityState.Modified;
    }

    public void Remove(PlayerSeasonStats stats)
    {
        context.PlayerSeasonStats.Remove(stats);
    }
}
