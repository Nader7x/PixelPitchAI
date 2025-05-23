using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories;



public class PlayerRepository(FootballDbContext context) : Repository<Player>(context), IPlayerRepository
{
    private readonly FootballDbContext _context = context;



    public async Task<Player?> GetByFullNameAsync(string fullName)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.FullName.ToLower() == fullName.ToLower());
    }
    
    
    public async Task<IEnumerable<Player>> GetByNationalityAsync(string nationality)
    {
        return await _context.Players
            .Where(p => p.Nationality.ToLower() == nationality.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Player>> GetByPreferredFootAsync(string? preferredFoot)
    {
        return await _context.Players
            .Where(p => p.PreferredFoot.ToLower() == preferredFoot.ToLower())
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public new Task<IEnumerable<Player>> FindAsync(Func<Player, bool> predicate)
    {
        var result =  _context.Players
            .AsEnumerable()
            .Where(predicate)
            .OrderBy(p => p.FullName)
            .ToList();

        return Task.FromResult<IEnumerable<Player>>(result) ;
    }
    public async Task<IEnumerable<Player>> SearchAsync(string query)
    {
        return await _context.Players
            .Where(p => 
                p.FullName.ToLower().Contains(query) ||
                p.KnownName.ToLower().Contains(query) ||
                p.FullName.ToLower().Contains(query) ||
                p.Nationality.ToLower().Contains(query) ||
                p.Team.Name.ToLower().Contains(query))
            .Include(p => p.Team)
            .ToListAsync();
    }
    
}