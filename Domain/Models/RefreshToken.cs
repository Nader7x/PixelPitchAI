using System;
using System;

namespace Domain.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public required string Token { get; set; }
    public string? JwtId { get; set; }
    public string? UserId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Expires { get; set; }
    public DateTime? Revoked { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => Revoked == null && !IsExpired;
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
