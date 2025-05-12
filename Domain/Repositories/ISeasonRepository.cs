using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface ISeasonRepository
{
    Task<Season?> GetByIdAsync(int id);
    Task<Season?> GetByNameAsync(string name);
    Task<IReadOnlyList<Season>> GetAllAsync();
    Task<IReadOnlyList<Season>> GetActiveSeasons();
    Task<IReadOnlyList<Season>> GetSeasonsByCountry(string country);
    Task<IReadOnlyList<Season>> GetSeasonsByLeagueName(string leagueName);
    
    Task<Season> AddAsync(Season season);
    void Update(Season season);
    void Remove(Season season);
}
