using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FootballDbContext _context;
    private bool _disposed;
    private IDbContextTransaction? _transaction;


    public UnitOfWork(FootballDbContext context, UserManager<ApplicationUser> userManager, IStadiumsRepository stadiums)
    {
        _context = context;
        Stadiums = stadiums;
        _disposed = false;

        // Initialize repositories
        Teams = new TeamRepository(_context);
        Coaches = new CoachRepository(_context);
        Matches = new MatchRepository(_context);
        Players = new PlayerRepository(_context);
        Seasons = new SeasonRepository(_context);
        TeamSeasons = new TeamSeasonsRepository(_context);
        MatchEvents = new MatchEventsRepository(_context);
        Competitions = new CompetitionRepository(_context);
        Notifications = new NotificationRepository(_context);
        ApplicationUser = new ApplicationUserRepository(_context, userManager);
    }

    public IPlayerRepository Players { get; }
    public ISeasonRepository Seasons { get; }
    public IMatchRepository Matches { get; }
    public ITeamRepository Teams { get; }
    public ITeamSeasonsRepository TeamSeasons { get; }
    public IMatchEventsRepository MatchEvents { get; }
    public IApplicationUserRepository ApplicationUser { get; }
    public ICoachRepository Coaches { get; }
    public IStadiumsRepository Stadiums { get; }
    public ICompetitionRepository Competitions { get; }
    public INotificationRepository Notifications { get; }


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
            if (_transaction != null) await _transaction.CommitAsync();
        }
        catch
        {
            if (_transaction != null) await _transaction.RollbackAsync();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null) await _transaction.RollbackAsync();
        }
        finally
        {
            _transaction?.Dispose();
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

    ~UnitOfWork()
    {
        Dispose(false);
    }
}