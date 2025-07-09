namespace Domain.Models;

public sealed class TeamSeason
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int SeasonId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Team Team { get; set; }
    public Season Season { get; set; }
}
