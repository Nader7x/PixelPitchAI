using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StadiumsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public StadiumsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetAllStadiumsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllStadiumsQueryResponse>> GetAllStadiums(
        [FromQuery] string country,
        [FromQuery] string city)
    {
        var query = new GetAllStadiumsQuery
        {
            Country = country,
            City = city
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetStadiumByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetStadiumByIdQueryResponse>> GetStadiumById(int id)
    {
        var query = new GetStadiumByIdQuery { Id = id };
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
    [ProducesResponseType(typeof(CreateStadiumCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateStadiumCommandResponse>> CreateStadium([FromBody] CreateStadiumDto stadiumDto)
    {
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
            ImageUrl = stadiumDto.ImageUrl,
            Description = stadiumDto.Description,
            Facilities = stadiumDto.Facilities,
            BuiltDate = stadiumDto.BuiltDate
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetStadiumById), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateStadiumCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateStadiumCommandResponse>> UpdateStadium(int id, [FromBody] UpdateStadiumDto stadiumDto)
    {
        if (id != stadiumDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        var command = new UpdateStadiumCommand
        {
            Id = stadiumDto.Id,
            Name = stadiumDto.Name,
            City = stadiumDto.City,
            Country = stadiumDto.Country,
            Capacity = stadiumDto.Capacity,
            SurfaceType = stadiumDto.SurfaceType,
            Address = stadiumDto.Address,
            Latitude = stadiumDto.Latitude,
            Longitude = stadiumDto.Longitude,
            ImageUrl = stadiumDto.ImageUrl,
            Description = stadiumDto.Description,
            Facilities = stadiumDto.Facilities,
            BuiltDate = stadiumDto.BuiltDate
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
    [ProducesResponseType(typeof(DeleteStadiumCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteStadiumCommandResponse>> DeleteStadium(int id)
    {
        var command = new DeleteStadiumCommand { Id = id };
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
