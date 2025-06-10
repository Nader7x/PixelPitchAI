using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.CQRS.Notifications.Commands;
using Application.Dtos;
using Application.Helpers;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Footex.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(
    IMediator mediator,
    IHttpClientFactory httpClientFactory,
    MatchMapper matchMapper,
    IOptions<SimulationServiceOptions> simulationOptions,
    IServiceScopeFactory serviceScopeFactory,
    IUnitOfWork unitOfWork,
    ILogger<MatchesController> logger,
    IHubContext<NotificationService, INotificationService> hubContext,
    ICacheService cacheService) : ControllerBase
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly IHubContext<NotificationService, INotificationService> _hubContext = hubContext;
    private readonly ILogger<MatchesController> _logger = logger;
    private readonly MatchMapper _matchMapper = matchMapper;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly SimulationServiceOptions _simulationOptions = simulationOptions.Value;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [HttpGet]
    [ProducesResponseType(typeof(GetAllMatchesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllMatchesQueryResponse>> GetAllMatches(
        [FromQuery] int? seasonId,
        [FromQuery] int? teamId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? matchWeek)
    {
        // Generate a cache key based on the query parameters
        var cacheKey =
            $"matches_all_{seasonId}_{teamId}_{status}_{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}_{matchWeek}";

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<GetAllMatchesQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetAllMatchesQuery
        {
            HomeSeasonId = seasonId,
            TeamId = teamId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            MatchWeek = matchWeek
        };

        var result = await mediator.Send(query);

        if (!result.Succeeded)
            return BadRequest(result);

        // Store in cache if successful - with a shorter TTL since match data changes frequently
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetMatchByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMatchByIdQueryResponse>> GetMatchById(int id)
    {
        // Try to get from cache first
        var cacheKey = $"match_{id}";
        var cachedResult = await _cacheService.GetAsync<GetMatchByIdQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetMatchByIdQuery { Id = id };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Store in cache if successful - with a shorter TTL for match data
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("Details/{matchId:int}")]
    // get match by id with details
    [ProducesResponseType(typeof(GetMatchByIdWithDetailsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMatchByIdWithDetailsQueryResponse>> GetMatchByIdWithDetails(int matchId)
    {
        // Try to get from cache first
        var cacheKey = $"match_details_{matchId}";
        var cachedResult = await _cacheService.GetAsync<GetMatchByIdWithDetailsQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Store in cache if successful - with a shorter TTL for match data
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }


    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CreateMatchCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMatchCommandResponse>> CreateMatch([FromBody] CreateMatchDto matchDto)
    {
        if (string.IsNullOrEmpty(matchDto.CreatorId))
            matchDto.CreatorId = User.GetNameId();

        var command = _matchMapper.ToCreateCommand(matchDto);

        var result = await mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate matches list cache when creating a new match
        await InvalidateMatchListCaches();

        return CreatedAtAction(nameof(GetMatchById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UpdateMatchCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateMatchCommandResponse>> UpdateMatch(int id, [FromBody] UpdateMatchDto matchDto)
    {
        if (id != matchDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });

        var command = new UpdateMatchCommand
        {
            Id = matchDto.Id,
            HomeSeasonId = matchDto.SeasonId,
            AwaySeasonId = matchDto.SeasonId,
            HomeTeamId = matchDto.HomeTeamId,
            AwayTeamId = matchDto.AwayTeamId,
            ScheduledDateTimeUtc = matchDto.ScheduledDateTimeUTC,
            StadiumId = matchDto.StadiumId,
            MatchWeek = matchDto.MatchWeek,
            HomeCoachId = matchDto.HomeCoachId,
            AwayCoachId = matchDto.AwayCoachId,
            HomeTeamScore = matchDto.HomeTeamScore,
            AwayTeamScore = matchDto.AwayTeamScore,
            WinningTeamId = matchDto.WinningTeamId,
            LosingTeamId = matchDto.LosingTeamId,
            IsDraw = matchDto.IsDraw,
            MatchStatus = matchDto.MatchStatus,
            HomeTeamPossession = matchDto.HomeTeamPossession,
            AwayTeamPossession = matchDto.AwayTeamPossession,
            HomeTeamShots = matchDto.HomeTeamShots,
            AwayTeamShots = matchDto.AwayTeamShots,
            HomeTeamShotsOnTarget = matchDto.HomeTeamShotsOnTarget,
            AwayTeamShotsOnTarget = matchDto.AwayTeamShotsOnTarget,
            HomeTeamCorners = matchDto.HomeTeamCorners,
            AwayTeamCorners = matchDto.AwayTeamCorners,
            HomeTeamFouls = matchDto.HomeTeamFouls,
            AwayTeamFouls = matchDto.AwayTeamFouls,
            HomeTeamYellowCards = matchDto.HomeTeamYellowCards,
            AwayTeamYellowCards = matchDto.AwayTeamYellowCards,
            HomeTeamRedCards = matchDto.HomeTeamRedCards,
            AwayTeamRedCards = matchDto.AwayTeamRedCards
        };

        var result = await mediator.Send(command);

        if (result.Succeeded) return Ok(result);
        if (result.NotFound)
            return NotFound(result);

        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteMatchCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteMatchCommandResponse>> DeleteMatch(int id)
    {
        var command = new DeleteMatchCommand { Id = id };
        var result = await mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Invalidate both specific match caches and all list caches that might include this match
        await _cacheService.RemoveAsync($"match_{id}");
        await _cacheService.RemoveAsync($"match_details_{id}");
        await InvalidateMatchListCaches();

        return Ok(result);
    }

    // Helper method to invalidate all match-related list caches
    [NonAction]
    private async Task InvalidateMatchListCaches()
    {
        // Using a pattern to match all match list caches
        await _cacheService.RemoveAsync("matches_all_*");
    }

    [HttpGet("{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(GetUserMatchesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetUserMatchesQueryResponse>> GetUserMatches(string userId)
    {
        var query = new GetUserMatchesQuery { UserId = userId };
        var result = await mediator.Send(query);

        if (!result.Succeeded) return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("LiveMatch/{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(GetLiveMatchQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLiveMatchQueryResponse>> GetLiveMatch(string userId)
    {
        var query = new GetLiveMatchQuery { UserId = userId };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("SimulateMatch/{userId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateMatchCommandResponse>> SimulateMatch(string userId,
        [FromBody] SimulateMatchDto simulationDto, CancellationToken cancellationToken)
    {
        if (HasLiveMatch(userId, cancellationToken).Result)
            return BadRequest(new { error = "You Can Not Simulate Two Matches At The Same Time" });

        var httpClient = httpClientFactory.CreateClient();

        try
        {
            // Check if simulation service is available
            var healthResponse = await httpClient.GetAsync($"{_simulationOptions.BaseUrl}/health", cancellationToken);
            _logger.LogInformation("Simulation Service Health Check {HealthResponse}",
                healthResponse.Content.ToString());
            if (!healthResponse.IsSuccessStatusCode)
                return StatusCode((int)healthResponse.StatusCode, "Simulation service is not available");

            var healthCheckResult =
                await healthResponse.Content.ReadFromJsonAsync<HealthCheckResponse>(cancellationToken);
            if (healthCheckResult == null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Failed to parse simulation service health check response.");

            if (!healthCheckResult.ModelLoaded || !healthCheckResult.XgboostLoaded)
            {
                var notReadyReason = new StringBuilder("Simulation service is not ready. ");
                if (!healthCheckResult.ModelLoaded) notReadyReason.Append("Model not loaded. ");
                if (!healthCheckResult.XgboostLoaded) notReadyReason.Append("XGBoost not loaded.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = notReadyReason.ToString().Trim(), healthDetails = healthCheckResult });
            }

            // Create match in database
            var homeSeasonYear = simulationDto.AwayTeamSeason.Split("/")[1];
            var awaySeasonYear = simulationDto.AwayTeamSeason.Split("/")[1];
            var homeInMatchName = $"{simulationDto.HomeTeamName.Replace(" ", "_")}_{homeSeasonYear}";
            var awayInMatchName = $"{simulationDto.AwayTeamName.Replace(" ", "_")}_{awaySeasonYear}";
            var command = new CreateMatchCommand
            {
                HomeTeamId = simulationDto.HomeTeamId,
                AwayTeamId = simulationDto.AwayTeamId,
                HomeSeasonId = simulationDto.HomeSeasonId,
                AwaySeasonId = simulationDto.AwaySeasonId,
                HomeTeamInMatchName = homeInMatchName,
                AwayTeamInMatchName = awayInMatchName,
                ScheduledDateTimeUtc = DateTime.UtcNow,
                MatchStatus = "SimulationInProgress",
                CreatorId = userId,
                ModelSimulationStartTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30),
                IsLive = true
            };
            var result = await mediator.Send(command, cancellationToken);
            if (!result.Succeeded)
                return BadRequest(result);

            // Prepare request for simulation service
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                match_id = result.Id,
                home_team_id = simulationDto.HomeTeamId,
                away_team_id = simulationDto.AwayTeamId,
                home_team_name = simulationDto.HomeTeamName,
                away_team_name = simulationDto.AwayTeamName,
                home_team_season = simulationDto.HomeTeamSeason,
                away_team_season = simulationDto.AwayTeamSeason,
                num_tokens_to_generate = 10000,
                temperature = 0.7,
                top_p = 0.9,
                top_k = 50,
                max_new_tokens = 1024
            }), Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);

            // Start the simulation
            var response =
                await httpClient.PostAsync($"{_simulationOptions.BaseUrl}/startMatch", content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var statusCommand = new UpdateMatchStatusCommand
                {
                    MatchId = result.Id
                };
                var statusResult = await mediator.Send(statusCommand, cancellationToken);
                if (!statusResult.Succeeded)
                {
                    result.Succeeded = false;
                    result.Error = statusResult.Error;
                    return BadRequest(result);
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode((int)response.StatusCode, error);
            }

            result.ApiResponse = await response.Content.ReadFromJsonAsync<StartMatchResponse>(cancellationToken);
            if (result.ApiResponse != null)
            {
                await RegisterApiWebhook(new ApiRegisterWebhookRequest
                {
                    SimulationId = result.ApiResponse.SimulationId,
                    WebhookUrl =
                        $"https://localhost:7082/api/matches/webhookNotification/{result.ApiResponse.SimulationId}",
                    WebhookSecret = _simulationOptions.ApiKey
                });
                await _unitOfWork.Matches
                    .UpdateSimulationIdAsync(result.Id, result.ApiResponse.SimulationId,
                        cancellationToken);
                var notification = new Notification
                {
                    UserId = userId,
                    Content =
                        $"Your match simulation for {simulationDto.HomeTeamName} vs {simulationDto.AwayTeamName} has started.We will keep you updated with the results.",
                    Type = NotificationType.SimulationStart,
                    Title = "Match Simulation Started"
                };
                var notificationCommand =
                    new CreateNotificationCommand { Notification = notification };
                var notificationResult = mediator.Send(notificationCommand, cancellationToken);
                if (!notificationResult.Result.Succeeded)
                    _logger.LogError("Failed to create notification for simulation start: {Error}",
                        notificationResult.Result.Error);
            }

            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException { ErrorCode: 10061 })
        {
            // Specific handling for connection refused errors
            _logger.LogError(ex,
                "Simulation service connection refused at {Url}. The service might be down or not running.",
                _simulationOptions.BaseUrl);

            // Return user-friendly error message
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Unable to connect to simulation service. Please try again later.",
                details =
                    "The simulation service is currently unavailable. This could be because it is not running or the connection settings are incorrect."
            });
        }
        catch (HttpRequestException ex)
        {
            // Handle other HTTP request exceptions
            _logger.LogError(ex, "HTTP request error during match simulation: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Error communicating with simulation service",
                details = ex.Message
            });
        }
        catch (Exception ex)
        {
            // General error handling for other exceptions
            _logger.LogError(ex, "Unexpected error during match simulation: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "An unexpected error occurred",
                details = "Please contact support if the issue persists."
            });
        }
    }

    [NonAction]
    private async Task<ApiRegisterWebhookResponse> RegisterApiWebhook(ApiRegisterWebhookRequest webhookRequest)
    {
        const int maxRetries = 3;
        var currentRetry = 0;

        while (currentRetry < maxRetries)
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15); // Set a reasonable timeout

                // Add API key if available
                if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);

                // Prepare the request content
                var content = new StringContent(JsonSerializer.Serialize(new
                {
                    webhook_url = webhookRequest.WebhookUrl,
                    webhook_secret = webhookRequest.WebhookSecret
                }), Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Attempting to register webhook for simulation {SimulationId} (Attempt {Attempt}/{MaxAttempts})",
                    webhookRequest.SimulationId, currentRetry + 1, maxRetries);

                // Make the API call to register webhook
                var response = await httpClient.PostAsync(
                    $"{_simulationOptions.BaseUrl}/simulations/{webhookRequest.SimulationId}/webhook",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to register webhook: HTTP {StatusCode}. Response: {Response}",
                        (int)response.StatusCode, errorContent);

                    if (IsRetryableStatusCode((int)response.StatusCode))
                    {
                        currentRetry++;
                        if (currentRetry < maxRetries)
                        {
                            var delayMs = CalculateExponentialBackoff(currentRetry);
                            _logger.LogInformation("Retrying webhook registration after {DelayMs}ms", delayMs);
                            await Task.Delay(delayMs);
                            continue;
                        }
                    }

                    return new ApiRegisterWebhookResponse
                    {
                        Message = "Failed to register webhook with simulation service",
                        Detail = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }

                // Parse the response
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiRegisterWebhookResponse>();
                var result = apiResponse ?? new ApiRegisterWebhookResponse
                {
                    Message = "Webhook registered successfully",
                    SimulationId = webhookRequest.SimulationId
                };

                _logger.LogInformation("Successfully registered webhook for simulation {SimulationId}",
                    webhookRequest.SimulationId);
                return result;
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException { ErrorCode: 10061 })
            {
                // Connection refused errors
                _logger.LogError(ex,
                    "Connection refused when registering webhook for simulation {SimulationId} at {BaseUrl}",
                    webhookRequest.SimulationId, _simulationOptions.BaseUrl);

                currentRetry++;
                if (currentRetry < maxRetries)
                {
                    var delayMs = CalculateExponentialBackoff(currentRetry);
                    await Task.Delay(delayMs);
                    continue;
                }

                return new ApiRegisterWebhookResponse
                {
                    Message = "Failed to register webhook: connection refused",
                    Detail = "The simulation service is unavailable. Please check if the service is running."
                };
            }
            catch (TaskCanceledException ex)
            {
                // Handle timeouts
                _logger.LogError(ex, "Timeout when registering webhook for simulation {SimulationId}",
                    webhookRequest.SimulationId);

                currentRetry++;
                if (currentRetry < maxRetries)
                {
                    var delayMs = CalculateExponentialBackoff(currentRetry);
                    await Task.Delay(delayMs);
                    continue;
                }

                return new ApiRegisterWebhookResponse
                {
                    Message = "Failed to register webhook: request timeout",
                    Detail = "The operation timed out. The simulation service might be overloaded."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering webhook for simulation {SimulationId}: {Message}",
                    webhookRequest.SimulationId, ex.Message);

                return new ApiRegisterWebhookResponse
                {
                    Message = "Failed to register webhook",
                    Detail = ex.Message
                };
            }

        // We should never reach here, but just in case
        return new ApiRegisterWebhookResponse
        {
            Message = "Failed to register webhook after maximum retries",
            Detail = "The operation could not be completed after multiple attempts."
        };
    }

    [NonAction]
    private bool IsRetryableStatusCode(int statusCode)
    {
        // Status codes that are worth retrying:
        // 408 (Request Timeout)
        // 429 (Too Many Requests)
        // 5xx (Server errors)
        return statusCode == 408 || statusCode == 429 || (statusCode >= 500 && statusCode < 600);
    }

    [NonAction]
    private int CalculateExponentialBackoff(int retryAttempt)
    {
        // Simple exponential backoff with jitter:
        // Base delay: 200ms
        // Max delay: 2000ms
        // Formula: min(maxDelay, baseDelay * 2^attempt) + random jitter

        const int baseDelayMs = 200;
        const int maxDelayMs = 2000;

        var exponentialDelay = Math.Min(maxDelayMs, baseDelayMs * Math.Pow(2, retryAttempt));
        var jitter = new Random().Next(0, 100); // Add up to 100ms of random jitter

        return (int)exponentialDelay + jitter;
    }

    [HttpPost("webhookNotification/{simulationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveWebhookNotification(string simulationId,
        [FromBody] WebhookNotificationPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received webhook notification for simulation {SimulationId} with status {Status}",
                simulationId, payload.Status);

            // Validate payload
            if (string.IsNullOrEmpty(payload.SimulationId) || payload.SimulationId != simulationId)
            {
                _logger.LogWarning("Simulation ID mismatch in webhook payload: expected {Expected}, got {Actual}",
                    simulationId, payload.SimulationId);
                return BadRequest(new { error = "Simulation ID mismatch" });
            }

            // Find the match associated with this simulation
            var match = await GetMatchBySimulationId(simulationId, cancellationToken);
            if (match == null)
            {
                _logger.LogWarning("No match found for simulation ID {SimulationId}", simulationId);
                return NotFound(new { error = "Match not found for simulation", simulation_id = simulationId });
            }

            // Handle different webhook statuses
            switch (payload.Status?.ToLower())
            {
                case "completed":
                    await HandleSimulationCompleted(match, payload, cancellationToken);
                    break;

                case "failed":
                    await HandleSimulationFailed(match, payload, cancellationToken);
                    break;

                default:
                    _logger.LogInformation(
                        "Webhook received for simulation {SimulationId} with status {Status} - no action needed",
                        simulationId, payload.Status);
                    break;
            }

            return Ok(new { message = "Webhook notification processed successfully", simulation_id = simulationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook notification for simulation {SimulationId}", simulationId);
            return BadRequest(new { error = "Failed to process webhook notification", details = ex.Message });
        }
    }


    // Helper method to check if the user has a live match
    [NonAction]
    private async Task<bool> HasLiveMatch(string userId, CancellationToken cancellationToken = default)
    {
        var query = new GetLiveMatchQuery { UserId = userId };
        var result = await mediator.Send(query, cancellationToken);
        return result.Succeeded && result.LiveMatch.Id != 0 && result.HasLiveMatch;
    }


    // Helper methods for simulation tracking
    [NonAction]
    private async Task<Match?> GetMatchBySimulationId(string simulationId, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Search for a match with this simulation_id
        var match = await unitOfWork.Matches.FindAsync(m => m.SimulationId == simulationId, cancellationToken);
        return match;
    }

    [NonAction]
    private async Task UpdateLocalMatchStatus(int matchId, string status, CancellationToken cancellationToken)
    {
        try
        {
            var statusCommand = new UpdateMatchStatusCommand
            {
                MatchId = matchId,
                NewStatus = status
            };

            await mediator.Send(statusCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the main operation
            Console.WriteLine($"Failed to update local match status: {ex.Message}");
        }
    }

    [NonAction]
    private async Task UpdateMatchWithSimulationResult(int matchId, SimulationResultResponse result,
        CancellationToken cancellationToken)
    {
        try
        {
            var updateCommand = new UpdateMatchCommand
            {
                Id = matchId,
                HomeTeamScore = result.HomeTeamScore,
                AwayTeamScore = result.AwayTeamScore,
                MatchStatus = "Completed"
            };

            await mediator.Send(updateCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the main operation
            Console.WriteLine($"Failed to update match with simulation result: {ex.Message}");
        }
    }

    [NonAction]
    private async Task HandleSimulationCompleted(Match match, WebhookNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling completed simulation for match {MatchId}, simulation {SimulationId}",
                match.Id, payload.SimulationId);

            // Update match status to completed
            await UpdateLocalMatchStatus(match.Id, "Completed", cancellationToken);

            // Get the full simulation result if we have a result URL
            if (!string.IsNullOrEmpty(payload.ResultUrl))
                try
                {
                    var httpClient = httpClientFactory.CreateClient();
                    if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
                        httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);

                    var response = await httpClient.GetAsync($"{_simulationOptions.BaseUrl}{payload.ResultUrl}",
                        cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var simulationResult =
                            await response.Content.ReadFromJsonAsync<SimulationResultResponse>(cancellationToken);
                        if (simulationResult != null)
                            await UpdateMatchWithSimulationResult(match.Id, simulationResult, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch simulation result from URL {ResultUrl}", payload.ResultUrl);
                }

            // Create and send notification to the user who created the match
            var notification = new Notification
            {
                Id = Guid.NewGuid()
                    .ToString(),
                Content =
                    $"Your match simulation between {match.HomeTeam?.Name ?? "Home Team"} and {match.AwayTeam?.Name ?? "Away Team"} has completed! The match is now ready to watch.",
                Type = NotificationType.MatchEnd,
                UserId = match.CreatorId,
                Time = DateTime.UtcNow,
                Title = "Match Simulation Completed"
            };

            // Send notification via SignalR
            await _hubContext.Clients.User(match.CreatorId)
                .SendMatchEndNotificationAsync(notification, payload.SimulationId);

            _logger.LogInformation("Successfully processed simulation completion for match {MatchId}", match.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling simulation completion for match {MatchId}", match.Id);
        }
    }

    [NonAction]
    private async Task HandleSimulationFailed(Match match, WebhookNotificationPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Handling failed simulation for match {MatchId}, simulation {SimulationId}",
                match.Id, payload.SimulationId);

            // Update match status to failed
            await UpdateLocalMatchStatus(match.Id, "Failed", cancellationToken);

            // Create and send notification to the user who created the match
            var errorMessage = !string.IsNullOrEmpty(payload.ErrorMessage)
                ? payload.ErrorMessage
                : "Unknown simulation error";

            var notification = new Notification
            {
                Id = Guid.NewGuid()
                    .ToString(),
                Content =
                    $"Your match simulation between {match.HomeTeam?.Name ?? "Home Team"} and {match.AwayTeam?.Name ?? "Away Team"} has failed. Error: {errorMessage}",
                Type = NotificationType.Error,
                UserId = match.CreatorId,
                Time = DateTime.UtcNow,
                Title = "Match Simulation Failed"
            };

            // Send notification via SignalR  
            await _hubContext.Clients.User(match.CreatorId).SendNotificationAsync(notification);

            _logger.LogInformation("Successfully processed simulation failure for match {MatchId}", match.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling simulation failure for match {MatchId}", match.Id);
        }
    }


    private class HealthCheckResponse
    {
        [JsonPropertyName("status")] public string? Status { get; init; }

        [JsonPropertyName("timestamp")] public string? Timestamp { get; init; }

        [JsonPropertyName("version")] public string? Version { get; init; }

        [JsonPropertyName("model_loaded")] public bool ModelLoaded { get; init; }

        [JsonPropertyName("xgboost_loaded")] public bool XgboostLoaded { get; init; }
    }
}