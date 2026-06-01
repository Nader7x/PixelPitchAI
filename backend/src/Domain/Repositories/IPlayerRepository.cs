using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IPlayerRepository : IRepository<Player>
{
    Task<Player?> GetByFullNameAsync(string? fullName);
    Task<IEnumerable<Player>> GetByNationalityAsync(string nationality);
    Task<IEnumerable<Player>> GetByPreferredFootAsync(string? preferredFoot);
    new Task<IEnumerable<Player>> FindAsync(Func<Player, bool> predicate);
    Task<IEnumerable<Player>> SearchAsync(string query);
    Task<Player?> GetByShirtNumberAndTeamAsync(int shirtNumber, int teamId);
}
