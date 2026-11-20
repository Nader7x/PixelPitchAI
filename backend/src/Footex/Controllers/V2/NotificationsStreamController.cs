using System.Security.Claims;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers.V2;

[ApiController]
[Route("api/v2/notifications")]
[Authorize]
public sealed class NotificationsStreamController : ControllerBase
{
    private readonly INotificationsBroadcaster _notificationsBroadcaster;
    private readonly ILogger<NotificationsStreamController> _logger;

    public NotificationsStreamController(
        INotificationsBroadcaster notificationsBroadcaster,
        ILogger<NotificationsStreamController> logger
    )
    {
        _notificationsBroadcaster = notificationsBroadcaster;
        _logger = logger;
    }

    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

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
            await Response.WriteAsync($": connected to notifications stream for {userId}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var msg in _notificationsBroadcaster.SubscribeAsync(userId, lastEventId, cancellationToken))
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
            _logger.LogDebug("Client disconnected from notifications SSE stream for user {UserId}", userId);
        }
    }
}
