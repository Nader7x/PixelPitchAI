using System.Text.Json;
using Application.Dtos.V2;
using Domain.Models;
using Infrastructure;
using Infrastructure.Protos.V2;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Footex.Controllers.V2;

[ApiController]
[Route("api/v2/experiments")]
[Authorize]
public sealed class ExperimentsController : ControllerBase
{
    private readonly FootballDbContext _dbContext;
    private readonly ISimulationGrpcClientV2 _grpcClientV2;
    private readonly IExperimentBroadcaster _experimentBroadcaster;
    private readonly INotificationsBroadcaster _notificationsBroadcaster;
    private readonly IMatchEventGrpcStreamConsumerV2 _grpcStreamConsumerV2;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExperimentsController> _logger;

    public ExperimentsController(
        FootballDbContext dbContext,
        ISimulationGrpcClientV2 grpcClientV2,
        IExperimentBroadcaster experimentBroadcaster,
        INotificationsBroadcaster notificationsBroadcaster,
        IMatchEventGrpcStreamConsumerV2 grpcStreamConsumerV2,
        IServiceScopeFactory scopeFactory,
        ILogger<ExperimentsController> logger
    )
    {
        _dbContext = dbContext;
        _grpcClientV2 = grpcClientV2;
        _experimentBroadcaster = experimentBroadcaster;
        _notificationsBroadcaster = notificationsBroadcaster;
        _grpcStreamConsumerV2 = grpcStreamConsumerV2;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MonteCarloExperimentResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> CreateExperiment(
        [FromBody] MonteCarloExperimentRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var experimentId = string.IsNullOrWhiteSpace(request.ExperimentId)
            ? $"exp_{Guid.NewGuid():N}"[..16]
            : request.ExperimentId;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        var requestJson = JsonSerializer.Serialize(
            request,
            MatchEventJsonContext.Default.MonteCarloExperimentRequestDto
        );

        var record = new TacticalExperimentRecord
        {
            Id = experimentId,
            UserId = userId,
            Title = request.Title ?? $"{request.QuestionType} ({request.NSimulations} sims)",
            QuestionType = request.QuestionType,
            Status = "running",
            NSimulations = request.NSimulations,
            BaseSeed = request.BaseSeed,
            RequestJson = requestJson,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.TacticalExperiments.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var grpcRequest = new MonteCarloExperimentRequest
        {
            ExperimentId = experimentId,
            NSimulations = (uint)Math.Max(1, request.NSimulations),
            BaseSeed = (ulong)Math.Max(0, request.BaseSeed),
            ComputeTarget = request.ComputeTarget,
            BaselineConfigJson = request.BaselineConfig != null
                ? JsonSerializer.Serialize(request.BaselineConfig, MatchEventJsonContext.Default.MatchConfigurationV2Dto)
                : "{}",
            InterventionsJson = request.Interventions != null
                ? JsonSerializer.Serialize(request.Interventions.ToList(), MatchEventJsonContext.Default.ListInterventionArmDto)
                : "[]",
            AllowOodExtrapolation = request.AllowOodExtrapolation,
        };

        _ = Task.Run(
            async () => await ExecuteExperimentAsync(experimentId, userId, grpcRequest),
            CancellationToken.None
        );

        var streamUrl = $"/api/v2/experiments/{experimentId}/stream";
        return Accepted(
            streamUrl,
            new MonteCarloExperimentResponseDto(
                experimentId,
                "running",
                request.ComputeTarget,
                request.NSimulations,
                0,
                [],
                StreamUrl: streamUrl
            )
        );
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExperiment(string id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.TacticalExperiments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (record == null)
            return NotFound();

        return Ok(record);
    }

    [HttpGet("{id}/stream")]
    public async Task StreamExperiment(string id, CancellationToken cancellationToken)
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

        try
        {
            await Response.WriteAsync($": connected to experiment stream {id}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var msg in _experimentBroadcaster.SubscribeAsync(id, lastEventId, cancellationToken))
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
            _logger.LogDebug("Client disconnected from experiment SSE stream {ExperimentId}", id);
        }
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserExperiments(string userId, CancellationToken cancellationToken)
    {
        var records = await _dbContext.TacticalExperiments
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpPost("{id}/replay-seed/{seed:int}")]
    [ProducesResponseType(typeof(SimulateMatchV2ResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaySeed(
        string id,
        int seed,
        CancellationToken cancellationToken
    )
    {
        var record = await _dbContext.TacticalExperiments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (record == null)
            return NotFound();

        var reqDto = JsonSerializer.Deserialize(
            record.RequestJson,
            MatchEventJsonContext.Default.MonteCarloExperimentRequestDto
        );
        var baseline = reqDto?.BaselineConfig;
        var replayConfig = baseline != null
            ? baseline with { RandomSeed = seed }
            : new MatchConfigurationV2Dto(
                0,
                seed,
                new TeamMatchConfigV2Dto(1, "Home"),
                new TeamMatchConfigV2Dto(2, "Away")
            );

        var configJson = JsonSerializer.Serialize(
            replayConfig,
            MatchEventJsonContext.Default.MatchConfigurationV2Dto
        );
        var syntheticMatchId = Math.Abs(HashCode.Combine(id, seed));
        var grpcReq = new SimulateMatchV2Request
        {
            MatchId = syntheticMatchId.ToString(),
            StreamMode = "broadcast",
            ComputeTarget = "auto",
            PlaybackSpeed = 1.0f,
            SpatialHz = 5,
            MatchConfigJson = configJson,
            CommentaryEnabled = true,
            CommentaryProvider = "ab_split",
            CommentaryLanguage = "en",
        };

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? record.UserId;
        _ = Task.Run(
            async () => await _grpcStreamConsumerV2.StartConsumingMatchStreamV2Async(grpcReq, userId, CancellationToken.None),
            CancellationToken.None
        );

        var streamUrl = $"/api/v2/matches/{syntheticMatchId}/stream";
        return Accepted(
            streamUrl,
            new SimulateMatchV2ResponseDto(
                true,
                syntheticMatchId,
                streamUrl,
                "SimulatingInProgress",
                "auto"
            )
        );
    }

    private async Task ExecuteExperimentAsync(
        string experimentId,
        string userId,
        MonteCarloExperimentRequest request
    )
    {
        try
        {
            await foreach (var frame in _grpcClientV2.RunMonteCarloExperimentAsync(request, CancellationToken.None))
            {
                var pct = frame.Total > 0 ? (double)frame.Completed / frame.Total * 100.0 : 0.0;
                await _experimentBroadcaster.BroadcastProgressAsync(
                    experimentId,
                    new ExperimentProgressDto(
                        experimentId,
                        frame.Stage,
                        (int)frame.Completed,
                        (int)frame.Total,
                        pct,
                        Message: frame.OodWarningLevel
                    )
                );

                if (frame.IsFinal)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
                    var record = await db.TacticalExperiments.FirstOrDefaultAsync(e => e.Id == experimentId);
                    if (record != null)
                    {
                        record.Status = string.IsNullOrWhiteSpace(frame.ErrorMessage) ? "completed" : "failed";
                        record.ResultJson = string.IsNullOrWhiteSpace(frame.ResultsJson) ? null : frame.ResultsJson;
                        record.ErrorMessage = string.IsNullOrWhiteSpace(frame.ErrorMessage) ? null : frame.ErrorMessage;
                        record.CompletedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }

                    if (!string.IsNullOrWhiteSpace(frame.ErrorMessage))
                    {
                        await _experimentBroadcaster.BroadcastFailedAsync(
                            experimentId,
                            new ExperimentProgressDto(experimentId, "failed", (int)frame.Completed, (int)frame.Total, pct, Message: frame.ErrorMessage)
                        );
                    }
                    else if (!string.IsNullOrWhiteSpace(frame.ResultsJson))
                    {
                        var responseDto = JsonSerializer.Deserialize(
                            frame.ResultsJson,
                            MatchEventJsonContext.Default.MonteCarloExperimentResponseDto
                        );
                        if (responseDto != null)
                        {
                            await _experimentBroadcaster.BroadcastCompletedAsync(experimentId, responseDto);
                        }
                    }

                    await _notificationsBroadcaster.BroadcastNotificationAsync(
                        userId,
                        new NotificationV2Dto(
                            Guid.NewGuid().ToString(),
                            userId,
                            "experiment_completed",
                            "Counterfactual Experiment Completed",
                            $"Experiment {experimentId} finished.",
                            DateTime.UtcNow,
                            ExperimentId: experimentId
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monte Carlo experiment {ExperimentId} failed", experimentId);
            await _experimentBroadcaster.BroadcastFailedAsync(
                experimentId,
                new ExperimentProgressDto(experimentId, "failed", 0, (int)request.NSimulations, 0, Message: ex.Message)
            );
        }
    }
}
