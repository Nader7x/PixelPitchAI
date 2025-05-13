using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamSeasonStatsRepository(FootballDbContext context) : Repository<TeamSeasonStats>(context), ITeamSeasonStatsRepository
{
    private readonly FootballDbContext _context = context;


    public async Task<TeamSeasonStats?> GetTeamStatsBySeasonAsync(int teamId, int seasonId)
    {
        return await _context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .FirstOrDefaultAsync(t => t.TeamId == teamId && t.SeasonId == seasonId);
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetByTeamIdAsync(int teamId)
    {
        return await _context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .Where(t => t.TeamId == teamId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetAllAsyncWithTeamsAndSeasons()
    {
        return await _context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .ToListAsync();
    }
    

    public async Task<IReadOnlyList<TeamSeasonStats>> GetBySeasonIdAsync(int seasonId)
    {
        return await _context.TeamSeasonStats
            .Include(t => t.Team)
            .Include(t => t.Season)
            .Where(t => t.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TeamSeasonStats>> GetLeagueTableAsync(int seasonId)
    {
        return await _context.TeamSeasonStats
            .Include(t => t.Team)
            .Where(t => t.SeasonId == seasonId)
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsScored)
            .ToListAsync();
    }
    
}
