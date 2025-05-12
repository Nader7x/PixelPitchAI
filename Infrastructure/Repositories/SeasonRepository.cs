using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SeasonRepository(FootballDbContext context) : ISeasonRepository
{
    public async Task<Season?> GetByIdAsync(int id)
    {
        return await context.Seasons.FindAsync(id);
    }

    public async Task<Season?> GetByNameAsync(string name)
    {
        return await context.Seasons
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Season>> GetAllAsync()
    {
        return await context.Seasons
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetActiveSeasons()
    {
        return await context.Seasons
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsByCountry(string country)
    {
        return await context.Seasons
            .Where(s => s.Country.ToLower() == country.ToLower())
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsByLeagueName(string leagueName)
    {
        return await context.Seasons
            .Where(s => s.LeagueName.ToLower() == leagueName.ToLower())
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
    }

    public async Task<Season> AddAsync(Season season)
    {
        await context.Seasons.AddAsync(season);
        return season;
    }

    public void Update(Season season)
    {
        context.Seasons.Attach(season);
        context.Entry(season).State = EntityState.Modified;
    }

    public void Remove(Season season)
    {
        context.Seasons.Remove(season);
    }
}