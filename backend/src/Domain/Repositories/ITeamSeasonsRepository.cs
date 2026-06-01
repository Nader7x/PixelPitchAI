using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ITeamSeasonsRepository : IRepository<TeamSeason>
{
    Task<IReadOnlyList<TeamSeason>> GetSeasonsByTeamIdAsync(int teamId);
    Task<TeamSeason?> GetByTeamAndSeasonIdAsync(int teamId, int seasonId);
    Task<IReadOnlyList<TeamSeason>> GetTeamsBySeasonIdAsync(int seasonId);
}
