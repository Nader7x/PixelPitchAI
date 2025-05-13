namespace Domain.Models;

public sealed class PlayerSeasonStats
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int SeasonId { get; set; }
    public int? TeamId { get; set; }
    public int Appearances { get; set; }
    public int MinutesPlayed { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int CleanSheets { get; set; }  // For goalkeepers
    public int Saves { get; set; }  // For goalkeepers
    public int Tackles { get; set; }
    public int Interceptions { get; set; }
    public int Clearances { get; set; }
    public int BlockedShots { get; set; }
    public int PassesCompleted { get; set; }
    public int PassesAttempted { get; set; }
    public decimal PassCompletionRate { get; set; }
    public int KeyPasses { get; set; }
    public int ChancesCreated { get; set; }
    public int ShotsOnTarget { get; set; }
    public int ShotsOffTarget { get; set; }
    public int? Rating { get; set; }  // Average rating out of 10
    
    // Navigation properties
    public Player Player { get; set; }
    public Season Season { get; set; }
    public Team Team { get; set; }
}