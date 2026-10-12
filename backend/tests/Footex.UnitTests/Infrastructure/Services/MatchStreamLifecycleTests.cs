using Domain.Interfaces;
using Domain.Models;
using global::Infrastructure;
using global::Infrastructure.Repositories;
using global::Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchStreamLifecycleTests
{
    [Fact]
    public async Task RepositoryAndLifecyclePersistAcrossFreshScopes()
    {
        var services = new ServiceCollection();
        var name = Guid.NewGuid().ToString();
        services.AddDbContext<FootballDbContext>(options => options.UseInMemoryDatabase(name));
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var db = provider.GetRequiredService<FootballDbContext>();
            var unit = new Mock<IUnitOfWork>();
            unit.SetupGet(u => u.Matches).Returns(new MatchRepository(db));
            unit.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => db.SaveChangesAsync(token));
            return unit.Object;
        });
        using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
            db.Matches.Add(new Domain.Models.Match { Id = 42, CreatorId = "user", MatchStatus = "Queued", IsLive = true });
            await db.SaveChangesAsync();
            Assert.NotNull(await new MatchRepository(db).UpdateSimulationIdAsync(42, "grpc_42", default));
        }
        var lifecycle = new MatchStreamLifecycle(provider.GetRequiredService<IServiceScopeFactory>());
        Assert.True(await lifecycle.TryStartAsync(42, default));
        Assert.False(await lifecycle.TryStartAsync(42, default));
        await lifecycle.FailQueuedAsync(42, default);
        using (var scope = provider.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<FootballDbContext>().Matches.FindAsync(42);
            Assert.Equal("grpc_42", match!.SimulationId);
            Assert.Equal("In Progress", match.MatchStatus);
            Assert.True(match.IsLive);
            Assert.NotNull(match.ModelSimulationStartTimeUtc);
        }
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
            db.Matches.Add(new Domain.Models.Match { Id = 43, CreatorId = "user", MatchStatus = "Queued", IsLive = true });
            await db.SaveChangesAsync();
        }
        await lifecycle.FailQueuedAsync(43, default);
        using (var scope = provider.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<FootballDbContext>().Matches.FindAsync(43);
            Assert.Equal("Failed", match!.MatchStatus);
            Assert.False(match.IsLive);
        }
    }
}
