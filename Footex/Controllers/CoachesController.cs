using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachesController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public CoachesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetAllCoachesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllCoachesQueryResponse>> GetAllCoaches(
        [FromQuery] string nationality,
        [FromQuery] int? teamId)
    {
        var query = new GetAllCoachesQuery
        {
            Nationality = nationality,
            TeamId = teamId
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetCoachByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetCoachByIdQueryResponse>> GetCoachById(int id)
    {
        var query = new GetCoachByIdQuery { Id = id };
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
    [ProducesResponseType(typeof(CreateCoachCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCoachCommandResponse>> CreateCoach([FromBody] CreateCoachDto coachDto)
    {
        var command = new CreateCoachCommand
        {
            FirstName = coachDto.FirstName,
            LastName = coachDto.LastName,
            DateOfBirth = coachDto.DateOfBirth,
            Nationality = coachDto.Nationality,
            Role = coachDto.Role,
            TeamId = coachDto.TeamId,
            ContractStartDate = coachDto.ContractStartDate,
            ContractEndDate = coachDto.ContractEndDate,
            PhotoUrl = coachDto.PhotoUrl,
            PreferredFormation = coachDto.PreferredFormation,
            CoachingStyle = coachDto.CoachingStyle
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetCoachById), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UpdateCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateCoachCommandResponse>> UpdateCoach(int id, [FromBody] UpdateCoachDto coachDto)
    {
        if (id != coachDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        var command = new UpdateCoachCommand
        {
            Id = coachDto.Id,
            FirstName = coachDto.FirstName,
            LastName = coachDto.LastName,
            DateOfBirth = coachDto.DateOfBirth,
            Nationality = coachDto.Nationality,
            Role = coachDto.Role,
            TeamId = coachDto.TeamId,
            ContractStartDate = coachDto.ContractStartDate,
            ContractEndDate = coachDto.ContractEndDate,
            PhotoUrl = coachDto.PhotoUrl,
            PreferredFormation = coachDto.PreferredFormation,
            CoachingStyle = coachDto.CoachingStyle
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
    [ProducesResponseType(typeof(DeleteCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteCoachCommandResponse>> DeleteCoach(int id)
    {
        var command = new DeleteCoachCommand { Id = id };
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
