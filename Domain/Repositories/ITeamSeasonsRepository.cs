
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamSeasonsRepository : IRepository<TeamSeasons>
{
    Task<IReadOnlyList<TeamSeasons>> GetSeasonsByTeamIdAsync(int teamId);
    Task<TeamSeasons?> GetByTeamAndSeasonIdAsync(int teamId, int seasonId);
    Task<IReadOnlyList<TeamSeasons>> GetTeamsBySeasonIdAsync(int seasonId);
}
