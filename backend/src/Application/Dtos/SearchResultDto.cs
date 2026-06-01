namespace Application.DTOs;

public class SearchResultDto
{
    public int TotalResults { get; init; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int PageSize { get; init; }
    public List<SearchItemDto> Items { get; init; } = [];
    public string? Error { get; init; }
}

public class SearchItemDto
{
    public string? Id { get; init; }
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string?> AdditionalData { get; set; } = new();
}
