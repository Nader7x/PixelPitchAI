using System.Text.Json.Serialization;

namespace Application.Dtos;

/// <summary>
/// Response DTO for simulation status tracking
/// </summary>
public class SimulationStatusResponse
{
    [JsonPropertyName("simulation_id")]
    public string SimulationId { get; init; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty; // "pending", "running", "completed", "failed"
    
    [JsonPropertyName("progress_percentage")]
    public double? ProgressPercentage { get; init; }
    
    [JsonPropertyName("estimated_completion_time")]
    public DateTime? EstimatedCompletionTime { get; init; }
    
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; init; }
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
    
    [JsonPropertyName("error")]
    public string? Error { get; init; }
    
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    // Internal properties for local match tracking
    public int MatchId { get; set; }
    public string? MatchStatus { get; set; }
}

/// <summary>
/// Response DTO for a simulation result when completed
/// </summary>
public class SimulationResultResponse
{
    [JsonPropertyName("simulation_id")]
    public string SimulationId { get; init; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
    
    [JsonPropertyName("match_id")]
    public int MatchId { get; set; }
    
    [JsonPropertyName("home_team_name")]
    public string HomeTeamName { get; init; } = string.Empty;
    
    [JsonPropertyName("away_team_name")]
    public string AwayTeamName { get; init; } = string.Empty;
    
    [JsonPropertyName("home_team_score")]
    public int? HomeTeamScore { get; init; }
    
    [JsonPropertyName("away_team_score")]
    public int? AwayTeamScore { get; init; }
    
    [JsonPropertyName("events_count")]
    public int EventsCount { get; init; }
    
    [JsonPropertyName("execution_time")]
    public double ExecutionTime { get; init; }
    
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; init; }
    
    [JsonPropertyName("preview")]
    public string? Preview { get; init; }
    
    [JsonPropertyName("error")]
    public string? Error { get; init; }
    
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; init; }
}

/// <summary>
/// Server-Sent Events response for real-time simulation updates
/// </summary>
public class SimulationStreamEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty; // "status_update", "progress", "completion", "error"
    
    [JsonPropertyName("simulation_id")]
    public string SimulationId { get; init; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object Data { get; init; } = new();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for webhook registration
/// </summary>
public class RegisterWebhookRequest
{
    [JsonPropertyName("webhook_url")]
    public string WebhookUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("webhook_secret")]
    public string? WebhookSecret { get; set; }
    
    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = ["completion", "failure"];
}

/// <summary>
/// Response DTO for webhook registration
/// </summary>
public class RegisterWebhookResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
    
    [JsonPropertyName("webhook_id")]
    public string? WebhookId { get; init; }
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
    
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Notification payload for real-time updates
/// </summary>
public class SimulationNotification
{
    public string SimulationId { get; set; } = string.Empty;
    public int MatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
