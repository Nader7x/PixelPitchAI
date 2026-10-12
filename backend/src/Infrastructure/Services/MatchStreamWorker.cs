using System.Collections.Concurrent;
using Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
///     Hosted background worker that consumes simulation streaming requests from IMatchStreamingQueue
///     and dispatches concurrent streaming tasks bounded by a concurrency semaphore to prevent head-of-line blocking.
/// </summary>
public sealed class MatchStreamWorker : BackgroundService
{
    private readonly IMatchStreamingQueue _queue;
    private readonly IMatchEventGrpcStreamConsumer _consumer;
    private readonly IOptions<EventIngestionOptions> _options;
    private readonly ILogger<MatchStreamWorker> _logger;
    private readonly IMatchStreamLifecycle _lifecycle;
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _activeStreams = new();

    public MatchStreamWorker(
        IMatchStreamingQueue queue,
        IMatchEventGrpcStreamConsumer consumer,
        IOptions<EventIngestionOptions> options,
        ILogger<MatchStreamWorker> logger,
        IMatchStreamLifecycle lifecycle)
    {
        _queue = queue;
        _consumer = consumer;
        _options = options;
        _logger = logger;
        _lifecycle = lifecycle;
    }

    public int ActiveStreamCount => _activeStreams.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxStreams = Math.Max(1, _options.Value.MaxConcurrentStreams);
        _logger.LogInformation("MatchStreamWorker started. Max concurrent streams: {MaxStreams}", maxStreams);

        using var semaphore = new SemaphoreSlim(maxStreams, maxStreams);
        Infrastructure.Protos.SimulateMatchRequest? pending = null;

        try
        {
            await foreach (var request in _queue.ReadAllAsync(stoppingToken))
            {
                pending = request;
                await semaphore.WaitAsync(stoppingToken);
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_activeStreams.TryAdd(request.MatchId, completion))
                {
                    _logger.LogWarning("Skipping duplicate active stream for match {MatchId}", request.MatchId);
                    semaphore.Release();
                    pending = null;
                    continue;
                }
                pending = null;
                // Register first, including streams that complete synchronously.
                _ = RunStreamAsync(request, completion, semaphore, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MatchStreamWorker shutting down due to cancellation request.");
        }
        finally
        {
            _queue.Complete();
            if (pending != null && !_activeStreams.ContainsKey(pending.MatchId))
                await FailQueuedAsync(pending.MatchId);
            while (_queue.TryRead(out var queued))
                if (queued != null && !_activeStreams.ContainsKey(queued.MatchId))
                    await FailQueuedAsync(queued.MatchId);
            if (!_activeStreams.IsEmpty)
            {
                _logger.LogInformation("Waiting for {Count} active streams to complete before worker shutdown...", _activeStreams.Count);
                await Task.WhenAll(_activeStreams.Values.Select(c => c.Task));
            }
        }
    }

    private async Task RunStreamAsync(Infrastructure.Protos.SimulateMatchRequest request,
        TaskCompletionSource completion, SemaphoreSlim semaphore, CancellationToken stoppingToken)
    {
        var started = false;
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            if (!int.TryParse(request.MatchId, out var matchId) ||
                !await _lifecycle.TryStartAsync(matchId, stoppingToken))
                return;
            started = true;
            await _consumer.StartConsumingMatchStreamAsync(request, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Match stream canceled for match {MatchId}", request.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Match stream failed for match {MatchId}", request.MatchId);
        }
        finally
        {
            if (!started)
                await FailQueuedAsync(request.MatchId);
            semaphore.Release();
            _activeStreams.TryRemove(request.MatchId, out _);
            completion.TrySetResult();
        }
    }

    private async Task FailQueuedAsync(string matchId)
    {
        if (!int.TryParse(matchId, out var id))
            return;
        try
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _lifecycle.FailQueuedAsync(id, cleanup.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear queued match {MatchId} during shutdown/start failure", matchId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();
        await base.StopAsync(cancellationToken);
    }
}
