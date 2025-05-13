using System;
using System.Collections.Generic;

namespace Domain.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Logo { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string League { get; set; }
    public int? StadiumId { get; set; }
    public DateTime FoundationDate { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    
    // Navigation properties
    public virtual Stadium Stadium { get; set; }
    public virtual ICollection<Player> Players { get; set; }
    public virtual ICollection<Coach> Coaches { get; set; }
    public virtual ICollection<TeamSeasonStats> TeamSeasonStats { get; set; }
    public virtual ICollection<PlayerSeasonStats> PlayerSeasonStats { get; set; }
    public virtual ICollection<Match> HomeMatches { get; set; }
    public virtual ICollection<Match> AwayMatches { get; set; }
}