using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public sealed class Match
{
    public int Id { get; init; }

    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    public int? HomeTeamSeasonId { get; set; }
    public Season? HomeTeamSeason { get; init; }

    public int? AwayTeamSeasonId { get; set; }
    public Season? AwayTeamSeason { get; init; }

    public string? HomeTeamInMatchName { get; set; }
    public string? AwayTeamInMatchName { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int? StadiumId { get; set; }
    public int? MatchWeek { get; set; }
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }

    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool? IsDraw { get; set; }

    // Match status and simulation tracking
    public string? MatchStatus { get; set; } = "Scheduled"; // Scheduled, PendingSimulation, SimulatingInProgress, Completed, Postponed, Canceled

    public DateTime? ModelSimulationStartTimeUtc { get; set; }

    public bool IsLive { get; set; }

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

    [MaxLength(255)]
    public string? SimulationId { get; set; }

    public ApplicationUser? Creator { get; init; }
    public MatchEvents? MatchEvents { get; set; }
    public MatchStatistics? MatchStatistics { get; set; }
}
