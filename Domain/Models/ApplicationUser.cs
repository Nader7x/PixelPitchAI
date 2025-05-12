using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Domain.Models;

public sealed class ApplicationUser : IdentityUser
{
    // Custom properties
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Football-specific preferences
    public int? FavoriteTeamId { get; set; }
    public Team FavoriteTeam { get; set; }
    
    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    // Helper methods
    public string FullName => $"{FirstName} {LastName}";
}
