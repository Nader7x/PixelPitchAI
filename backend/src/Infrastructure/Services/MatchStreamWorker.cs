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
    private readonly ConcurrentDictionary<string, Task> _activeStreams = new();

    public MatchStreamWorker(
        IMatchStreamingQueue queue,
        IMatchEventGrpcStreamConsumer consumer,
        IOptions<EventIngestionOptions> options,
        ILogger<MatchStreamWorker> logger)
    {
        _queue = queue;
        _consumer = consumer;
        _options = options;
        _logger = logger;
    }

    public int ActiveStreamCount => _activeStreams.Count;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxStreams = Math.Max(1, _options.Value.MaxConcurrentStreams);
        _logger.LogInformation("MatchStreamWorker started. Max concurrent streams: {MaxStreams}", maxStreams);

        using var semaphore = new SemaphoreSlim(maxStreams, maxStreams);

        try
        {
            await foreach (var request in _queue.ReadAllAsync(stoppingToken))
            {
                await semaphore.WaitAsync(stoppingToken);

                var streamTask = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("MatchStreamWorker starting stream for match {MatchId}. Active streams: {ActiveCount}", request.MatchId, _activeStreams.Count + 1);
                        await _consumer.StartConsumingMatchStreamAsync(request, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Match stream canceled for match {MatchId}", request.MatchId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled error during gRPC match stream for match {MatchId}", request.MatchId);
                    }
                    finally
                    {
                        _activeStreams.TryRemove(request.MatchId, out _);
                        semaphore.Release();
                        _logger.LogInformation("MatchStreamWorker finished stream for match {MatchId}. Remaining active: {Remaining}", request.MatchId, _activeStreams.Count);
                    }
                }, stoppingToken);

                _activeStreams.TryAdd(request.MatchId, streamTask);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MatchStreamWorker shutting down due to cancellation request.");
        }
        finally
        {
            if (!_activeStreams.IsEmpty)
            {
                _logger.LogInformation("Waiting for {Count} active streams to complete before worker shutdown...", _activeStreams.Count);
                await Task.WhenAll(_activeStreams.Values);
            }
        }
    }
}
