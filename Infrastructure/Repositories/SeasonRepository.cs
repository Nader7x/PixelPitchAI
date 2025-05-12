using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SeasonRepository : ISeasonRepository
{
    private readonly FootballDbContext _context;

    public SeasonRepository(FootballDbContext context)
    {
        _context = context;
    }

    public async Task<Season?> GetByIdAsync(int id)
    {
        return await _context.Seasons.FindAsync(id);
    }

    public async Task<Season?> GetByNameAsync(string name)
    {
        return await _context.Seasons
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task<IReadOnlyList<Season>> GetAllAsync()
    {
        return await _context.Seasons
            .OrderByDescending(s => s.CurrentRound)
            .ToListAsync();
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

    public async Task<Season> AddAsync(Season season)
    {
        await _context.Seasons.AddAsync(season);
        return season;
    }

    public void Update(Season season)
    {
        _context.Seasons.Attach(season);
        _context.Entry(season).State = EntityState.Modified;
    }

    public void Remove(Season season)
    {
        _context.Seasons.Remove(season);
    }
}
