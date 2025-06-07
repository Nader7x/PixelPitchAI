using Domain.Interfaces;
using Domain.Models;

namespace Domain.Repositories;

public interface ISeasonRepository : IRepository<Season>
{
    Task<Season?> GetByNameAsync(string name);
    Task<Season?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Season>> GetActiveSeasons();
    Task<IReadOnlyList<Season>> GetSeasonsByCountry(string country);
    Task<IReadOnlyList<Season>> GetSeasonsByLeagueName(string leagueName);
    Task<IReadOnlyList<Season>> GetSeasonMatches(int homeTeamSeasonId, int awayTeamSeasonId);
}