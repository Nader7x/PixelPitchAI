using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MatchEventsRepository(FootballDbContext context) : IMatchEventsRepository
{
    public async Task<MatchEvents?> GetByIdAsync(int id)
    {
        return await context.MatchEvents
            .Include(e => e.Match)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<MatchEvents?> GetByMatchIdAsync(int matchId)
    {
        return await context.MatchEvents
            .Include(e => e.Match)
            .FirstOrDefaultAsync(e => e.MatchId == matchId);
    }

    public async Task<IReadOnlyList<MatchEvents>> GetAllAsync()
    {
        return await context.MatchEvents
            .Include(e => e.Match)
            .ToListAsync();
    }

    public async Task<MatchEvents> AddAsync(MatchEvents events)
    {
        await context.MatchEvents.AddAsync(events);
        return events;
    }

    public void Update(MatchEvents events)
    {
        context.MatchEvents.Attach(events);
        context.Entry(events).State = EntityState.Modified;
    }

    public void Remove(MatchEvents events)
    {
        context.MatchEvents.Remove(events);
    }
}