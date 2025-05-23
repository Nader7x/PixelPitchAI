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
public class PlayersController(IMediator mediator, IFileStorageService fileStorageService, PlayerMapper playermapper) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly PlayerMapper _playermapper = playermapper;
    private string CONTAINER_NAME = "players";


    [HttpGet]
    [ProducesResponseType(typeof(GetAllPlayersQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllPlayersQueryResponse>> GetAllPlayers(
        [FromQuery] string? nationality,
        [FromQuery] string? preferredFoot,
        [FromQuery] int? teamId)
    {
        var query = new GetAllPlayersQuery
        {
            Nationality = nationality,
            PreferredFoot = preferredFoot,
            TeamId = teamId
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetPlayerByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetPlayerByIdQueryResponse>> GetPlayerById(int id)
    {
        var query = new GetPlayerByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);
                
            return BadRequest(result);
        }
        
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

        var command = _playermapper.ToCreateCommand(playerDto);

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

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

        var command = _playermapper.ToUpdateCommand(playerDto);
        command.Id = id;

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

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

        return Ok(result);
    }
}
