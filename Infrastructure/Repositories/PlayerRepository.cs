using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Repositories;

public class PlayerRepository(FootballDbContext context)
    : Repository<Player>(context),
        IPlayerRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<Player?> GetByFullNameAsync(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return null;

        return await _context
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.FullName != null && p.FullName.ToLower() == fullName.ToLower()
            );
    }

    public async Task<IEnumerable<Player>> GetByNationalityAsync(string nationality)
    {
        if (string.IsNullOrEmpty(nationality))
            return Enumerable.Empty<Player>();

        return await _context
            .Players.Where(p =>
                p.Nationality != null && p.Nationality.ToLower() == nationality.ToLower()
            )
            .OrderBy(p => p.FullName ?? "")
            .Skip(40)
            .Take(20)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Player>> GetByPreferredFootAsync(string? preferredFoot)
    {
        if (preferredFoot.IsNullOrEmpty())
            return Enumerable.Empty<Player>();

        if (preferredFoot!.ToLower() == "right")
            return await _context
                .Players.Where(p => p.PreferredFoot != null && p.PreferredFoot.ToLower() == "right")
                .OrderBy(p => p.FullName ?? "")
                .Skip(40)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

        return await _context
            .Players.Where(p =>
                p.PreferredFoot != null && p.PreferredFoot.ToLower() == preferredFoot.ToLower()
            )
            .OrderBy(p => p.FullName ?? "")
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<IEnumerable<Player>> FindAsync(Func<Player, bool> predicate)
    {
        var result = _context
            .Players.AsNoTracking()
            .AsEnumerable()
            .Where(predicate)
            .OrderBy(p => p.FullName ?? "")
            .Skip(40)
            .Take(20)
            .ToList();

        return Task.FromResult<IEnumerable<Player>>(result);
    }

    public async Task<IEnumerable<Player>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Player>();

        var searchTerm = query.ToLower().Trim();

        return await _context
            .Players.Where(p =>
                (p.FullName != null && p.FullName.ToLower().Contains(searchTerm))
                || (p.KnownName != null && p.KnownName.ToLower().Contains(searchTerm))
                || (p.Nationality != null && p.Nationality.ToLower().Contains(searchTerm))
                || (
                    p.Team != null
                    && p.Team.Name != null
                    && p.Team.Name.ToLower().Contains(searchTerm)
                )
            )
            .AsSplitQuery()
            .Include(p => p.Team)
            .AsNoTracking()
            .ToListAsync();
    }
}
