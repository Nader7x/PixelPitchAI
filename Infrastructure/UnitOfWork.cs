using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private readonly FootballDbContext _context;
    private bool _disposed;


    public UnitOfWork(FootballDbContext context, UserManager<ApplicationUser> userManager, IStadiumsRepository stadiums)
    {
        _context = context;
        Stadiums = stadiums;
        _disposed = false;
        
        // Initialize repositories
        Players = new PlayerRepository(_context);
        Seasons = new SeasonRepository(_context);
        Matches = new MatchRepository(_context);
        Teams = new TeamRepository(_context);
        PlayerSeasonStats = new PlayerSeasonStatsRepository(_context);
        TeamSeasonStats = new TeamSeasonStatsRepository(_context);
        MatchEvents = new MatchEventsRepository(_context);
        ApplicationUserRepository = new ApplicationUserRepository(_context, userManager);
        Coaches = new CoachRepository(_context);
    }

    public IPlayerRepository Players { get; private set; }
    public ISeasonRepository Seasons { get; private set; }
    public IMatchRepository Matches { get; private set; }
    public ITeamRepository Teams { get; private set; }
    public IPlayerSeasonStatsRepository PlayerSeasonStats { get; private set; }
    public ITeamSeasonStatsRepository TeamSeasonStats { get; private set; }
    public IMatchEventsRepository MatchEvents { get; private set; }
    public IApplicationUserRepository ApplicationUserRepository { get; private set; }
    public ICoachRepository Coaches { get; private set; }
    public IStadiumsRepository Stadiums { get; }


    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
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
            _context.Dispose();
        }
        _disposed = true;
    }
}
