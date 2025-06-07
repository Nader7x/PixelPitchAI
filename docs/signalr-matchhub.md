# SignalR MatchHub Documentation

## Overview

The `MatchHub` is a specialized SignalR Hub that provides real-time match-specific communications and live updates during football matches. It enables instant match event broadcasting, live commentary, and match-specific notifications to connected clients.

## Architecture

### Class Definition

```csharp
public class MatchHub : Hub<IMatchHub>
```

### Location

- **File**: `Application/Services/MatchHub.cs`
- **Namespace**: `Application.Services`
- **Interface**: `IMatchHub`
- **Hub URL**: `/matchHub` (typical configuration)

## Core Features

### Real-time Match Events

- Live score updates
- Goal notifications
- Card issuance alerts
- Player substitutions
- Match status changes (kickoff, halftime, full-time)

### Group Management

The MatchHub likely implements group-based messaging for:

- **Match-specific groups**: Users following specific matches
- **Team-based groups**: Fans following particular teams
- **Stadium groups**: Users at specific venues
- **Tournament groups**: Users following competitions

## Interface Contract (IMatchHub)

The service implements the `IMatchHub` interface defining client-callable methods:

### Core Client Methods

```csharp
public interface IMatchHub
{
    Task SendMatchUpdateAsync(object matchUpdate);
    Task SendGoalNotificationAsync(object goalData);
    Task SendCardNotificationAsync(object cardData);
    Task SendSubstitutionAsync(object substitutionData);
    Task SendMatchStatusAsync(string matchId, string status);
    Task SendLiveCommentary(string matchId, string commentary);
}
```

## Connection Management

### Group Operations

#### Join Match Group

```csharp
public async Task JoinMatchGroup(string matchId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"Match_{matchId}");
    await Clients.Group($"Match_{matchId}")
        .SendMatchUpdateAsync(new { message = "User joined match updates" });
}
```

#### Leave Match Group

```csharp
public async Task LeaveMatchGroup(string matchId)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Match_{matchId}");
}
```

#### Join Team Group

```csharp
public async Task JoinTeamGroup(string teamId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"Team_{teamId}");
}
```

## Match Event Broadcasting

### Live Score Updates

```csharp
// Server-side broadcasting
public async Task BroadcastScoreUpdate(string matchId, ScoreUpdateDto scoreUpdate)
{
    await Clients.Group($"Match_{matchId}")
        .SendMatchUpdateAsync(scoreUpdate);
}
```

### Goal Events

```csharp
public async Task BroadcastGoal(string matchId, GoalEventDto goalEvent)
{
    await Clients.Group($"Match_{matchId}")
        .SendGoalNotificationAsync(goalEvent);

    // Also notify team-specific groups
    await Clients.Group($"Team_{goalEvent.ScoringTeamId}")
        .SendGoalNotificationAsync(goalEvent);
}
```

### Match Status Changes

```csharp
public async Task BroadcastMatchStatus(string matchId, MatchStatusDto status)
{
    await Clients.Group($"Match_{matchId}")
        .SendMatchStatusAsync(matchId, status.Status);
}
```

## Client Integration

### JavaScript Client Setup

```javascript
const matchConnection = new signalR.HubConnectionBuilder()
  .withUrl("/matchHub")
  .withAutomaticReconnect()
  .build();

// Event handlers
matchConnection.on("SendMatchUpdateAsync", function (update) {
  updateMatchDisplay(update);
});

matchConnection.on("SendGoalNotificationAsync", function (goal) {
  showGoalNotification(goal);
  updateScore(goal);
});

matchConnection.on("SendCardNotificationAsync", function (card) {
  showCardNotification(card);
});

matchConnection.on("SendSubstitutionAsync", function (substitution) {
  updatePlayerList(substitution);
});

// Join specific match updates
async function joinMatch(matchId) {
  await matchConnection.invoke("JoinMatchGroup", matchId);
}

// Start connection
matchConnection.start().then(function () {
  console.log("Connected to MatchHub");
});
```

### React/Vue Client Integration

```javascript
// React Hook example
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

export const useMatchHub = (matchId) => {
  const [connection, setConnection] = useState(null);
  const [matchEvents, setMatchEvents] = useState([]);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("/matchHub")
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);

    newConnection.start().then(() => {
      newConnection.invoke("JoinMatchGroup", matchId);

      newConnection.on("SendMatchUpdateAsync", (update) => {
        setMatchEvents((prev) => [...prev, update]);
      });
    });

    return () => {
      newConnection.stop();
    };
  }, [matchId]);

  return { connection, matchEvents };
};
```

## Data Transfer Objects

### MatchUpdateDto

```csharp
public class MatchUpdateDto
{
    public string MatchId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string Description { get; set; }
    public object Data { get; set; }
}
```

### GoalEventDto

```csharp
public class GoalEventDto
{
    public string MatchId { get; set; }
    public string ScoringTeamId { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Minute { get; set; }
    public string GoalType { get; set; } // Regular, Penalty, Own Goal, etc.
    public ScoreDto CurrentScore { get; set; }
}
```

### CardEventDto

```csharp
public class CardEventDto
{
    public string MatchId { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string TeamId { get; set; }
    public string CardType { get; set; } // Yellow, Red
    public int Minute { get; set; }
    public string Reason { get; set; }
}
```

### SubstitutionDto

```csharp
public class SubstitutionDto
{
    public string MatchId { get; set; }
    public string TeamId { get; set; }
    public string PlayerOutId { get; set; }
    public string PlayerInId { get; set; }
    public string PlayerOutName { get; set; }
    public string PlayerInName { get; set; }
    public int Minute { get; set; }
}
```

## Integration with RabbitMQ

The MatchHub works closely with the RabbitMQ `MatchEventRabbitMqClient`:

### Event Flow

1. **Match Event Occurs** → RabbitMQ processes event
2. **Database Updated** → Event persisted to database
3. **SignalR Broadcast** → MatchHub broadcasts to connected clients
4. **Client Updates** → Real-time UI updates

### Coordination Pattern

```csharp
// In RabbitMQ event handler
public async Task HandleMatchEvent(MatchEventMessage message)
{
    // 1. Update database
    await _matchService.UpdateMatchEvent(message);

    // 2. Broadcast via MatchHub
    await _matchHubContext.Clients
        .Group($"Match_{message.MatchId}")
        .SendMatchUpdateAsync(message.ToDto());
}
```

## Performance Features

### Connection Optimization

- **Automatic Reconnection**: Built-in reconnection logic
- **Connection Pooling**: Managed by SignalR framework
- **Efficient Broadcasting**: Group-based targeting reduces overhead

### Scalability Considerations

- **Redis Backplane**: For multi-server deployments
- **Connection Limits**: Monitor and manage concurrent connections
- **Message Batching**: Batch multiple events when possible

## Use Cases

### 1. Live Match Following

- Real-time score tracking
- Instant goal celebrations
- Live match commentary
- Statistical updates

### 2. Fantasy Football Integration

- Player performance updates
- Injury notifications
- Lineup changes
- Points calculations

### 3. Sports Betting

- Live odds updates
- Event confirmations
- Result notifications
- Market changes

### 4. Media Broadcasting

- Live blog updates
- Social media integration
- Press updates
- Video highlight triggers

## Error Handling & Reliability

### Connection Management

```javascript
// Automatic reconnection with exponential backoff
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/matchHub")
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .build();

connection.onreconnecting(() => {
  console.log("Reconnecting to MatchHub...");
});

connection.onreconnected(() => {
  console.log("Reconnected to MatchHub");
  // Rejoin groups if needed
  rejoinMatchGroups();
});
```

### Fallback Strategies

- **Polling Fallback**: HTTP polling if WebSocket fails
- **Event Replay**: Request missed events on reconnection
- **State Synchronization**: Refresh match state on reconnection

## Monitoring & Analytics

### Key Metrics

- **Active Connections**: Number of live match followers
- **Event Broadcast Rate**: Messages per second during matches
- **Group Membership**: Users per match/team groups
- **Connection Duration**: How long users stay connected

### Performance Monitoring

```csharp
// Custom metrics collection
public class MatchHubMetrics
{
    public int ActiveConnections { get; set; }
    public int ActiveMatches { get; set; }
    public Dictionary<string, int> GroupMembership { get; set; }
    public TimeSpan AverageConnectionDuration { get; set; }
}
```

## Security Considerations

### Input Validation

- Validate all match IDs and team IDs
- Sanitize user-generated content
- Rate limiting on group operations

### Access Control

- Verify user permissions for private matches
- Implement subscription-based access for premium content
- Audit trail for administrative actions

## Configuration

### Startup Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapHub<MatchHub>("/matchHub");
    });
}
```

### Environment-specific Settings

```json
{
  "SignalR": {
    "MatchHub": {
      "MaxConnectionsPerMatch": 10000,
      "EventBroadcastDelay": 500,
      "EnableDetailedLogging": true
    }
  }
}
```

## Troubleshooting

### Common Issues

1. **High Connection Count**: Implement connection limits and monitoring
2. **Message Loss**: Add acknowledgment system for critical events
3. **Performance Degradation**: Monitor group sizes and optimize broadcasting
4. **Memory Leaks**: Ensure proper group cleanup on disconnection

### Debug Tools

```csharp
// Connection diagnostic endpoint
[ApiController]
public class MatchHubDiagnosticsController : ControllerBase
{
    [HttpGet("matchhub/diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        var hubContext = HttpContext.RequestServices
            .GetRequiredService<IHubContext<MatchHub, IMatchHub>>();

        return Ok(new
        {
            ActiveConnections = GetActiveConnectionCount(),
            ActiveGroups = GetActiveGroups(),
            SystemHealth = "OK"
        });
    }
}
```

## Best Practices

### Development

1. **Type Safety**: Use strongly-typed hubs with interfaces
2. **Error Handling**: Implement comprehensive error handling
3. **Testing**: Create unit tests for hub methods
4. **Documentation**: Document all client-callable methods

### Production

1. **Monitoring**: Implement comprehensive monitoring
2. **Scaling**: Plan for horizontal scaling with Redis backplane
3. **Security**: Implement proper authentication and authorization
4. **Performance**: Monitor and optimize message broadcasting

### Client-side

1. **Reconnection**: Implement robust reconnection logic
2. **State Management**: Sync client state after reconnection
3. **Error Handling**: Handle connection failures gracefully
4. **User Experience**: Provide feedback for connection status
