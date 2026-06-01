using System.Collections;
using Domain.Interfaces;
using Infrastructure;
using MediatR;
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

    protected async Task FreeDbAsync(params object[] dbSets) // Accepts any objects, we'll cast them inside
    {
        if (dbSets.Length == 0)
            return;

        foreach (var dbSetObject in dbSets)
        {
            switch (dbSetObject)
            {
                // Check if the passed object is an IEnumerable (which DbSet<T> is)
                case IEnumerable<object> enumerableSet:
                    // If it's already IEnumerable<object>, use it directly
                    Context.RemoveRange(enumerableSet);
                    break;
                case IEnumerable nonGenericEnumerable:
                    // If it's a non-generic IEnumerable (like DbSet<T> cast to IEnumerable),
                    // cast its elements to the object to use RemoveRange.
                    Context.RemoveRange(nonGenericEnumerable.Cast<object>());
                    break;
                default:
                    // Optional: Log a warning or throw an exception if an unexpected type is passed
                    Console.WriteLine($"Warning: Skipping unexpected type in FreeDbAsync: {dbSetObject?.GetType().Name ?? "null"}");
                    break;
            }
        }

        await Context.SaveChangesAsync();
    }


    public void Dispose()
    {
        // Dispose of the scope to release resources
        FactoryServiceScope.Dispose();
        SuppressFinalize(this);
    }
}