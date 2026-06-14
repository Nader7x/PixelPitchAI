using System.Collections;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static System.GC;

namespace Footex.IntegrationTests.Common;

[Collection("Database collection")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly IServiceScope FactoryServiceScope;
    protected readonly IMediator Mediator;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly FootballDbContext Context;

    protected BaseIntegrationTest(FootexWebApplicationFactory factory)
    {
        FactoryServiceScope = factory.Services.CreateScope();
        Mediator = FactoryServiceScope.ServiceProvider.GetRequiredService<IMediator>();
        UnitOfWork = FactoryServiceScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Context = FactoryServiceScope.ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync()
    {
        FactoryServiceScope.Dispose();
        return Task.CompletedTask;
    }

    protected async Task FreeDbAsync(params object[] dbSets) // Accepts any objects, we'll cast them inside
    {
        if (dbSets.Length == 0)
            return;

        foreach (var dbSetObject in dbSets)
        {
            switch (dbSetObject)
            {
                case DbSet<Match> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<MatchEvents> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Team> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Player> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Coach> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Competition> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Stadium> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Season> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<TeamSeason> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<RefreshToken> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<Notification> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<MatchStatistics> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case DbSet<ApplicationUser> dbSet:
                    await dbSet.ExecuteDeleteAsync();
                    break;
                case IEnumerable<object> enumerableSet:
                    Context.RemoveRange(enumerableSet);
                    break;
                case IEnumerable nonGenericEnumerable:
                    Context.RemoveRange(nonGenericEnumerable.Cast<object>());
                    break;
                default:
                    Console.WriteLine($"Warning: Skipping unexpected type in FreeDbAsync: {dbSetObject?.GetType().Name ?? "null"}");
                    break;
            }
        }

        await Context.SaveChangesAsync();
    }
}