using System.Runtime.CompilerServices;
using Domain.Models;
using Infrastructure.Protos;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventGrpcStreamConsumerTests
{
    private readonly Mock<ISimulationGrpcClient> _mockGrpcClient;
    private readonly Mock<IMatchEventProcessor> _mockProcessor;
    private readonly Mock<ILogger<MatchEventGrpcStreamConsumer>> _mockLogger;
    private readonly MatchEventGrpcStreamConsumer _consumer;

    public MatchEventGrpcStreamConsumerTests()
    {
        _mockGrpcClient = new Mock<ISimulationGrpcClient>();
        _mockProcessor = new Mock<IMatchEventProcessor>();
        _mockLogger = new Mock<ILogger<MatchEventGrpcStreamConsumer>>();

        _consumer = new MatchEventGrpcStreamConsumer(
            _mockGrpcClient.Object,
            _mockProcessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task StartConsumingMatchStreamAsync_ProcessesEventsAndFlushesCompleted()
    {
        var request = new SimulateMatchRequest
        {
            MatchId = "100",
            HomeTeamName = "Real Madrid",
            AwayTeamName = "Barcelona"
        };

        var match = new Match { Id = 100, CreatorId = "user1", HomeTeamInMatchName = "Real Madrid", AwayTeamInMatchName = "Barcelona" };
        _mockProcessor.Setup(p => p.GetOrLoadMatchEntityAsync("100")).ReturnsAsync(match);
        _mockProcessor.Setup(p => p.GetOrInitMatchScore("100", match)).Returns((0, 0));

        async IAsyncEnumerable<MatchEventRaw> GenerateStream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new MatchEventRaw
            {
                MatchId = "100",
                EventIndex = 1,
                RawEventText = "[MATCH START]",
                IsEndOfMatch = false
            };
            yield return new MatchEventRaw
            {
                MatchId = "100",
                EventIndex = 2,
                RawEventText = "12:34 - Real Madrid - shot by Player, outcome: Goal",
                IsEndOfMatch = false
            };
            yield return new MatchEventRaw
            {
                MatchId = "100",
                EventIndex = 3,
                RawEventText = "[MATCH END]",
                IsEndOfMatch = true
            };
            await Task.CompletedTask;
        }

        _mockGrpcClient
            .Setup(c => c.StreamMatchEventsAsync(request, It.IsAny<CancellationToken>()))
            .Returns(GenerateStream());

        await _consumer.StartConsumingMatchStreamAsync(request, CancellationToken.None);

        _mockProcessor.Verify(p => p.ProcessEventAsync(It.IsAny<FootballMatchEvent>(), match, It.IsAny<CancellationToken>()), Times.AtLeast(2));
        _mockProcessor.Verify(p => p.FlushAndPersistMatchAsync("100", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task StartConsumingMatchStreamAsync_OnCancellation_FlushesTruncated()
    {
        var request = new SimulateMatchRequest
        {
            MatchId = "101",
            HomeTeamName = "Chelsea",
            AwayTeamName = "Arsenal"
        };

        var match = new Match { Id = 101, CreatorId = "user1", HomeTeamInMatchName = "Chelsea", AwayTeamInMatchName = "Arsenal" };
        _mockProcessor.Setup(p => p.GetOrLoadMatchEntityAsync("101")).ReturnsAsync(match);
        _mockProcessor.Setup(p => p.GetOrInitMatchScore("101", match)).Returns((0, 0));

        using var cts = new CancellationTokenSource();

        async IAsyncEnumerable<MatchEventRaw> GenerateCancelledStream([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new MatchEventRaw
            {
                MatchId = "101",
                EventIndex = 1,
                RawEventText = "[MATCH START]",
                IsEndOfMatch = false
            };
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            await Task.CompletedTask;
        }

        _mockGrpcClient
            .Setup(c => c.StreamMatchEventsAsync(request, It.IsAny<CancellationToken>()))
            .Returns(GenerateCancelledStream(cts.Token));

        await _consumer.StartConsumingMatchStreamAsync(request, cts.Token);

        _mockProcessor.Verify(p => p.FlushAndPersistMatchAsync("101", false, CancellationToken.None), Times.Once);
    }
}
