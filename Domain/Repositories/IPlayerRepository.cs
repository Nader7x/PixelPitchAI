using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(int id);
    Task<Player?> GetByFullNameAsync(string fullName);
    Task<Player?> GetByStatsBombIdAsync(int? statsBombId);
    Task<IReadOnlyList<Player>> GetAllAsync();
    Task<IReadOnlyList<Player>> GetByNationalityAsync(string nationality);
    Task<IReadOnlyList<Player>> GetByPreferredFootAsync(string preferredFoot);
    Task<IReadOnlyList<Player>> FindAsync(Func<Player, bool> predicate);
    
    Task<Player> AddAsync(Player player);
    void Update(Player player);
    void Remove(Player player);
}
