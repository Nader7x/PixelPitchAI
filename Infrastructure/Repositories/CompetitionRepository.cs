using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class CompetitionRepository(FootballDbContext context) : Repository<Competition>(context),ICompetitionRepository
{
    
}