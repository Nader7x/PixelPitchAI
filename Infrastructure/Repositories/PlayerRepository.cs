using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly FootballDbContext _context;

    public PlayerRepository(FootballDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(int id)
    {
        return await _context.Players.FindAsync(id);
    }

    public async Task<Player?> GetByFullNameAsync(string fullName)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.FullName.ToLower() == fullName.ToLower());
    }

    public async Task<Player?> GetByStatsBombIdAsync(int? statsBombId)
    {
        if (!statsBombId.HasValue) return null;
        
        return await _context.Players
            .FirstOrDefaultAsync(p => p.StatsBombPlayerId == statsBombId.Value);
    }

    public async Task<IReadOnlyList<Player>> GetAllAsync()
    {
        return await _context.Players
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Player>> GetByNationalityAsync(string nationality)
    {
        return await _context.Players
            .Where(p => p.Nationality.ToLower() == nationality.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Player>> GetByPreferredFootAsync(string preferredFoot)
    {
        return await _context.Players
            .Where(p => p.PreferredFoot.ToLower() == preferredFoot.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public Task<IReadOnlyList<Player>> FindAsync(Func<Player, bool> predicate)
    {
        var result = _context.Players
            .AsEnumerable()
            .Where(predicate)
            .OrderBy(p => p.FullName)
            .ToList();
            
        return Task.FromResult((IReadOnlyList<Player>)result);
    }

    public async Task<Player> AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
        return player;
    }

    public void Update(Player player)
    {
        _context.Players.Attach(player);
        _context.Entry(player).State = EntityState.Modified;
    }

    public void Remove(Player player)
    {
        _context.Players.Remove(player);
    }
}
