using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class StadiumsRepository(FootballDbContext dbContext) : Repository<Stadium>(dbContext), IStadiumsRepository
{
    private readonly FootballDbContext _dbContext = dbContext;
}