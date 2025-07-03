using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamSeasonsRepository(FootballDbContext context)
    : Repository<TeamSeasons>(context),
        ITeamSeasonsRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<IReadOnlyList<TeamSeasons>> GetSeasonsByTeamIdAsync(int teamId)
    {
        return await _context
            .TeamSeasons.Where(ts => ts.TeamId == teamId)
            .Include(ts => ts.Season)
            .ToListAsync();
    }

    public async Task<TeamSeasons?> GetByTeamAndSeasonIdAsync(int teamId, int seasonId)
    {
        return await _context.TeamSeasons.FirstOrDefaultAsync(ts =>
            ts.TeamId == teamId && ts.SeasonId == seasonId
        );
    }

    public async Task<IReadOnlyList<TeamSeasons>> GetTeamsBySeasonIdAsync(int seasonId)
    {
        return await _context
            .TeamSeasons.Where(ts => ts.SeasonId == seasonId)
            .Include(ts => ts.Team)
            .ToListAsync();
    }
}
