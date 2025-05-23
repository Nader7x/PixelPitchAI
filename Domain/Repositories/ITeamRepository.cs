
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamRepository : IRepository<Team>
{
    Task<Team?> GetByNameAsync(string? name);
    Task<IReadOnlyList<Team>> GetTeamsByCriteriaAsync(Func<Team, bool> predicate);
    
    
    // Additional methods
    Task<List<Team>> GetByLeagueAsync(string league);
    Task<List<Team>> GetByCountryAsync(string country);
    Task<IEnumerable<Team>> SearchAsync(string query);
    Task<Team?> GetByIdAsyncWithStadium(int id);

}
