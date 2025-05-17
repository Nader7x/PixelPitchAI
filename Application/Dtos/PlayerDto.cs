using System;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

public class PlayerDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string? Nationality { get; set; }
    public string? PreferredFoot { get; set; }
    public string? PhotoUrl { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? ShirtNumber { get; set; }
    public int? StatsBombPlayerId { get; set; }
}

public class CreatePlayerDto
{
    public string FullName { get; set; }
    public string? Nationality { get; set; }
    public string PreferredFoot { get; set; }
    public string? PhotoUrl { get; set; }
    public IFormFile ? Photo { get; set; }
    public int? TeamId { get; set; }
    public int? ShirtNumber { get; set; }
    public int? StatsBombPlayerId { get; set; }
}

public class UpdatePlayerDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string? Nationality { get; set; }
    public string PreferredFoot { get; set; }
    public string PhotoUrl { get; set; }
    public IFormFile ? Photo { get; set; }
    public int? TeamId { get; set; }
    public int? ShirtNumber { get; set; }
    public int? StatsBombPlayerId { get; set; }
}
