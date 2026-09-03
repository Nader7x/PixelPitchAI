using System.Text.Json;
using Application.Interfaces;
using Domain.Models;
using Infrastructure.Protos;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IMatchEventGrpcStreamConsumer
{
    Task StartConsumingMatchStreamAsync(SimulateMatchRequest request, CancellationToken cancellationToken);
}

/// <summary>
///     Pipeline B: Direct memory-to-memory gRPC server-streaming consumer.
///     Consumes raw events directly from the Python simulation engine,
///     parses them with ZeroAllocationEventParser, and dispatches to SSE clients and Redis.
/// </summary>
public sealed class MatchEventGrpcStreamConsumer : IMatchEventGrpcStreamConsumer
{
    private readonly ISimulationGrpcClient _grpcClient;
    private readonly IMatchEventBroadcaster _broadcaster;
    private readonly ILiveMatchStatisticsService _liveMatchStatisticsService;
    private readonly ILogger<MatchEventGrpcStreamConsumer> _logger;

    public MatchEventGrpcStreamConsumer(
        ISimulationGrpcClient grpcClient,
        IMatchEventBroadcaster broadcaster,
        ILiveMatchStatisticsService liveMatchStatisticsService,
        ILogger<MatchEventGrpcStreamConsumer> logger)
    {
        _grpcClient = grpcClient;
        _broadcaster = broadcaster;
        _liveMatchStatisticsService = liveMatchStatisticsService;
        _logger = logger;
    }

    public async Task StartConsumingMatchStreamAsync(SimulateMatchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting direct gRPC event stream consumer for match {MatchId}", request.MatchId);

        var homeScore = 0;
        var awayScore = 0;
        var eventIndex = 0;

        try
        {
            await foreach (var rawEvent in _grpcClient.StreamMatchEventsAsync(request, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(rawEvent.RawEventText))
                    continue;

                eventIndex = rawEvent.EventIndex > 0 ? rawEvent.EventIndex : eventIndex + 1;

                if (ZeroAllocationEventParser.TryParseEvent(
                    rawEvent.RawEventText.AsSpan(),
                    rawEvent.MatchId,
                    eventIndex,
                    ref homeScore,
                    ref awayScore,
                    request.HomeTeamName,
                    request.AwayTeamName,
                    out var matchEvent) && matchEvent != null)
                {
                    // Broadcast event to connected SSE clients
                    await _broadcaster.BroadcastEventAsync(rawEvent.MatchId, matchEvent, cancellationToken);

                    // If match completed, log completion
                    if (rawEvent.IsEndOfMatch || matchEvent.event_type == "match_end")
                    {
                        _logger.LogInformation("Direct gRPC stream match completed for match {MatchId}", rawEvent.MatchId);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("gRPC stream consumer canceled for match {MatchId}", request.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming direct gRPC stream for match {MatchId}", request.MatchId);
        }
    }
}
