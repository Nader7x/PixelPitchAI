using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(int id);
    Task<IReadOnlyList<Match>> GetAllAsync();
    Task<IReadOnlyList<Match>> GetBySeasonIdAsync(int seasonId);
    Task<IReadOnlyList<Match>> GetByTeamIdAsync(int teamId);
    Task<IReadOnlyList<Match>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(int count);
    Task<IReadOnlyList<Match>> GetRecentMatchesAsync(int count);
    Task<IReadOnlyList<Match>> GetByStatusAsync(string status);
    
    Task<Match> AddAsync(Match match);
    void Update(Match match);
    void Remove(Match match);
}
