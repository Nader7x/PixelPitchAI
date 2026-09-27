# RabbitMQ MatchEventRabbitMqClient Documentation

## Overview

The `MatchEventRabbitMqClient` is a high-performance background service that manages RabbitMQ message consumption for match simulation events in the PixelPitchAI application (Pipeline A). It provides robust event ingestion with zero-allocation `ReadOnlySpan<char>` parsing, automatic recovery, performance monitoring, and real-time Server-Sent Events (SSE) broadcasting.

## Architecture

### Class Definition

```csharp
public class MatchEventRabbitMqClient : BackgroundService
```

### Location

- **File**: `Infrastructure/Services/MatchEventRabbitMqClient.cs`
- **Namespace**: `Infrastructure.Services`
- **Base Class**: `BackgroundService`
- **Service Type**: Singleton Background Service

## Core Components

### Dependencies

- **IServiceProvider**: Dependency injection container for scoped services (EF Core, repositories)
- **ILogger<MatchEventRabbitMqClient>**: Logging service
- **IConnection**: RabbitMQ connection management
- **IModel**: RabbitMQ channel management
- **IMatchEventBroadcaster**: Server-Sent Events (SSE) broadcaster for live client streaming
- **ZeroAllocationEventParser**: Reflection-free, zero-allocation `ReadOnlySpan<char>` parser
- **ILiveMatchStatisticsService**: In-memory atomic cache for live match statistics

### Key Features

1. **Automatic Recovery**: Handles connection failures and automatic network reconnection
2. **Zero-Allocation Parsing**: Parses incoming raw text simulation lines using `ReadOnlySpan<char>` without heap allocations
3. **Hot State Caching**: Synchronizes with Redis and in-memory live statistics cache
4. **Server-Sent Events (SSE) Broadcasting**: Forwards parsed events to `IMatchEventBroadcaster` for browser delivery (`/api/matches/{id}/events/stream`)
5. **Batched Database Persistence**: Accumulates match events in-memory and executes a single batched `SaveChangesAsync()` commit to PostgreSQL upon `[MATCH END]`

## Configuration

### RabbitMQ Settings

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "QueueName": "match_events_queue",
    "ExchangeName": "match_events",
    "RoutingKey": "match.events",
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryInterval": 5000,
    "PrefetchCount": 10
  }
}
```

### Service Registration

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<MatchEventRabbitMqClient>();
services.AddHostedService<MatchEventRabbitMqClient>();
```

## Event Processing Pipeline

### 1. Message Reception

```csharp
public override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                await ProcessMessage(ea.Body.ToArray(), ea.DeliveryTag);
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        }
        catch (Exception ex)
        {
            await HandleConnectionError(ex);
        }
    }
}
```

### 2. Message Processing

```csharp
private async Task ProcessMessage(byte[] messageBody, ulong deliveryTag)
{
    try
    {
        // 1. Decode raw simulation text line
        var rawText = Encoding.UTF8.GetString(messageBody);

        // 2. Zero-allocation parsing via ReadOnlySpan<char>
        if (!ZeroAllocationEventParser.TryParseEvent(rawText.AsSpan(), out var matchEvent))
        {
            _channel.BasicAck(deliveryTag, false);
            return;
        }

        // 3. Process event & update hot state statistics
        await ProcessMatchEvent(matchEvent);

        // 4. Broadcast to clients via Server-Sent Events (SSE)
        await _broadcaster.BroadcastEventAsync(matchEvent.match_id, matchEvent);

        // 5. Acknowledge message
        _channel.BasicAck(deliveryTag, false);
    }
    catch (Exception ex)
    {
        await HandleProcessingError(ex, deliveryTag);
    }
}
```

### 3. Event Broadcasting via Server-Sent Events (SSE)

```csharp
private async Task BroadcastEvent(FootballMatchEvent matchEvent)
{
    // Push event to SSE broadcaster channel (consumed at GET /api/matches/{id}/events/stream)
    await _broadcaster.BroadcastEventAsync(matchEvent.match_id, matchEvent);

    // If statistics are updated, broadcast statistics event
    if (_statisticsService.TryGetLatestStats(matchEvent.match_id, out var stats))
    {
        await _broadcaster.BroadcastStatisticsAsync(matchEvent.match_id, stats);
    }
}
```

## Message Types

### MatchEventMessage

```csharp
public class MatchEventMessage
{
    public string EventId { get; set; }
    public string MatchId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string TeamId { get; set; }
    public string PlayerId { get; set; }
    public Dictionary<string, object> EventData { get; set; }
    public int Minute { get; set; }
    public string Description { get; set; }
}
```

### Event Types

- **Goal**: Scoring events with player and team information
- **Card**: Yellow and red card issuances
- **Substitution**: Player substitutions
- **MatchStatus**: Game state changes (kickoff, halftime, full-time)
- **Injury**: Player injury events
- **VAR**: Video Assistant Referee decisions
- **Offside**: Offside calls
- **Foul**: Foul events and free kicks

## Performance Monitoring

### Metrics Collection

```csharp
public class MatchEventMetrics
{
    public int MessagesProcessed { get; set; }
    public int ProcessingErrors { get; set; }
    public double AverageProcessingTime { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime LastProcessedEvent { get; set; }
    public Dictionary<string, int> EventTypeCount { get; set; }
}
```

### Health Monitoring

```csharp
private async Task MonitorHealth()
{
    var healthCheck = new
    {
        ServiceName = "MatchEventRabbitMqClient",
        Status = _connection.IsOpen ? "Healthy" : "Unhealthy",
        LastHeartbeat = DateTime.UtcNow,
        MessagesProcessed = _metrics.MessagesProcessed,
        ErrorRate = CalculateErrorRate(),
        QueueDepth = GetQueueDepth()
    };

    _logger.LogInformation("Health Check: {@HealthCheck}", healthCheck);
}
```

## Caching Strategy

### Event Caching

```csharp
private readonly IMemoryCache _cache;
private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

private async Task UpdateCache(MatchEventMessage message)
{
    var cacheKey = $"match_events_{message.MatchId}";
    var events = _cache.Get<List<MatchEventMessage>>(cacheKey) ?? new List<MatchEventMessage>();

    events.Add(message);

    // Keep only last 50 events per match
    if (events.Count > 50)
    {
        events = events.TakeLast(50).ToList();
    }

    _cache.Set(cacheKey, events, _cacheExpiration);
}
```

### Performance Optimization

```csharp
private readonly ConcurrentDictionary<string, SemaphoreSlim> _processingLocks
    = new ConcurrentDictionary<string, SemaphoreSlim>();

private async Task ProcessMatchEvent(MatchEventMessage message)
{
    var semaphore = _processingLocks.GetOrAdd(message.MatchId, _ => new SemaphoreSlim(1, 1));

    await semaphore.WaitAsync();
    try
    {
        // Process event with match-level locking
        await ProcessEventInternal(message);
    }
    finally
    {
        semaphore.Release();
    }
}
```

## Error Handling & Recovery

### Connection Recovery

```csharp
private async Task HandleConnectionError(Exception ex)
{
    _logger.LogError(ex, "RabbitMQ connection error occurred");

    // Implement exponential backoff
    var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, _retryCount)));
    await Task.Delay(delay);

    try
    {
        await InitializeConnection();
        _retryCount = 0;
    }
    catch (Exception reconnectEx)
    {
        _logger.LogError(reconnectEx, "Failed to reconnect to RabbitMQ");
        _retryCount++;
    }
}
```

### Message Processing Errors

```csharp
private async Task HandleProcessingError(Exception ex, ulong deliveryTag)
{
    _logger.LogError(ex, "Error processing message with delivery tag {DeliveryTag}", deliveryTag);

    // Implement retry logic with dead letter queue
    if (_retryCount < 3)
    {
        // Negative acknowledgment with requeue
        _channel.BasicNack(deliveryTag, false, true);
        _retryCount++;
    }
    else
    {
        // Send to dead letter queue
        _channel.BasicNack(deliveryTag, false, false);
        await LogToDeadLetterQueue(ex, deliveryTag);
    }
}
```

## Database Integration

### Event Persistence

```csharp
private async Task PersistEvent(MatchEventMessage message)
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FootexDbContext>();

    var eventEntity = new MatchEvent
    {
        Id = Guid.NewGuid(),
        MatchId = Guid.Parse(message.MatchId),
        EventType = message.EventType,
        Timestamp = message.Timestamp,
        Minute = message.Minute,
        Description = message.Description,
        EventData = JsonSerializer.Serialize(message.EventData),
        ProcessedAt = DateTime.UtcNow
    };

    dbContext.MatchEvents.Add(eventEntity);
    await dbContext.SaveChangesAsync();
}
```

### Event Aggregation

```csharp
private async Task UpdateMatchStatistics(MatchEventMessage message)
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FootexDbContext>();

    var match = await dbContext.Matches
        .FirstOrDefaultAsync(m => m.Id == Guid.Parse(message.MatchId));

    if (match != null)
    {
        switch (message.EventType)
        {
            case "Goal":
                await UpdateGoalStatistics(match, message);
                break;
            case "Card":
                await UpdateCardStatistics(match, message);
                break;
        }

        match.LastUpdated = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }
}
```

## Integration Patterns

### Event Sourcing

```csharp
private async Task<List<MatchEventMessage>> ReplayEvents(string matchId, DateTime fromTimestamp)
{
    var cacheKey = $"match_events_{matchId}";
    var cachedEvents = _cache.Get<List<MatchEventMessage>>(cacheKey);

    if (cachedEvents != null)
    {
        return cachedEvents.Where(e => e.Timestamp >= fromTimestamp).ToList();
    }

    // Fallback to database
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FootexDbContext>();

    var events = await dbContext.MatchEvents
        .Where(e => e.MatchId == Guid.Parse(matchId) && e.Timestamp >= fromTimestamp)
        .Select(e => new MatchEventMessage
        {
            EventId = e.Id.ToString(),
            MatchId = e.MatchId.ToString(),
            EventType = e.EventType,
            Timestamp = e.Timestamp,
            Minute = e.Minute,
            Description = e.Description,
            EventData = JsonSerializer.Deserialize<Dictionary<string, object>>(e.EventData)
        })
        .ToListAsync();

    return events;
}
```

### CQRS Pattern

```csharp
// Command handling
private async Task HandleMatchEventCommand(MatchEventMessage message)
{
    // 1. Validate command
    await ValidateCommand(message);

    // 2. Execute business logic
    await ExecuteBusinessLogic(message);

    // 3. Persist changes
    await PersistChanges(message);

    // 4. Publish domain events
    await PublishDomainEvents(message);
}

// Query handling
private async Task<MatchEventQueryResult> HandleMatchEventQuery(string matchId)
{
    // Use cached data for queries
    var cacheKey = $"match_query_{matchId}";
    var cachedResult = _cache.Get<MatchEventQueryResult>(cacheKey);

    if (cachedResult != null)
    {
        return cachedResult;
    }

    // Fetch from database if not cached
    var result = await FetchFromDatabase(matchId);
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

    return result;
}
```

## Monitoring & Diagnostics

### Logging Configuration

```csharp
private void ConfigureLogging()
{
    _logger.LogInformation("MatchEventRabbitMqClient starting up");

    // Structured logging for better analysis
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["ServiceName"] = "MatchEventRabbitMqClient",
        ["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString()
    }))
    {
        _logger.LogInformation("Service initialized successfully");
    }
}
```

### Performance Counters

```csharp
private readonly Dictionary<string, long> _performanceCounters = new()
{
    ["MessagesReceived"] = 0,
    ["MessagesProcessed"] = 0,
    ["ProcessingErrors"] = 0,
    ["AverageProcessingTimeMs"] = 0,
    ["DatabaseWrites"] = 0,
    ["CacheHits"] = 0,
    ["CacheMisses"] = 0
};

private void UpdatePerformanceCounters(string counterName, long value)
{
    _performanceCounters[counterName] = value;

    // Emit metrics to monitoring system
    if (_metricsCollector != null)
    {
        _metricsCollector.RecordMetric(counterName, value);
    }
}
```

## Configuration Options

### Advanced Settings

```json
{
  "MatchEventRabbitMqClient": {
    "ConnectionString": "amqp://localhost:5672",
    "QueueName": "match_events_queue",
    "ExchangeName": "match_events",
    "RoutingKey": "match.events",
    "PrefetchCount": 10,
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryInterval": 5000,
    "MaxRetryAttempts": 3,
    "ProcessingTimeout": 30000,
    "CacheExpiration": 1800,
    "HealthCheckInterval": 60000,
    "EnableDetailedLogging": true,
    "EnablePerformanceCounters": true,
    "DeadLetterExchange": "match_events_dlx",
    "BatchSize": 100,
    "MaxConcurrentProcessing": 5
  }
}
```

### Environment-specific Configuration

```csharp
public class MatchEventRabbitMqClientOptions
{
    public string ConnectionString { get; set; }
    public string QueueName { get; set; } = "match_events_queue";
    public string ExchangeName { get; set; } = "match_events";
    public string RoutingKey { get; set; } = "match.events";
    public int PrefetchCount { get; set; } = 10;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public int NetworkRecoveryInterval { get; set; } = 5000;
    public int MaxRetryAttempts { get; set; } = 3;
    public int ProcessingTimeout { get; set; } = 30000;
    public int CacheExpiration { get; set; } = 1800;
    public bool EnableDetailedLogging { get; set; } = false;
}
```

## Testing Strategies

### Unit Testing

```csharp
[Test]
public async Task ProcessMessage_ValidRawEvent_ShouldParseAndBroadcastViaSse()
{
    // Arrange
    var rawText = "12, 14, 25, Pass, (34.5, 52.1), (45.0, 60.2), Successful pass";
    var body = Encoding.UTF8.GetBytes(rawText);

    // Act
    await _client.ProcessMessage(body, 1);

    // Assert
    _mockBroadcaster.Verify(x => x.BroadcastEventAsync(
        It.IsAny<string>(),
        It.Is<FootballMatchEvent>(e => e.type == "Pass")), Times.Once);
}
```

### Integration Testing

```csharp
[Test]
public async Task IntegrationTest_EndToEndEventProcessing()
{
    // Arrange
    var rawText = "12, 14, 25, Goal, (88.5, 50.1), (100.0, 50.0), Goal scored!";

    // Act
    await PublishMessageToQueue(rawText);
    await WaitForProcessing(TimeSpan.FromSeconds(5));

    // Assert
    var sseMessages = GetBroadcastedSseMessages();
    Assert.IsTrue(sseMessages.Any(m => m.EventType == "match_event"));
}
```

## Troubleshooting Guide

### Common Issues

#### 1. Connection Failures

**Symptoms**: Service fails to start or frequently disconnects
**Solutions**:

- Check RabbitMQ server status
- Verify connection string format
- Review network connectivity
- Check authentication credentials

#### 2. Message Processing Delays

**Symptoms**: Events are processed slowly or with delays
**Solutions**:

- Increase PrefetchCount for better throughput
- Optimize database queries
- Review cache configuration
- Monitor resource usage

#### 3. Memory Leaks

**Symptoms**: Gradually increasing memory usage
**Solutions**:

- Implement proper disposal of resources
- Review cache expiration policies
- Monitor object lifecycle
- Use memory profiling tools

#### 4. Dead Letter Queue Accumulation

**Symptoms**: Messages accumulating in dead letter queue
**Solutions**:

- Review error handling logic
- Validate message formats
- Check processing timeout settings
- Implement better error recovery

### Diagnostic Commands

```bash
# Check RabbitMQ queue status
rabbitmqctl list_queues name messages

# Monitor service logs
tail -f /var/log/footex/matchevent-client.log

# Check performance metrics
curl http://localhost:5000/api/diagnostics/matchevent-client

# View dead letter queue
rabbitmqctl list_queues name messages | grep dlx
```

## Best Practices

### Development

1. **Error Handling**: Implement comprehensive error handling with appropriate logging
2. **Testing**: Write unit and integration tests for all major components
3. **Monitoring**: Implement detailed monitoring and alerting
4. **Documentation**: Maintain up-to-date documentation for all configurations

### Production

1. **Scaling**: Use multiple instances with proper load balancing
2. **Monitoring**: Implement comprehensive monitoring and alerting
3. **Backup**: Regular backups of message queues and configurations
4. **Security**: Implement proper authentication and authorization

### Performance

1. **Caching**: Implement effective caching strategies
2. **Batching**: Process messages in batches when possible
3. **Async Processing**: Use asynchronous processing throughout
4. **Resource Management**: Properly manage connections and resources

### Maintenance

1. **Regular Updates**: Keep RabbitMQ and dependencies updated
2. **Performance Tuning**: Regularly review and optimize performance
3. **Capacity Planning**: Monitor growth and plan for scaling
4. **Disaster Recovery**: Implement comprehensive disaster recovery procedures
