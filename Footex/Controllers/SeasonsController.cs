using Application.CQRS.Seasons.Commands;
using Application.CQRS.Seasons.Queries;
using Application.Dtos;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeasonsController(IMediator mediator, SeasonMapper seasonMapper) : ControllerBase
{
    private readonly SeasonMapper _seasonMapper = seasonMapper;

    [HttpGet]
    [ProducesResponseType(typeof(GetAllSeasonsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllSeasonsQueryResponse>> GetAllSeasons(
        [FromQuery] string? leagueName,
        [FromQuery] string? country,
        [FromQuery] bool? isActive)
    {
        var query = new GetAllSeasonsQuery
        {
            LeagueName = leagueName,
            Country = country,
            IsActive = isActive
        };
        
        var result = await mediator.Send(query);
        
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
        var result = await mediator.Send(query);
        
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
        var command = _seasonMapper.ToCreateCommand(seasonDto);
        
        var result = await mediator.Send(command);
        
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

        var command = _seasonMapper.ToUpdateCommand(seasonDto);
        
        var result = await mediator.Send(command);
        
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
        var result = await mediator.Send(command);
        
        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);
                
            return BadRequest(result);
        }
        
        return Ok(result);
    }
}
