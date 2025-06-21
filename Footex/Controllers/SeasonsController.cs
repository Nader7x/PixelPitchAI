using Application.CQRS.Seasons.Commands;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeasonsController(IMediator mediator, SeasonMapper seasonMapper, ICacheService cacheService)
    : ControllerBase
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly SeasonMapper _seasonMapper = seasonMapper;

    [HttpGet]
    [ProducesResponseType(typeof(GetAllSeasonsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllSeasonsQueryResponse>> GetAllSeasons(
        [FromQuery] string? leagueName,
        [FromQuery] string? country,
        [FromQuery] bool? isActive)
    {
        // Generate a cache key based on the query parameters
        var cacheKey = $"seasons_all_{leagueName}_{country}_{isActive}";

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<GetAllSeasonsQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetAllSeasonsQuery
        {
            LeagueName = leagueName,
            Country = country,
            IsActive = isActive
        };

        var result = await mediator.Send(query);

        if (!result.Succeeded)
            return BadRequest(result);

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetSeasonByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetSeasonByIdQueryResponse>> GetSeasonById(int id)
    {
        // Try to get from cache first
        var cacheKey = $"season_{id}";
        var cachedResult = await _cacheService.GetAsync<GetSeasonByIdQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetSeasonByIdQuery { Id = id };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("SeasonTeams/{id}")]
    [ProducesResponseType(typeof(GetSeasonTeamsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetSeasonTeamsQueryResponse>> GetSeasonTeams(int id)
    {
        // Try to get from cache first
        var cacheKey = $"season_teams_{id}";
        var cachedResult = await _cacheService.GetAsync<GetSeasonTeamsQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetSeasonTeamsQuery { SeasonId = id };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
            return BadRequest(result);

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateSeasonCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateSeasonCommandResponse>> CreateSeason([FromBody] CreateSeasonDto seasonDto)
    {
        var command = _seasonMapper.ToCreateCommand(seasonDto);

        var result = await mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate the seasons list cache since we've added a new season
        await InvalidateSeasonListCaches();
        // Store the new season in cache
        await _cacheService.SetAsync($"season_{result.Id}", result, TimeSpan.FromMinutes(15));
        return CreatedAtAction(nameof(GetSeasonById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateSeasonCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateSeasonCommandResponse>> UpdateSeason(int id,
        [FromBody] UpdateSeasonDto seasonDto)
    {
        var command = _seasonMapper.ToUpdateCommand(seasonDto);
        command.Id = id;

        var result = await mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Invalidate both the specific season cache and season list caches
        await InvalidateSeasonListCaches();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteSeasonCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteSeasonCommandResponse>> DeleteSeason(int id)
    {
        var command = new DeleteSeasonCommand { Id = id };
        var result = await mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Invalidate both the specific season cache and season list caches
        await InvalidateSeasonListCaches();
        

        return Ok(result);
    }
    [NonAction]
    // Helper method to invalidate all season list caches
    private async Task InvalidateSeasonListCaches()
    {
        await _cacheService.RemoveByPatternAsync("seasons_all_*");
        await _cacheService.RemoveByPatternAsync("season_teams_*");
        await _cacheService.RemoveByPatternAsync("season_*");
        await _cacheService.RemoveByPatternAsync("seasons_*");
    }
}