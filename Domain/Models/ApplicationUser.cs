using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models;

public sealed class ApplicationUser : IdentityUser
{
    // Custom properties
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; init; } = true;

    // Football-specific preferences
    public int? FavoriteTeamId { get; set; }

    public Team? FavoriteTeam { get; init; }

    // Add this property to the ApplicationUser class
    [MaxLength(50)]
    public string? Gender { get; set; }

    public int Age { get; set; }

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }

    public ICollection<Match> Matches { get; init; } = [];

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; init; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; init; } = new List<Notification>();

    // Helper methods
    public string FullName => $"{FirstName} {LastName}";
}
