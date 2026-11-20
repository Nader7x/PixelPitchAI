using Application.Dtos.V2;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Footex.UnitTests.Services;

public class V2StreamingAndStorageTests
{
    [Fact]
    public async Task MatchEventBroadcaster_ReplaysOnlyEventsAfterLastEventId()
    {
        var broadcaster = new MatchEventBroadcaster(NullLogger<MatchEventBroadcaster>.Instance);
        var evt1 = new StructuredEventV2Dto(
            1, 1, 1, 10, 9, 1001, "Pedri", "pass", "ThroughBall", "Complete",
            50f, 40f, 75f, 30f, 0f, false, "Right Foot", "Ground", "", 0, 0,
            0f, 0f, 0.72f, 0.08f, 0.09f, 0.15f, 120, 1
        );
        var spatial2 = new SpatialFrameV2Dto(
            120000, 75f, 30f, 0f, 0.56f,
            [new PlayerSpatialStateV2Dto(10, 1001, 50f, 40f, 1.2f, 0.3f, 0.1f, 0.95f, true, false)]
        );
        var telemetry3 = new TelemetrySnapshotV2Dto(
            0.32f, 0.11f, 0.28f, 0.05f, 0.45f, 0.18f, 58f, 8.2f, 11.4f, 62f, 0.25f,
            3, 1, 1, 0, 45, 32, 86.7f, 78.1f, 0.81f, 0.75f, 2, 0
        );

        await broadcaster.BroadcastStructuredEventAsync("42", evt1, 1);
        await broadcaster.BroadcastSpatialFrameAsync("42", spatial2, 2);
        await broadcaster.BroadcastTelemetryAsync("42", telemetry3, 3);

        using var cts = new CancellationTokenSource();
        var received = new List<SseMessage>();
        await foreach (var msg in broadcaster.SubscribeAsync("42", lastEventId: 1, cts.Token))
        {
            received.Add(msg);
            if (received.Count == 2)
                cts.Cancel();
        }

        received.Should().HaveCount(2);
        received[0].EventType.Should().Be("spatial_frame");
        received[0].SequenceNo.Should().Be(2);
        received[1].EventType.Should().Be("telemetry");
        received[1].SequenceNo.Should().Be(3);
    }

    [Fact]
    public async Task NotificationsBroadcaster_EmitsTypedEventAndReplaysAfterLastEventId()
    {
        var broadcaster = new NotificationsBroadcaster(NullLogger<NotificationsBroadcaster>.Instance);

        await broadcaster.BroadcastNotificationAsync(
            "user-1",
            new NotificationV2Dto("n1", "user-1", "simulation_started", "Started", "Match started", DateTime.UtcNow, MatchId: "10")
        );
        await broadcaster.BroadcastNotificationAsync(
            "user-1",
            new NotificationV2Dto("n2", "user-1", "experiment_completed", "Done", "Experiment done", DateTime.UtcNow, ExperimentId: "exp_1")
        );

        using var cts = new CancellationTokenSource();
        var received = new List<SseMessage>();
        await foreach (var msg in broadcaster.SubscribeAsync("user-1", lastEventId: 1, cts.Token))
        {
            received.Add(msg);
            cts.Cancel();
        }

        received.Should().ContainSingle();
        received[0].EventType.Should().Be("experiment_completed");
        received[0].SequenceNo.Should().Be(2);
    }

    [Fact]
    public async Task ExperimentBroadcaster_ReplaysProgressAndCompletion()
    {
        var broadcaster = new ExperimentBroadcaster();
        await broadcaster.BroadcastProgressAsync(
            "exp_9",
            new ExperimentProgressDto("exp_9", "simulating", 50, 100, 50.0, "local_cuda")
        );
        await broadcaster.BroadcastCompletedAsync(
            "exp_9",
            new MonteCarloExperimentResponseDto("exp_9", "completed", "local_cuda", 100, 1500, [])
        );

        using var cts = new CancellationTokenSource();
        var received = new List<SseMessage>();
        await foreach (var msg in broadcaster.SubscribeAsync("exp_9", lastEventId: null, cts.Token))
        {
            received.Add(msg);
            if (received.Count == 2)
                cts.Cancel();
        }

        received.Should().HaveCount(2);
        received[0].EventType.Should().Be("experiment_progress");
        received[1].EventType.Should().Be("experiment_completed");
    }
}
