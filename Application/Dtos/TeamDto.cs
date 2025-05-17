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
}

public class CreateTeamDto
{
    public string? Name { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string LeagueName { get; set; } = string.Empty;
    public int? StadiumId { get; set; }
    public DateTime Founded { get; set; }
}

public class UpdateTeamDto
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string LeagueName { get; set; } = string.Empty;
    public int? StadiumId { get; set; }
    public DateTime Founded { get; set; }
}