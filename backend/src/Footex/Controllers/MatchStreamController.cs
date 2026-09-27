using System.Text.Json;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchStreamController : ControllerBase
{
    private readonly IMatchEventBroadcaster _broadcaster;
    private readonly ILogger<MatchStreamController> _logger;

    public MatchStreamController(IMatchEventBroadcaster broadcaster, ILogger<MatchStreamController> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    /// <summary>
    ///     High-performance Server-Sent Events (SSE) endpoint for real-time match events and statistics.
    ///     Requires authenticated JWT token (passed via Authorization header or ?access_token= query string).
    /// </summary>
    /// <param name="id">Match ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id:int}/events/stream")]
    public async Task StreamMatchEvents(int id, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var matchIdStr = id.ToString();
        _logger.LogInformation("Client connected to SSE stream for match {MatchId}", matchIdStr);

        try
        {
            // Initial handshake comment
            await Response.WriteAsync($": connected to match stream {matchIdStr}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var msg in _broadcaster.SubscribeAsync(matchIdStr, cancellationToken))
            {
                await Response.WriteAsync($"event: {msg.EventType}\ndata: {msg.Data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected from SSE stream for match {MatchId}", matchIdStr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE event stream for match {MatchId}", matchIdStr);
        }
    }
}
