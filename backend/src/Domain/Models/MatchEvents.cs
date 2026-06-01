using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Domain.Models;

public sealed class MatchEvents
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public DateTime LastUpdated { get; set; }

    [MaxLength(1000000)]
    public string EventsJson { get; set; } = "[]"; // JSON string containing all match events

    // Individual event detail fields for queries without deserializing the whole JSON
    public int GoalsHomeTeam { get; set; }
    public int GoalsAwayTeam { get; set; }
    public int TotalEvents { get; set; }
    public int TotalShots { get; set; }
    public int TotalPasses { get; set; }
    public int TotalFouls { get; set; }
    public int TotalCards { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }
    public int TotalOffsides { get; set; }
    public int TotalCorners { get; set; }
    public int TotalSubstitutions { get; set; }
    public int TotalInjuries { get; set; }
    public int TotalPenalties { get; set; }
    public int TotalThrowIns { get; set; }
    public int TotalOuts { get; set; }
    public int TotalGoals { get; set; }
    public int TotalGoalKicks { get; set; }
    public int TotalGoalkeeperSaves { get; set; }
    public int TotalDribbles { get; set; }
    public int TotalPossessionWon { get; set; }
    public int TotalFreeKicks { get; set; }
    public int TotalDuels { get; set; }
    public int TotalErrors { get; set; }
    public int TotalBlocks { get; set; }
    public int TotalClearances { get; set; }
    public int TotalInterceptions { get; set; }

    // Navigation property
    public Match? Match { get; set; }

    // Helper methods for working with the JSON data
    public T? GetEvents<T>()
    {
        return string.IsNullOrEmpty(EventsJson)
            ? default
            : JsonSerializer.Deserialize<T>(EventsJson);
    }

    public void SetEvents<T>(T events)
    {
        EventsJson = JsonSerializer.Serialize(events);
        LastUpdated = DateTime.UtcNow;
    }
}
