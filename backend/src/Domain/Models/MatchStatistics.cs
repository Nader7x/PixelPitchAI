using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class MatchStatistics
{
    public int Id { get; init; }
    public required int MatchId { get; set; }

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
    public long? HomeTeamPossessionDurationSeconds { get; set; }
    public long? AwayTeamPossessionDurationSeconds { get; set; }
    public int? LastEventTimestampSeconds { get; set; }

    [MaxLength(500)]
    public string? LastEventPossessingTeamName { get; set; }

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

    public Match? Match { get; set; }
}
