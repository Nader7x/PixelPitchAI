using Application.Interfaces;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public sealed class UnitOfWork(
    FootballDbContext context,
    IPlayerRepository playerRepository,
    ISeasonRepository seasonRepository,
    IMatchRepository matchRepository,
    ITeamRepository teamRepository,
    IPlayerSeasonStatsRepository playerSeasonStatsRepository,
    ITeamSeasonStatsRepository teamSeasonStatsRepository,
    IMatchEventsRepository matchEventsRepository)
    : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IPlayerRepository Players { get; } = playerRepository;
    public ISeasonRepository Seasons { get; } = seasonRepository;
    public IMatchRepository Matches { get; } = matchRepository;
    public ITeamRepository Teams { get; } = teamRepository;
    public IPlayerSeasonStatsRepository PlayerSeasonStats { get; } = playerSeasonStatsRepository;
    public ITeamSeasonStatsRepository TeamSeasonStats { get; } = teamSeasonStatsRepository;
    public IMatchEventsRepository MatchEvents { get; } = matchEventsRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _transaction.CommitAsync();
        }
        catch
        {
            await _transaction.RollbackAsync();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            context.Dispose();
        }
        _disposed = true;
    }
}
