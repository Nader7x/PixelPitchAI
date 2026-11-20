using FluentAssertions;
using Infrastructure.Protos.V2;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Footex.IntegrationTests.Services;

public class SimulationGrpcClientV2LiveTests
{
    private static bool IsLivePythonEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("PIXELPITCH_STREAM_TEST_PYTHON"),
            "1",
            StringComparison.Ordinal
        );

    private static SimulationGrpcClientV2 CreateClient()
    {
        var address =
            Environment.GetEnvironmentVariable("SimulationGrpc__Address")
            ?? Environment.GetEnvironmentVariable("SimulationGrpc__ServerUrl")
            ?? "http://127.0.0.1:50061";

        return new SimulationGrpcClientV2(
            Options.Create(new SimulationGrpcOptions { Address = address, ServerUrl = address }),
            NullLogger<SimulationGrpcClientV2>.Instance
        );
    }

    [Fact]
    public async Task LivePythonGrpcV2_HealthStreamAndOverride_Succeeds()
    {
        if (!IsLivePythonEnabled())
            return;

        using var client = CreateClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // 1. GetHealthV2
        var health = await client.GetHealthV2Async(includeCheckpoints: true, cts.Token);
        health.Should().NotBeNull();
        health!.Healthy.Should().BeTrue($"expected SimulationServiceV2 to be healthy, details: {health.DetailsJson}");

        // 2. StreamMatchV2 (short match with MaxEvents = 10) + ApplyLiveTacticalOverride
        var matchId = $"live-e2e-{Guid.NewGuid():N}"[..16];
        var request = new SimulateMatchV2Request
        {
            MatchId = matchId,
            StreamMode = "instant",
            ComputeTarget = "auto",
            PlaybackSpeed = 10.0f,
            SpatialHz = 2,
            MaxEvents = 10,
            MatchConfigJson = "{}",
            CommentaryEnabled = false,
            CommentaryProvider = "template_only",
            CommentaryLanguage = "en",
        };

        var frames = new List<MatchStreamFrameV2>();
        LiveTacticalOverrideAck? overrideAck = null;

        await foreach (var frame in client.StreamMatchV2Async(request, cts.Token))
        {
            frames.Add(frame);
            if (overrideAck == null)
            {
                overrideAck = await client.ApplyLiveTacticalOverrideAsync(
                    new LiveTacticalOverrideRequest
                    {
                        MatchId = matchId,
                        TeamSide = "home",
                        TacticalInstructionsPatchJson = "{\"pressing_intensity\":0.8}",
                        FormationBaseName = "4-3-3",
                    },
                    cts.Token
                );
            }
        }

        frames.Should().NotBeEmpty("expected live SimulationServiceV2 stream to yield V2 frames");
        frames.Should().Contain(f => f.Kind == FrameKind.Event || f.Kind == FrameKind.Lifecycle);

        overrideAck.Should().NotBeNull();
        overrideAck!.MatchId.Should().Be(matchId);
    }
}
