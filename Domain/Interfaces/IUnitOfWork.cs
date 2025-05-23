using Domain.Repositories;

namespace Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPlayerRepository Players { get; }
    ISeasonRepository Seasons { get; }
    IMatchRepository Matches { get; }
    ITeamRepository Teams { get; }
    ITeamSeasonsRepository TeamSeasons { get; }
    IMatchEventsRepository MatchEvents { get; }
    IApplicationUserRepository ApplicationUser { get; }
    ICoachRepository Coaches { get; }
    IStadiumsRepository Stadiums { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}