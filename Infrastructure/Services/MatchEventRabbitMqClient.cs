using System.Collections.Concurrent;
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

namespace Infrastructure.Services;

public class MatchEventRabbitMqClient : BackgroundService
{
    private readonly AsyncEventHandler<CallbackExceptionEventArgs> _callbackExceptionHandler;
    private readonly AsyncEventHandler<CallbackExceptionEventArgs> _channelCallbackExceptionHandler;
    private readonly AsyncEventHandler<ConnectionRecoveryErrorEventArgs> _connectionRecoveryErrorHandler;
    private readonly AsyncEventHandler<ShutdownEventArgs> _connectionShutdownHandler;
    private readonly IHubContext<MatchHub, IMatchHub> _hubContext;
    private readonly IConnectionFactory? _injectedConnectionFactory;
    private readonly ILiveMatchStatisticsService _liveMatchStatisticsService;
    private readonly ConcurrentDictionary<string, Match> _loadedMatches = new();
    private readonly ILogger<MatchEventRabbitMqClient> _logger;

    private readonly ConcurrentDictionary<string, List<FootballMatchEvent>?> _matchEventsCache =
        new();

    private readonly object _matchEventsCacheLock = new();

    private readonly IPerformanceMonitoringService _performanceMonitoringService;
    private readonly RabbitMqOptions _rabbitMqSettings;
    private readonly AsyncEventHandler<AsyncEventArgs> _recoverySucceededHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private IChannel? _channel;

    private IConnection? _connection;
    private string? _consumerTag;
    private int _eventSequence;

    public MatchEventRabbitMqClient(
        ILogger<MatchEventRabbitMqClient> logger,
        IHubContext<MatchHub, IMatchHub> hubContext,
        IServiceScopeFactory serviceScopeFactory,
        IPerformanceMonitoringService performanceMonitoringService,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILiveMatchStatisticsService liveMatchStatisticsService,
        IConnectionFactory? injectedConnectionFactory = null
    )
    {
        _logger = logger;
        _hubContext = hubContext;
        _serviceScopeFactory = serviceScopeFactory;
        _performanceMonitoringService = performanceMonitoringService;
        _liveMatchStatisticsService = liveMatchStatisticsService;
        _rabbitMqSettings = rabbitMqOptions.Value;
        _injectedConnectionFactory = injectedConnectionFactory;

        _connectionShutdownHandler = OnConnectionShutdownAsync;
        _callbackExceptionHandler = OnCallbackExceptionAsync;
        _connectionRecoveryErrorHandler = OnConnectionRecoveryErrorAsync;
        _recoverySucceededHandler = OnRecoverySucceededAsync;
        _channelCallbackExceptionHandler = OnChannelCallbackExceptionAsync;
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
            RequestedConnectionTimeout = TimeSpan.FromMilliseconds(15000),
        };

        var connectionFactory = _injectedConnectionFactory ?? factory;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Attempting to connect to RabbitMQ at {Host}:{Port}/{VHost}...",
                    _rabbitMqSettings.HostName,
                    _rabbitMqSettings.Port,
                    _rabbitMqSettings.VirtualHost
                );

                await CloseChannelAndConnectionAsync();
                _connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                _logger.LogInformation("Connection to RabbitMQ established.");

                if (_connection != null)
                {
                    _connection.ConnectionShutdownAsync += _connectionShutdownHandler;
                    _connection.CallbackExceptionAsync += _callbackExceptionHandler;
                    _connection.ConnectionRecoveryErrorAsync += _connectionRecoveryErrorHandler;
                    _connection.RecoverySucceededAsync += _recoverySucceededHandler;
                }

                if (_connection == null)
                {
                    _logger.LogError("Connection is null after creation attempt.");
                    continue;
                }

                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                if (_channel is not { IsOpen: true })
                {
                    _logger.LogError(
                        "Failed to create or open a channel after connecting to RabbitMQ."
                    );
                    await CloseConnectionAsync();
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                }

                _channel.CallbackExceptionAsync += _channelCallbackExceptionHandler;
                _logger.LogInformation("Channel created successfully.");

                var arguments = new Dictionary<string, object?> { { "x-message-ttl", 86400000 } }; // 24 hours

                await _channel.ExchangeDeclareAsync(
                    "match_events",
                    ExchangeType.Topic,
                    true,
                    false,
                    cancellationToken: stoppingToken
                );
                await _channel.QueueDeclareAsync(
                    _rabbitMqSettings.QueueName,
                    true,
                    false,
                    false,
                    arguments,
                    cancellationToken: stoppingToken
                );
                await _channel.QueueBindAsync(
                    _rabbitMqSettings.QueueName,
                    "match_events",
                    "match.events",
                    cancellationToken: stoppingToken
                );
                await _channel.BasicQosAsync(0, 1, false, stoppingToken);

                _logger.LogInformation(
                    "Successfully connected to RabbitMQ and declared topology at {Host}:{Port}/{VHost}",
                    _rabbitMqSettings.HostName,
                    _rabbitMqSettings.Port,
                    _rabbitMqSettings.VirtualHost
                );
                return true;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex, "RabbitMQ broker unreachable. Retrying in 15 seconds...");
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Socket error connecting to RabbitMQ. Retrying in 15 seconds..."
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RabbitMQ initialization cancelled as service is stopping.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during RabbitMQ initialization. Retrying in 15 seconds..."
                );
            }
            finally
            {
                if (_connection is not { IsOpen: true })
                    await CloseChannelAndConnectionAsync();
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        return false;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MatchEventRabbitMqClient starting...");
        if (await TryInitializeRabbitMq(cancellationToken))
        {
            await base.StartAsync(cancellationToken);
            _logger.LogInformation(
                "MatchEventRabbitMqClient started successfully and RabbitMQ connection established."
            );
        }
        else
        {
            _logger.LogError(
                "MatchEventRabbitMqClient failed to initialize RabbitMQ. Service will not process events."
            );
        }
    }

    private async Task HandleReceivedMessageAsync(
        BasicDeliverEventArgs ea,
        CancellationToken stoppingToken
    )
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received match event: {Message}", message);

            var matchEvent = JsonSerializer.Deserialize<FootballMatchEvent>(
                message,
                MatchEventJsonContext.Default.FootballMatchEvent
            );
            if (matchEvent?.match_id != null)
            {
                var matchEntity = await GetOrLoadMatchEntity(matchEvent.match_id);
                if (matchEntity != null)
                    await ProcessMatchEventWithEntity(matchEvent, matchEntity);
                await CacheMatchEvent(matchEvent);
                if (matchEvent is { event_type: "match_end", action: "match_end" })
                {
                    if (matchEntity != null)
                        matchEntity.IsLive = false;
                    await SaveMatchEventsToDatabase(matchEvent.match_id);
                    _loadedMatches.TryRemove(matchEvent.match_id, out _);
                    _logger.LogInformation(
                        "Removed match {MatchId} from cache after match end",
                        matchEvent.match_id
                    );
                }

                await BroadcastEventToClients(matchEvent);

                if (IsSignificantEvent(matchEvent))
                    if (matchEntity != null)
                    {
                        await BroadcastMatchStatistics(matchEvent, matchEntity);
                        await _liveMatchStatisticsService.AddMatchToLiveStatistics(matchEntity);
                    }
            }

            if (_channel is { IsOpen: true })
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            else
                _logger.LogWarning(
                    "Channel closed or null before acknowledgment of delivery tag {DeliveryTag}. Message might be redelivered.",
                    ea.DeliveryTag
                );
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(
                jsonEx,
                "Failed to deserialize RabbitMQ message. Delivery Tag: {DeliveryTag}. NACKing without requeue.",
                ea.DeliveryTag
            );
            if (_channel is { IsOpen: true })
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
        }
        catch (AlreadyClosedException closedEx)
        {
            _logger.LogWarning(
                closedEx,
                "Attempted to operate on a closed channel/connection while processing message for delivery tag {DeliveryTag}. Message may be redelivered.",
                ea.DeliveryTag
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing received RabbitMQ message. Delivery Tag: {DeliveryTag}. NACKing without requeue.",
                ea.DeliveryTag
            );
            if (_channel is { IsOpen: true })
                try
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(
                        nackEx,
                        "Error sending NACK for delivery tag {DeliveryTag} after processing error.",
                        ea.DeliveryTag
                    );
                }
        }
    }

    /// <summary>
    ///     Determines if a match event is significant enough to warrant broadcasting updated statistics.
    ///     Only significant events like goals, cards, substitutions, and period changes trigger statistics updates.
    /// </summary>
    /// <param name="matchEvent">The football match event to analyze</param>
    /// <returns>True if the event should trigger a statistics broadcast, false otherwise</returns>
    private static bool IsSignificantEvent(FootballMatchEvent? matchEvent)
    {
        if (matchEvent is null)
            return false;
        var action = matchEvent.action.ToLowerInvariant();
        var eventType = matchEvent.event_type?.ToLowerInvariant();
        var type = matchEvent.type?.ToLowerInvariant();

        if (type is "kick off" or "free kick" or "corner")
            return true;

        if (
            action.Contains("card")
            || action.Contains("yellow")
            || action.Contains("red")
            || eventType == "card"
            || eventType == "yellow_card"
            || eventType == "red_card"
        )
            return true;
        if (matchEvent is { long_pass: not null and true })
            return true;

        if (
            action.Contains("substitution")
            || action.Contains("sub")
            || eventType == "substitution"
        )
            return true;

        if (
            action
            is "match_start"
                or "match_end"
                or "first_half_end"
                or "second_half_start"
                or "stoppage_time_start"
        )
            return true;

        if (
            action.Contains("penalty")
            || eventType == "penalty"
            || matchEvent.outcome?.ToLowerInvariant() == "penalty"
        )
            return true;

        if (action.Contains("own") && action.Contains("goal"))
            return true;

        return eventType switch
        {
            "shot"
            or "duel"
            or "foul committed"
            or "foul won"
            or "dribble"
            or "interception"
            or "clearance"
            or "block"
            or "carry"
            or "ball_recovery" => true,
            _ => false,
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is not { IsOpen: true })
        {
            _logger.LogWarning(
                "RabbitMQ channel unavailable at ExecuteAsync. Client will not consume events."
            );
            return;
        }

        _logger.LogInformation(
            "MatchEventRabbitMqClient ExecuteAsync started. Consuming from queue: {QueueName} with 1-second intervals",
            _rabbitMqSettings.QueueName
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_channel is not { IsOpen: true })
            {
                _logger.LogWarning(
                    "Channel found closed in ExecuteAsync loop. Attempting re-initialization."
                );
                if (!await TryInitializeRabbitMq(stoppingToken))
                {
                    _logger.LogError(
                        "Failed to re-initialize RabbitMQ in ExecuteAsync. Stopping event processing."
                    );
                    break;
                }
            }

            if (_channel is { IsOpen: true })
                try
                {
                    var result = await _channel.BasicGetAsync(
                        _rabbitMqSettings.QueueName,
                        false,
                        stoppingToken
                    );
                    if (result != null)
                    {
                        var deliverEventArgs = new BasicDeliverEventArgs(
                            "",
                            result.DeliveryTag,
                            result.Redelivered,
                            result.Exchange,
                            result.RoutingKey,
                            result.BasicProperties,
                            result.Body,
                            stoppingToken
                        );

                        await HandleReceivedMessageAsync(deliverEventArgs, stoppingToken);

                        _logger.LogDebug(
                            "Processed message with delivery tag {DeliveryTag}",
                            result.DeliveryTag
                        );
                    }
                    else
                    {
                        _logger.LogDebug("No messages available in queue, waiting...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during manual message consumption");
                }

            await Task.Delay(TimeSpan.FromMilliseconds(700), stoppingToken);
        }

        _logger.LogInformation("ExecuteAsync stopping.");
    }

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
            e.Initiator,
            e.Cause?.ToString() ?? "N/A",
            e.ReplyCode,
            e.ReplyText
        );
        return Task.CompletedTask;
    }

    private Task OnChannelCallbackExceptionAsync(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(
            e.Exception,
            "RabbitMQ channel callback exception. Channel: {ChannelNum}, Detail: {Detail}",
            (sender as IChannel)?.ChannelNumber,
            e.Detail
        );
        return Task.CompletedTask;
    }

    protected virtual async Task CloseChannelAsync()
    {
        var channelToClose = Interlocked.Exchange(ref _channel, null);        
        if (channelToClose is { IsOpen: true })
        {
            channelToClose.CallbackExceptionAsync -= _channelCallbackExceptionHandler;
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                try
                {
                    await channelToClose.BasicCancelAsync(_consumerTag);
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
                await channelToClose.CloseAsync();
                _logger.LogInformation("RabbitMQ channel closed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during channel close.");
            }

            channelToClose.Dispose();
            _channel = null;
        }
    }

    protected virtual async Task CloseConnectionAsync()
    {
        if (_connection is { IsOpen: true })
        {
            _connection.ConnectionShutdownAsync -= _connectionShutdownHandler;
            _connection.CallbackExceptionAsync -= _callbackExceptionHandler;
            _connection.ConnectionRecoveryErrorAsync -= _connectionRecoveryErrorHandler;
            _connection.RecoverySucceededAsync -= _recoverySucceededHandler;

            try
            {
                await _connection.CloseAsync();
                _logger.LogInformation("RabbitMQ connection closed.");
            }
            catch (AlreadyClosedException ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection already closed during explicit close attempt. This is expected during some shutdown scenarios.");
            }
            catch (ObjectDisposedException ex) // Catches if the object itself was already disposed
            {
                _logger.LogWarning(ex, "RabbitMQ connection object already disposed during explicit close attempt.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected exception during connection close.");
            }

            // After the try-catch, ensure _connection is robustly disposed and nulled
            if (_connection != null)
            {
                _connection.Dispose(); // This might still throw if it was already disposed by another mechanism
                _connection = null;
            }
        }

        _connection = null;
    }

    protected virtual async Task CloseChannelAndConnectionAsync()
    {
        if (_channel is null || _connection is null)
        {
            _logger.LogWarning("Attempted to close channel/connection, but they are already null.");
            return;
        }

        if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
        {
            await CloseChannelAsync();
            await CloseConnectionAsync();
            _logger.LogInformation("Closing RabbitMQ channel and connection gracefully.");
        }
        else
        {
            _logger.LogWarning(
                "Channel or connection already closed or null. Skipping close operations."
            );
        }
    }

    private async Task BroadcastEventToClients(FootballMatchEvent matchEvent)
    {
        try
        {
            if (matchEvent.match_id != null)
            {
                await _hubContext
                    .Clients.Group(matchEvent.match_id)
                    .SendMatchEventAsync("match_event", int.Parse(matchEvent.match_id), matchEvent);
                _logger.LogInformation(
                    "Broadcasted match event {Index} for match {Id} to clients",
                    matchEvent.event_index,
                    matchEvent.match_id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting event for match {Id}", matchEvent.match_id);
        }
    }

    private async Task<Match?> GetOrLoadMatchEntity(string matchId)
    {
        if (_loadedMatches.TryGetValue(matchId, out var cachedMatch))
            return cachedMatch;

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stopwatch = Stopwatch.StartNew();
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(int.Parse(matchId));
            stopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "LoadMatchEntity",
                stopwatch.Elapsed.TotalMilliseconds
            );
            if (match != null)
            {
                _loadedMatches.TryAdd(matchId, match);
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
            if (matchEvent.match_id != null)
            {
                var matchEventsEntity =
                    matchEntity.MatchEvents
                    ?? new MatchEvents
                    {
                        MatchId = int.Parse(matchEvent.match_id),
                        EventsJson = "[]",
                        LastUpdated = DateTime.UtcNow,
                        TotalEvents = 0,
                    };

                matchEntity.MatchEvents ??= matchEventsEntity;

                await eventAnalysis.UpdateMatchStatistics(
                    matchEvent,
                    matchEventsEntity,
                    matchEntity,
                    false
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing match event {Index} for match {Id}",
                matchEvent.event_index,
                matchEvent.match_id
            );
        }
    }

    private async Task BroadcastMatchStatistics(FootballMatchEvent matchEvent, Match matchEntity)
    {
        try
        {
            if (matchEntity.MatchStatistics != null)
            {
                var matchStatistics = new
                {
                    matchId = matchEntity.Id,
                    timeStamp = matchEvent.timestamp,
                    homeTeam = new
                    {
                        name = matchEntity.HomeTeam?.Name ?? matchEntity.HomeTeamInMatchName,
                        score = matchEntity.HomeTeamScore ?? 0,
                        shots = matchEntity.MatchStatistics.HomeTeamShots ?? 0,
                        shotsOnTarget = matchEntity.MatchStatistics.HomeTeamShotsOnTarget ?? 0,
                        possession = matchEntity.MatchStatistics.HomeTeamPossession ?? 0,
                        passes = matchEntity.MatchStatistics.HomeTeamPasses ?? 0,
                        passAccuracy = matchEntity.MatchStatistics.HomeTeamPassAccuracy ?? 0,
                        corners = matchEntity.MatchStatistics.HomeTeamCorners ?? 0,
                        fouls = matchEntity.MatchStatistics.HomeTeamFouls ?? 0,
                        yellowCards = matchEntity.MatchStatistics.HomeTeamYellowCards ?? 0,
                        redCards = matchEntity.MatchStatistics.HomeTeamRedCards ?? 0,
                        offsides = matchEntity.MatchStatistics.HomeTeamOffsides ?? 0,
                    },
                    awayTeam = new
                    {
                        name = matchEntity.AwayTeam?.Name ?? matchEntity.AwayTeamInMatchName,
                        score = matchEntity.AwayTeamScore ?? 0,
                        shots = matchEntity.MatchStatistics.AwayTeamShots ?? 0,
                        shotsOnTarget = matchEntity.MatchStatistics.AwayTeamShotsOnTarget ?? 0,
                        possession = matchEntity.MatchStatistics.AwayTeamPossession ?? 0,
                        passes = matchEntity.MatchStatistics.AwayTeamPasses ?? 0,
                        passAccuracy = matchEntity.MatchStatistics.AwayTeamPassAccuracy ?? 0,
                        corners = matchEntity.MatchStatistics.AwayTeamCorners ?? 0,
                        fouls = matchEntity.MatchStatistics.AwayTeamFouls ?? 0,
                        yellowCards = matchEntity.MatchStatistics.AwayTeamYellowCards ?? 0,
                        redCards = matchEntity.MatchStatistics.AwayTeamRedCards ?? 0,
                        offsides = matchEntity.MatchStatistics.AwayTeamOffsides ?? 0,
                    },
                    matchInfo = new
                    {
                        status = matchEntity.MatchStatus,
                        isLive = matchEntity.IsLive,
                        currentMinute = matchEvent.minute,
                        lastEventTime = matchEvent.time_seconds,
                        eventType = matchEvent.action,
                        eventTeam = matchEvent.team,
                    },
                    lastUpdated = DateTime.UtcNow,
                };
                await _hubContext
                    .Clients.Group($"MatchStatistics-{matchEntity.Id.ToString()}")
                    .SendMatchStatisticsAsync(
                        "match_statistics_update",
                        matchEntity.Id,
                        matchStatistics
                    );
                _logger.LogInformation(
                    "Broadcasted match statistics for match {Id} at {Time}",
                    matchEntity.Id,
                    DateTime.UtcNow
                );
                _logger.LogInformation(
                    "Match statistics: {Statistics}",
                    JsonSerializer.Serialize(matchStatistics)
                );
            }
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
            lock (_matchEventsCacheLock)
            {
                if (!_matchEventsCache.TryGetValue(matchEvent.match_id!, out var eventsList))
                {
                    eventsList = [];
                    if (matchEvent.match_id != null)
                        _matchEventsCache[matchEvent.match_id] = eventsList;
                }

                eventsList?.Add(matchEvent);
                _eventSequence = Math.Max(_eventSequence, matchEvent.event_index + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error caching event {Index} for match {Id}",
                matchEvent.event_index,
                matchEvent.match_id
            );
        }

        return Task.CompletedTask;
    }

    private async Task SaveMatchEventsToDatabase(string matchId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            List<FootballMatchEvent>? events;

            lock (_matchEventsCacheLock)
            {
                if (
                    !_matchEventsCache.Remove(matchId, out events)
                    || events == null
                    || events.Count == 0
                )
                {
                    _logger.LogInformation(
                        "No cached events to save for match {MatchId}, or cache already cleared.",
                        matchId
                    );
                    return;
                }
            }

            _logger.LogInformation(
                "Attempting to save {EventCount} events for match {MatchId}",
                events.Count,
                matchId
            );

            using var scope = _serviceScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var eventAnalysis = scope.ServiceProvider.GetRequiredService<IEventAnalysisService>();

            var dbStopwatch = Stopwatch.StartNew();
            var matchToUpdate = await unitOfWork.Matches.GetByIdWithDetailsAsync(
                int.Parse(matchId)
            );
            dbStopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "GetMatchWithDetails_SaveEvents",
                dbStopwatch.Elapsed.TotalMilliseconds
            );
            if (matchToUpdate == null)
            {
                _logger.LogWarning("Match {Id} not found for saving events.", matchId);
                return;
            }

            var matchEventsEntity = new MatchEvents
            {
                MatchId = int.Parse(matchId),
                EventsJson = "[]",
                LastUpdated = DateTime.UtcNow,
            };

            await unitOfWork.MatchEvents.AddAsync(matchEventsEntity);
            matchToUpdate.MatchEvents = matchEventsEntity;

            _logger.LogInformation(
                "Processing {EventCount} events for statistics updates for match {MatchId}",
                events.Count,
                matchId
            );
            foreach (var ev in events.OrderBy(e => e.time_seconds))
                await eventAnalysis.UpdateMatchStatistics(ev, matchEventsEntity, matchToUpdate);

            if (events.Any(e => e is { event_type: "match_end", action: "match_end" }))
            {
                matchToUpdate.IsLive = false;
                matchToUpdate.MatchStatus = "Completed";
                _logger.LogInformation(
                    "Match {MatchId} completed, updated status accordingly",
                    matchId
                );
            }

            matchEventsEntity.SetEvents(events);
            matchEventsEntity.TotalEvents = events.Count;
            matchEventsEntity.LastUpdated = DateTime.UtcNow;

            var saveStopwatch = Stopwatch.StartNew();
            await unitOfWork.SaveChangesAsync();
            saveStopwatch.Stop();
            _performanceMonitoringService.RecordDatabaseCall(
                "SaveChanges_MatchEvents",
                saveStopwatch.Elapsed.TotalMilliseconds
            );

            _logger.LogInformation(
                "Saved {Count} events for match {Id} to DB in {Ms}ms (Source: {Src})",
                events.Count,
                matchId,
                stopwatch.Elapsed.TotalMilliseconds,
                "db_loaded"
            );
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MatchEventRabbitMqClient StopAsync called.");

        _loadedMatches.Clear();
        _logger.LogInformation("Cleared match entity cache.");

        await CloseChannelAndConnectionAsync();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("MatchEventRabbitMqClient stopped.");
    }

    public override void Dispose()
    {
        _logger.LogInformation("MatchEventRabbitMqClient disposing.");
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
        base.Dispose();
        GC.SuppressFinalize(this);
        _logger.LogInformation("MatchEventRabbitMqClient disposed.");
    }
}
