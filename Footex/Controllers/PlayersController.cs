using Application.CQRS.Players.Commands;
using Application.CQRS.Players.Queries;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PlayersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetAllPlayersQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllPlayersQueryResponse>> GetAllPlayers(
        [FromQuery] string nationality,
        [FromQuery] string position,
        [FromQuery] string preferredFoot,
        [FromQuery] int? teamId)
    {
        var query = new GetAllPlayersQuery
        {
            Nationality = nationality,
            Position = position,
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
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CreatePlayerCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePlayerCommandResponse>> CreatePlayer([FromBody] CreatePlayerDto playerDto)
    {
        var command = new CreatePlayerCommand
        {
            FullName = playerDto.FullName,
            Nationality = playerDto.Nationality,
            PreferredFoot = playerDto.PreferredFoot,
            PhotoUrl = playerDto.PhotoUrl,
            TeamId = playerDto.TeamId,
            ShirtNumber = playerDto.ShirtNumber,
            StatsBombPlayerId = playerDto.StatsBombPlayerId
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetPlayerById), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UpdatePlayerCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdatePlayerCommandResponse>> UpdatePlayer(int id, [FromBody] UpdatePlayerDto playerDto)
    {
        if (id != playerDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        var command = new UpdatePlayerCommand
        {
            Id = playerDto.Id,
            FullName = playerDto.FullName,
            Nationality = playerDto.Nationality,
            PreferredFoot = playerDto.PreferredFoot,
            PhotoUrl = playerDto.PhotoUrl,
            TeamId = playerDto.TeamId,
            ShirtNumber = playerDto.ShirtNumber,
            StatsBombPlayerId = playerDto.StatsBombPlayerId
        };
        
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
