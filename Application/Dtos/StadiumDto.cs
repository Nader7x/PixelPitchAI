using System;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

public class StadiumDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int Capacity { get; set; }
    public string? SurfaceType { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Facilities { get; set; }
    public DateTime? BuiltDate { get; set; }
}

public class CreateStadiumDto
{
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int Capacity { get; set; }
    public string? SurfaceType { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile Image { get; set; }
    public string? Description { get; set; }
    public string? Facilities { get; set; }
    public DateTime BuiltDate { get; set; }
}

public class UpdateStadiumDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int Capacity { get; set; }
    public string? SurfaceType { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public IFormFile? Image { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Facilities { get; set; }
    public DateTime BuiltDate { get; set; }
}
