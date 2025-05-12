using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlayerRepository(FootballDbContext context) : IPlayerRepository
{
    public async Task<Player?> GetByIdAsync(int id)
    {
        return await context.Players.FindAsync(id);
    }

    public async Task<Player?> GetByFullNameAsync(string fullName)
    {
        return await context.Players
            .FirstOrDefaultAsync(p => p.FullName.ToLower() == fullName.ToLower());
    }

    public async Task<Player?> GetByStatsBombIdAsync(int? statsBombId)
    {
        if (!statsBombId.HasValue) return null;

        return await context.Players
            .FirstOrDefaultAsync(p => p.StatsBombPlayerId == statsBombId.Value);
    }

    public async Task<IReadOnlyList<Player>> GetAllAsync()
    {
        return await context.Players
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Player>> GetByNationalityAsync(string nationality)
    {
        return await context.Players
            .Where(p => p.Nationality.ToLower() == nationality.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Player>> GetByPreferredFootAsync(string preferredFoot)
    {
        return await context.Players
            .Where(p => p.PreferredFoot.ToLower() == preferredFoot.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public Task<IReadOnlyList<Player>> FindAsync(Func<Player, bool> predicate)
    {
        var result = context.Players
            .AsEnumerable()
            .Where(predicate)
            .OrderBy(p => p.FullName)
            .ToList();

        return Task.FromResult((IReadOnlyList<Player>)result);
    }

    public async Task<Player> AddAsync(Player player)
    {
        await context.Players.AddAsync(player);
        return player;
    }

    public void Update(Player player)
    {
        context.Players.Attach(player);
        context.Entry(player).State = EntityState.Modified;
    }

    public void Remove(Player player)
    {
        context.Players.Remove(player);
    }
}