using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using Infrastructure.Configuration;


namespace Infrastructure.Services
{
    public class MatchEventRabbitMqClient(
        ILogger<MatchEventRabbitMqClient> logger,
        IHubContext<MatchHub, IMatchHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService,
        IOptions<RabbitMqOptions> rabbitMqOptions)
        : BackgroundService
    {
        private int _eventSequence;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly RabbitMqOptions _rabbitMqSettings = rabbitMqOptions.Value;

        private readonly Dictionary<string, List<FootballMatchEvent>?> _matchEventsCache = new();

        // Cache for loaded match entities - key: matchId, value: match entity
        private readonly Dictionary<string, Match> _loadedMatches = new();

        // Lock for thread-safe access to match cache
        private readonly object _matchCacheLock = new();

        // Timer for periodic flushing of events to a database as a safety mechanism
        private Timer? _flushTimer;

        private async Task InitializeRabbitMq(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMqSettings.HostName,
                    UserName = _rabbitMqSettings.UserName,
                    Password = _rabbitMqSettings.Password,
                    VirtualHost = _rabbitMqSettings.VirtualHost,
                    Port = _rabbitMqSettings.Port,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                var arguments = new Dictionary<string, object>
                {
                    { "x-message-ttl", 86400000 } // 24 hours in milliseconds
                };

                await _channel.ExchangeDeclareAsync(
                    exchange: "match_events",
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(
                    queue: _rabbitMqSettings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: arguments!, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(
                    queue: _rabbitMqSettings.QueueName,
                    exchange: "match_events",
                    routingKey: "match.events", cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(0, 1, false, cancellationToken: stoppingToken);
                logger.LogInformation(
                    "RabbitMQ connected to {SettingsHostName}:{SettingsPort}/{SettingsVirtualHost}",
                    _rabbitMqSettings.HostName,
                    _rabbitMqSettings.Port, _rabbitMqSettings.VirtualHost);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not initialize RabbitMQ connection");
                throw;
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await InitializeRabbitMq(cancellationToken);
                _flushTimer = new Timer(FlushEventsToDatabase, null, TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1)); // Example: flush every minute
                await base.StartAsync(cancellationToken);
                logger.LogInformation("MatchEventRabbitMqClient started successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start MatchEventRabbitMqClient.");
                // Consider how to handle startup failure (e.g., stopping the application host)
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                logger.LogError("Channel is not initialized. Cannot start consuming events.");
                return Task.CompletedTask;
            }

            _flushTimer = new Timer(FlushEventsToDatabase, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    logger.LogInformation("Received match event: {Message}", message);

                    // Deserialize directly from JSON
                    var matchEvent = JsonSerializer.Deserialize<FootballMatchEvent>(message);
                    if (matchEvent != null)
                    {
                        // Load the match entity once and cache it for later events
                        var matchEntity = await GetOrLoadMatchEntity(matchEvent.match_id);

                        if (matchEntity != null)
                        {
                            // Process the event with the cached match entity
                            await ProcessMatchEventWithEntity(matchEvent, matchEntity);
                        }

                        await CacheMatchEvent(matchEvent);

                        // If this is a match_end event, save all cached events to the database and cleanup
                        if (matchEvent is { event_type: "match_end", action: "match_end" })
                        {
                            await SaveMatchEventsToDatabase(matchEvent.match_id);
                            // Remove match from cache when match ends
                            lock (_matchCacheLock)
                            {
                                _loadedMatches.Remove(matchEvent.match_id);
                                logger.LogInformation("Removed match {MatchId} from cache after match end",
                                    matchEvent.match_id);
                            }
                        }

                        // Broadcast the event to clients
                        await BroadcastEventToClients(matchEvent).WaitAsync(stoppingToken);
                    }

                    // Try to acknowledge with proper error handling
                    if (_channel.IsOpen)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    else
                    {
                        logger.LogWarning("Channel closed before acknowledgment. Trying to recover connection.");
                        // Don't try to re-acknowledge after recovery as the delivery tag won't be valid
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing match event");
                    try
                    {
                        if (_channel.IsOpen)
                        {
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                        }
                    }
                    catch (Exception nackEx)
                    {
                        logger.LogError(nackEx, "Error sending NACK");
                    }
                }
            };
            _channel.BasicConsumeAsync(
                queue: _rabbitMqSettings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            return Task.CompletedTask;
        }

        private async Task BroadcastEventToClients(FootballMatchEvent matchEvent)
        {
            await hubContext.Clients.Group(matchEvent.match_id)
                .SendMatchEventAsync("match_event", int.Parse(matchEvent.match_id), matchEvent);

            logger.LogInformation("Broadcasted match event {FootballMatchEvent} to clients", matchEvent);
        }

        /// <summary>
        /// Loads a match entity from a database and caches it. Only loads once per match.
        /// </summary>
        /// <param name="matchId">The match ID</param>
        /// <returns>The cached or newly loaded match entity</returns>
        private async Task<Match?> GetOrLoadMatchEntity(string matchId)
        {
            // Check if the match is already cached
            lock (_matchCacheLock)
            {
                if (_loadedMatches.TryGetValue(matchId, out var cachedMatch))
                {
                    logger.LogDebug("Using cached match entity for match {MatchId}", matchId);
                    return cachedMatch;
                }
            }

            // Load match from a database
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var stopwatch = Stopwatch.StartNew();
                var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
                stopwatch.Stop();

                performanceMonitoringService.RecordDatabaseCall("LoadMatchEntity", stopwatch.Elapsed.TotalMilliseconds);

                if (match != null)
                {
                    // Cache the loaded match
                    lock (_matchCacheLock)
                    {
                        _loadedMatches[matchId] = match;
                    }

                    logger.LogInformation("Loaded and cached match entity for match {MatchId}", matchId);
                    return match;
                }
                else
                {
                    logger.LogWarning("Match with ID {MatchId} not found in database", matchId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading match entity for match {MatchId}", matchId);
                return null;
            }
        }

        /// <summary>
        /// Processes a match event using the cached match entity for real-time updates.
        /// This method performs live statistics updates without hitting the database repeatedly.
        /// </summary>
        /// <param name="matchEvent">The football match event</param>
        /// <param name="matchEntity">The cached match entity</param>
        private async Task ProcessMatchEventWithEntity(FootballMatchEvent matchEvent, Match matchEntity)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();

                // Create or get the MatchEvents entity
                var matchEventsEntity = matchEntity.MatchEvents;
                if (matchEventsEntity == null)
                {
                    matchEventsEntity = new MatchEvents
                    {
                        MatchId = int.Parse(matchEvent.match_id),
                        EventsJson = "[]",
                        LastUpdated = DateTime.UtcNow,
                        TotalEvents = 0
                    };
                    matchEntity.MatchEvents = matchEventsEntity;
                } // Update match statistics in real-time using the cached entity

                await eventAnalysis.UpdateMatchStatistics(matchEvent, matchEventsEntity, matchEntity, false);
                // Broadcast real-time match statistics to clients
                await BroadcastMatchStatistics(matchEvent, matchEntity);

                logger.LogDebug("Processed event {EventIndex} for cached match {MatchId}",
                    matchEvent.event_index, matchEvent.match_id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing match event {EventIndex} for match {MatchId} with cached entity",
                    matchEvent.event_index, matchEvent.match_id);
            }
        }

        /// <summary>
        /// Broadcasts real-time match statistics to connected clients via SignalR.
        /// This method sends updated match data after each event is processed.
        /// </summary>
        /// <param name="matchEvent">The current match event</param>
        /// <param name="matchEntity">The updated match entity with current statistics</param>
        private async Task BroadcastMatchStatistics(FootballMatchEvent matchEvent, Match matchEntity)
        {
            try
            {
                // Create statistics object to broadcast
                var matchStatistics = new
                {
                    matchId = matchEntity.Id,
                    homeTeam = new
                    {
                        name = matchEntity.HomeTeam?.Name ?? matchEntity.HomeTeamInMatchName,
                        score = matchEntity.HomeTeamScore ?? 0,
                        shots = matchEntity.HomeTeamShots ?? 0,
                        shotsOnTarget = matchEntity.HomeTeamShotsOnTarget ?? 0,
                        possession = matchEntity.HomeTeamPossession ?? 0,
                        passes = matchEntity.HomeTeamPasses ?? 0,
                        passAccuracy = matchEntity.HomeTeamPassAccuracy ?? 0,
                        corners = matchEntity.HomeTeamCorners ?? 0,
                        fouls = matchEntity.HomeTeamFouls ?? 0,
                        yellowCards = matchEntity.HomeTeamYellowCards ?? 0,
                        redCards = matchEntity.HomeTeamRedCards ?? 0,
                        offsides = matchEntity.HomeTeamOffsides ?? 0
                    },
                    awayTeam = new
                    {
                        name = matchEntity.AwayTeam?.Name ?? matchEntity.AwayTeamInMatchName,
                        score = matchEntity.AwayTeamScore ?? 0,
                        shots = matchEntity.AwayTeamShots ?? 0,
                        shotsOnTarget = matchEntity.AwayTeamShotsOnTarget ?? 0,
                        possession = matchEntity.AwayTeamPossession ?? 0,
                        passes = matchEntity.AwayTeamPasses ?? 0,
                        passAccuracy = matchEntity.AwayTeamPassAccuracy ?? 0,
                        corners = matchEntity.AwayTeamCorners ?? 0,
                        fouls = matchEntity.AwayTeamFouls ?? 0,
                        yellowCards = matchEntity.AwayTeamYellowCards ?? 0,
                        redCards = matchEntity.AwayTeamRedCards ?? 0,
                        offsides = matchEntity.AwayTeamOffsides ?? 0
                    },
                    matchInfo = new
                    {
                        status = matchEntity.MatchStatus,
                        isLive = matchEntity.IsLive,
                        currentMinute = matchEvent.minute,
                        lastEventTime = matchEvent.time_seconds,
                        eventType = matchEvent.action,
                        eventTeam = matchEvent.team
                    },
                    lastUpdated = DateTime.UtcNow
                };

                // Broadcast to all clients in the match group
                await hubContext.Clients.Group(matchEntity.Id.ToString())
                    .SendMatchStatisticsAsync("match_statistics_update", matchEntity.Id, matchStatistics);

                logger.LogDebug("Broadcasted match statistics for match {MatchId} after event {EventIndex}",
                    matchEntity.Id, matchEvent.event_index);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting match statistics for match {MatchId}", matchEntity.Id);
            }
        }

        private Task CacheMatchEvent(FootballMatchEvent matchEvent)
        {
            try
            {
                var matchId = matchEvent.match_id;

                // Add to in-memory cache
                lock (_matchEventsCache)
                {
                    if (!_matchEventsCache.TryGetValue(matchId, out var value))
                    {
                        value = [];
                        _matchEventsCache[matchId] = value;
                        logger.LogDebug("Created new event cache for match {MatchId}", matchId);
                    }

                    value?.Add(matchEvent);

                    // Update the event sequence for tracking
                    _eventSequence = Math.Max(_eventSequence, matchEvent.event_index + 1);
                }

                logger.LogDebug("Cached match event {EventIndex} for match {MatchId}",
                    matchEvent.event_index, matchEvent.match_id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error caching match event {EventIndex} for match {MatchId}",
                    matchEvent.event_index, matchEvent.match_id);
            }

            return Task.CompletedTask;
        }

        private async Task SaveMatchEventsToDatabase(string matchId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                List<FootballMatchEvent>? events;
                Match? cachedMatch = null;

                // Get events from a cache
                lock (_matchEventsCache)
                {
                    if (!_matchEventsCache.Remove(matchId, out events))
                    {
                        logger.LogWarning("No cached events found for match {MatchId}", matchId);
                        return;
                    }
                }

                // Get cached match entity if available
                lock (_matchCacheLock)
                {
                    _loadedMatches.TryGetValue(matchId, out cachedMatch);
                }

                using var scope = serviceScopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();
                Match match;
                if (cachedMatch != null)
                {
                    // Use cached match entity - it's already being tracked
                    match = cachedMatch;
                    logger.LogDebug("Using cached match entity for saving events for match {MatchId}", matchId);
                }
                else
                {
                    // Fallback to loading from a database if not cached
                    var dbStopwatch = Stopwatch.StartNew();
                    var loadedMatch = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
                    dbStopwatch.Stop();
                    performanceMonitoringService.RecordDatabaseCall("GetMatchWithDetails",
                        dbStopwatch.Elapsed.TotalMilliseconds);

                    if (loadedMatch == null)
                    {
                        logger.LogWarning("Match with ID {MatchId} not found when trying to save events.", matchId);
                        return;
                    }

                    match = loadedMatch;
                }

                var matchEventsEntity = match.MatchEvents;
                if (matchEventsEntity == null)
                {
                    // Create a new match events entity
                    matchEventsEntity = new MatchEvents
                    {
                        MatchId = int.Parse(matchId),
                        EventsJson = "[]", // Initialize with an empty JSON array
                        LastUpdated = DateTime.UtcNow,
                        TotalEvents = 0
                    };
                    await unitOfWork.MatchEvents.AddAsync(matchEventsEntity);
                    match.MatchEvents = matchEventsEntity; // Associate it with the match
                }

                // Process events if we have any
                if (events is { Count: > 0 })
                {
                    // If we didn't use cached match, process events normally
                    if (cachedMatch == null)
                    {
                        foreach (var matchEvent in events)
                        {
                            await eventAnalysis.UpdateMatchStatistics(matchEvent, matchEventsEntity, match);
                        }
                    }
                    // If we used cached match, the statistics are already updated, save the events

                    matchEventsEntity.SetEvents(events);
                    matchEventsEntity.TotalEvents = events.Count;
                    matchEventsEntity.LastUpdated = DateTime.UtcNow;

                    // Final match calculations (these are already done in cached match)
                    if (cachedMatch == null)
                    {
                        match.HomeTeamPassAccuracy = match.HomeTeamPassesCompleted / match.HomeTeamPasses;
                        match.AwayTeamPassAccuracy = match.AwayTeamPassesCompleted / match.AwayTeamPasses;
                        match.HomeTeamLongBallsAccuracy = match.HomeAccurateLongBalls / match.HomeLongBalls;
                        match.AwayTeamLongBallsAccuracy = match.AwayAccurateLongBalls / match.AwayLongBalls;
                        match.LastEventTimestampSeconds = events.Max(e => e.time_seconds);
                        match.IsLive = false;
                        match.MatchStatus = "Completed";
                    }

                    // Record database save operation
                    var saveStopwatch = Stopwatch.StartNew();
                    await unitOfWork.SaveChangesAsync();
                    saveStopwatch.Stop();
                    performanceMonitoringService.RecordDatabaseCall("SaveMatchEvents",
                        saveStopwatch.Elapsed.TotalMilliseconds);

                    logger.LogInformation(
                        "Saved {Count} events for match {MatchId} to database in {Duration}ms (using {Source})",
                        events.Count, matchId, stopwatch.Elapsed.TotalMilliseconds,
                        cachedMatch != null ? "cached entity" : "database entity");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving match events for match {MatchId} to database", matchId);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private void FlushEventsToDatabase(object? state)
        {
            // Safe copy of keys to avoid modification during iteration
            List<string> matchIds;
            lock (_matchEventsCache)
            {
                matchIds = _matchEventsCache.Keys.ToList();
            }

            foreach (var matchId in matchIds)
            {
                // Use Task.Run to avoid blocking the timer thread
                Task.Run(() => SaveMatchEventsToDatabase(matchId))
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            logger.LogError(t.Exception, "Failed to flush events for match {MatchId}", matchId);
                        }
                    });
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _flushTimer?.Change(Timeout.Infinite, 0);

            // Flush any remaining events when shutting down
            List<string> matchIds;
            lock (_matchEventsCache)
            {
                matchIds = _matchEventsCache.Keys.ToList();
            }

            foreach (var matchId in matchIds)
            {
                await SaveMatchEventsToDatabase(matchId);
            }

            // Clear the match cache
            lock (_matchCacheLock)
            {
                _loadedMatches.Clear();
                logger.LogInformation("Cleared match entity cache during shutdown");
            }

            await base.StopAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _flushTimer?.Dispose();
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }
}