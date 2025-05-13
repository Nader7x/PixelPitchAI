using System;
using System.Collections.Generic;

namespace Domain.Models;

public class Season
{
    public int Id { get; set; }
    public string Name { get; set; }  // e.g., "La Liga 2023/2024"
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public string LeagueName { get; set; }  // e.g., "La Liga", "Premier League"
    public string Country { get; set; }  // e.g., "Spain", "England"
    public int TotalRounds { get; set; }  // Total number of rounds in the league
    public int CurrentRound { get; set; }  // Current active round
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Match> Matches { get; set; }
    public virtual ICollection<TeamSeasonStats> TeamSeasonStats { get; set; }
    public virtual ICollection<PlayerSeasonStats> PlayerSeasonStats { get; set; }
}