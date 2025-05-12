using System;
using System.Collections.Generic;

namespace Domain.Models;

public class Player
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string KnownName { get; set; }  // e.g., "Messi", "Ronaldo", nickname or commonly used name
    public string Nationality { get; set; }
    public int? ShirtNumber { get; set; }
    public string? PreferredFoot { get; set; }
    public int? TeamId { get; set; }
    public string? PhotoUrl { get; set; }
    public int? StatsBombPlayerId { get; set; }  // External ID for mapping to original StatsBomb data
    
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Team Team { get; set; }
    public virtual ICollection<PlayerSeasonStats> PlayerSeasonStats { get; set; }
}