using System.Text.Json.Serialization;

namespace Application.Dtos;

public class MatchDto
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string SeasonName { get; set; }
    public int HomeTeamId { get; set; }
    public string? HomeTeamName { get; set; }
    public int AwayTeamId { get; set; }
    public string? AwayTeamName { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int? StadiumId { get; set; }
    public string? StadiumName { get; set; }
    public int? MatchWeek { get; set; }
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }

    // Match result data
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool IsDraw { get; set; }

    // Match status and simulation tracking
    public string? MatchStatus { get; set; }

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
    public DateTime? ModelSimulationStartTimeUtc { get; set; }
}

public class CreateMatchDto
{
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }

    [JsonIgnore] public DateTime ScheduledDateTimeUtc { get; set; } = DateTime.UtcNow;

    public int? StadiumId { get; set; }
    public int? HomeSeasonId { get; set; }
    public int? AwaySeasonId { get; set; }

    [JsonIgnore] public string? MatchStatus { get; set; } = "Scheduled";

    public string? CreatorId { get; set; }
}

public class UpdateMatchDto
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public DateTime ScheduledDateTimeUTC { get; set; }
    public int? StadiumId { get; set; }
    public int? MatchWeek { get; set; }
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }

    // Match result data
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool IsDraw { get; set; }

    // Match status and simulation tracking
    public string? MatchStatus { get; set; }

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
}

public class MatchDetailsDto
{
    public int Id { get; set; }
    public SeasonDto? HomeTeamSeason { get; set; }
    public SeasonDto? AwayTeamSeason { get; set; }
    public TeamDto? HomeTeam { get; set; }
    public TeamDto? AwayTeam { get; set; }
    public string? HomeTeamInMatchName { get; set; }
    public string? AwayTeamInMatchName { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public StadiumDto? Stadium { get; set; }
    public int? MatchWeek { get; set; }
    public CoachDto? HomeCoach { get; set; }
    public CoachDto? AwayCoach { get; set; }
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public int? WinningTeamId { get; set; }
    public int? LosingTeamId { get; set; }
    public bool? IsDraw { get; set; }
    public string? MatchStatus { get; set; } = string.Empty;
    public DateTime? ModelSimulationStartTimeUtc { get; set; }

    public int? HomeTeamPossession { get; set; }
    public int? AwayTeamPossession { get; set; }
    public int? HomeTeamShots { get; set; }
    public int? AwayTeamShots { get; set; }
    public int? HomeTeamShotsOnTarget { get; set; }
    public int? AwayTeamShotsOnTarget { get; set; }
    public int? HomeTeamShotsOffTarget { get; set; }
    public int? AwayTeamShotsOffTarget { get; set; }
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
    public int? AwayTeamPasses { get; set; }
    public int? HomeTeamPassesCompleted { set; get; }
    public int? AwayTeamPassesCompleted { set; get; }
    public double? HomeTeamPassAccuracy { get; set; }
    public double? AwayTeamPassAccuracy { get; set; }
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
    public string? CreatorId { get; set; }
    public bool? IsLive { get; set; }
}

public class SimulateMatchDto
{
    public required int HomeTeamId { get; set; }
    public required int AwayTeamId { get; set; }
    public required string HomeTeamName { get; set; }
    public required string AwayTeamName { get; set; }
    public required string HomeTeamSeason { get; set; }
    public required string AwayTeamSeason { get; set; }
    public required int HomeSeasonId { get; set; }
    public required int AwaySeasonId { get; set; }
}