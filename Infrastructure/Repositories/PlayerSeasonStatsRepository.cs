using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlayerSeasonStatsRepository(FootballDbContext context) : Repository<PlayerSeasonStats>(context), IPlayerSeasonStatsRepository
{
    private readonly FootballDbContext _context = context;



    public async Task<PlayerSeasonStats?> GetPlayerStatsBySeasonAsync(int playerId, int seasonId, int? teamId = null)
    {
        var query = _context.PlayerSeasonStats
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



    public async Task<IReadOnlyList<PlayerSeasonStats>> GetByPlayerIdAsync(int playerId)
    {
        return await _context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.PlayerId == playerId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetBySeasonIdAsync(int seasonId)
    {
        return await _context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetByTeamIdAsync(int teamId)
    {
        return await _context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Season)
            .Include(p => p.Team)
            .Where(p => p.TeamId == teamId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetTopScorersAsync(int seasonId, int count)
    {
        return await _context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .OrderByDescending(p => p.Goals)
            .ThenByDescending(p => p.Goals / 90)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PlayerSeasonStats>> GetTopAssistsAsync(int seasonId, int count)
    {
        return await _context.PlayerSeasonStats
            .Include(p => p.Player)
            .Include(p => p.Team)
            .Where(p => p.SeasonId == seasonId)
            .OrderByDescending(p => p.Assists)
            .ThenByDescending(p => p.Assists / 90)
            .Take(count)
            .ToListAsync();
    }
    
}