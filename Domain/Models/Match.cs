using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Domain.Models;

public sealed class Match
{
    public Match()
    {
    }

    [SetsRequiredMembers]
    public Match(int id, string creatorId)
    {
        Id = id;
        CreatorId = creatorId;
    }

    public int Id { get; init; }

    // Renamed from MatchSeasonId - represents the primary season of the match event itself (e.g., a tournament season)
    // Can be nullable if a match doesn't belong to an overarching season.
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    // Season the Home Team is associated with for this match context (e.g., their league season)
    public int? HomeTeamSeasonId { get; set; }
    public Season? HomeTeamSeason { get; init; }

    // Season the Away Team is associated with for this match context (e.g., their league season)
    public int? AwayTeamSeasonId { get; set; }
    public Season? AwayTeamSeason { get; init; }

    public string? HomeTeamInMatchName { get; set; }
    public string? AwayTeamInMatchName { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int? StadiumId { get; set; }
    public int? MatchWeek { get; set; } // e.g., 1, 2, 3... for league progression
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }

    // Match result data
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool? IsDraw { get; set; }

    // Match status and simulation tracking
    public string? MatchStatus { get; set; } =
        "Scheduled"; // Scheduled, PendingSimulation, SimulatingInProgress, Completed, Postponed, Cancelled

    public DateTime? ModelSimulationStartTimeUtc { get; set; }

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
    public int? HomeTeamOffsides { get; set; }
    public int? AwayTeamOffsides { get; set; }
    public int? HomeTeamPasses { get; set; }
    public int? HomeTeamPassesCompleted { set; get; }
    public int? AwayTeamPassesCompleted { set; get; }

    public int? AwayTeamPasses { get; set; }
    public double? HomeTeamPassAccuracy { get; set; }
    public double? AwayTeamPassAccuracy { get; set; }

    // New fields for possession calculation
    public long? HomeTeamPossessionDurationSeconds { get; set; } // Total seconds home team had possession
    public long? AwayTeamPossessionDurationSeconds { get; set; } // Total seconds away team had possession
    public int? LastEventTimestampSeconds { get; set; } // Timestamp of the last processed event

    [MaxLength(500)]
    public string?
        LastEventPossessingTeamName { get; set; } // Name of the team possessing the ball after the last event

    public int? HomeTeamDribbles { get; set; }
    public int? AwayTeamDribbles { get; set; }
    public int? HomeTeamSaves { get; set; }
    public int? AwayTeamSaves { get; set; }
    public int? HomeTeamDuels { get; set; }
    public int? AwayTeamDuels { get; set; }
    public int? HomeTeamDuelsWon { get; set; }
    public int? AwayTeamDuelsWon { get; set; }
    public int? HomeTeamClearances { get; set; }
    public int? AwayTeamClearances { get; set; }
    public int? HomeTeamPossessionWon { get; set; }
    public int? AwayTeamPossessionWon { get; set; }
    public int? HomeTeamRecoveries { get; set; }
    public int? AwayTeamRecoveries { get; set; }
    public int? HomeTeamGoalKicks { get; set; }
    public int? AwayTeamGoalKicks { get; set; }
    public int? HomeLongBalls { get; set; }
    public int? AwayLongBalls { get; set; }
    public int? HomeAccurateLongBalls { get; set; }
    public int? AwayAccurateLongBalls { get; set; }
    public double? HomeTeamLongBallsAccuracy { get; set; }
    public double? AwayTeamLongBallsAccuracy { get; set; }
    public int? HomeTeamFreeKicks { get; set; }
    public int? AwayTeamFreeKicks { get; set; }
    public int? AwayTeamShotsOffTarget { get; set; }
    public int? HomeTeamShotsOffTarget { get; set; }

    // Monitoring Field
    public bool IsLive { get; set; } = false; // Indicates if the match is currently live or not


    // Audit fields
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Team? HomeTeam { get; init; }
    public Team? AwayTeam { get; init; }
    public Stadium? Stadium { get; init; }
    public Coach? HomeCoach { get; init; }
    public Coach? AwayCoach { get; init; }

    /// <summary>
    ///     User that created the match.
    /// </summary>

    [MaxLength(255)]
    public required string CreatorId { get; set; }

    [MaxLength(255)] public string? SimulationId { get; set; } // ID for the simulation process, if applicable

    public ApplicationUser? Creator { get; init; }
    public MatchEvents? MatchEvents { get; set; }

    public void ResetStatistics()
    {
        HomeTeamPossession = 0;
        AwayTeamPossession = 0;
        HomeTeamShots = 0;
        AwayTeamShots = 0;
        HomeTeamShotsOnTarget = 0;
        AwayTeamShotsOnTarget = 0;
        HomeTeamCorners = 0;
        AwayTeamCorners = 0;
        HomeTeamFouls = 0;
        AwayTeamFouls = 0;
        HomeTeamYellowCards = 0;
        AwayTeamYellowCards = 0;
        HomeTeamRedCards = 0;
        AwayTeamRedCards = 0;
        HomeTeamOffsides = 0;
        AwayTeamOffsides = 0;
        HomeTeamPasses = 0;
        HomeTeamPassesCompleted = 0;
        AwayTeamPassesCompleted = 0;
        AwayTeamPasses = 0;
        
        // Reset possession tracking
        HomeTeamPossessionDurationSeconds = 0;
        AwayTeamPossessionDurationSeconds = 0;
        LastEventTimestampSeconds = null; // Reset to null to indicate no events processed yet
        LastEventPossessingTeamName = null;

        // Reset other statistics
        HomeTeamDribbles = 0;
        AwayTeamDribbles = 0;
        HomeTeamSaves = 0;
        AwayTeamSaves = 0;
        HomeTeamDuels = 0;
        AwayTeamDuels = 0;
        HomeTeamDuelsWon = 0;
        AwayTeamDuelsWon = 0;
        HomeTeamClearances = 0;
        AwayTeamClearances = 0;
        HomeTeamPossessionWon = 0;
        AwayTeamPossessionWon = 0;
        HomeTeamRecoveries = 0;
        AwayTeamRecoveries = 0;
        HomeLongBalls = 0;
        AwayLongBalls = 0;
        HomeAccurateLongBalls = 0;
        AwayAccurateLongBalls = 0;

        // Reset long ball accuracy
        HomeTeamLongBallsAccuracy = null; // Reset to null to indicate no long balls processed yet
        AwayTeamLongBallsAccuracy =
            null; // Reset to null to indicate no long balls processed yet

        HomeTeamFreeKicks = 0; // Reset free kicks
        AwayTeamFreeKicks =
            0; // Reset free kicks

        // Reset possession tracking
        HomeTeamPossessionDurationSeconds = 0;
        AwayTeamPossessionDurationSeconds = 0;
        LastEventTimestampSeconds = null; // Reset to null to indicate no events processed yet
        LastEventPossessingTeamName = null;

        // Reset other statistics
        HomeTeamDribbles = 0;
        AwayTeamDribbles = 0;
        HomeTeamSaves = 0;
        AwayTeamSaves = 0;
        HomeTeamDuels = 0;
        AwayTeamDuels = 0;
        HomeTeamDuelsWon = 0;
        AwayTeamDuelsWon = 0;
        HomeTeamClearances = 0;
        AwayTeamClearances = 0;
        HomeTeamPossessionWon = 0;
        AwayTeamPossessionWon = 0;
        HomeTeamRecoveries = 0;
        AwayTeamRecoveries = 0;
        HomeLongBalls = 0;
        AwayLongBalls = 0;
        HomeAccurateLongBalls = 0;
        AwayAccurateLongBalls = 0;

        // Reset long ball accuracy
        HomeTeamLongBallsAccuracy = null; // Reset to null to indicate no long balls processed yet
        AwayTeamLongBallsAccuracy = null; // Reset to null to indicate no long balls processed yet

        HomeTeamFreeKicks = 0; // Reset free kicks
        AwayTeamFreeKicks = 0; // Reset free kicks
        AwayTeamShotsOffTarget = 0;
        HomeTeamShotsOffTarget = 0; // Reset shots off target
        IsLive = false; // Reset live status
        MatchStatus = "Scheduled"; // Reset match status to scheduled

        WinningTeamId = null; // Reset winning team
        LosingTeamId = null; // Reset losing team
        IsDraw = null; // Reset draw status
        MatchEvents = null; // Reset match events
        // reset scores
        HomeTeamScore = null;
        AwayTeamScore = null;
    }
}