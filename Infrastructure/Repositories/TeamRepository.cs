using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly FootballDbContext _context;

    public TeamRepository(FootballDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> GetByIdAsync(int id)
    {
        return await _context.Teams.FindAsync(id);
    }

    public async Task<Team?> GetByNameAsync(string name)
    {
        return await _context.Teams
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Team>> GetAllAsync()
    {
        return await _context.Teams
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

    public async Task<Team> AddAsync(Team team)
    {
        await _context.Teams.AddAsync(team);
        return team;
    }

    public void Update(Team team)
    {
        _context.Teams.Attach(team);
        _context.Entry(team).State = EntityState.Modified;
    }

    public void Remove(Team team)
    {
        _context.Teams.Remove(team);
    }
}
