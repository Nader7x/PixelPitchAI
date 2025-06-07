namespace Application.Dtos;

public class PreloadMatchesRequest
{
    public IEnumerable<string> MatchIds { get; set; } = new List<string>();
}