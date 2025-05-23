using Application.CQRS.Coaches.Commands;
using Application.CQRS.Coaches.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachesController(IMediator mediator, IFileStorageService fileStorageService, CoachMapper coachMapper)
    : ControllerBase
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IMediator _mediator = mediator;
    private readonly CoachMapper _coachMapper = coachMapper;
    private string CONTAINER_NAME = "coaches";


    [HttpGet("filter")]
    [ProducesResponseType(typeof(GetAllCoachesQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllCoachesQueryResponse>> GetAllCoaches(
        [FromQuery] string? nationality,
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
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateCoachCommandResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCoachCommandResponse>> CreateCoach([FromForm] CreateCoachDto coachDto)
    {

        // Handle file upload if present
        string? photoUrl = coachDto.PhotoUrl;
        if (coachDto.Photo != null)
        {
            photoUrl = await _fileStorageService.UploadImageAsync(coachDto.Photo, CONTAINER_NAME);
        }
        Console.WriteLine("Photo URL: " + photoUrl);
        coachDto.PhotoUrl = photoUrl;
        var command = _coachMapper.ToCreateCommand(coachDto);

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UpdateCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateCoachCommandResponse>> UpdateCoach(int id, [FromForm] UpdateCoachDto coachDto)
    {



        // Get existing coach
        var getQuery = new GetCoachByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Handle file upload if present
        string? photoUrl = coachDto.PhotoUrl;
        if (coachDto.Photo != null)
        {
            // Delete old photo if it exists
            if (!string.IsNullOrEmpty(existingResult.Coach.PhotoUrl))
            {
                await _fileStorageService.DeleteImageAsync(existingResult.Coach.PhotoUrl, CONTAINER_NAME);
            }

            // Upload new photo
            photoUrl = await _fileStorageService.UploadImageAsync(coachDto.Photo, CONTAINER_NAME);
        }

        coachDto.PhotoUrl = photoUrl;

        var command = _coachMapper.ToUpdateCommand(coachDto);
        command.Id = id;

        var result = await _mediator.Send(command);

        if (result.Succeeded) return Ok(result);
        if (result.NotFound)
            return NotFound(result);
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeleteCoachCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteCoachCommandResponse>> DeleteCoach(int id)
    {
    
        // Get existing coach to delete the image
        var getQuery = new GetCoachByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);

        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);

        // Delete photo if it exists
        if (!string.IsNullOrEmpty(existingResult.Coach.PhotoUrl))
        {
            await _fileStorageService.DeleteImageAsync(existingResult.Coach.PhotoUrl, CONTAINER_NAME);
        }

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