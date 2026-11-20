using System.Text.Json;
using Application.Dtos.V2;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Protos.V2;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Footex.Controllers.V2;

[ApiController]
[Route("api/v2/matches")]
[Authorize]
public sealed class MatchesV2Controller : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchEventBroadcaster _broadcaster;
    private readonly ISimulationGrpcClientV2 _grpcClientV2;
    private readonly IMatchEventGrpcStreamConsumerV2 _grpcStreamConsumerV2;
    private readonly INotificationsBroadcaster _notificationsBroadcaster;
    private readonly ISpatialTrackStorageService _spatialTrackStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchesV2Controller> _logger;

    public MatchesV2Controller(
        IUnitOfWork unitOfWork,
        IMatchEventBroadcaster broadcaster,
        ISimulationGrpcClientV2 grpcClientV2,
        IMatchEventGrpcStreamConsumerV2 grpcStreamConsumerV2,
        INotificationsBroadcaster notificationsBroadcaster,
        ISpatialTrackStorageService spatialTrackStorage,
        IServiceScopeFactory scopeFactory,
        ILogger<MatchesV2Controller> logger
    )
    {
        _unitOfWork = unitOfWork;
        _broadcaster = broadcaster;
        _grpcClientV2 = grpcClientV2;
        _grpcStreamConsumerV2 = grpcStreamConsumerV2;
        _notificationsBroadcaster = notificationsBroadcaster;
        _spatialTrackStorage = spatialTrackStorage;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("simulate/{userId}")]
    [ProducesResponseType(typeof(SimulateMatchV2ResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulateMatchV2(
        string userId,
        [FromBody] SimulateMatchV2RequestDto request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("User ID is required.");

        var homeTeam = await _unitOfWork.Teams.GetByIdAsync(request.HomeTeamId, cancellationToken);
        var awayTeam = await _unitOfWork.Teams.GetByIdAsync(request.AwayTeamId, cancellationToken);
        if (homeTeam == null || awayTeam == null)
            return BadRequest("Home or Away team not found.");

        var config = request.MatchConfig ?? new MatchConfigurationV2Dto(
            0,
            Random.Shared.Next(1, 1_000_000),
            new TeamMatchConfigV2Dto(homeTeam.Id, homeTeam.Name ?? "Home"),
            new TeamMatchConfigV2Dto(awayTeam.Id, awayTeam.Name ?? "Away")
        );

        var configJson = JsonSerializer.Serialize(config, MatchEventJsonContext.Default.MatchConfigurationV2Dto);

        var match = new Match
        {
            HomeTeamId = request.HomeTeamId,
            AwayTeamId = request.AwayTeamId,
            HomeTeamSeasonId = request.HomeTeamSeasonId,
            AwayTeamSeasonId = request.AwayTeamSeasonId,
            HomeTeamInMatchName = homeTeam.Name,
            AwayTeamInMatchName = awayTeam.Name,
            ScheduledDateTimeUtc = DateTime.UtcNow,
            MatchStatus = "SimulatingInProgress",
            IsLive = true,
            CreatorId = userId,
            TacticalConfigJson = configJson,
        };

        await _unitOfWork.Matches.AddAsync(match);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var grpcRequest = new SimulateMatchV2Request
        {
            MatchId = match.Id.ToString(),
            StreamMode = request.StreamMode,
            ComputeTarget = request.ComputeTarget,
            PlaybackSpeed = request.PlaybackSpeed,
            SpatialHz = (uint)Math.Clamp(request.SpatialHz, 1, 25),
            MatchConfigJson = configJson,
            CommentaryEnabled = request.Commentary?.Enabled ?? true,
            CommentaryProvider = request.Commentary?.Provider ?? "ab_split",
            CommentaryLanguage = request.Commentary?.Language ?? "en",
        };

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _grpcStreamConsumerV2.StartConsumingMatchStreamV2Async(
                        grpcRequest,
                        userId,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background V2 gRPC stream failed for match {MatchId}", match.Id);
                }
            },
            CancellationToken.None
        );

        await _notificationsBroadcaster.BroadcastNotificationAsync(
            userId,
            new NotificationV2Dto(
                Guid.NewGuid().ToString(),
                userId,
                "simulation_started",
                "Match Simulation Started",
                $"{homeTeam.Name} vs {awayTeam.Name} V2 simulation started.",
                DateTime.UtcNow,
                MatchId: match.Id.ToString()
            ),
            cancellationToken
        );

        var streamUrl = $"/api/v2/matches/{match.Id}/stream";
        return Accepted(
            streamUrl,
            new SimulateMatchV2ResponseDto(
                true,
                match.Id,
                streamUrl,
                "SimulatingInProgress",
                request.ComputeTarget
            )
        );
    }

    [HttpGet("{id:int}/stream")]
    public async Task StreamMatchV2(int id, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        long? lastEventId = null;
        if (
            Request.Headers.TryGetValue("Last-Event-ID", out var headerVal)
            && long.TryParse(headerVal.ToString(), out var parsedId)
        )
        {
            lastEventId = parsedId;
        }

        var matchIdStr = id.ToString();
        try
        {
            await Response.WriteAsync($": connected to v2 match stream {matchIdStr}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var msg in _broadcaster.SubscribeAsync(matchIdStr, lastEventId, cancellationToken))
            {
                if (msg.EventType == "ping")
                {
                    await Response.WriteAsync(":\n\n", cancellationToken);
                }
                else
                {
                    var idPrefix = msg.SequenceNo > 0 ? $"id: {msg.SequenceNo}\n" : string.Empty;
                    await Response.WriteAsync(
                        $"{idPrefix}event: {msg.EventType}\ndata: {msg.Data}\n\n",
                        cancellationToken
                    );
                }
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Client disconnected from V2 SSE stream for match {MatchId}", matchIdStr);
        }
    }

    [HttpPost("{id:int}/tactics")]
    [ProducesResponseType(typeof(LiveTacticalOverrideResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyLiveTacticalOverride(
        int id,
        [FromBody] LiveTacticalOverrideDto request,
        CancellationToken cancellationToken
    )
    {
        var patchJson = JsonSerializer.Serialize(request, MatchEventJsonContext.Default.LiveTacticalOverrideDto);
        var ack = await _grpcClientV2.ApplyLiveTacticalOverrideAsync(
            new LiveTacticalOverrideRequest
            {
                MatchId = id.ToString(),
                TeamSide = request.TeamSide,
                TacticalInstructionsPatchJson = patchJson,
                FormationBaseName = request.FormationName ?? string.Empty,
                SubstitutionJson =
                    request.OutSlotIndex.HasValue && request.InPlayerId.HasValue
                        ? $"{{\"out_slot\":{request.OutSlotIndex.Value},\"in_player_id\":{request.InPlayerId.Value}}}"
                        : string.Empty,
            },
            cancellationToken
        );

        return Ok(
            new LiveTacticalOverrideResponseDto(
                ack?.Applied ?? false,
                id,
                ack?.Message ?? "No response from simulation engine"
            )
        );
    }

    [HttpGet("{id:int}/replay")]
    [ProducesResponseType(typeof(MatchReplayV2ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchReplay(int id, CancellationToken cancellationToken)
    {
        var match = await _unitOfWork.Matches.GetByIdWithDetailsAsync(id);
        if (match == null)
            return NotFound();

        var downloadUrl = string.IsNullOrWhiteSpace(match.SpatialTrackObjectKey)
            ? null
            : $"/api/v2/matches/{id}/spatial-track";

        return Ok(
            new MatchReplayV2ResponseDto(
                match.Id,
                match.SpatialTrackObjectKey,
                match.SpatialTrackCodec,
                downloadUrl,
                match.ModelVersion,
                match.HomeXG,
                match.AwayXG,
                match.HomeXT,
                match.AwayXT,
                match.MatchEvents?.EventsJson ?? "[]"
            )
        );
    }

    [HttpGet("{id:int}/spatial-track")]
    public async Task<IActionResult> GetSpatialTrack(int id, CancellationToken cancellationToken)
    {
        var match = await _unitOfWork.Matches.GetByIdAsync(id, cancellationToken);
        if (match == null || string.IsNullOrWhiteSpace(match.SpatialTrackObjectKey))
            return NotFound();

        var stream = await _spatialTrackStorage.OpenSpatialTrackReadStreamAsync(
            match.SpatialTrackObjectKey,
            cancellationToken
        );
        if (stream == null)
            return NotFound();

        return File(stream, "application/zstd", $"match-{id}-spatial.zst");
    }
}
