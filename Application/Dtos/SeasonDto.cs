using System;
using System;
using System.Collections.Generic;

namespace Application.Dtos;

public class SeasonDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LeagueName { get; set; }
    public string Country { get; set; }
    public int CurrentRound { get; set; }
    public int TotalRounds { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MatchCount { get; set; }
    public List<TeamStandingDto> TeamStandings { get; set; }
}

public class CreateSeasonDto
{
    public string Name { get; set; }
    public string LeagueName { get; set; }
    public string Country { get; set; }
    public int TotalRounds { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateSeasonDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LeagueName { get; set; }
    public string Country { get; set; }
    public int? CurrentRound { get; set; }
    public int TotalRounds { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TeamStandingDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; }
    public int Position { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }
    public int Points { get; set; }
}

