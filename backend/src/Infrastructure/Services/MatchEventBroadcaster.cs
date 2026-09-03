using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Domain.Models;
using MessagePack;
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

    public async ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(matchId, out var channels) || channels.IsEmpty)
            return;

        var json = JsonSerializer.Serialize(matchEvent);
        var msg = new SseMessage("match_event", json);

        foreach (var (id, channel) in channels)
        {
            if (!channel.Writer.TryWrite(msg))
            {
                await channel.Writer.WriteAsync(msg, cancellationToken);
            }
        }
    }

    public async ValueTask BroadcastStatisticsAsync(string matchId, object statistics, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(matchId, out var channels) || channels.IsEmpty)
            return;

        var json = JsonSerializer.Serialize(statistics);
        var msg = new SseMessage("match_statistics", json);

        foreach (var (id, channel) in channels)
        {
            if (!channel.Writer.TryWrite(msg))
            {
                await channel.Writer.WriteAsync(msg, cancellationToken);
            }
        }
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
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }
        finally
        {
            if (_subscribers.TryGetValue(matchId, out var subs))
            {
                subs.TryRemove(subId, out _);
                _logger.LogInformation("Client {SubId} unsubscribed from match stream {MatchId}", subId, matchId);
            }
        }
    }
}
