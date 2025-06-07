# SignalR NotificationService Documentation

## Overview

The `NotificationService` is a JWT-secured SignalR Hub that provides real-time notification capabilities for authenticated users in the Footex application. It enables instant messaging and notification delivery to connected clients.

## Architecture

### Class Definition

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationService : Hub<INotificationService>
```

### Location

- **File**: `Application/Services/NotificationService.cs`
- **Namespace**: `Application.Services`
- **Interface**: `INotificationService`

## Security & Authentication

### Authorization

- **Scheme**: JWT Bearer Token Authentication
- **Requirement**: All connections must be authenticated with valid JWT tokens
- **User Context**: Access to authenticated user information through `Context.User`

### Security Features

- JWT token validation on connection
- User identity extraction using `GetNameId()` helper
- Secure user-specific messaging

## Core Features

### Connection Management

#### OnConnectedAsync()

Handles new client connections with personalized welcome messages.

**Implementation**:

```csharp
public override Task OnConnectedAsync()
{
    var userId = Context.User.GetNameId();
    if (userId != null)
    {
        Clients.User(userId).SendMessageAsync("Welcome back!");
    }
    return base.OnConnectedAsync();
}
```

**Features**:

- Automatic user identification on connection
- Personalized welcome message delivery
- Graceful handling of anonymous connections

## Interface Contract (INotificationService)

The service implements the `INotificationService` interface which defines the contract for client-side methods that can be called from the server.

### Typical Interface Methods

Based on the implementation pattern, the interface likely includes:

- `SendMessageAsync(string message)` - Send general messages
- `SendNotificationAsync(object notification)` - Send structured notifications
- `SendUserSpecificMessageAsync(string userId, string message)` - Targeted messaging

## Client Integration

### Connection Establishment

```javascript
// JavaScript client example
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/notificationHub", {
    accessTokenFactory: () => {
      return localStorage.getItem("jwt-token");
    },
  })
  .build();
```

### Message Handling

```javascript
// Listen for messages
connection.on("SendMessageAsync", function (message) {
  console.log("Notification received:", message);
});

// Start connection
connection.start().then(function () {
  console.log("Connected to NotificationService");
});
```

## Usage Scenarios

### 1. System Notifications

- User registration confirmations
- Password reset notifications
- Account status updates
- System maintenance alerts

### 2. Real-time Updates

- Live score updates
- Match status changes
- Player transfer notifications
- Season updates

### 3. User-specific Messages

- Personal notifications
- Achievement unlocks
- Reminder notifications
- Targeted announcements

## Integration with Other Services

### Dependencies

- **ClaimsExtensions**: Helper for user identity extraction
- **JWT Authentication**: Token validation and user context
- **Domain Interfaces**: Interface contracts for type safety

### Related Services

- **MatchHub**: For match-specific real-time updates
- **RabbitMQ Client**: For event-driven notification triggers
- **Notification Controllers**: For REST API notification management

## Performance Considerations

### Scalability Features

- Stateless design for horizontal scaling
- JWT-based authentication (no server-side session storage)
- Efficient user targeting with SignalR groups

### Best Practices

- Connection pooling handled by SignalR framework
- Automatic reconnection support on client side
- Message queuing for offline users (implementation dependent)

## Error Handling

### Connection Errors

- Invalid JWT tokens result in connection rejection
- Network disconnections handled by SignalR framework
- Client-side reconnection logic recommended

### Message Delivery

- Fire-and-forget message delivery
- No guarantee of message receipt (consider implementing acknowledgments if needed)
- Connection state validation before message sending

## Configuration

### Startup Configuration

```csharp
// In Program.cs or Startup.cs
services.AddSignalR();

app.MapHub<NotificationService>("/notificationHub");
```

### Authentication Setup

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // JWT configuration
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

## Monitoring & Diagnostics

### Key Metrics

- Active connection count
- Message delivery rate
- Authentication failure rate
- Connection duration statistics

### Logging

- Connection events logged automatically
- Custom logging for business logic
- Error tracking for failed message deliveries

## Future Enhancements

### Potential Features

- Message acknowledgment system
- Offline message queuing
- Message priority levels
- User notification preferences
- Push notification integration

### Scalability Improvements

- Redis backplane for multi-server deployments
- Connection state management
- Load balancing considerations

## Troubleshooting

### Common Issues

1. **Connection Refused**: Check JWT token validity and format
2. **Messages Not Received**: Verify client-side event handlers
3. **Authentication Failures**: Validate JWT configuration and token format
4. **Performance Issues**: Monitor connection count and message frequency

### Debug Steps

1. Validate JWT token in browser developer tools
2. Check network connectivity and WebSocket support
3. Review server logs for authentication errors
4. Test with minimal client implementation

## API Reference

### Server Methods

- `OnConnectedAsync()`: Handles new connections
- `OnDisconnectedAsync()`: Handles disconnections (inherited)

### Client Methods (via INotificationService)

- `SendMessageAsync(string message)`: Send text messages
- Additional methods defined in interface implementation

## Security Best Practices

1. **Token Validation**: Always validate JWT tokens on connection
2. **User Authorization**: Verify user permissions before sending sensitive notifications
3. **Rate Limiting**: Implement connection and message rate limits
4. **Input Validation**: Sanitize all message content
5. **Audit Logging**: Log all notification activities for security auditing
