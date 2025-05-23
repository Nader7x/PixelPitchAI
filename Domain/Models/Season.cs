namespace Domain.Models;

public class Season
{
    public int Id { get; set; }
    public string Name { get; set; }  // e.g., "La Liga 2023/2024"
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public string LeagueName { get; set; }  // e.g., "La Liga", "Premier League"
    public string Country { get; set; }  // e.g., "Spain", "England"
    public int TotalRounds { get; set; } = 38;  // Total number of rounds in the league, default is 38
    public int CurrentRound { get; set; }  // Current active round
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Navigation properties
    public virtual ICollection<Match>? Matches { get; set; }
    public virtual ICollection<TeamSeasons> SeasonTeams { get; set; }
}
