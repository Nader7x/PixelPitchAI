using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

public class PlayerDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? KnownName { get; set; }
    public string? Nationality { get; set; }
    public string? PreferredFoot { get; set; }
    public string? PhotoUrl { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
}

public class CreatePlayerDto
{
    public string? FullName { get; set; }
    public string? Nationality { get; set; }
    public string? KnownName { get; set; }
    public string PreferredFoot { get; set; }
    public string? PhotoUrl { get; set; }
    public IFormFile? Photo { get; set; }
    public int? TeamId { get; set; }
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
}

public class UpdatePlayerDto
{
    public string? FullName { get; set; }
    public string? KnownName { get; set; }
    public string? Nationality { get; set; }
    public string PreferredFoot { get; set; }
    public string? PhotoUrl { get; set; }
    public IFormFile? Photo { get; set; }
    public int? TeamId { get; set; }
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
}