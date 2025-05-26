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


namespace Infrastructure.Services
{
    public class MatchEventRabbitMqClient(
        ILogger<MatchEventRabbitMqClient> logger,
        IHubContext<MatchHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService)
        : BackgroundService
    {
        private int _eventSequence;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly Dictionary<string, List<FootballMatchEvent>?> _matchEventsCache = new();
        
        // Timer for periodic flushing of events to database as a safety mechanism
        private Timer? _flushTimer;

        private async Task InitializeRabbitMq(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = RabbitMqSettings.HostName,
                    UserName = RabbitMqSettings.UserName,
                    Password = RabbitMqSettings.Password,
                    VirtualHost = RabbitMqSettings.VirtualHost,
                    Port = RabbitMqSettings.Port,
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
                    queue: RabbitMqSettings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: arguments!, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(
                    queue: RabbitMqSettings.QueueName,
                    exchange: "match_events",
                    routingKey: "match.events", cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(0, 1, false, cancellationToken: stoppingToken);
                logger.LogInformation(
                    "RabbitMQ connected to {SettingsHostName}:{SettingsPort}/{SettingsVirtualHost}",
                    RabbitMqSettings.HostName,
                    RabbitMqSettings.Port, RabbitMqSettings.VirtualHost);
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
                        await CacheMatchEvent(matchEvent);
                        // If this is a match_end event, save all cached events to the database
                        if (matchEvent is { event_type: "match_end", action: "match_end" })
                        {
                            await SaveMatchEventsToDatabase(matchEvent.match_id);
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
                queue: RabbitMqSettings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            return Task.CompletedTask;
        }

        private async Task BroadcastEventToClients(FootballMatchEvent matchEvent)
        {
            await hubContext.Clients.Group(matchEvent.match_id)
                .SendAsync("ReceiveMatchEvent", matchEvent);

            logger.LogInformation("Broadcasted match event {FootballMatchEvent} to clients", matchEvent);
        }

        private Task CacheMatchEvent(FootballMatchEvent matchEvent)
        {
            try
            {
                var matchId = matchEvent.match_id;
                var isFirstEvent = false;

                // Add to in-memory cache
                lock (_matchEventsCache)
                {
                    if (!_matchEventsCache.TryGetValue(matchId, out var value))
                    {
                        value = [];
                        _matchEventsCache[matchId] = value;
                        isFirstEvent = true;
                    }

                    value?.Add(matchEvent);

                    // Update the event sequence for tracking
                    _eventSequence = Math.Max(_eventSequence, matchEvent.event_index + 1);
                }

                // Preload match for live statistics when first event is received
                if (isFirstEvent)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = serviceScopeFactory.CreateScope();
                            var liveMatchService =
                                scope.ServiceProvider.GetRequiredService<ILiveMatchStatisticsService>();
                            await liveMatchService.PreloadMatchForLiveStatistics(matchId);
                            logger.LogInformation("Auto-preloaded match {MatchId} for live statistics on first event",
                                matchId);
                        }
                        catch (Exception preloadEx)
                        {
                            logger.LogWarning(preloadEx, "Failed to auto-preload match {MatchId} for live statistics",
                                matchId);
                        }
                    });
                }

                logger.LogInformation("Cached match event {EventIndex} for match {MatchId}",
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

                // Get events from cache
                lock (_matchEventsCache)
                {
                    if (!_matchEventsCache.Remove(matchId, out events))
                    {
                        logger.LogWarning("No cached events found for match {MatchId}", matchId);
                        return;
                    }
                }

                using var scope = serviceScopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();
                var liveMatchService = scope.ServiceProvider.GetRequiredService<ILiveMatchStatisticsService>();

                // Record database call for performance monitoring
                var dbStopwatch = Stopwatch.StartNew();

                // Fetch the Match object once, including details needed for analysis
                var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));

                dbStopwatch.Stop();
                performanceMonitoringService.RecordDatabaseCall("GetMatchWithDetails",
                    dbStopwatch.Elapsed.TotalMilliseconds);

                if (match == null)
                {
                    logger.LogWarning("Match with ID {MatchId} not found when trying to save events.", matchId);
                    return;
                }

                var matchEventsEntity = match.MatchEvents; // Assuming GetByIdWithDetailsAsync includes MatchEvents
                if (matchEventsEntity == null)
                {
                    // Create new match events entity
                    matchEventsEntity = new MatchEvents
                    {
                        MatchId = int.Parse(matchId),
                        EventsJson = "[]", // Initialize with empty JSON array
                        LastUpdated = DateTime.UtcNow,
                        TotalEvents = 0
                    };
                    await unitOfWork.MatchEvents.AddAsync(matchEventsEntity);
                    match.MatchEvents = matchEventsEntity; // Associate it with the match
                }

                // Process each event for match statistics update
                if (events != null)
                {
                    foreach (var matchEvent in events)
                    {
                        await eventAnalysis.UpdateMatchStatistics(matchEvent, matchEventsEntity, match);
                    } // Save all events at once

                    matchEventsEntity.SetEvents(events);
                    matchEventsEntity.TotalEvents = events.Count;
                    matchEventsEntity.LastUpdated = DateTime.UtcNow;
                    if (match != null)
                    {
                        match.HomeTeamPassAccuracy = match.HomeTeamPassesCompleted / match.HomeTeamPasses;
                        match.AwayTeamPassAccuracy = match.AwayTeamPassesCompleted / match.AwayTeamPasses;
                        match.HomeTeamLongBallsAccuracy = match.HomeAccurateLongBalls / match.HomeLongBalls;
                        match.AwayTeamLongBallsAccuracy = match.AwayAccurateLongBalls / match.AwayLongBalls;
                        match.LastEventTimestampSeconds = events.Max(e => e.time_seconds);
                        match.IsLive = false;
                        match.MatchStatus = "Completed";

                        // Update cached match in LiveMatchStatisticsService for real-time access
                        try
                        {
                            if (liveMatchService is LiveMatchStatisticsService liveService)
                            {
                                liveService.UpdateCachedMatch(matchId, match);
                                logger.LogDebug("Updated cached match {MatchId} in LiveMatchStatisticsService",
                                    matchId);
                            }
                        }
                        catch (Exception cacheEx)
                        {
                            logger.LogWarning(cacheEx,
                                "Failed to update cached match {MatchId} in LiveMatchStatisticsService", matchId);
                        }
                    }

                    // Record database save operation
                    var saveStopwatch = Stopwatch.StartNew();
                    await unitOfWork.SaveChangesAsync();
                    saveStopwatch.Stop();
                    performanceMonitoringService.RecordDatabaseCall("SaveMatchEvents",
                        saveStopwatch.Elapsed.TotalMilliseconds);

                    logger.LogInformation("Saved {Count} events for match {MatchId} to database in {Duration}ms",
                        events.Count, matchId, stopwatch.Elapsed.TotalMilliseconds);
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


    public abstract class RabbitMqSettings
    {
        public static string HostName => "localhost";
        public static string UserName => "guest";
        public static string Password => "guest";
        public static string VirtualHost => "/";
        public static int Port => 5672;
        public static string QueueName => "match_events_queue";
    }
}