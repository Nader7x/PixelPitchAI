using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(int id);
    Task<Team?> GetByNameAsync(string name);
    Task<IReadOnlyList<Team>> GetAllAsync();
    Task<IReadOnlyList<Team>> GetByCountryAsync(string country);
    Task<IReadOnlyList<Team>> GetByLeagueAsync(string league);
    
    Task<Team> AddAsync(Team team);
    void Update(Team team);
    void Remove(Team team);
}
