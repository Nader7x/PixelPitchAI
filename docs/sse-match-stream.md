# Server-Sent Events (SSE) Match Streaming Architecture

## Overview

In PixelPitchAI, live match simulation commentary, coordinates, and statistics are streamed exclusively via **Server-Sent Events (SSE)**. 

SSE replaces obsolete WebSocket and SignalR protocols for match simulation streaming, providing:
- **Unidirectional Efficiency**: Football match simulation is strictly server-to-client broadcast. SSE avoids the bidirectional state, ping-pong pinging, and connection negotiation overhead of WebSockets.
- **Native Browser Support**: Uses the standard browser EventSource API with automatic reconnection, requiring zero heavy client-side libraries.
- **Reverse Proxy & HTTP/2 Compatibility**: Fully compliant with modern HTTP/1.1 and HTTP/2 multiplexing, streaming through reverse proxies (such as Caddy or Nginx) using X-Accel-Buffering: no.
- **Native AOT & Reflection-Free**: Implemented using high-performance, bounded System.Threading.Channels.Channel<SseMessage> without runtime reflection.

> [!NOTE]
> SignalR is retained strictly at /Notify for asynchronous user alerts and notification badges. It is **never** used for match simulation play-by-play events.

---

## 1. Endpoint Specification

### GET /api/matches/{id:int}/events/stream

Opens an indefinite, streaming HTTP response delivering real-time simulation events for the specified match.

- **Route**: /api/matches/{id:int}/events/stream
- **Controller**: Footex.Controllers.MatchStreamController
- **Authorization**: [Authorize] (Requires valid JWT)
- **Transport Protocol**: Server-Sent Events (	ext/event-stream)

### Authentication via Query Parameter

Standard browser EventSource does not support custom HTTP request headers. To authenticate SSE streams, the client passes the JWT token as a query parameter:

`http
GET /api/matches/101/events/stream?access_token=eyJhbGciOi... HTTP/1.1
Host: localhost:5025
Accept: text/event-stream
`

In Program.cs, ASP.NET Core JwtBearerEvents.OnMessageReceived intercepts the query parameter for /api/matches paths:

`csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) &&
            (path.StartsWithSegments("/Notify")
             || path.StartsWithSegments("/api/matches")))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
`

---

## 2. Wire Protocol & Handshake

### HTTP Response Headers

Upon accepting the connection, the server immediately sets the following response headers:

`http
HTTP/1.1 200 OK
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
X-Accel-Buffering: no
`

- Content-Type: text/event-stream: Identifies the stream as an SSE feed.
- Cache-Control: no-cache: Prevents intermediary proxies from caching partial responses.
- Connection: keep-alive: Maintains the TCP socket open indefinitely.
- X-Accel-Buffering: no: Informs Caddy, Nginx, or cloud reverse proxies to disable response buffering and flush chunks immediately.

### Connection Handshake

The server immediately flushes an SSE comment line to establish the connection and confirm receipt:

`	ext
: connected to match stream 101

`

---

## 3. Event Types & Wire Payloads

All events conform to the standard SSE event format:
`	ext
event: <event_type>
data: <json_payload>

`

The broadcaster publishes two distinct event types:

### Event Type: match_event

Emitted for each play-by-play simulation event (e.g. Pass, Shot, Goal, Foul, Tackle, Offside, Match Start/End).

**Wire Example**:
`	ext
event: match_event
data: {"minute":24,"second":15,"type":"Pass","teamId":1,"teamName":"Arsenal","playerId":10,"playerName":"Bukayo Saka","coordinates":{"x":65.2,"y":42.8},"endCoordinates":{"x":78.0,"y":55.4},"text":"Saka plays a through ball into the penalty area"}

`

**JSON Schema (FootballMatchEvent)**:
`json
{
  "minute": 24,
  "second": 15,
  "type": "Pass",
  "teamId": 1,
  "teamName": "Arsenal",
  "playerId": 10,
  "playerName": "Bukayo Saka",
  "coordinates": {
    "x": 65.2,
    "y": 42.8
  },
  "endCoordinates": {
    "x": 78.0,
    "y": 55.4
  },
  "text": "Saka plays a through ball into the penalty area"
}
`

### Event Type: match_statistics

Emitted periodically or on key occurrences (goals, halftime, match completion) with cumulative match analytics.

**Wire Example**:
`	ext
event: match_statistics
data: {"homePossession":56.2,"awayPossession":43.8,"homeShots":8,"awayShots":4,"homeShotsOnTarget":5,"awayShotsOnTarget":2,"homePasses":245,"awayPasses":189,"homePassAccuracy":84.5,"awayPassAccuracy":79.2,"homeFouls":6,"awayFouls":9,"homeYellowCards":1,"awayYellowCards":2,"homeRedCards":0,"awayRedCards":0}

`

---

## 4. Backend Implementation Architecture

The streaming pipeline is powered by System.Threading.Channels for lock-free, zero-allocation multi-subscriber fanout:

`
┌────────────────────────────────────────────────────────┐
│  Simulation Ingestion: Pipeline A (RabbitMQ) or        │
│                        Pipeline B (Direct gRPC)        │
└───────────────────────────┬────────────────────────────┘
                            │
              ZeroAllocationEventParser
                            │
                            ▼
          IMatchEventBroadcaster.BroadcastEventAsync
                            │
    ┌───────────────────────┴───────────────────────┐
    │                                               │
    ▼                                               ▼
Channel<SseMessage> (Client 1)       Channel<SseMessage> (Client 2)
    │                                               │
    ▼                                               ▼
MatchStreamController Stream         MatchStreamController Stream
GET /api/matches/{id}/events/stream  GET /api/matches/{id}/events/stream
`

### Broadcaster Design (MatchEventBroadcaster.cs)

- Uses a nested concurrent dictionary: ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<SseMessage>>>.
- Outer key: matchId (string).
- Inner key: Guid representing an individual connected client session.
- Channel configuration:
  - Bounded capacity: 500 messages per client.
  - Full mode: BoundedChannelFullMode.DropOldest (prevents memory spikes if a client network slows down).
  - Single reader: 	rue (each client channel is read only by its respective HTTP response loop).
  - Single writer: alse (RabbitMQ background service and gRPC consumer can write concurrently).

---

## 5. Frontend Client Consumption

The frontend connects directly using the native browser EventSource in rontend/Services/MatchStreamService.ts:

`	ypescript
export class MatchStreamService {
  private eventSource: EventSource | null = null;

  public connect(
    matchId: number | string,
    token: string,
    callbacks: {
      onEvent: (event: FootballMatchEvent) => void;
      onStatistics?: (stats: MatchStatistics) => void;
      onError?: (error: any) => void;
    }
  ): void {
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5025";
    const url = ${baseUrl}/api/matches//events/stream?access_token=;

    this.eventSource = new EventSource(url);

    // Listen for play-by-play match events
    this.eventSource.addEventListener("match_event", (e: MessageEvent) => {
      try {
        const payload: FootballMatchEvent = JSON.parse(e.data);
        callbacks.onEvent(payload);
      } catch (err) {
        console.error("Failed to parse match_event payload", err);
      }
    });

    // Listen for live match statistics updates
    this.eventSource.addEventListener("match_statistics", (e: MessageEvent) => {
      try {
        const stats: MatchStatistics = JSON.parse(e.data);
        callbacks.onStatistics?.(stats);
      } catch (err) {
        console.error("Failed to parse match_statistics payload", err);
      }
    });

    this.eventSource.onerror = (err) => {
      console.warn("SSE connection error", err);
      callbacks.onError?.(err);
    };
  }

  public disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }
}
`

---

## 6. Summary Comparison: SSE vs. Legacy SignalR

| Feature | Server-Sent Events (SSE) (Active Architecture) | SignalR / WebSockets (Legacy Architecture) |
| :--- | :--- | :--- |
| **Endpoint** | GET /api/matches/{id}/events/stream | /matchSimulationHub (Purged/Deprecated) |
| **Directionality** | Unidirectional (Server-to-Client) | Full Duplex Bidirectional |
| **Client Requirement** | Native browser EventSource (Zero dependencies) | @microsoft/signalr client library |
| **Auth Transport** | ?access_token= query string | WebSocket query string or HTTP header |
| **Proxy Traversal** | Standard HTTP/1.1 or HTTP/2, X-Accel-Buffering: no | Requires HTTP 101 Switching Protocols |
| **Memory Allocation** | Bounded System.Threading.Channels with drop-oldest | Complex SignalR Hub state & group maps |
| **Native AOT** | 100% Native AOT ready | Requires reflection & trim descriptors |
