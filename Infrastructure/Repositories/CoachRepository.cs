using Domain.Models;
using Domain.Repositories;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories;

public class CoachRepository(FootballDbContext context) : Repository<Coach>(context), ICoachRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<IEnumerable<Coach>> SearchAsync(string query)
    {
        return await _context.Coaches
            .Where(c => 
                c.FirstName.ToLower().Contains(query) ||
                c.LastName.ToLower().Contains(query) ||
                c.FullName.ToLower().Contains(query) ||
                c.Role.ToLower().Contains(query) ||
                c.Nationality.ToLower().Contains(query) ||
                (c.Team != null && c.Team.Name.ToLower().Contains(query)))
            .Include(c => c.Team)
            .ToListAsync();
    }
}
