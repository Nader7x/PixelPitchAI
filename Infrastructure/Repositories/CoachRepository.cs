using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CoachRepository(FootballDbContext context)
    : Repository<Coach>(context),
        ICoachRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<IEnumerable<Coach>> SearchAsync(string query)
    {
        var searchTerm = query.ToLower().Trim();

        return await _context
            .Coaches.Where(c =>
                (c.FirstName != null && c.FirstName.ToLower().Contains(searchTerm))
                || (c.LastName != null && c.LastName.ToLower().Contains(searchTerm))
                || (
                    c.FirstName != null
                    && c.LastName != null
                    && (c.FirstName + " " + c.LastName).ToLower().Contains(searchTerm)
                )
                || (c.Role != null && c.Role.ToLower().Contains(searchTerm))
                || (c.Nationality != null && c.Nationality.ToLower().Contains(searchTerm))
                || (
                    c.Team != null
                    && c.Team.Name != null
                    && c.Team.Name.ToLower().Contains(searchTerm)
                )
            )
            .AsSplitQuery()
            .Include(c => c.Team)
            .AsNoTracking()
            .ToListAsync();
    }
}
