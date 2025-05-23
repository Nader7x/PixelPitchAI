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


