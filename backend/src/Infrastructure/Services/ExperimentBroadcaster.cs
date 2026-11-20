using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Application.Dtos.V2;

namespace Infrastructure.Services;

public interface IExperimentBroadcaster
{
    ValueTask BroadcastProgressAsync(string experimentId, ExperimentProgressDto progress, CancellationToken cancellationToken = default);
    ValueTask BroadcastCompletedAsync(string experimentId, MonteCarloExperimentResponseDto result, CancellationToken cancellationToken = default);
    ValueTask BroadcastFailedAsync(string experimentId, ExperimentProgressDto error, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SseMessage> SubscribeAsync(string experimentId, long? lastEventId, CancellationToken cancellationToken);
}

public sealed class ExperimentBroadcaster : IExperimentBroadcaster
{
    private const int MaxReplayBufferSize = 25;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>> _subscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SseMessage>> _recentMessages = new();
    private readonly ConcurrentDictionary<string, long> _sequenceCounters = new();

    public ValueTask BroadcastProgressAsync(string experimentId, ExperimentProgressDto progress, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(progress, MatchEventJsonContext.Default.ExperimentProgressDto);
        Publish(experimentId, "experiment_progress", json);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastCompletedAsync(string experimentId, MonteCarloExperimentResponseDto result, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(result, MatchEventJsonContext.Default.MonteCarloExperimentResponseDto);
        Publish(experimentId, "experiment_completed", json);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastFailedAsync(string experimentId, ExperimentProgressDto error, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(error, MatchEventJsonContext.Default.ExperimentProgressDto);
        Publish(experimentId, "experiment_failed", json);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SseMessage> SubscribeAsync(
        string experimentId,
        long? lastEventId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var subId = Guid.NewGuid();
        var channel = Channel.CreateBounded<SseMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        var expSubs = _subscribers.GetOrAdd(experimentId, _ => new ConcurrentDictionary<Guid, Channel<SseMessage>>());
        expSubs.TryAdd(subId, channel);

        var lastDeliveredSeq = lastEventId ?? 0;
        try
        {
            if (_recentMessages.TryGetValue(experimentId, out var recentBuffer))
            {
                foreach (var replayMsg in recentBuffer.ToArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (lastEventId.HasValue && replayMsg.SequenceNo <= lastEventId.Value)
                        continue;

                    lastDeliveredSeq = Math.Max(lastDeliveredSeq, replayMsg.SequenceNo);
                    yield return replayMsg;
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var waitTask = channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var delayTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

                var completedTask = await Task.WhenAny(waitTask, delayTask);
                if (completedTask == delayTask)
                {
                    yield return new SseMessage("ping", string.Empty);
                    continue;
                }

                if (await waitTask)
                {
                    while (channel.Reader.TryRead(out var item))
                    {
                        if (item.SequenceNo <= lastDeliveredSeq)
                            continue;

                        lastDeliveredSeq = Math.Max(lastDeliveredSeq, item.SequenceNo);
                        yield return item;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            if (_subscribers.TryGetValue(experimentId, out var subs))
            {
                subs.TryRemove(subId, out _);
                if (subs.IsEmpty)
                    _subscribers.TryRemove(KeyValuePair.Create(experimentId, subs));
            }
        }
    }

    private void Publish(string experimentId, string eventType, string json)
    {
        var seq = _sequenceCounters.AddOrUpdate(experimentId, 1, (_, current) => current + 1);
        var msg = new SseMessage(eventType, json, 0, seq);

        var buffer = _recentMessages.GetOrAdd(experimentId, _ => new ConcurrentQueue<SseMessage>());
        buffer.Enqueue(msg);
        while (buffer.Count > MaxReplayBufferSize && buffer.TryDequeue(out _)) { }

        if (_subscribers.TryGetValue(experimentId, out var channels) && !channels.IsEmpty)
        {
            foreach (var (subId, channel) in channels)
            {
                if (!channel.Writer.TryWrite(msg))
                    channels.TryRemove(subId, out _);
            }
        }
    }
}
