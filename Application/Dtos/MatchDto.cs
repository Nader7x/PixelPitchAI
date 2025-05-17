using System;

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
    public DateTime ScheduledDateTimeUTC { get; set; }
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
    public string MatchStatus { get; set; }
    
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

public class CreateMatchDto
{
    public int SeasonId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public DateTime ScheduledDateTimeUTC { get; set; }
    public int? StadiumId { get; set; }
    public int? MatchWeek { get; set; }
    public int? HomeCoachId { get; set; }
    public int? AwayCoachId { get; set; }
    public string MatchStatus { get; set; } = "Scheduled";
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
    public string MatchStatus { get; set; }
    
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
