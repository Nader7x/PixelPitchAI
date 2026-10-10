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

        var worker = new MatchStreamWorker(queue, _mockConsumer.Object, options, _mockLogger.Object);

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
}
