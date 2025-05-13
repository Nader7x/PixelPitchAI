using Domain.Repositories;

namespace Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPlayerRepository Players { get; }
    ISeasonRepository Seasons { get; }
    IMatchRepository Matches { get; }
    ITeamRepository Teams { get; }
    IPlayerSeasonStatsRepository PlayerSeasonStats { get; }
    ITeamSeasonStatsRepository TeamSeasonStats { get; }
    IMatchEventsRepository MatchEvents { get; }
    IApplicationUserRepository ApplicationUserRepository { get; }
    ICoachRepository Coaches { get; }
    IStadiumsRepository Stadiums { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}