
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerRepository : IRepository<Player>
{
    Task<Player?> GetByFullNameAsync(string fullName);
    Task<Player?> GetByStatsBombIdAsync(int? statsBombId);
    Task<IReadOnlyList<Player>> GetByNationalityAsync(string nationality);
    Task<IReadOnlyList<Player>> GetByPreferredFootAsync(string preferredFoot);
    Task<IReadOnlyList<Player>> FindAsync(Func<Player, bool> predicate);
    

}
