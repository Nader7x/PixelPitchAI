using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public interface IMatchStreamLifecycle
{
    Task<bool> TryStartAsync(int matchId, CancellationToken cancellationToken);
    Task FailQueuedAsync(int matchId, CancellationToken cancellationToken);
}

/// <summary>Persists admission and worker state using scopes independent of HTTP requests.</summary>
public sealed class MatchStreamLifecycle(IServiceScopeFactory scopeFactory) : IMatchStreamLifecycle
{
    public async Task<bool> TryStartAsync(int matchId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var match = await unit.Matches.GetByIdAsync(matchId);
        if (match?.MatchStatus != "Queued")
            return false;
        match.MatchStatus = "In Progress";
        match.ModelSimulationStartTimeUtc = DateTime.UtcNow;
        match.UpdatedAt = DateTime.UtcNow;
        await unit.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task FailQueuedAsync(int matchId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var match = await unit.Matches.GetByIdAsync(matchId);
        if (match?.MatchStatus != "Queued")
            return;
        match.MatchStatus = "Failed";
        match.IsLive = false;
        match.UpdatedAt = DateTime.UtcNow;
        await unit.SaveChangesAsync(cancellationToken);
    }
}
