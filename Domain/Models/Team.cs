namespace Domain.Models;

public sealed class Team
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Logo { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? League { get; set; }
    public int? StadiumId { get; set; }
    public DateTime FoundationDate { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }

    // Navigation properties
    public Stadium? Stadium { get; set; }
    public ICollection<Player>? Players { get; set; }
    public ICollection<Coach>? Coaches { get; set; }
    public ICollection<TeamSeasons>? TeamSeasons { get; set; }
    public ICollection<Match>? HomeMatches { get; set; }
    public ICollection<Match>? AwayMatches { get; set; }
}