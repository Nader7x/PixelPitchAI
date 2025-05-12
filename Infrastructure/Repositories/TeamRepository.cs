using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TeamRepository(FootballDbContext context) : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(int id)
    {
        return await context.Teams.FindAsync(id);
    }

    public async Task<Team?> GetByNameAsync(string name)
    {
        return await context.Teams
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Team>> GetAllAsync()
    {
        return await context.Teams
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByCountryAsync(string country)
    {
        return await context.Teams
            .Where(t => t.Country.ToLower() == country.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Team>> GetByLeagueAsync(string league)
    {
        return await context.Teams
            .Where(t => t.League.ToLower() == league.ToLower())
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Team> AddAsync(Team team)
    {
        await context.Teams.AddAsync(team);
        return team;
    }

    public void Update(Team team)
    {
        context.Teams.Attach(team);
        context.Entry(team).State = EntityState.Modified;
    }

    public void Remove(Team team)
    {
        context.Teams.Remove(team);
    }
}