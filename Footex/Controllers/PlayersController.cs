using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(
    IMediator mediator, 
    IFileStorageService fileStorageService, 
    PlayerMapper playerMapper,
    ICacheService cacheService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly PlayerMapper _playerMapper = playerMapper;
    private readonly ICacheService _cacheService = cacheService;
    private string CONTAINER_NAME = "players";

    [HttpGet]
    [ProducesResponseType(typeof(GetAllPlayersQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllPlayersQueryResponse>> GetAllPlayers(
        [FromQuery] string? nationality,
        [FromQuery] string? preferredFoot,
        [FromQuery] int? teamId, 
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize
        )
    {
        // Generate a cache key based on the query parameters
        string cacheKey = $"players_all_{nationality}_{preferredFoot}_{teamId}_{pageNumber}_{pageSize}";
        
        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<GetAllPlayersQueryResponse>(cacheKey);
        
        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }
        
        // Cache miss, fetch from database
        var query = new GetAllPlayersQuery
        {
            Nationality = nationality,
            PreferredFoot = preferredFoot,
            TeamId = teamId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
        
        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        
        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetPlayerByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetPlayerByIdQueryResponse>> GetPlayerById(int id)
    {
        // Try to get from cache first
        var cacheKey = $"player_{id}";
        var cachedResult = await _cacheService.GetAsync<GetPlayerByIdQueryResponse>(cacheKey);
        
        if (cachedResult != null)
        {
            Response.Headers.Append("X-Cache-Hit", "true");
            return Ok(cachedResult);
        }
        
        // Cache miss, fetch from database
        var query = new GetPlayerByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);
                
            return BadRequest(result);
        }
        
        // Store in cache if successful
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        
        Response.Headers.Append("X-Cache-Hit", "false");
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreatePlayerCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePlayerCommandResponse>> CreatePlayer([FromForm] CreatePlayerDto playerDto)
    {
    
        // Handle file upload if present
        string? photoUrl = playerDto.PhotoUrl;
        if (playerDto.Photo != null)
        {
            photoUrl = await _fileStorageService.UploadImageAsync(playerDto.Photo, CONTAINER_NAME);
        }
        playerDto.PhotoUrl = photoUrl;

        var command = _playerMapper.ToCreateCommand(playerDto);

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        // Invalidate players listing cache since we've added a new player
        await InvalidatePlayerListCaches();
        
        return CreatedAtAction(nameof(GetPlayerById), new { id = result.Id }, result);
    }

    
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdatePlayerCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdatePlayerCommandResponse>> UpdatePlayer(int id, [FromForm] UpdatePlayerDto playerDto)
    {
        // Get existing player
        var getQuery = new GetPlayerByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Handle file upload if present
        if (playerDto.Photo != null)
        {
            // Delete old photo if it exists
            if (!string.IsNullOrEmpty(existingResult.Player.PhotoUrl))
            {
                await _fileStorageService.DeleteImageAsync(existingResult.Player.PhotoUrl, CONTAINER_NAME);
            }

            // Upload new photo
            playerDto.PhotoUrl = await _fileStorageService.UploadImageAsync(playerDto.Photo, CONTAINER_NAME);
        }

        var command = _playerMapper.ToUpdateCommand(playerDto);
        command.Id = id;

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }
        
        // Invalidate both the specific player cache and the player lists
        await _cacheService.RemoveAsync($"player_{id}");
        await InvalidatePlayerListCaches();

        return Ok(result);
    }

    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeletePlayerCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeletePlayerCommandResponse>> DeletePlayer(int id)
    {
        // Get existing player to delete the image
        var getQuery = new GetPlayerByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Delete photo if it exists
        if (!string.IsNullOrEmpty(existingResult.Player.PhotoUrl))
        {
            await _fileStorageService.DeleteImageAsync(existingResult.Player.PhotoUrl, CONTAINER_NAME);
        }

        var command = new DeletePlayerCommand { Id = id };
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }
        
        // Invalidate both the specific player cache and the player lists
        await _cacheService.RemoveAsync($"player_{id}");
        await InvalidatePlayerListCaches();

        return Ok(result);
    }
    
    // Helper method to invalidate all player list caches
    // Since we could have multiple filtered versions of the player list cached
    private async Task InvalidatePlayerListCaches()
    {
        // Simple implementation: use a wildcarded key pattern to clear all player list caches
        // In Redis we would do this with SCAN and DEL commands
        // Since we're using a .NET Redis client, we'll approximate by using a cache key that starts with "players_all_"
        await _cacheService.RemoveAsync("players_all_*");
    }
}
