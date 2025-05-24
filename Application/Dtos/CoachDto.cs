using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

public class CoachDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string Role { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string PhotoUrl { get; set; }
    public string? PreferredFormation { get; set; }
    public string? CoachingStyle { get; set; }
    public string Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class CreateCoachDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public int? TeamId { get; set; }
    public string? PhotoUrl { get; set; }
    public IFormFile? Photo { get; set; }
    public string? PreferredFormation { get; set; }
    public string? CoachingStyle { get; set; }
    public string? Role { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Biography { get; set; }
}

public class UpdateCoachDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public int? TeamId { get; set; } 
    public string? PhotoUrl { get; set; }
    public IFormFile? Photo { get; set; }
    public string? PreferredFormation { get; set; }
    public string? CoachingStyle { get; set; }
    public string? Role { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Biography { get; set; }
}
