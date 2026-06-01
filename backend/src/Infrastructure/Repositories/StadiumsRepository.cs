using Domain.Models;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StadiumsRepository(FootballDbContext dbContext)
    : Repository<Stadium>(dbContext),
        IStadiumsRepository
{
    private readonly FootballDbContext _dbContext = dbContext;

    Task<Stadium?> IStadiumsRepository.GetStadiumByNameAsync(string name)
    {
        return _dbContext.Stadiums.FirstOrDefaultAsync(s =>
            s.Name != null && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
    }

    async Task<IEnumerable<Stadium>> IStadiumsRepository.SearchAsync(string query)
    {
        return await _dbContext
            .Stadiums.Where(s =>
                s.Name != null && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            )
            .ToListAsync();
    }
}
