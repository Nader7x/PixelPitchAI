using Application.CQRS;
using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(
    ITeamMapper teamMapper,
    IFileStorageService azureBlobStorageService,
    ICacheService cacheService
) : ControllerBase
{
    private readonly IFileStorageService _azureBlobStorageService = azureBlobStorageService;
    private readonly ICacheService _cacheService = cacheService;
    private readonly ITeamMapper _teamMapper = teamMapper;
    private readonly string CONTAINER_NAME = "teams";

    /// <summary>
    ///     Get all teams
    /// </summary>
    /// <returns>List of teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTeamsQueryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllTeamsQueryResponse>> GetAll(
        [FromServices] IRequestHandler<GetAllTeamsQuery, GetAllTeamsQueryResponse> handler
    )
    {
        // Try to get from the cache first
        const string cacheKey = "teams_all";
        var cachedResult = await _cacheService.GetAsync<GetAllTeamsQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        // Cache miss, fetch from a database
        var query = new GetAllTeamsQuery();
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        // Store in a cache if successful
        if (result.Succeeded)
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    /// <summary>
    ///     Get team by ID
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>Team details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetTeamByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTeamByIdQueryResponse>> GetById(
        int id,
        [FromServices] IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse> handler
    )
    {
        // Try to get from cache first
        var cacheKey = $"team_{id}";
        var cachedResult = await _cacheService.GetAsync<GetTeamByIdQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        // Cache miss, fetch from database
        var query = new GetTeamByIdQuery { Id = id };
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        // Store in cache if successful
        if (result is { Succeeded: true, Team: not null })
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);
            return BadRequest(result.Error);
        }
        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    /// <summary>
    ///     Create a new team
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Created team</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateTeamCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateTeamCommandResponse>> Create(
        [FromForm] CreateTeamDto dto,
        [FromServices] IRequestHandler<CreateTeamCommand, CreateTeamCommandResponse> handler
    )
    {
        if (dto.Image != null)
        {
            var imageUrl = await _azureBlobStorageService.UploadImageAsync(
                dto.Image,
                CONTAINER_NAME
            );
            dto.Logo = imageUrl;
        }

        var command = _teamMapper.ToCreateCommand(dto);
        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate the teams list cache since we've added a new team
        await _cacheService.RemoveAsync("teams_all");

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    ///     Update an existing team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="dto">Team update data</param>
    /// <returns>Updated team</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateTeamCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTeamCommandResponse>> Update(
        int id,
        [FromForm] UpdateTeamDto dto,
        [FromServices] IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse> getHandler,
        [FromServices] IRequestHandler<UpdateTeamCommand, UpdateTeamCommandResponse> updateHandler
    )
    {
        dto.Id = id;
        if (dto.Image != null)
        {
            var existingTeam = await getHandler.Handle(new GetTeamByIdQuery { Id = id }, HttpContext.RequestAborted);
            if (
                existingTeam is { Succeeded: true, Team: not null }
                && !string.IsNullOrEmpty(existingTeam.Team.Logo)
            )
                await _azureBlobStorageService.DeleteImageAsync(
                    existingTeam.Team.Logo,
                    CONTAINER_NAME
                );
            dto.Logo = await _azureBlobStorageService.UploadImageAsync(dto.Image, CONTAINER_NAME);
        }

        var command = _teamMapper.ToUpdateCommand(dto);
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await updateHandler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return result.NotFound ? NotFound() : BadRequest(result);

        // Invalidate both the specific team cache and the all-teams cache
        await InvalidateTeamCaches();

        return Ok(result);
    }

    /// <summary>
    ///     Delete a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>Operation result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] IRequestHandler<DeleteTeamCommand, DeleteTeamCommandResponse> handler
    )
    {
        var command = new DeleteTeamCommand { Id = id };
        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return NotFound(result.Error);

        // Invalidate both the specific team cache and the all teams cache
        await InvalidateTeamCaches();

        return NoContent();
    }

    [HttpGet("Seasons/{id:int}")]
    [ProducesResponseType(typeof(GetTeamSeasonsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTeamSeasonsQueryResponse>> GetTeamSeasons(
        int id,
        [FromServices] IRequestHandler<GetTeamSeasonsQuery, GetTeamSeasonsQueryResponse> handler
    )
    {
        var query = new GetTeamSeasonsQuery { TeamId = id };
        var result = await handler.Handle(query, HttpContext.RequestAborted);
        if (!result.Succeeded)
            return NotFound(result);
        return Ok(result);
    }

    [NonAction]
    private async Task InvalidateTeamCaches()
    {
        // Invalidate the teams list cache
        await _cacheService.RemoveAsync("teams_all");
        // Invalidate all individual team caches
        await _cacheService.RemoveByPatternAsync("team_*");
        // Optionally, you can also invalidate team seasons cache if needed
        await _cacheService.RemoveByPatternAsync("team_seasons_*");
        // You can add more patterns if you have other related caches
        await _cacheService.RemoveByPatternAsync("team_players_*");
    }
}
