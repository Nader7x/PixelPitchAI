using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ICoachRepository : IRepository<Coach>
{
    Task<IEnumerable<Coach>> SearchAsync(string query);

}
