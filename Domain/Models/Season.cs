namespace Domain.Models;

public sealed class Season
{
    public int Id { get; set; }
    public required string Name { get; set; } // e.g., "La Liga 2023/2024"
    public bool IsActive { get; set; }
    public bool? IsCompleted { get; set; }
    public required string? LeagueName { get; set; } // e.g., "La Liga", "Premier League"
    public required string? Country { get; set; } // e.g., "Spain", "England"
    public int? TotalRounds { get; set; } = 38; // Total number of rounds in the league, default is 38
    public int? CurrentRound { get; set; } // Current active round
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? CompetitionId { get; set; } // Foreign key to Competition

    // Navigation properties
    public ICollection<Match>? Matches { get; set; }
    public ICollection<TeamSeasons>? SeasonTeams { get; set; }
    public Competition? Competition { get; set; } // Required navigation property to Competition
}
