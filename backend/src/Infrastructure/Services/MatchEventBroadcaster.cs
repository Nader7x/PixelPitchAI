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
}

public readonly record struct SseMessage(string EventType, string Data);

public sealed class MatchEventBroadcaster : IMatchEventBroadcaster
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>> _subscribers = new();
    private readonly ILogger<MatchEventBroadcaster> _logger;

    public MatchEventBroadcaster(ILogger<MatchEventBroadcaster> logger)
    {
        _logger = logger;
    }

    public ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(matchId, out var channels) || channels.IsEmpty)
            return ValueTask.CompletedTask;

        var json = JsonSerializer.Serialize(matchEvent, MatchEventJsonContext.Default.FootballMatchEvent);
        var msg = new SseMessage("match_event", json);

        foreach (var (subId, channel) in channels)
        {
            if (!channel.Writer.TryWrite(msg))
            {
                channels.TryRemove(subId, out _);
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
