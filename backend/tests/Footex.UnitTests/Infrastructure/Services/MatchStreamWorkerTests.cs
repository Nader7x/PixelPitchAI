using Infrastructure.Configuration;
using Infrastructure.Protos;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchStreamWorkerTests
{
    private readonly Mock<IMatchEventGrpcStreamConsumer> _mockConsumer;
    private readonly Mock<ILogger<MatchStreamWorker>> _mockLogger;

    public MatchStreamWorkerTests()
    {
        _mockConsumer = new Mock<IMatchEventGrpcStreamConsumer>();
        _mockLogger = new Mock<ILogger<MatchStreamWorker>>();
    }

    [Fact]
    public async Task Worker_ConsumesQueuedRequestsConcurrently()
    {
        var queue = new MatchStreamingQueue(10);
        var options = Options.Create(new EventIngestionOptions
        {
            Mode = "GrpcStream",
            MaxConcurrentStreams = 5
        });

        var lifecycle = new Mock<IMatchStreamLifecycle>();
        lifecycle.Setup(l => l.TryStartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var worker = new MatchStreamWorker(queue, _mockConsumer.Object, options, _mockLogger.Object, lifecycle.Object);

        using var cts = new CancellationTokenSource();
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        _mockConsumer
            .Setup(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "1"), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                tcs1.SetResult();
                await Task.Delay(50);
            });

        _mockConsumer
            .Setup(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "2"), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                tcs2.SetResult();
                await Task.Delay(50);
            });

        // Enqueue 2 requests
        await queue.EnqueueAsync(new SimulateMatchRequest { MatchId = "1" });
        await queue.EnqueueAsync(new SimulateMatchRequest { MatchId = "2" });

        var workerTask = worker.StartAsync(cts.Token);

        // Wait for both to be invoked concurrently
        await Task.WhenAll(tcs1.Task, tcs2.Task);

        _mockConsumer.Verify(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "1"), It.IsAny<CancellationToken>()), Times.Once);
        _mockConsumer.Verify(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "2"), It.IsAny<CancellationToken>()), Times.Once);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SynchronousCompletionNeverLeavesStaleEntries()
    {
        var queue = new MatchStreamingQueue(300);
        var lifecycle = new Mock<IMatchStreamLifecycle>();
        lifecycle.Setup(l => l.TryStartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        _mockConsumer.Setup(c => c.StartConsumingMatchStreamAsync(It.IsAny<SimulateMatchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(() => { if (Interlocked.Increment(ref count) == 200) drained.TrySetResult(); return Task.CompletedTask; });
        using var worker = new MatchStreamWorker(queue, _mockConsumer.Object,
            Options.Create(new EventIngestionOptions { MaxConcurrentStreams = 2 }), _mockLogger.Object, lifecycle.Object);
        for (var i = 1; i <= 200; i++) Assert.True(queue.TryEnqueue(new SimulateMatchRequest { MatchId = i.ToString() }));
        await worker.StartAsync(default);
        await drained.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await worker.StopAsync(default);
        Assert.Equal(0, worker.ActiveStreamCount);
        Assert.False(queue.TryEnqueue(new SimulateMatchRequest { MatchId = "201" }));
    }

    [Fact]
    public async Task DuplicateActiveRequestDoesNotRunOrRemoveOwnersEntry()
    {
        var queue = new MatchStreamingQueue(10);
        var lifecycle = new Mock<IMatchStreamLifecycle>();
        lifecycle.Setup(l => l.TryStartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockConsumer.Setup(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "1"), It.IsAny<CancellationToken>()))
            .Returns(async (SimulateMatchRequest _, CancellationToken token) => { started.TrySetResult(); await Task.Delay(Timeout.Infinite, token); });
        _mockConsumer.Setup(c => c.StartConsumingMatchStreamAsync(It.Is<SimulateMatchRequest>(r => r.MatchId == "2"), It.IsAny<CancellationToken>()))
            .Returns(() => { secondStarted.TrySetResult(); return Task.CompletedTask; });
        using var worker = new MatchStreamWorker(queue, _mockConsumer.Object,
            Options.Create(new EventIngestionOptions { MaxConcurrentStreams = 2 }), _mockLogger.Object, lifecycle.Object);
        queue.TryEnqueue(new SimulateMatchRequest { MatchId = "1" });
        await worker.StartAsync(default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        queue.TryEnqueue(new SimulateMatchRequest { MatchId = "1" });
        queue.TryEnqueue(new SimulateMatchRequest { MatchId = "2" });
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await worker.StopAsync(default);
        lifecycle.Verify(l => l.TryStartAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, worker.ActiveStreamCount);
    }

    [Fact]
    public async Task ShutdownClearsQueuedRequestsIncludingRequestWaitingForCapacity()
    {
        var queue = new MatchStreamingQueue(10);
        var lifecycle = new Mock<IMatchStreamLifecycle>();
        lifecycle.Setup(l => l.TryStartAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockConsumer.Setup(c => c.StartConsumingMatchStreamAsync(It.IsAny<SimulateMatchRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (SimulateMatchRequest _, CancellationToken token) => { started.TrySetResult(); await Task.Delay(Timeout.Infinite, token); });
        using var worker = new MatchStreamWorker(queue, _mockConsumer.Object,
            Options.Create(new EventIngestionOptions { MaxConcurrentStreams = 1 }), _mockLogger.Object, lifecycle.Object);
        foreach (var id in new[] { "1", "2", "3" }) queue.TryEnqueue(new SimulateMatchRequest { MatchId = id });
        await worker.StartAsync(default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await worker.StopAsync(default);
        lifecycle.Verify(l => l.FailQueuedAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        lifecycle.Verify(l => l.FailQueuedAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, worker.ActiveStreamCount);
    }
}
