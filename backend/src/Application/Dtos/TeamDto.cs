using Microsoft.AspNetCore.Http;

namespace Application.Dtos;

public class TeamDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Logo { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? League { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public DateTime? FoundationDate { get; set; }
    public string? StadiumName { get; set; }
}

public class CreateTeamDto
{
    public string? Name { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public string? ShortName { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public string? Logo { get; set; } = string.Empty;
    public int? StadiumId { get; set; }
    public DateTime FoundationDate { get; set; }
    public int? CoachId { get; set; }
}

public class UpdateTeamDto
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public int? StadiumId { get; set; }
    public string? City { get; set; } = string.Empty;
    public DateTime FoundationDate { get; set; }
    public IFormFile? Image { get; set; }
    public string? Logo { get; set; } = string.Empty;
    public string? ShortName { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; } = string.Empty;
    public string? SecondaryColor { get; set; } = string.Empty;
    public int? CoachId { get; set; }
}

public class TeamSeasonsDto
{
    public int SeasonId { get; set; }
    public string? SeasonName { get; set; }
}
