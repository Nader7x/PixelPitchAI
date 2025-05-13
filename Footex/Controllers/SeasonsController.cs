using Application.CQRS.Seasons.Commands;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeasonsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public SeasonsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetAllSeasonsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllSeasonsQueryResponse>> GetAllSeasons(
        [FromQuery] string leagueName,
        [FromQuery] string country,
        [FromQuery] bool? isActive)
    {
        var query = new GetAllSeasonsQuery
        {
            LeagueName = leagueName,
            Country = country,
            IsActive = isActive
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetSeasonByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetSeasonByIdQueryResponse>> GetSeasonById(int id)
    {
        var query = new GetSeasonByIdQuery { Id = id };
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
    [ProducesResponseType(typeof(CreateSeasonCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateSeasonCommandResponse>> CreateSeason([FromBody] CreateSeasonDto seasonDto)
    {
        var command = new CreateSeasonCommand
        {
            Name = seasonDto.Name,
            LeagueName = seasonDto.LeagueName,
            Country = seasonDto.Country,
            TotalRounds = seasonDto.TotalRounds,
            IsActive = seasonDto.IsActive,
            StartDate = seasonDto.StartDate,
            EndDate = seasonDto.EndDate
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetSeasonById), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateSeasonCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateSeasonCommandResponse>> UpdateSeason(int id, [FromBody] UpdateSeasonDto seasonDto)
    {
        if (id != seasonDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        var command = new UpdateSeasonCommand
        {
            Id = seasonDto.Id,
            Name = seasonDto.Name,
            LeagueName = seasonDto.LeagueName,
            Country = seasonDto.Country,
            CurrentRound = seasonDto.CurrentRound,
            TotalRounds = seasonDto.TotalRounds,
            IsActive = seasonDto.IsActive,
            IsCompleted = seasonDto.IsCompleted,
            StartDate = seasonDto.StartDate,
            EndDate = seasonDto.EndDate
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
    [ProducesResponseType(typeof(DeleteSeasonCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteSeasonCommandResponse>> DeleteSeason(int id)
    {
        var command = new DeleteSeasonCommand { Id = id };
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
