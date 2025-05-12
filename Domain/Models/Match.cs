using System;

namespace Domain.Models;

public class Match
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public DateTime ScheduledDateTimeUTC { get; set; }
    public int? StadiumId { get; set; }
    public int? MatchWeek { get; set; }  // e.g., 1, 2, 3... for league progression
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }
    
    // Match result data
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool IsDraw { get; set; }
    
    // Match status and simulation tracking
    public string MatchStatus { get; set; } = "Scheduled";  // Scheduled, PendingSimulation, SimulatingInProgress, Completed, Postponed, Cancelled
    public DateTime? ModelSimulationStartTimeUTC { get; set; }
    
    // Match statistics
    public int? HomeTeamPossession { get; set; }
    public int? AwayTeamPossession { get; set; }
    public int? HomeTeamShots { get; set; }
    public int? AwayTeamShots { get; set; }
    public int? HomeTeamShotsOnTarget { get; set; }
    public int? AwayTeamShotsOnTarget { get; set; }
    public int? HomeTeamCorners { get; set; }
    public int? AwayTeamCorners { get; set; }
    public int? HomeTeamFouls { get; set; }
    public int? AwayTeamFouls { get; set; }
    public int? HomeTeamYellowCards { get; set; }
    public int? AwayTeamYellowCards { get; set; }
    public int? HomeTeamRedCards { get; set; }
    public int? AwayTeamRedCards { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Season Season { get; set; }
    public virtual Team HomeTeam { get; set; }
    public virtual Team AwayTeam { get; set; }
    public virtual Stadium Stadium { get; set; }
    public virtual Coach HomeCoach { get; set; }
    public virtual Coach AwayCoach { get; set; }
    public virtual MatchEvents MatchEvents { get; set; }
}