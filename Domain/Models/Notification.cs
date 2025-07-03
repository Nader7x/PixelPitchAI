using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Models;

public class Notification
{
    [Key]
    [MaxLength(255)]
    public string Id { get; init; }

    [MaxLength(255)]
    public required string Title { get; set; }

    [MaxLength(512)]
    public required string Content { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required NotificationType Type { get; set; }

    public DateTime Time { get; init; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    [MaxLength(70)]
    public required string UserId { get; set; }
}

// This class represents a notification in the system.
public enum NotificationType
{
    MatchStart,
    MatchEnd,
    SimulationStart,
    SimulationEnd,
    MatchUpdate,
    SimulationUpdate,
    SystemAlert,
    UserMessage,
    Info,
    Warning,
    Error,
    Success,
}
