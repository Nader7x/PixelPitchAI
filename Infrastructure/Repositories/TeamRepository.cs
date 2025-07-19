using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamRepository(FootballDbContext context) : Repository<Team>(context), ITeamRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<Team?> GetByNameAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _context
            .Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name != null && t.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Team>> GetTeamsByCriteriaAsync(Func<Team, bool> predicate)
    {
        var result = _context.Teams.AsEnumerable().Where(predicate).OrderBy(t => t.Name).ToList();

        return await Task.FromResult(result);
    }

    async Task<List<Team>> ITeamRepository.GetByLeagueAsync(string league)
    {
        if (string.IsNullOrEmpty(league))
            return new List<Team>();

        return await _context
            .Teams.Where(t => t.League != null && t.League.ToLower() == league.ToLower())
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    async Task<List<Team>> ITeamRepository.GetByCountryAsync(string country)
    {
        if (string.IsNullOrEmpty(country))
            return new List<Team>();

        return await _context
            .Teams.Where(t => t.Country != null && t.Country.ToLower() == country.ToLower())
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Team>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Team>();

        var searchTerm = query.ToLower().Trim();

        return await _context
            .Teams.Where(t =>
                (t.Name != null && t.Name.ToLower().Contains(searchTerm))
                || (t.League != null && t.League.ToLower().Contains(searchTerm))
            )
            .AsSplitQuery()
            .Include(t => t.Players)
            .Include(t => t.HomeMatches)
            .Include(t => t.AwayMatches)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Team?> GetByIdAsyncWithStadium(int id)
    {
        return await _context.Teams.Include(t => t.Stadium).FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Team?> GetTeamWithPlayersAsync(int teamId)
    {
        return await _context
            .Teams.Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == teamId);
    }

    public async Task<Team?> GetTeamWithCoachesAsync(int teamId)
    {
        return await _context
            .Teams.Include(t => t.Coaches)
            .FirstOrDefaultAsync(t => t.Id == teamId);
    }

    public async Task<IEnumerable<Team>> GetByCityAsync(string city)
    {
        if (string.IsNullOrEmpty(city))
            return [];

        return await _context
            .Teams.Where(t => t.City != null && t.City.ToLower() == city.ToLower())
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .Cast<Team>()
            .ToListAsync();
    }

    public async Task<IEnumerable<Team>> GetTeamsFoundedAfterYearAsync(int year)
    {
        if (year <= 0)
            return [];

        return await _context
            .Teams.Where(t => t.FoundationDate != null && t.FoundationDate.Year > year)
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByCountryAsync(string country)
    {
        if (string.IsNullOrEmpty(country))
            return new List<Team>();

        return await _context
            .Teams.Where(t => t.Country != null && t.Country.ToLower() == country.ToLower())
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByLeagueAsync(string league)
    {
        if (string.IsNullOrEmpty(league))
            return new List<Team>();

        return await _context
            .Teams.Where(t => t.League != null && t.League.ToLower() == league.ToLower())
            .OrderBy(t => t.Name ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    public void ClearChangeTracker()
    {
        _context.ChangeTracker.Clear();
    }
}
