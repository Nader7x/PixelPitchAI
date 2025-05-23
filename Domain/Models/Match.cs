using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public sealed class Match
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
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
    public string MatchStatus { get; set; } =
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
    public string? LastEventPossessingTeamName { get; set; } // Name of the team possessing the ball after the last event

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


    // Audit fields
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Season? Season { get; init; }
    public Team? HomeTeam { get; init; }
    public Team? AwayTeam { get; init; }
    public Stadium? Stadium { get; init; }
    public Coach? HomeCoach { get; init; }
    public Coach? AwayCoach { get; init; }

    /// <summary>
    /// User that created the match.
    /// </summary>

    [MaxLength(255)]
    public required string CreatorId { get; set; }

    public ApplicationUser? Creator { get; set; }
    public MatchEvents? MatchEvents { get; set; }
}

