using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface IMatchEventsRepository
{
    Task<MatchEvents?> GetByIdAsync(int id);
    Task<MatchEvents?> GetByMatchIdAsync(int matchId);
    Task<IReadOnlyList<MatchEvents>> GetAllAsync();
    
    Task<MatchEvents> AddAsync(MatchEvents events);
    void Update(MatchEvents events);
    void Remove(MatchEvents events);
}
