using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MatchEventsRepository : IMatchEventsRepository
{
    private readonly FootballDbContext _context;

    public MatchEventsRepository(FootballDbContext context)
    {
        _context = context;
    }

    public async Task<MatchEvents?> GetByIdAsync(int id)
    {
        return await _context.MatchEvents
            .Include(e => e.Match)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<MatchEvents?> GetByMatchIdAsync(int matchId)
    {
        return await _context.MatchEvents
            .Include(e => e.Match)
            .FirstOrDefaultAsync(e => e.MatchId == matchId);
    }

    public async Task<IReadOnlyList<MatchEvents>> GetAllAsync()
    {
        return await _context.MatchEvents
            .Include(e => e.Match)
            .ToListAsync();
    }

    public async Task<MatchEvents> AddAsync(MatchEvents events)
    {
        await _context.MatchEvents.AddAsync(events);
        return events;
    }

    public void Update(MatchEvents events)
    {
        _context.MatchEvents.Attach(events);
        _context.Entry(events).State = EntityState.Modified;
    }

    public void Remove(MatchEvents events)
    {
        _context.MatchEvents.Remove(events);
    }
}
