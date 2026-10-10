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
///     Consumes raw events directly from the Python simulation engine stream,
///     parses them with ZeroAllocationEventParser, delegates processing to IMatchEventProcessor,
///     and guarantees post-match PostgreSQL persistence mirroring Pipeline A.
/// </summary>
public sealed class MatchEventGrpcStreamConsumer : IMatchEventGrpcStreamConsumer
{
    private readonly ISimulationGrpcClient _grpcClient;
    private readonly IMatchEventProcessor _matchEventProcessor;
    private readonly ILogger<MatchEventGrpcStreamConsumer> _logger;

    public MatchEventGrpcStreamConsumer(
        ISimulationGrpcClient grpcClient,
        IMatchEventProcessor matchEventProcessor,
        ILogger<MatchEventGrpcStreamConsumer> logger)
    {
        _grpcClient = grpcClient;
        _matchEventProcessor = matchEventProcessor;
        _logger = logger;
    }

    public async Task StartConsumingMatchStreamAsync(SimulateMatchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting direct gRPC event stream consumer for match {MatchId}", request.MatchId);

        var matchEntity = await _matchEventProcessor.GetOrLoadMatchEntityAsync(request.MatchId);
        var (homeScore, awayScore) = _matchEventProcessor.GetOrInitMatchScore(request.MatchId, matchEntity);
        var eventIndex = 0;
        var isNormalEnd = false;

        try
        {
            await foreach (var rawEvent in _grpcClient.StreamMatchEventsAsync(request, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(rawEvent.RawEventText))
                    continue;

                eventIndex = rawEvent.EventIndex > 0 ? rawEvent.EventIndex : eventIndex + 1;

                var homeName = matchEntity?.HomeTeam?.Name ?? matchEntity?.HomeTeamInMatchName ?? request.HomeTeamName;
                var awayName = matchEntity?.AwayTeam?.Name ?? matchEntity?.AwayTeamInMatchName ?? request.AwayTeamName;

                if (ZeroAllocationEventParser.TryParseEvent(
                    rawEvent.RawEventText.AsSpan(),
                    rawEvent.MatchId,
                    eventIndex,
                    ref homeScore,
                    ref awayScore,
                    homeName,
                    awayName,
                    out var matchEvent) && matchEvent != null)
                {
                    _matchEventProcessor.UpdateMatchScore(rawEvent.MatchId, homeScore, awayScore);

                    if (matchEntity != null)
                    {
                        matchEntity.HomeTeamScore = homeScore;
                        matchEntity.AwayTeamScore = awayScore;
                        await _matchEventProcessor.ProcessEventAsync(matchEvent, matchEntity, cancellationToken);
                    }

                    if (rawEvent.IsEndOfMatch || matchEvent.event_type == "match_end" || matchEvent.action == "match_end")
                    {
                        isNormalEnd = true;
                        _logger.LogInformation("Direct gRPC stream match completed for match {MatchId}", rawEvent.MatchId);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("gRPC stream consumer canceled for match {MatchId}", request.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming direct gRPC stream for match {MatchId}", request.MatchId);
        }
        finally
        {
            try
            {
                await _matchEventProcessor.FlushAndPersistMatchAsync(
                    request.MatchId,
                    isNormalEnd: isNormalEnd,
                    cancellationToken: CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing and persisting match events to database for match {MatchId}", request.MatchId);
            }
        }
    }
}
