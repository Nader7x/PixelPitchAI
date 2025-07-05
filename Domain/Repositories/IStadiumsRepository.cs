using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface IStadiumsRepository : IRepository<Stadium>
{
    Task<Stadium?> GetStadiumByNameAsync(string name);
    Task<IEnumerable<Stadium>> SearchAsync(string query);
}
