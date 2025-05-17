using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamRepository(FootballDbContext context) : Repository<Team>(context), ITeamRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<Team?> GetByNameAsync(string? name)
    {
        return await _context.Teams
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Team>> GetTeamsByCriteriaAsync(Func<Team, bool> predicate)
    {
        var result = _context.Teams
            .AsEnumerable()
            .Where(predicate)
            .OrderBy(t => t.Name)
            .ToList();

        return await Task.FromResult(result);
    }
    
    async Task<List<Team>> ITeamRepository.GetByLeagueAsync(string league)
    {
        return await _context.Teams
            .Where(t => t.League.ToLower() == league.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
    async Task<List<Team>> ITeamRepository.GetByCountryAsync(string country)
    {
        return await _context.Teams
            .Where(t => t.Country.ToLower() == country.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Team>> GetWithStatsForSeasonAsync(int seasonId)
    {
        return await _context.Teams
            .Include(t => t.PlayerSeasonStats)
            .ThenInclude(ps => ps.Season)
            .Where(t => t.PlayerSeasonStats.Any(ps => ps.SeasonId == seasonId))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByCountryAsync(string country)
    {
        return await _context.Teams
            .Where(t => t.Country.ToLower() == country.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByLeagueAsync(string league)
    {
        return await _context.Teams
            .Where(t => t.League.ToLower() == league.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
    public async Task<IEnumerable<Team>> SearchAsync(string query)
    {
        return await _context.Teams
            .Where(t => 
                t.Name.ToLower().Contains(query) ||
                t.League.ToLower().Contains(query))
            .Include(t => t.Players)
            .Include(t => t.HomeMatches)
            .Include(t => t.AwayMatches)
            .ToListAsync();
    }

    public async Task<Team?> GetByIdAsyncWithStadium(int id)
    {
        return await _context.Teams
            .Include(t => t.Stadium)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}