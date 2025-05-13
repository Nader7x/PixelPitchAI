using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MatchEventsRepository(FootballDbContext context) : Repository<MatchEvents>(context), IMatchEventsRepository
{
    private readonly FootballDbContext _context = context;



    public async Task<MatchEvents?> GetByMatchIdAsync(int matchId)
    {
        return await _context.MatchEvents
            .Include(e => e.Match)
            .FirstOrDefaultAsync(e => e.MatchId == matchId);
    }

    
}