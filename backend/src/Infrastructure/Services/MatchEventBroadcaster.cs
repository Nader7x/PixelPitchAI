using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Application.Dtos;
using Application.Dtos.V2;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IMatchEventBroadcaster
{
    ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default);
    ValueTask BroadcastStatisticsAsync(string matchId, object statistics, CancellationToken cancellationToken = default);
    ValueTask BroadcastStructuredEventAsync(string matchId, StructuredEventV2Dto matchEvent, long sequenceNo, CancellationToken cancellationToken = default);
    ValueTask BroadcastSpatialFrameAsync(string matchId, SpatialFrameV2Dto spatialFrame, long sequenceNo, CancellationToken cancellationToken = default);
    ValueTask BroadcastTelemetryAsync(string matchId, TelemetrySnapshotV2Dto telemetry, long sequenceNo, CancellationToken cancellationToken = default);
    ValueTask BroadcastCommentaryAsync(string matchId, CommentaryCueV2Dto commentary, long sequenceNo, CancellationToken cancellationToken = default);
    ValueTask BroadcastLifecycleAsync(string matchId, MatchLifecycleV2Dto lifecycle, long sequenceNo, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SseMessage> SubscribeAsync(string matchId, CancellationToken cancellationToken);
    IAsyncEnumerable<SseMessage> SubscribeAsync(string matchId, long? lastEventId, CancellationToken cancellationToken);
    void ClearReplayBuffer(string matchId);
}

public readonly record struct SseMessage(string EventType, string Data, int EventIndex = 0, long SequenceNo = 0);

public sealed class MatchEventBroadcaster : IMatchEventBroadcaster
{
    private const int MaxReplayBufferSize = 100;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>> _subscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<SseMessage>> _recentMessages = new();
    private readonly ConcurrentDictionary<string, long> _sequenceCounters = new();
    private readonly ILogger<MatchEventBroadcaster> _logger;

    public MatchEventBroadcaster(ILogger<MatchEventBroadcaster> logger)
    {
        _logger = logger;
    }

    public ValueTask BroadcastEventAsync(string matchId, FootballMatchEvent matchEvent, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(matchEvent, MatchEventJsonContext.Default.FootballMatchEvent);
        var seq = NextSequence(matchId, 0);
        var msg = new SseMessage("match_event", json, matchEvent.event_index, seq);
        EnqueueAndPublish(matchId, msg, recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastStatisticsAsync(string matchId, object statistics, CancellationToken cancellationToken = default)
    {
        if (!_subscribers.TryGetValue(matchId, out var channels) || channels.IsEmpty)
            return ValueTask.CompletedTask;

        string json = statistics switch
        {
            string str => str,
            TelemetrySnapshotV2Dto telemetry => JsonSerializer.Serialize(telemetry, MatchEventJsonContext.Default.TelemetrySnapshotV2Dto),
            V1MatchStatisticsUpdateDto v1Stats => JsonSerializer.Serialize(v1Stats, MatchEventJsonContext.Default.V1MatchStatisticsUpdateDto),
            _ => throw new NotSupportedException($"Type {statistics.GetType().Name} is not registered in MatchEventJsonContext for Native AOT serialization.")
        };

        var seq = NextSequence(matchId, 0);
        var msg = new SseMessage("match_statistics", json, 0, seq);
        EnqueueAndPublish(matchId, msg, recordInReplay: false);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastStructuredEventAsync(string matchId, StructuredEventV2Dto matchEvent, long sequenceNo, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(matchEvent, MatchEventJsonContext.Default.StructuredEventV2Dto);
        var seq = NextSequence(matchId, sequenceNo);
        EnqueueAndPublish(matchId, new SseMessage("match_event", json, (int)matchEvent.EventIndex, seq), recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastSpatialFrameAsync(string matchId, SpatialFrameV2Dto spatialFrame, long sequenceNo, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(spatialFrame, MatchEventJsonContext.Default.SpatialFrameV2Dto);
        var seq = NextSequence(matchId, sequenceNo);
        EnqueueAndPublish(matchId, new SseMessage("spatial_frame", json, 0, seq), recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastTelemetryAsync(string matchId, TelemetrySnapshotV2Dto telemetry, long sequenceNo, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(telemetry, MatchEventJsonContext.Default.TelemetrySnapshotV2Dto);
        var seq = NextSequence(matchId, sequenceNo);
        EnqueueAndPublish(matchId, new SseMessage("telemetry", json, 0, seq), recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastCommentaryAsync(string matchId, CommentaryCueV2Dto commentary, long sequenceNo, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(commentary, MatchEventJsonContext.Default.CommentaryCueV2Dto);
        var seq = NextSequence(matchId, sequenceNo);
        EnqueueAndPublish(matchId, new SseMessage("commentary", json, (int)commentary.AnchorEventIndex, seq), recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask BroadcastLifecycleAsync(string matchId, MatchLifecycleV2Dto lifecycle, long sequenceNo, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(lifecycle, MatchEventJsonContext.Default.MatchLifecycleV2Dto);
        var seq = NextSequence(matchId, sequenceNo);
        EnqueueAndPublish(matchId, new SseMessage("match_lifecycle", json, 0, seq), recordInReplay: true);
        return ValueTask.CompletedTask;
    }

    public void ClearReplayBuffer(string matchId)
    {
        _recentMessages.TryRemove(matchId, out _);
        _sequenceCounters.TryRemove(matchId, out _);
    }

    public IAsyncEnumerable<SseMessage> SubscribeAsync(string matchId, CancellationToken cancellationToken) =>
        SubscribeAsync(matchId, lastEventId: null, cancellationToken);

    public async IAsyncEnumerable<SseMessage> SubscribeAsync(
        string matchId,
        long? lastEventId,
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

        var lastDeliveredSeq = lastEventId ?? 0;
        var lastReplayedIndex = 0;
        try
        {
            if (_recentMessages.TryGetValue(matchId, out var recentBuffer))
            {
                foreach (var replayMsg in recentBuffer.ToArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (lastEventId.HasValue && replayMsg.SequenceNo > 0 && replayMsg.SequenceNo <= lastEventId.Value)
                        continue;

                    lastDeliveredSeq = Math.Max(lastDeliveredSeq, replayMsg.SequenceNo);
                    lastReplayedIndex = Math.Max(lastReplayedIndex, replayMsg.EventIndex);
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
                        if (item.SequenceNo > 0 && item.SequenceNo <= lastDeliveredSeq)
                            continue;
                        if (item.SequenceNo == 0 && item.EventIndex > 0 && item.EventIndex <= lastReplayedIndex)
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

    private long NextSequence(string matchId, long explicitSeq)
    {
        if (explicitSeq > 0)
        {
            _sequenceCounters.AddOrUpdate(matchId, explicitSeq, (_, current) => Math.Max(current, explicitSeq));
            return explicitSeq;
        }
        return _sequenceCounters.AddOrUpdate(matchId, 1, (_, current) => current + 1);
    }

    private void EnqueueAndPublish(string matchId, SseMessage msg, bool recordInReplay)
    {
        if (recordInReplay)
        {
            var buffer = _recentMessages.GetOrAdd(matchId, _ => new ConcurrentQueue<SseMessage>());
            buffer.Enqueue(msg);
            while (buffer.Count > MaxReplayBufferSize && buffer.TryDequeue(out _)) { }
        }

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
    }
}
