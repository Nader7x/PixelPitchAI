using System;

namespace Domain.Models;

public class Coach
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Nationality { get; set; }
    public string Role { get; set; }  // Head Coach, Assistant Coach, Goalkeeper Coach, etc.
    public int? TeamId { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string PhotoUrl { get; set; }
    public string PreferredFormation { get; set; }
    public string CoachingStyle { get; set; }  // Attacking, Defensive, Possession, etc.
    
    // Navigation property
    public virtual Team Team { get; set; }
}