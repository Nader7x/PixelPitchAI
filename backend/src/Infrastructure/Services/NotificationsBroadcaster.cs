using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Application.Dtos.V2;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface INotificationsBroadcaster
{
    ValueTask BroadcastNotificationAsync(string userId, NotificationV2Dto notification, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SseMessage> SubscribeAsync(string userId, long? lastEventId, CancellationToken cancellationToken);
}

public sealed class NotificationsBroadcaster : INotificationsBroadcaster
{
    private const int MaxReplayBufferSize = 25;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>> _subscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SseMessage>> _recentMessages = new();
    private readonly ConcurrentDictionary<string, long> _sequenceCounters = new();
    private readonly ILogger<NotificationsBroadcaster> _logger;

    public NotificationsBroadcaster(ILogger<NotificationsBroadcaster> logger)
    {
        _logger = logger;
    }

    public ValueTask BroadcastNotificationAsync(
        string userId,
        NotificationV2Dto notification,
        CancellationToken cancellationToken = default
    )
    {
        var json = JsonSerializer.Serialize(notification, MatchEventJsonContext.Default.NotificationV2Dto);
        var seq = _sequenceCounters.AddOrUpdate(userId, 1, (_, current) => current + 1);
        var eventName = notification.Type switch
        {
            "simulation_started" => "simulation_started",
            "match_completed" => "match_completed",
            "experiment_completed" => "experiment_completed",
            _ => "notification",
        };
        var msg = new SseMessage(eventName, json, 0, seq);

        var buffer = _recentMessages.GetOrAdd(userId, _ => new ConcurrentQueue<SseMessage>());
        buffer.Enqueue(msg);
        while (buffer.Count > MaxReplayBufferSize && buffer.TryDequeue(out _)) { }

        if (_subscribers.TryGetValue(userId, out var channels) && !channels.IsEmpty)
        {
            foreach (var (subId, channel) in channels)
            {
                if (!channel.Writer.TryWrite(msg))
                    channels.TryRemove(subId, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SseMessage> SubscribeAsync(
        string userId,
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

        var userSubs = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<SseMessage>>());
        userSubs.TryAdd(subId, channel);
        _logger.LogInformation("Client {SubId} subscribed to notifications SSE for user {UserId}", subId, userId);

        var lastDeliveredSeq = lastEventId ?? 0;
        try
        {
            if (_recentMessages.TryGetValue(userId, out var recentBuffer))
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
            if (_subscribers.TryGetValue(userId, out var subs))
            {
                subs.TryRemove(subId, out _);
                if (subs.IsEmpty)
                    _subscribers.TryRemove(KeyValuePair.Create(userId, subs));
            }
        }
    }
}
