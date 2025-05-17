using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Search across teams, matches, players, and coaches
    /// </summary>
    /// <param name="query">Search term (minimum 2 characters)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Search results with pagination information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto), 200)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest("Search query must be at least 2 characters long");
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 50)
        {
            pageSize = 10;
        }

        var results = await _searchService.SearchAsync(query, page, pageSize);
        return Ok(results);
    }
}
