using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IMatchEventsRepository : IRepository<MatchEvents>
{
    Task<MatchEvents?> GetByMatchIdAsync(int matchId);
}