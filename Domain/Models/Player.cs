namespace Domain.Models;



public sealed class Player
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? KnownName { get; set; }  // e.g., "Messi", "Ronaldo", nickname or commonly used name
    public string? Nationality { get; set; }
    public int? ShirtNumber { get; set; }
    public string? PreferredFoot { get; set; }
    public int? TeamId { get; set; }
    public string? PhotoUrl { get; set; }
    public  string? Position { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Team? Team { get; set; }
}