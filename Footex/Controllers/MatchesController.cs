using System.Text;
using System.Text.Json;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.Dtos;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(IMediator mediator, IHttpClientFactory httpClientFactory, MatchMapper matchmapper) : ControllerBase
{
    private readonly MatchMapper _matchmapper = matchmapper;
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

        var result = await mediator.Send(query);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GetMatchByIdQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMatchByIdQueryResponse>> GetMatchById(int id)
    {
        var query = new GetMatchByIdQuery { Id = id };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("Details/{matchId:int}")]
    // get match by id with details 
    [ProducesResponseType(typeof(GetMatchByIdWithDetailsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetMatchByIdWithDetailsQueryResponse>> GetMatchByIdWithDetails(int matchId)
    {
        var query = new GetMatchByIdWithDetailsQuery { MatchId = matchId };
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
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(CreateMatchCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMatchCommandResponse>> CreateMatch([FromBody] CreateMatchDto matchDto)
    {
        if(string.IsNullOrEmpty(matchDto.CreatorId))
            matchDto.CreatorId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? string.Empty;

        var command = _matchmapper.ToCreateCommand(matchDto);

        var result = await mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMatchById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
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

        var result = await mediator.Send(command);

        if (result.Succeeded) return Ok(result);
        if (result.NotFound)
            return NotFound(result);

        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteMatchCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteMatchCommandResponse>> DeleteMatch(int id)
    {
        var command = new DeleteMatchCommand { Id = id };
        var result = await mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{userId}")]
    [Authorize]
    [ProducesResponseType(typeof(GetUserMatchesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetUserMatchesQueryResponse>> GetUserMatches(string userId)
    {
        var query = new GetUserMatchesQuery() { UserId = userId };
        var result = await mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("/simulateMatch/{userId}")]
    [Authorize]
    public async Task<ActionResult<CreateMatchCommandResponse>> SimulateMatch(string userId,
        [FromBody] SimulateMatchDto simulationDto)
    {
        var httpClient = httpClientFactory.CreateClient();
        var seasonYear = simulationDto.AwayTeamSeason.Split("/")[1];
        var homeInMatchName = $"{simulationDto.HomeTeamName.Replace(" ", "_")}_{seasonYear}";
        var awayInMatchName = $"{simulationDto.AwayTeamName.Replace(" ", "_")}_{seasonYear}";
        var command = new CreateMatchCommand()
        {
            HomeTeamId = simulationDto.HomeTeamId,
            AwayTeamId = simulationDto.AwayTeamId,
            HomeTeamInMatchName = homeInMatchName,
            AwayTeamInMatchName = awayInMatchName,
            ScheduledDateTimeUtc = DateTime.UtcNow,
            MatchStatus = "SimulatingInProgress",
            CreatorId = userId,
            ModelSimulationStartTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30),
        };
        var result = await mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(result);
        var health = await httpClient.GetAsync("http://localhost:8000/health");
        if (!health.IsSuccessStatusCode)
            return StatusCode((int)health.StatusCode, "Simulation service is not available");
        var response = await httpClient.PostAsync("http://localhost:8000/startMatch",
            new StringContent(JsonSerializer.Serialize(new
            {
                match_id = result.Id,
                home_team_id = simulationDto.HomeTeamId,
                away_team_id = simulationDto.AwayTeamId,
                home_team_name = simulationDto.HomeTeamName,
                away_team_name = simulationDto.AwayTeamName,
                home_team_season = simulationDto.HomeTeamSeason,
                away_team_season = simulationDto.AwayTeamSeason,
            }), Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode)
        {
            var statusCommand = new UpdateMatchStatusCommand()
            {
                MatchId = result.Id,
            };
            var statusResult = await mediator.Send(statusCommand);
            if (!statusResult.Succeeded)
                return BadRequest(statusResult);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }
        return Ok(response);
    }
}