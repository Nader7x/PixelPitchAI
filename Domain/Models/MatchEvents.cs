using System.Text.Json;

namespace Domain.Models;

public sealed class MatchEvents
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public DateTime LastUpdated { get; set; }
    public string EventsJson { get; set; } // JSON string containing all match events

    // Individual event detail fields for queries without deserializing the whole JSON
    public int GoalsHomeTeam { get; set; }
    public int GoalsAwayTeam { get; set; }
    public int TotalEvents { get; set; }
    public int TotalShots { get; set; }
    public int TotalPasses { get; set; }
    public int TotalFouls { get; set; }
    public int TotalCards { get; set; }
    public int TotalCorners { get; set; }

    // Navigation property
    public Match? Match { get; set; }

    // Helper methods for working with the JSON data
    public T? GetEvents<T>()
    {
        if (string.IsNullOrEmpty(EventsJson))
            return default;

        return JsonSerializer.Deserialize<T>(EventsJson);
    }

    public void SetEvents<T>(T events)
    {
        EventsJson = JsonSerializer.Serialize(events);
        LastUpdated = DateTime.UtcNow;
    }
}