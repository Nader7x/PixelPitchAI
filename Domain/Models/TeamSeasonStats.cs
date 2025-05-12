using System;

namespace Domain.Models;

public class TeamSeasonStats
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int SeasonId { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public int GoalDifference { get; set; } // Stored property
    public int CleanSheets { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int Points { get; set; }
    public int Position { get; set; }
    public string Form { get; set; }  // e.g., "WWLDW", last 5 matches
    
    // Additional statistics
    public decimal AveragePossession { get; set; }
    public decimal PassAccuracy { get; set; }
    public int Shots { get; set; }
    public int ShotsOnTarget { get; set; }
    public decimal ConversionRate { get; set; }  // Goals / Shots on target
    public int Corners { get; set; }
    public int Fouls { get; set; }
    public decimal ExpectedGoals { get; set; }  // xG statistic
    public decimal ExpectedGoalsAgainst { get; set; }  // xGA statistic
    
    // Home/Away splits
    public int HomeWins { get; set; }
    public int HomeDraws { get; set; }
    public int HomeLosses { get; set; }
    public int HomeGoalsScored { get; set; }
    public int HomeGoalsConceded { get; set; }
    public int AwayWins { get; set; }
    public int AwayDraws { get; set; }
    public int AwayLosses { get; set; }
    public int AwayGoalsScored { get; set; }
    public int AwayGoalsConceded { get; set; }
    
    // Audit fields
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Team Team { get; set; }
    public virtual Season Season { get; set; }
    
    // Method to update calculated properties
    public void UpdateCalculatedProperties()
    {
        GoalDifference = GoalsScored - GoalsConceded;
    }
}