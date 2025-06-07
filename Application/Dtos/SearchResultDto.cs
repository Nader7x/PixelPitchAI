namespace Application.DTOs;

public class SearchResultDto
{
    public int TotalResults { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public List<SearchItemDto> Items { get; set; } = [];
    public string? Error { get; set; } // For error messages or exceptions during search
}

public class SearchItemDto
{
    public string? Id { get; set; }
    public string? Type { get; set; } // "Team", "Player", "Match", "Coach", etc.
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Url { get; set; } // Frontend URL to navigate to this item
    public Dictionary<string, string?> AdditionalData { get; set; } = new();
}