using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StadiumsController(
    IMediator mediator,
    IStadiumMapper stadiumMapper,
    IFileStorageService fileStorageService,
    ICacheService cacheService)
    : ControllerBase
{
    private const string CONTAINER_NAME = "stadiums";
    private readonly ICacheService _cacheService = cacheService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IMediator _mediator = mediator;
    private readonly IStadiumMapper _stadiumMapper = stadiumMapper;


    [HttpGet]
    [ProducesResponseType(typeof(GetAllStadiumsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllStadiumsQueryResponse>> GetAllStadiums(
        [FromQuery] string? country,
        [FromQuery] string? city)
    {
        await _cacheService.RemoveAsync("stadiums_all_*");
        // Generate a cache key based on the query parameters
        var cacheKey = $"stadiums_all_{country}_{city}";

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<GetAllStadiumsQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetAllStadiumsQuery
        {
            Country = country,
            City = city
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
            return BadRequest(result);

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetStadiumByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetStadiumByIdQueryResponse>> GetStadiumById(int id)
    {
        // Try to get from cache first
        var cacheKey = $"stadium_{id}";
        var cachedResult = await _cacheService.GetAsync<GetStadiumByIdQueryResponse>(cacheKey);

        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }

        var query = new GetStadiumByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateStadiumCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateStadiumCommandResponse>> CreateStadium([FromForm] CreateStadiumDto stadiumDto)
    {
        // Handle file upload if present
        var imageUrl = stadiumDto.ImageUrl;
        if (stadiumDto.Image != null)
            imageUrl = await _fileStorageService.UploadImageAsync(stadiumDto.Image, CONTAINER_NAME);

        var command = new CreateStadiumCommand
        {
            Name = stadiumDto.Name,
            City = stadiumDto.City,
            Country = stadiumDto.Country,
            Capacity = stadiumDto.Capacity,
            SurfaceType = stadiumDto.SurfaceType,
            Address = stadiumDto.Address,
            Latitude = stadiumDto.Latitude,
            Longitude = stadiumDto.Longitude,
            ImageUrl = imageUrl,
            Description = stadiumDto.Description,
            Facilities = stadiumDto.Facilities,
            BuiltDate = stadiumDto.BuiltDate
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate stadiums list cache since we've added a new stadium
        await InvalidateStadiumListCaches();

        return CreatedAtAction(nameof(GetStadiumById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateStadiumCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateStadiumCommandResponse>> UpdateStadium(int id,
        [FromForm] UpdateStadiumDto stadiumDto)
    {
        // Get existing stadium to check if we need to replace the image
        var getQuery = new GetStadiumByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Handle file upload if present
        var imageUrl = stadiumDto.ImageUrl;
        if (stadiumDto.Image != null)
        {
            // Delete old image if it exists
            if (!string.IsNullOrEmpty(existingResult.Stadium.ImageUrl))
                await _fileStorageService.DeleteImageAsync(existingResult.Stadium.ImageUrl, CONTAINER_NAME);

            // Upload new image
            imageUrl = await _fileStorageService.UploadImageAsync(stadiumDto.Image, CONTAINER_NAME);
        }

        stadiumDto.ImageUrl = imageUrl;
        var command = _stadiumMapper.ToUpdateCommand(stadiumDto);
        command.Id = id;

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Invalidate both the specific stadium cache and the stadiums list caches
        await _cacheService.RemoveAsync($"stadium_{id}");
        await InvalidateStadiumListCaches();

        return Ok(result);
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteStadiumCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteStadiumCommandResponse>> DeleteStadium(int id)
    {
        // Get existing stadium to delete the image
        var getQuery = new GetStadiumByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Delete image if it exists
        if (!string.IsNullOrEmpty(existingResult.Stadium.ImageUrl))
            await _fileStorageService.DeleteImageAsync(existingResult.Stadium.ImageUrl, CONTAINER_NAME);

        var command = new DeleteStadiumCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        // Invalidate both the specific stadium cache and the stadiums list caches
        await _cacheService.RemoveAsync($"stadium_{id}");
        await InvalidateStadiumListCaches();

        return Ok(result);
    }

    // Helper method to invalidate all stadium list caches
    private async Task InvalidateStadiumListCaches()
    {
        // Using a pattern to match all stadium list cache keys
        await _cacheService.RemoveAsync("stadiums_all_*");
        await _cacheService.RemoveByPatternAsync("stadiums_all_");
        await _cacheService.RemoveByPatternAsync("stadium_*");
        await _cacheService.RemoveByPatternAsync("stadiums_*");
        await _cacheService.RemoveByPatternAsync("stadiums_*_all");
        await _cacheService.RemoveByPatternAsync("stadiums_*_city_*");
    }
}