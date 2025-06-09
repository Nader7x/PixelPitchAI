using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
// Assuming IEventAnalysisService is here

// Assuming your RabbitMqOptions is here

namespace Infrastructure.Services;

public class MatchEventRabbitMqClient : BackgroundService
{
    private readonly IHubContext<MatchHub, IMatchHub>
        _hubContext; // Assuming IMatchHub defines SendMatchEventAsync and SendMatchStatisticsAsync

    private readonly Dictionary<string, Match> _loadedMatches = new();
    private readonly ILogger<MatchEventRabbitMqClient> _logger;
    private readonly object _matchCacheLock = new();

    private readonly Dictionary<string, List<FootballMatchEvent>?> _matchEventsCache = new();
    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly RabbitMqOptions _rabbitMqSettings;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IChannel? _channel;

    private IConnection? _connection;
    private string? _consumerTag;
    private int _eventSequence;
    private Timer? _flushTimer;

    public MatchEventRabbitMqClient(
        ILogger<MatchEventRabbitMqClient> logger,
        IHubContext<MatchHub, IMatchHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _logger = logger;
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _performanceMonitoringService = performanceMonitoringService;
        _rabbitMqSettings = rabbitMqOptions.Value;
    }

    private async Task<bool> TryInitializeRabbitMq(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqSettings.HostName,
            UserName = _rabbitMqSettings.UserName,
            Password = _rabbitMqSettings.Password,
            VirtualHost = _rabbitMqSettings.VirtualHost,
            Port = _rabbitMqSettings.Port,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            ContinuationTimeout = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromMilliseconds(15000)
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}:{Port}/{VHost}...",
                    _rabbitMqSettings.HostName, _rabbitMqSettings.Port, _rabbitMqSettings.VirtualHost);

                await CloseChannelAndConnectionAsync(); // Clean up previous attempts

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _logger.LogInformation("Connection to RabbitMQ established.");

                // Assigning async Task handlers to synchronous events.
                // The compiler handles this, effectively making them async void calls from the event source's perspective.
                _connection.ConnectionShutdownAsync += async (s, e) => await OnConnectionShutdownAsync(s, e);
                _connection.CallbackExceptionAsync += async (s, e) => await OnCallbackExceptionAsync(s, e);
                _connection.ConnectionRecoveryErrorAsync +=
                    async (s, e) => await OnConnectionRecoveryErrorAsync(s, e);
                _connection.RecoverySucceededAsync += async (s, e) => await OnRecoverySucceededAsync(s, e);


                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogError("Failed to create or open a channel after connecting to RabbitMQ.");
                    await CloseConnectionAsync();
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

                _channel.CallbackExceptionAsync += async (s, e) => await OnChannelCallbackExceptionAsync(s, e);
                _logger.LogInformation("Channel created successfully.");

                var arguments = new Dictionary<string, object> { { "x-message-ttl", 86400000 } }; // 24 hours

                await _channel.ExchangeDeclareAsync("match_events", ExchangeType.Topic,
                    true, false, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(_rabbitMqSettings.QueueName, true,
                    false, false, arguments, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(_rabbitMqSettings.QueueName, "match_events",
                    "match.events", cancellationToken: stoppingToken);
                await _channel.BasicQosAsync(0, 1, false, stoppingToken);

                _logger.LogInformation(
                    "Successfully connected to RabbitMQ and declared topology at {Host}:{Port}/{VHost}",
                    _rabbitMqSettings.HostName, _rabbitMqSettings.Port, _rabbitMqSettings.VirtualHost);
                return true;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex, "RabbitMQ broker unreachable. Retrying in 15 seconds...");
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Socket error connecting to RabbitMQ. Retrying in 15 seconds...");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RabbitMQ initialization cancelled as service is stopping.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during RabbitMQ initialization. Retrying in 15 seconds...");
            }
            finally
            {
                if (_connection == null || !_connection.IsOpen)
                    // If connection setup failed partway or connection is lost, ensure cleanup
                    await CloseChannelAndConnectionAsync();
            }

            if (!stoppingToken.IsCancellationRequested) await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        return false;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MatchEventRabbitMqClient starting...");
        if (await TryInitializeRabbitMq(cancellationToken))
        {
            _flushTimer = new Timer(FlushEventsToDatabase, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            await base.StartAsync(cancellationToken);
            _logger.LogInformation(
                "MatchEventRabbitMqClient started successfully and RabbitMQ connection established.");
        }
        else
        {
            _logger.LogError(
                "MatchEventRabbitMqClient failed to initialize RabbitMQ. Service will not process events.");
        }
    }

    private async Task HandleReceivedMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received match event: {Message}", message);

            var matchEvent = JsonSerializer.Deserialize<FootballMatchEvent>(message);
            if (matchEvent != null)
            {
                var matchEntity = await GetOrLoadMatchEntity(matchEvent.match_id);
                if (matchEntity != null) await ProcessMatchEventWithEntity(matchEvent, matchEntity);

                await CacheMatchEvent(matchEvent);
                if (matchEvent is { event_type: "match_end", action: "match_end" })
                {
                    await SaveMatchEventsToDatabase(matchEvent.match_id);
                    lock (_matchCacheLock)
                    {
                        _loadedMatches.Remove(matchEvent.match_id);
                        _logger.LogInformation("Removed match {MatchId} from cache after match end",
                            matchEvent.match_id);
                    }
                }

                // Add 1-second delay to simulate realistic match event synchronization time
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                
                await BroadcastEventToClients(matchEvent);
            }

            if (_channel != null && _channel.IsOpen)
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            else
                _logger.LogWarning(
                    "Channel closed or null before acknowledgment of delivery tag {DeliveryTag}. Message might be redelivered.",
                    ea.DeliveryTag);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx,
                "Failed to deserialize RabbitMQ message. Delivery Tag: {DeliveryTag}. NACKing without requeue.",
                ea.DeliveryTag);
            if (_channel != null && _channel.IsOpen) await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
        catch (AlreadyClosedException closedEx)
        {
            _logger.LogWarning(closedEx,
                "Attempted to operate on a closed channel/connection while processing message for delivery tag {DeliveryTag}. Message may be redelivered.",
                ea.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing received RabbitMQ message. Delivery Tag: {DeliveryTag}. NACKing without requeue.",
                ea.DeliveryTag);
            if (_channel != null && _channel.IsOpen)
                try
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx,
                        "Error sending NACK for delivery tag {DeliveryTag} after processing error.",
                        ea.DeliveryTag);
                }
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ channel unavailable at ExecuteAsync. Client will not consume events.");
            return;
        }

        _logger.LogInformation("MatchEventRabbitMqClient ExecuteAsync started. Consuming from queue: {QueueName}",
            _rabbitMqSettings.QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleReceivedMessageAsync(ea, stoppingToken);

        try
        {
            if (_channel != null && _channel.IsOpen)
            {
                _consumerTag = await _channel.BasicConsumeAsync(_rabbitMqSettings.QueueName, false,
                    consumer);
                _logger.LogInformation("Consumer attached with tag: {ConsumerTag}. Waiting for messages.",
                    _consumerTag);
            }
            else
            {
                _logger.LogWarning("Cannot start consumer; channel unavailable in ExecuteAsync.");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("Channel found closed in ExecuteAsync loop. Attempting re-initialization.");
                if (!await TryInitializeRabbitMq(stoppingToken))
                {
                    _logger.LogError(
                        "Failed to re-initialize RabbitMQ in ExecuteAsync. Stopping event processing.");
                    break;
                }

                if (_channel != null && _channel.IsOpen) // Check if channel is now open
                {
                    _logger.LogInformation("Re-initialized RabbitMQ. Re-attaching consumer.");
                    // Re-create consumer with the new channel
                    consumer = new AsyncEventingBasicConsumer(_channel); // Assign to the outer scope variable
                    consumer.ReceivedAsync += async (_, ea) => await HandleReceivedMessageAsync(ea, stoppingToken);
                    try
                    {
                        _consumerTag = await _channel.BasicConsumeAsync(_rabbitMqSettings.QueueName,
                            false, consumer);
                        _logger.LogInformation("Consumer re-attached with tag: {ConsumerTag}.", _consumerTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to re-attach consumer after re-initialization.");
                        break; // Critical failure
                    }
                }
                else
                {
                    _logger.LogError("Channel still not open after re-initialization attempt. Stopping.");
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("ExecuteAsync stopping.");
    }

    // --- RabbitMQ Event Handlers (async Task versions) ---
    private Task OnRecoverySucceededAsync(object? sender, AsyncEventArgs e)
    {
        _logger.LogInformation("RabbitMQ connection recovered successfully.");
        return Task.CompletedTask;
    }

    private Task OnConnectionRecoveryErrorAsync(object? sender, ConnectionRecoveryErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ connection recovery failed.");
        return Task.CompletedTask;
    }

    private Task OnCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ connection callback exception: {Detail}", e.Detail);
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning(
            "RabbitMQ connection shut down. Initiator: {Initiator}, Cause: {Cause}, Reply Code: {ReplyCode}, Reply Text: {ReplyText}",
            e.Initiator, e.Cause?.ToString() ?? "N/A", e.ReplyCode, e.ReplyText);
        return Task.CompletedTask;
    }

    private Task OnChannelCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception,
            "RabbitMQ channel callback exception. Channel: {ChannelNum}, Detail: {Detail}",
            (sender as IChannel)?.ChannelNumber, e.Detail);
        return Task.CompletedTask;
    }


    // --- Helper methods for closing resources ---
    private async Task CloseChannelAsync()
    {
        if (_channel != null)
        {
            if (_channel.IsOpen)
            {
                _channel.CallbackExceptionAsync -= async (s, e) => await OnChannelCallbackExceptionAsync(s, e);
                if (!string.IsNullOrEmpty(_consumerTag))
                {
                    try
                    {
                        await _channel.BasicCancelAsync(_consumerTag);
                        _logger.LogInformation("Consumer '{Tag}' cancelled.", _consumerTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Exception cancelling consumer '{Tag}'.", _consumerTag);
                    }

                    _consumerTag = null;
                }

                try
                {
                    await _channel.CloseAsync();
                    _logger.LogInformation("RabbitMQ channel closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception during channel close.");
                }
            }

            _channel.Dispose();
            _channel = null;
        }
    }

    private async Task CloseConnectionAsync()
    {
        if (_connection != null)
        {
            // Unsubscribe from events using the same lambda pattern
            _connection.ConnectionShutdownAsync -= async (s, e) => await OnConnectionShutdownAsync(s, e);
            _connection.CallbackExceptionAsync -= async (s, e) => await OnCallbackExceptionAsync(s, e);
            _connection.ConnectionRecoveryErrorAsync -= async (s, e) => await OnConnectionRecoveryErrorAsync(s, e);
            _connection.RecoverySucceededAsync -= async (s, e) => await OnRecoverySucceededAsync(s, e);


            if (_connection.IsOpen)
                try
                {
                    await _connection.CloseAsync();
                    _logger.LogInformation("RabbitMQ connection closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception during connection close.");
                }

            _connection.Dispose();
            _connection = null;
        }
    }

    private async Task CloseChannelAndConnectionAsync()
    {
        await CloseChannelAsync();
        await CloseConnectionAsync();
    }

    private async Task BroadcastEventToClients(FootballMatchEvent matchEvent)
    {
        try
        {
            await _hubContext.Clients.Group(matchEvent.match_id)
                .SendMatchEventAsync("match_event", int.Parse(matchEvent.match_id),
                    matchEvent); // Ensure IMatchHub has SendMatchEventAsync
            _logger.LogInformation("Broadcasted match event {Index} for match {Id} to clients",
                matchEvent.event_index, matchEvent.match_id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting event for match {Id}", matchEvent.match_id);
        }
    }

    private async Task<Match?> GetOrLoadMatchEntity(string matchId)
    {
        lock (_matchCacheLock)
        {
            if (_loadedMatches.TryGetValue(matchId, out var cachedMatch)) return cachedMatch;
        }

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stopwatch = Stopwatch.StartNew();
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
            stopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall("LoadMatchEntity",
                stopwatch.Elapsed.TotalMilliseconds);
            if (match != null)
            {
                lock (_matchCacheLock)
                {
                    _loadedMatches[matchId] = match;
                }

                return match;
            }

            _logger.LogWarning("Match with ID {Id} not found", matchId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading match entity for match {Id}", matchId);
            return null;
        }
    }

    private async Task ProcessMatchEventWithEntity(FootballMatchEvent matchEvent, Match matchEntity)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();
            var matchEventsEntity = matchEntity.MatchEvents ?? new MatchEvents
            {
                MatchId = int.Parse(matchEvent.match_id), EventsJson = "[]", LastUpdated = DateTime.UtcNow,
                TotalEvents = 0
            };
            if (matchEntity.MatchEvents == null) matchEntity.MatchEvents = matchEventsEntity;
            await eventAnalysis.UpdateMatchStatistics(matchEvent, matchEventsEntity, matchEntity,
                false); // Assuming this method exists
            await BroadcastMatchStatistics(matchEvent, matchEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing match event {Index} for match {Id}", matchEvent.event_index,
                matchEvent.match_id);
        }
    }

    private async Task BroadcastMatchStatistics(FootballMatchEvent matchEvent, Match matchEntity)
    {
        try
        {
            // Restoring the detailed statistics object as per the earlier version
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
            await _hubContext.Clients.Group(matchEntity.Id.ToString())
                .SendMatchStatisticsAsync("match_statistics_update", matchEntity.Id,
                    matchStatistics); // Ensure IMatchHub has SendMatchStatisticsAsync
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting match stats for match {Id}", matchEntity.Id);
        }
    }

    private Task CacheMatchEvent(FootballMatchEvent matchEvent)
    {
        try
        {
            lock (_matchEventsCache)
            {
                if (!_matchEventsCache.TryGetValue(matchEvent.match_id, out var eventsList))
                {
                    eventsList = [];
                    _matchEventsCache[matchEvent.match_id] = eventsList;
                }

                eventsList?.Add(matchEvent);
                _eventSequence = Math.Max(_eventSequence, matchEvent.event_index + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching event {Index} for match {Id}", matchEvent.event_index,
                matchEvent.match_id);
        }

        return Task.CompletedTask;
    }

    private async Task SaveMatchEventsToDatabase(string matchId)
    {
        // Restoring the more complete logic from the user's earlier provided file snippet
        var stopwatch = Stopwatch.StartNew();
        try
        {
            List<FootballMatchEvent>? events;
            Match? cachedMatch = null;

            lock (_matchEventsCache)
            {
                if (!_matchEventsCache.Remove(matchId, out events) || events == null || !events.Any())
                {
                    _logger.LogInformation(
                        "No cached events to save for match {MatchId}, or cache already cleared.", matchId);
                    return;
                }
            }

            _logger.LogInformation("Attempting to save {EventCount} events for match {MatchId}", events.Count,
                matchId);


            lock (_matchCacheLock)
            {
                _loadedMatches.TryGetValue(matchId, out cachedMatch);
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();

            Match matchToUpdate;
            if (cachedMatch != null)
            {
                matchToUpdate = cachedMatch; // Already tracked if loaded via GetOrLoadMatchEntity
                _logger.LogDebug("Using cached match entity for saving events for match {MatchId}", matchId);
            }
            else
            {
                var dbStopwatch = Stopwatch.StartNew();
                matchToUpdate = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
                dbStopwatch.Stop();
                _performanceMonitoringService.RecordDatabaseCall("GetMatchWithDetails_SaveEvents",
                    dbStopwatch.Elapsed.TotalMilliseconds);
                if (matchToUpdate == null)
                {
                    _logger.LogWarning("Match {Id} not found for saving events.", matchId);
                    return;
                }
            }

            var matchEventsEntity = matchToUpdate.MatchEvents;
            if (matchEventsEntity == null)
            {
                matchEventsEntity = new MatchEvents
                {
                    MatchId = int.Parse(matchId), EventsJson = "[]", LastUpdated = DateTime.UtcNow, TotalEvents = 0
                };
                await unitOfWork.MatchEvents.AddAsync(matchEventsEntity); // This adds to UoW context
                matchToUpdate.MatchEvents = matchEventsEntity;
            }

            if (events.Any(e => e.event_type == "match_end" && e.action == "match_end"))
            {
                if (cachedMatch == null)
                    foreach (var ev in events.OrderBy(e => e.time_seconds))
                        await eventAnalysis.UpdateMatchStatistics(ev, matchEventsEntity, matchToUpdate);

                matchToUpdate.IsLive = false;
                matchToUpdate.MatchStatus = "Completed";
            }


            matchEventsEntity.SetEvents(events);
            matchEventsEntity.TotalEvents = events.Count;
            matchEventsEntity.LastUpdated = DateTime.UtcNow;

            var saveStopwatch = Stopwatch.StartNew();
            await unitOfWork.SaveChangesAsync();
            saveStopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall("SaveChanges_MatchEvents",
                saveStopwatch.Elapsed.TotalMilliseconds);

            _logger.LogInformation("Saved {Count} events for match {Id} to DB in {Ms}ms (Source: {Src})",
                events.Count, matchId, stopwatch.Elapsed.TotalMilliseconds,
                cachedMatch != null ? "cached" : "db_loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving events for match {Id}", matchId);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private void FlushEventsToDatabase(object? state)
    {
        List<string> matchIds;
        lock (_matchEventsCache)
        {
            matchIds = new List<string>(_matchEventsCache.Keys);
        }

        if (!matchIds.Any()) return;

        _logger.LogInformation("FlushTimer: Checking {Count} matches for event flushing.", matchIds.Count);
        foreach (var matchId in matchIds)
            Task.Run(async () =>
            {
                try
                {
                    await SaveMatchEventsToDatabase(matchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Async flush failed for match {Id}", matchId);
                }
            });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MatchEventRabbitMqClient StopAsync called.");
        _flushTimer?.Change(Timeout.Infinite, 0);

        List<string> matchIdsToFlush;
        lock (_matchEventsCache)
        {
            matchIdsToFlush = new List<string>(_matchEventsCache.Keys);
        }

        _logger.LogInformation("Flushing events for {Count} matches during shutdown.", matchIdsToFlush.Count);
        foreach (var matchId in matchIdsToFlush) await SaveMatchEventsToDatabase(matchId);

        lock (_matchCacheLock)
        {
            _loadedMatches.Clear();
            _logger.LogInformation("Cleared match entity cache.");
        }

        await CloseChannelAndConnectionAsync();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("MatchEventRabbitMqClient stopped.");
    }

    public override void Dispose()
    {
        _logger.LogInformation("MatchEventRabbitMqClient disposing.");
        _flushTimer?.Dispose();
        _flushTimer = null;

        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;

        base.Dispose();
        GC.SuppressFinalize(this);
        _logger.LogInformation("MatchEventRabbitMqClient disposed.");
    }
}