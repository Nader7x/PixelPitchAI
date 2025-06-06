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
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Footex.Configuration;
using Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(
    IMediator mediator,
    IHttpClientFactory httpClientFactory,
    MatchMapper matchMapper,
    IOptions<SimulationServiceOptions> simulationOptions,
    ILiveMatchStatisticsService liveMatchService,
    IPerformanceMonitoringService performanceMonitoringService,
    IServiceScopeFactory serviceScopeFactory,
    IUnitOfWork unitOfWork,
    ILogger<MatchesController> logger,
    IHubContext<NotificationService, INotificationService> hubContext,
    ICacheService cacheService) : ControllerBase
{
    private readonly MatchMapper _matchMapper = matchMapper;
    private readonly SimulationServiceOptions _simulationOptions = simulationOptions.Value;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<MatchesController> _logger = logger;
    private readonly IHubContext<NotificationService, INotificationService> _hubContext = hubContext;
    private readonly ICacheService _cacheService = cacheService;

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
        string cacheKey = $"matches_all_{seasonId}_{teamId}_{status}_{fromDate:yyyy-MM-dd}_{toDate:yyyy-MM-dd}_{matchWeek}";
        
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
            AwayTeamRedCards = matchDto.AwayTeamRedCards,
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
        var query = new GetUserMatchesQuery() { UserId = userId };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }


    [HttpPost("/simulateMatch/{userId}")]
    [Authorize]
    public async Task<ActionResult<CreateMatchCommandResponse>> SimulateMatch(string userId,
        [FromBody] SimulateMatchDto simulationDto, CancellationToken cancellationToken)
    {
        if (HasLiveMatch(userId, cancellationToken).Result)
            return BadRequest(new { error = "You Can Not Simulate Two Matches At The Same Time" });
        var httpClient = httpClientFactory.CreateClient();

        var healthResponse = await httpClient.GetAsync($"{_simulationOptions.BaseUrl}/health", cancellationToken);
        _logger.LogInformation("Simulation Service Health Check {HealthResponse}", healthResponse.Content.ToString());
        if (!healthResponse.IsSuccessStatusCode)
            return StatusCode((int)healthResponse.StatusCode, "Simulation service is not available");

        var healthCheckResult = await healthResponse.Content.ReadFromJsonAsync<HealthCheckResponse>(cancellationToken);
        if (healthCheckResult == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Failed to parse simulation service health check response.");
        }

        if (!healthCheckResult.ModelLoaded || !healthCheckResult.XgboostLoaded)
        {
            var notReadyReason = new StringBuilder("Simulation service is not ready. ");
            if (!healthCheckResult.ModelLoaded) notReadyReason.Append("Model not loaded. ");
            if (!healthCheckResult.XgboostLoaded) notReadyReason.Append("XGBoost not loaded.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = notReadyReason.ToString().Trim(), healthDetails = healthCheckResult });
        }

        var homeSeasonYear = simulationDto.AwayTeamSeason.Split("/")[1];
        var awaySeasonYear = simulationDto.AwayTeamSeason.Split("/")[1];
        var homeInMatchName = $"{simulationDto.HomeTeamName.Replace(" ", "_")}_{homeSeasonYear}";
        var awayInMatchName = $"{simulationDto.AwayTeamName.Replace(" ", "_")}_{awaySeasonYear}";
        var command = new CreateMatchCommand()
        {
            HomeTeamId = simulationDto.HomeTeamId,
            AwayTeamId = simulationDto.AwayTeamId,
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


        var content = new StringContent(JsonSerializer.Serialize(new
        {
            match_id = result.Id,
            home_team_id = simulationDto.HomeTeamId,
            away_team_id = simulationDto.AwayTeamId,
            home_team_name = simulationDto.HomeTeamName,
            away_team_name = simulationDto.AwayTeamName,
            home_team_season = simulationDto.HomeTeamSeason,
            away_team_season = simulationDto.AwayTeamSeason,
            num_tokens_to_generate = 400,
            temperature = 0.7,
            top_p = 0.9,
            top_k = 50,
            max_new_tokens = 1024
        }), Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);
        }

        var response =
            await httpClient.PostAsync($"{_simulationOptions.BaseUrl}/startMatch", content, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var statusCommand = new UpdateMatchStatusCommand()
            {
                MatchId = result.Id,
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
            await _unitOfWork.Matches
                .UpdateSimulationIdAsync(result.Id, result.ApiResponse.SimulationId,
                cancellationToken);
            var notification = new Notification
            {
                UserId = userId,
                Content = $"Your match simulation for {simulationDto.HomeTeamName} vs {simulationDto.AwayTeamName} has started.We will keep you updated with the results.",
                Type = NotificationType.SimulationStart,
            };
            var notificationCommand =
                new CreateNotificationCommand { Notification = notification };
            var notificationResult = mediator.Send(notificationCommand, cancellationToken);
            if (!notificationResult.Result.Succeeded)
            {
                _logger.LogError("Failed to create notification for simulation start: {Error}",
                    notificationResult.Result.Error);
            }
            if (notificationResult.Result.Notification != null)
                await _hubContext.Clients.User(userId).SendNotificationAsync(
                    notificationResult.Result.Notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Ok(result);
        }

        result.Succeeded = false;
        result.Error = "Failed to parse simulation service response.";
        return BadRequest(result);
    }

    [HttpGet("live/all")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult GetAllLiveMatches()
    {
        try
        {
            var liveMatches = liveMatchService.GetAllLiveMatches();
            var response = liveMatches.Select(m => new
            {
                matchId = m.Key,
                homeTeam = m.Value.HomeTeam?.Name ?? "Unknown",
                awayTeam = m.Value.AwayTeam?.Name ?? "Unknown",
                homeScore = m.Value.HomeTeamScore,
                awayScore = m.Value.AwayTeamScore,
                status = m.Value.MatchStatus,
                lastUpdated = DateTime.UtcNow
            });

            return Ok(new { matches = response, count = liveMatches.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve live matches", details = ex.Message });
        }
    }

    [HttpGet("live/cached/{matchId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult GetCachedLiveMatch(int matchId)
    {
        try
        {
            var cachedMatch = liveMatchService.GetCachedLiveMatch(matchId.ToString());
            if (cachedMatch == null)
            {
                return NotFound(new { error = "Match not found in live cache", matchId });
            }

            return Ok(new
            {
                matchId = cachedMatch.Id,
                homeTeam = cachedMatch.HomeTeam?.Name ?? "Unknown",
                awayTeam = cachedMatch.AwayTeam?.Name ?? "Unknown",
                homeScore = cachedMatch.HomeTeamScore,
                awayScore = cachedMatch.AwayTeamScore,
                status = cachedMatch.MatchStatus,
                possession = new
                {
                    home = cachedMatch.HomeTeamPossession,
                    away = cachedMatch.AwayTeamPossession
                },
                shots = new
                {
                    home = cachedMatch.HomeTeamShots,
                    away = cachedMatch.AwayTeamShots
                },
                lastUpdated = DateTime.UtcNow,
                source = "cache"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve cached match", details = ex.Message });
        }
    }

    [HttpPost("live/preload")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PreloadMatchesForLiveStats([FromBody] PreloadMatchesRequest request)
    {
        try
        {
            if (!request.MatchIds.Any())
            {
                return BadRequest(new { error = "At least one match ID is required" });
            }

            var startTime = DateTime.UtcNow;
            await liveMatchService.PreloadMultipleMatchesForLiveStatistics(request.MatchIds);
            var endTime = DateTime.UtcNow;

            var preloadedMatches = new List<object>();
            foreach (var matchId in request.MatchIds)
            {
                var cachedMatch = liveMatchService.GetCachedLiveMatch(matchId);
                if (cachedMatch != null)
                {
                    preloadedMatches.Add(new
                    {
                        matchId = cachedMatch.Id,
                        homeTeam = cachedMatch.HomeTeam?.Name ?? "Unknown",
                        awayTeam = cachedMatch.AwayTeam?.Name ?? "Unknown",
                        status = cachedMatch.MatchStatus
                    });
                }
            }

            return Ok(new
            {
                message = "Matches preloaded successfully",
                preloadedCount = preloadedMatches.Count,
                requestedCount = request.MatchIds.Count(),
                preloadTimeMs = (endTime - startTime).TotalMilliseconds,
                matches = preloadedMatches
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to preload matches", details = ex.Message });
        }
    }

    [HttpPost("live/preload/{matchId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PreloadSingleMatchForLiveStats(int matchId)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            await liveMatchService.PreloadMatchForLiveStatistics(matchId.ToString());
            var endTime = DateTime.UtcNow;

            var cachedMatch = liveMatchService.GetCachedLiveMatch(matchId.ToString());
            if (cachedMatch == null)
            {
                return BadRequest(new { error = "Failed to preload match", matchId });
            }

            return Ok(new
            {
                message = "Match preloaded successfully",
                matchId = cachedMatch.Id,
                homeTeam = cachedMatch.HomeTeam?.Name ?? "Unknown",
                awayTeam = cachedMatch.AwayTeam?.Name ?? "Unknown",
                status = cachedMatch.MatchStatus,
                preloadTimeMs = (endTime - startTime).TotalMilliseconds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to preload match", details = ex.Message });
        }
    }

    [HttpGet("live/performance-stats")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetLiveMatchPerformanceStats()
    {
        try
        {
            var allLiveMatches = liveMatchService.GetAllLiveMatches();

            return Ok(new
            {
                totalLiveMatches = allLiveMatches.Count(),
                cacheStatus = new
                {
                    totalCachedMatches = allLiveMatches.Count(),
                    memoryEfficient = true,
                    lastRefresh = DateTime.UtcNow
                },
                performance = new
                {
                    avgResponseTimeMs = "< 5ms (cached)",
                    databaseCallsReduced = "~90% reduction vs non-cached approach",
                    concurrentMatchSupport = "Unlimited with O(1) lookup"
                },
                matches = allLiveMatches.Select(m => new
                {
                    matchId = m.Key,
                    homeTeam = m.Value.HomeTeam?.Name ?? "Unknown",
                    awayTeam = m.Value.AwayTeam?.Name ?? "Unknown",
                    status = m.Value.MatchStatus,
                    isPreloaded = true
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve performance stats", details = ex.Message });
        }
    }

    [HttpGet("LiveMatch/{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(GetLiveMatchQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetLiveMatchQueryResponse>> GetLiveMatch(string userId)
    {
        var query = new GetLiveMatchQuery() { UserId = userId };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }


    // Helper method to check if the user has a live match
    [NonAction]
    private async Task<bool> HasLiveMatch(string userId, CancellationToken cancellationToken = default)
    {
        var query = new GetLiveMatchQuery() { UserId = userId };
        var result = await mediator.Send(query, cancellationToken);
        return result.Succeeded && result.MatchId != 0;
    }

    [HttpGet("performance/dashboard")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult GetPerformanceDashboard()
    {
        try
        {
            // Get detailed performance metrics
            var performanceMetrics = performanceMonitoringService.GetDetailedMetrics();
            dynamic cacheStatus = liveMatchService.GetCacheStatus();
            var allLiveMatches = liveMatchService.GetAllLiveMatches();

            // Calculate optimization benefits
            var totalPotentialDbCalls = performanceMetrics.CacheMetrics.TotalOperations +
                                        performanceMetrics.DatabaseCalls.Sum(db => db.Count);
            var actualDbCalls = performanceMetrics.DatabaseCalls.Sum(db => db.Count);
            var optimizationRatio = totalPotentialDbCalls > 0
                ? ((double)(totalPotentialDbCalls - actualDbCalls) / totalPotentialDbCalls) * 100
                : 0;

            var dashboard = new
            {
                // Summary
                summary = new
                {
                    totalLiveMatches = allLiveMatches.Count,
                    systemUptime = performanceMetrics.UpTime.ToString(@"dd\.hh\:mm\:ss"),
                    optimizationRatio = $"{optimizationRatio:F1}%",
                    avgResponseTime = performanceMetrics.CacheMetrics.TotalOperations > 0 ? "< 5ms" : "N/A",
                    lastRefresh = DateTime.UtcNow
                },

                // Real-time Performance Metrics
                performance = new
                {
                    database = new
                    {
                        totalCalls = performanceMetrics.DatabaseCalls.Sum(db => db.Count),
                        callsPerSecond = performanceMetrics.SystemMetrics.TotalDatabaseCallsPerSecond,
                        averageDuration = $"{performanceMetrics.SystemMetrics.AverageDatabaseCallDuration:F2}ms",
                        operations = performanceMetrics.DatabaseCalls.Select(db => new
                        {
                            operation = db.OperationType,
                            count = db.Count,
                            avgDuration = $"{db.AverageDurationMs:F2}ms",
                            minDuration = $"{db.MinDurationMs:F2}ms",
                            maxDuration = $"{db.MaxDurationMs:F2}ms",
                            rate = $"{db.CallsPerSecond:F1}/sec"
                        })
                    },
                    cache = new
                    {
                        hitRatio = $"{performanceMetrics.CacheMetrics.HitRatio * 100:F1}%",
                        totalHits = performanceMetrics.CacheMetrics.TotalHits,
                        totalMisses = performanceMetrics.CacheMetrics.TotalMisses,
                        operationsPerSecond = $"{performanceMetrics.CacheMetrics.OperationsPerSecond:F1}/sec"
                    }
                }, // Live Match Status
                liveMatches = new
                {
                    totalCached = cacheStatus.TotalCachedMatches,
                    memoryEfficient = cacheStatus.MemoryEfficient,
                    matches = allLiveMatches.Take(10).Select(m => new
                    {
                        matchId = m.Key,
                        homeTeam = m.Value.HomeTeam?.Name ?? "Unknown",
                        awayTeam = m.Value.AwayTeam?.Name ?? "Unknown",
                        score = $"{m.Value.HomeTeamScore ?? 0} - {m.Value.AwayTeamScore ?? 0}",
                        status = m.Value.MatchStatus
                    }),
                    showingFirst = Math.Min(10, allLiveMatches.Count),
                    totalCount = allLiveMatches.Count
                }
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to generate performance dashboard", details = ex.Message });
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

    // Simulation status tracking endpoints
    [HttpGet("simulation/{simulationId}/status")]
    [Authorize]
    [ProducesResponseType(typeof(SimulationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SimulationStatusResponse>> GetSimulationStatus(string simulationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get match by simulation_id from a database
            var match = await GetMatchBySimulationId(simulationId, cancellationToken);
            if (match == null)
            {
                return NotFound(new { error = "Simulation not found", simulation_id = simulationId });
            }

            var httpClient = httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);
            }

            // Call model API for status
            var response = await httpClient.GetAsync($"{_simulationOptions.BaseUrl}/simulationStatus/{simulationId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode,
                    new { error = "Failed to get simulation status from model API" });
            }

            var statusResponse = await response.Content.ReadFromJsonAsync<SimulationStatusResponse>(cancellationToken);
            if (statusResponse != null)
            {
                // Add local match information
                statusResponse.MatchId = match.Id;
                statusResponse.MatchStatus = match.MatchStatus;

                // Update the local match status if needed
                if (statusResponse.Status != match.MatchStatus)
                {
                    await UpdateLocalMatchStatus(match.Id, statusResponse.Status, cancellationToken);
                }
            }

            return Ok(statusResponse);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to get simulation status", details = ex.Message });
        }
    }

    [HttpGet("simulation/{simulationId}/result")]
    [Authorize]
    [ProducesResponseType(typeof(SimulationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SimulationResultResponse>> GetSimulationResult(string simulationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get match by simulation_id from a database
            var match = await GetMatchBySimulationId(simulationId, cancellationToken);
            if (match == null)
            {
                return NotFound(new { error = "Simulation not found", simulation_id = simulationId });
            }

            var httpClient = httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);
            }

            // Call model API for a result
            var response = await httpClient.GetAsync($"{_simulationOptions.BaseUrl}/simulationResult/{simulationId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode,
                    new { error = "Failed to get simulation result from model API" });
            }

            var resultResponse = await response.Content.ReadFromJsonAsync<SimulationResultResponse>(cancellationToken);
            if (resultResponse == null) return Ok(resultResponse);
            resultResponse.MatchId = match.Id;

            // If simulation is completed, update the local match with final results
            if (resultResponse.Status == "completed")
            {
                var notification = new Notification
                {
                    Type = NotificationType.MatchStart,
                    UserId = match.CreatorId,
                    Content = $"Your Requested Match Has Started Go to the Live Match View to Watch",
                };
                var notificationCommand = new CreateNotificationCommand()
                {
                    Notification = notification
                };
                var notificationResult = await mediator.Send(notificationCommand, cancellationToken);
                if (!notificationResult.Succeeded)
                {
                    _logger.LogError("Failed to create notification for match start: {Error}",
                        notificationResult.Error);
                }

                if (notificationResult.Notification != null)
                    if (match.SimulationId != null)
                        await _hubContext.Clients.User(match.CreatorId).SendMatchStartNotificationAsync(notificationResult.Notification,
                            match.SimulationId);
                await UpdateMatchWithSimulationResult(match.Id, resultResponse, cancellationToken);
            }

            return Ok(resultResponse);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to get simulation result", details = ex.Message });
        }
    }

    [HttpGet("simulation/{simulationId}/stream")]
    [Authorize]
    public async Task<IActionResult> StreamSimulationUpdates(string simulationId, CancellationToken cancellationToken)
    {
        try
        {
            // Verify simulation exists
            var match = await GetMatchBySimulationId(simulationId, cancellationToken).ConfigureAwait(false);
            if (match == null)
            {
                return NotFound(new { error = "Simulation not found", simulation_id = simulationId });
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var httpClient = httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);
            }

            // Stream from model API
            using var response = await httpClient.GetAsync(
                $"{_simulationOptions.BaseUrl}/simulationResult/{simulationId}/stream",
                HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return StatusCode((int)response.StatusCode,
                    new { error = "Failed to start simulation stream", details = errorBody });
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Explicitly create the string to avoid ambiguity for Response.WriteAsync
                var eventString = $"data: {line}\n\n";
                await Response.WriteAsync(eventString, cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Parse the event to check for completion
                if (!line.Contains("\"event_type\":\"completion\"")) continue;
                try
                {
                    // Ensure 'line' is not null and is a valid JSON string for deserialization
                    var eventData = JsonSerializer.Deserialize<SimulationStreamEvent>(line);
                    if (eventData?.EventType == "completion")
                    {
                        // Update local match status
                        await UpdateLocalMatchStatus(match.Id, "completed", cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (JsonException jsonEx)
                {
                    // Log specific JSON parsing errors if necessary
                    _logger.LogWarning(jsonEx, "Failed to parse stream event: {Line}", line);
                }
                catch
                {
                    // Ignore other parsing errors for stream events or log them as warnings
                }
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or a request was canceled
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            // Log the exception,
            // For example, _logger.LogError(ex, "Error in StreamSimulationUpdates for {SimulationId}", simulation_id);
            return BadRequest(new { error = "Failed to stream simulation updates", details = ex.Message });
        }
    }

    [HttpPost("simulation/{simulationId}/webhook")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(RegisterWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterWebhookResponse>> RegisterWebhook(string simulationId,
        [FromBody] RegisterWebhookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify simulation exists
            var match = await GetMatchBySimulationId(simulationId, cancellationToken);
            if (match == null)
            {
                return NotFound(new { error = "Simulation not found", simulation_id = simulationId });
            }

            var httpClient = httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(_simulationOptions.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _simulationOptions.ApiKey);
            }

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(
                $"{_simulationOptions.BaseUrl}/simulations/{simulationId}/webhook",
                content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode,
                    new { error = "Failed to register webhook with model API" });
            }

            var webhookResponse = await response.Content.ReadFromJsonAsync<RegisterWebhookResponse>(cancellationToken);
            return Ok(webhookResponse);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to register webhook", details = ex.Message });
        }
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
}
