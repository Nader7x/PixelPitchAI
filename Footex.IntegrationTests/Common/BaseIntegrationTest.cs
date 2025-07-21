using Domain.Interfaces;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static System.GC;

namespace Footex.IntegrationTests.Common;

[Collection("Database collection")]
public abstract class BaseIntegrationTest : IDisposable
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

    protected async Task FreeDbAsync<T>(DbSet<T> dbSet)
        where T : class
    {
        // Ensure the database is cleared before each test
        if (await dbSet.AnyAsync())
        {
            dbSet.RemoveRange(dbSet);
            await Context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        // Dispose of the scope to release resources
        FactoryServiceScope.Dispose();
        SuppressFinalize(this);
    }
}
