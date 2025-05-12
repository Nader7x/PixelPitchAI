using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamSeasonStatsRepository(FootballDbContext context) : ITeamSeasonStatsRepository
{
    public async Task<TeamSeasonStats?> GetByIdAsync(int id)
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TeamSeasonStats?> GetTeamStatsBySeasonAsync(int teamId, int seasonId)
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .FirstOrDefaultAsync(t => t.TeamId == teamId && t.SeasonId == seasonId);
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetAllAsync()
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetByTeamIdAsync(int teamId)
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .Where(t => t.TeamId == teamId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetBySeasonIdAsync(int seasonId)
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .Where(t => t.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetLeagueTableAsync(int seasonId)
    {
        return await context.TeamSeasonStats
            .Include(t => t.Team)
            .Where(t => t.SeasonId == seasonId)
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsScored)
            .ToListAsync();
    }

    public async Task<TeamSeasonStats> AddAsync(TeamSeasonStats stats)
    {
        await context.TeamSeasonStats.AddAsync(stats);
        return stats;
    }

    public void Update(TeamSeasonStats stats)
    {
        context.TeamSeasonStats.Attach(stats);
        context.Entry(stats).State = EntityState.Modified;
    }

    public void Remove(TeamSeasonStats stats)
    {
        context.TeamSeasonStats.Remove(stats);
    }
}
