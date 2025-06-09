using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SeasonRepository(FootballDbContext context) : Repository<Season>(context), ISeasonRepository
{
    private readonly FootballDbContext _context = context;


    public async Task<Season?> GetByNameAsync(string name)
    {
        return await _context.Seasons
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task<Season?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.Seasons
            .FirstOrDefaultAsync(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase),
                cancellationToken);
    }


    public async Task<IReadOnlyList<Season>> GetActiveSeasons()
    {
        return await _context.Seasons
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsByCountry(string country)
    {
        return await _context.Seasons
            .Where(s => s.Country.ToLower() == country.ToLower())
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsByLeagueName(string leagueName)
    {
        return await _context.Seasons
            .Where(s => s.LeagueName.ToLower() == leagueName.ToLower())
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetSeasonMatches(int homeTeamSeasonId, int awayTeamSeasonId)
    {
        return await _context.Seasons
            .Include(s => s.Matches)!
            .ThenInclude(m => m.HomeTeam)
            .Include(s => s.Matches)
            .ThenInclude(m => m.AwayTeam)
            .Where(s => s.Id == homeTeamSeasonId || s.Id == awayTeamSeasonId)
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }
}