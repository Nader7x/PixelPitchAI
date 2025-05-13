using Application.CQRS.Teams.Commands;
using Application.CQRS.Teams.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all teams
    /// </summary>
    /// <returns>List of teams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTeamsQueryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllTeamsQueryResponse>> GetAll()
    {
        var query = new GetAllTeamsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get team by ID
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>Team details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetTeamByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTeamByIdQueryResponse>> GetById(int id)
    {
        var query = new GetTeamByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    /// <param name="command">Team creation data</param>
    /// <returns>Created team</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateTeamCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateTeamCommandResponse>> Create([FromBody] CreateTeamCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="command">Team update data</param>
    /// <returns>Updated team</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateTeamCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTeamCommandResponse>> Update(int id, [FromBody] UpdateTeamCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");
            
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return result.NotFound ? NotFound() : BadRequest(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Delete a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>Operation result</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteTeamCommand { Id = id };
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return NotFound();
            
        return NoContent();
    }
}
