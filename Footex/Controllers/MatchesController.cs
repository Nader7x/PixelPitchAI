using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public MatchesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetAllMatchesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllMatchesQueryResponse>> GetAllMatches(
        [FromQuery] int? seasonId,
        [FromQuery] int? teamId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? matchWeek)
    {
        var query = new GetAllMatchesQuery
        {
            SeasonId = seasonId,
            TeamId = teamId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            MatchWeek = matchWeek
        };
        
        var result = await _mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetMatchByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMatchByIdQueryResponse>> GetMatchById(int id)
    {
        var query = new GetMatchByIdQuery { Id = id };
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
    [ProducesResponseType(typeof(CreateMatchCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMatchCommandResponse>> CreateMatch([FromBody] CreateMatchDto matchDto)
    {
        var command = new CreateMatchCommand
        {
            SeasonId = matchDto.SeasonId,
            HomeTeamId = matchDto.HomeTeamId,
            AwayTeamId = matchDto.AwayTeamId,
            ScheduledDateTimeUTC = matchDto.ScheduledDateTimeUTC,
            StadiumId = matchDto.StadiumId,
            MatchWeek = matchDto.MatchWeek,
            HomeCoachId = matchDto.HomeCoachId,
            AwayCoachId = matchDto.AwayCoachId,
            MatchStatus = matchDto.MatchStatus
        };
        
        var result = await _mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetMatchById), new { id = result.Id }, result);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UpdateMatchCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateMatchCommandResponse>> UpdateMatch(int id, [FromBody] UpdateMatchDto matchDto)
    {
        if (id != matchDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        var command = new UpdateMatchCommand
        {
            Id = matchDto.Id,
            SeasonId = matchDto.SeasonId,
            HomeTeamId = matchDto.HomeTeamId,
            AwayTeamId = matchDto.AwayTeamId,
            ScheduledDateTimeUTC = matchDto.ScheduledDateTimeUTC,
            StadiumId = matchDto.StadiumId,
            MatchWeek = matchDto.MatchWeek,
            HomeCoachId = matchDto.HomeCoachId,
            AwayCoachId = matchDto.AwayCoachId,
            HomeTeamScore = matchDto.HomeTeamScore,
            AwayTeamScore = matchDto.AwayTeamScore,
            WinningTeamId = matchDto.WinningTeamId,
            LosingTeamId = matchDto.LosingTeamId,
            IsDraw = matchDto.IsDraw,
            MatchStatus = matchDto.MatchStatus,
            HomeTeamPossession = matchDto.HomeTeamPossession,
            AwayTeamPossession = matchDto.AwayTeamPossession,
            HomeTeamShots = matchDto.HomeTeamShots,
            AwayTeamShots = matchDto.AwayTeamShots,
            HomeTeamShotsOnTarget = matchDto.HomeTeamShotsOnTarget,
            AwayTeamShotsOnTarget = matchDto.AwayTeamShotsOnTarget,
            HomeTeamCorners = matchDto.HomeTeamCorners,
            AwayTeamCorners = matchDto.AwayTeamCorners,
            HomeTeamFouls = matchDto.HomeTeamFouls,
            AwayTeamFouls = matchDto.AwayTeamFouls,
            HomeTeamYellowCards = matchDto.HomeTeamYellowCards,
            AwayTeamYellowCards = matchDto.AwayTeamYellowCards,
            HomeTeamRedCards = matchDto.HomeTeamRedCards,
            AwayTeamRedCards = matchDto.AwayTeamRedCards
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
    [ProducesResponseType(typeof(DeleteMatchCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteMatchCommandResponse>> DeleteMatch(int id)
    {
        var command = new DeleteMatchCommand { Id = id };
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
