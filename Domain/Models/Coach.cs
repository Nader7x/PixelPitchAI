using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

public sealed class Coach
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; }
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Nationality { get; set; }
    
    [Required]
    [StringLength(50)]
    public string? Role { get; set; }
    
    public int? YearsOfExperience { get; set; }
    
    public string? PhotoUrl { get; set; }
    
    [StringLength(500)]
    public string? Biography { get; set; }
    public string? PreferredFormation { get; set; }
    public string? CoachingStyle { get; set; }
    
    // Foreign keys
    public int? TeamId { get; set; }
    
    // Navigation properties
    public Team? Team { get; set; }
}
