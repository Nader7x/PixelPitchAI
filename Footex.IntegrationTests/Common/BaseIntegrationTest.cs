using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Footex.IntegrationTests.Common;

public abstract class BaseIntegrationTest : IClassFixture<FootexWebApplicationFactory>, IDisposable
{
    protected readonly FootexWebApplicationFactory Factory;
    protected readonly IServiceScope ServiceScope;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly FootballDbContext Context;

    protected BaseIntegrationTest(FootexWebApplicationFactory factory)
    {
        Factory = factory;
        ServiceScope = factory.Services.CreateScope();
        ServiceProvider = ServiceScope.ServiceProvider;
        Context = ServiceProvider.GetRequiredService<FootballDbContext>();
    }

    public virtual void Dispose()
    {
        ServiceScope?.Dispose();
    }
}
