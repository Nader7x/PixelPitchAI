using Application.CQRS;
using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachesController(
    IFileStorageService fileStorageService,
    ICoachMapper coachMapper,
    ICacheService cacheService
) : ControllerBase
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly ICoachMapper _coachMapper = coachMapper;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly string CONTAINER_NAME = "coaches";

    [HttpGet("filter")]
    [ProducesResponseType(typeof(GetAllCoachesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllCoachesQueryResponse>> GetAllCoaches(
        [FromQuery] string? nationality,
        [FromQuery] int? teamId,
        [FromServices] IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse> handler
    )
    {
        // Generate a cache key based on the query parameters
        var cacheKey = $"coaches_all_{nationality}_{teamId}";

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<GetAllCoachesQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        // Cache miss, fetch from database
        var query = new GetAllCoachesQuery { Nationality = nationality, TeamId = teamId };

        var result = await handler.Handle(query, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetCoachByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetCoachByIdQueryResponse>> GetCoachById(
        int id,
        [FromServices] IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse> handler
    )
    {
        var cacheKey = $"coach_{id}";
        var cachedResult = await _cacheService.GetAsync<GetCoachByIdQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetCoachByIdQuery { Id = id };
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateCoachCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCoachCommandResponse>> CreateCoach(
        [FromForm] CreateCoachDto coachDto,
        [FromServices] IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse> handler
    )
    {
        // Handle file upload if present
        var photoUrl = coachDto.PhotoUrl;
        if (coachDto.Photo != null)
            photoUrl = await _fileStorageService.UploadImageAsync(coachDto.Photo, CONTAINER_NAME);
        coachDto.PhotoUrl = photoUrl;
        var command = _coachMapper.ToCreateCommand(coachDto);

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate coach list caches when creating a new coach
        await InvalidateCoachListCaches();

        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateCoachCommandResponse>> UpdateCoach(
        int id,
        [FromForm] UpdateCoachDto coachDto,
        [FromServices] IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse> getHandler,
        [FromServices] IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse> updateHandler
    )
    {
        // Get existing coach
        var getQuery = new GetCoachByIdQuery { Id = id };
        var existingResult = await getHandler.Handle(getQuery, HttpContext.RequestAborted);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        var photoUrl = coachDto.PhotoUrl;
        if (coachDto.Photo != null)
        {
            if (!string.IsNullOrEmpty(existingResult.Coach.PhotoUrl))
                await _fileStorageService.DeleteImageAsync(
                    existingResult.Coach.PhotoUrl,
                    CONTAINER_NAME
                );

            photoUrl = await _fileStorageService.UploadImageAsync(coachDto.Photo, CONTAINER_NAME);
        }

        var command = _coachMapper.ToUpdateCommand(coachDto);
        command.PhotoUrl = photoUrl;
        command.Id = id;

        var result = await updateHandler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);
            return BadRequest(result);
        }

        await _cacheService.RemoveAsync($"coach_{id}");
        await InvalidateCoachListCaches();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteCoachCommandResponse>> DeleteCoach(
        int id,
        [FromServices] IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse> getHandler,
        [FromServices] IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse> deleteHandler
    )
    {
        var getQuery = new GetCoachByIdQuery { Id = id };
        var existingResult = await getHandler.Handle(getQuery, HttpContext.RequestAborted);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        if (!string.IsNullOrEmpty(existingResult.Coach.PhotoUrl))
            await _fileStorageService.DeleteImageAsync(
                existingResult.Coach.PhotoUrl,
                CONTAINER_NAME
            );

        var command = new DeleteCoachCommand { Id = id };
        var result = await deleteHandler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        await _cacheService.RemoveAsync($"coach_{id}");
        await InvalidateCoachListCaches();

        return Ok(result);
    }

    private async Task InvalidateCoachListCaches()
    {
        await _cacheService.RemoveByPatternAsync("coaches_all_*");
        await _cacheService.RemoveByPatternAsync("coach_*");
        await _cacheService.RemoveByPatternAsync("coaches_*");
        await _cacheService.RemoveByPatternAsync("coaches_by_team_*");
    }
}
