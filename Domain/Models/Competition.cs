namespace Domain.Models;

public class Competition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string? Country { get; set; } = string.Empty;
    public string? Logo { get; set; } = string.Empty;
    public ICollection<Season?>? Seasons { get; set; } = [];
}
