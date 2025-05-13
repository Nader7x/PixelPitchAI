using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class CoachRepository(FootballDbContext context) : Repository<Coach>(context),ICoachRepository
{
    private readonly FootballDbContext _context = context;

}