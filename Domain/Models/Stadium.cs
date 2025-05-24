using System;
using System.Collections.Generic;

namespace Domain.Models;

public sealed class Stadium
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int Capacity { get; set; }
    public DateTime? BuiltDate { get; set; }
    public DateTime? LastRenovation { get; set; }
    public string? SurfaceType { get; set; }  // Grass, Artificial, Hybrid
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool HasRoof { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Facilities { get; set; }  // JSON string containing available facilities
    public double? CostMillionsEuros { get; set; }  // Cost of the stadium in millions of euros
    public string? Nickname { get; set; }  // Nickname of the stadium, if any
    public string? Architect { get; set; }  // Architect or firm that designed the stadium
    
    // Navigation properties
    public ICollection<Team> Teams { get; set; }
}
