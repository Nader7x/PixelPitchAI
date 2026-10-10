using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IMatchEventBroadcaster
{
    ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default);
    ValueTask BroadcastStatisticsAsync(string matchId, object statistics, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SseMessage> SubscribeAsync(string matchId, CancellationToken cancellationToken);
    void ClearReplayBuffer(string matchId);
}

public readonly record struct SseMessage(string EventType, string Data, int EventIndex = 0);

public sealed class MatchEventBroadcaster : IMatchEventBroadcaster
{
    private const int MaxReplayBufferSize = 15;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>> _subscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SseMessage>> _recentMessages = new();
    private readonly ILogger<MatchEventBroadcaster> _logger;

    public MatchEventBroadcaster(ILogger<MatchEventBroadcaster> logger)
    {
        _logger = logger;
    }

    public ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(matchEvent, MatchEventJsonContext.Default.FootballMatchEvent);
        var msg = new SseMessage("match_event", json, matchEvent.event_index);

        // Record in ring buffer to eliminate connect race condition for late SSE clients
        var buffer = _recentMessages.GetOrAdd(matchId, _ => new ConcurrentQueue<SseMessage>());
        buffer.Enqueue(msg);
        while (buffer.Count > MaxReplayBufferSize && buffer.TryDequeue(out _)) { }

        if (_subscribers.TryGetValue(matchId, out var channels) && !channels.IsEmpty)
        {
            foreach (var (subId, channel) in channels)
            {
                if (!channel.Writer.TryWrite(msg))
                {
                    channels.TryRemove(subId, out _);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastStatisticsAsync(string matchId, object statistics, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(matchId, out var channels) || channels.IsEmpty)
            return ValueTask.CompletedTask;

        var json = statistics is string str ? str : JsonSerializer.Serialize(statistics);
        var msg = new SseMessage("match_statistics", json);

        foreach (var (subId, channel) in channels)
        {
            if (!channel.Writer.TryWrite(msg))
            {
                channels.TryRemove(subId, out _);
            }
        }

        return ValueTask.CompletedTask;
    }

    public void ClearReplayBuffer(string matchId)
    {
        _recentMessages.TryRemove(matchId, out _);
    }

    public async IAsyncEnumerable<SseMessage> SubscribeAsync(
        string matchId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subId = Guid.NewGuid();
        var channel = Channel.CreateBounded<SseMessage>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        var matchSubs = _subscribers.GetOrAdd(matchId, _ => new ConcurrentDictionary<Guid, Channel<SseMessage>>());
        matchSubs.TryAdd(subId, channel);
        _logger.LogInformation("Client {SubId} subscribed to match stream {MatchId}. Total subscribers: {Count}", subId, matchId, matchSubs.Count);

        // Replay recent buffered events to late-connecting subscribers
        var lastReplayedIndex = 0;
        if (_recentMessages.TryGetValue(matchId, out var recentBuffer))
        {
            var replaySnapshot = recentBuffer.ToArray();
            foreach (var replayMsg in replaySnapshot)
            {
                lastReplayedIndex = Math.Max(lastReplayedIndex, replayMsg.EventIndex);
                yield return replayMsg;
            }
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var waitTask = channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var delayTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

                var completedTask = await Task.WhenAny(waitTask, delayTask);
                if (completedTask == delayTask)
                {
                    // Yield keep-alive heartbeat comment
                    yield return new SseMessage("ping", string.Empty);
                    continue;
                }

                if (await waitTask)
                {
                    while (channel.Reader.TryRead(out var item))
                    {
                        // Deduplicate events already sent via the replay buffer
                        if (item.EventIndex > 0 && item.EventIndex <= lastReplayedIndex)
                            continue;

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
            if (_subscribers.TryGetValue(matchId, out var subs))
            {
                subs.TryRemove(subId, out _);
                if (subs.IsEmpty)
                {
                    _subscribers.TryRemove(KeyValuePair.Create(matchId, subs));
                }
                _logger.LogInformation("Client {SubId} unsubscribed from match stream {MatchId}", subId, matchId);
            }
        }
    }
}
