using Application.CQRS.Stadiums.Commands;
using Application.CQRS.Stadiums.Queries;
using Application.Dtos;
using Application.Interfaces;
using Application.Mappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StadiumsController(IMediator mediator, StadiumMapper stadiumMapper, IFileStorageService fileStorageService)
    : ControllerBase
{
    private readonly StadiumMapper _stadiumMapper = stadiumMapper;
    private readonly IMediator _mediator = mediator;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private const string CONTAINER_NAME = "stadiums";


    [HttpGet]
    [ProducesResponseType(typeof(GetAllStadiumsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllStadiumsQueryResponse>> GetAllStadiums(
        [FromQuery] string? country,
        [FromQuery] string? city)
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
    public async Task<ActionResult<CreateStadiumCommandResponse>> CreateStadium([FromForm] CreateStadiumDto stadiumDto)
    {
        // Handle file upload if present
        string? imageUrl = stadiumDto.ImageUrl;
        if (stadiumDto.Image != null)
        {
            imageUrl = await _fileStorageService.UploadImageAsync(stadiumDto.Image, CONTAINER_NAME);
        }
        
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
            ImageUrl = imageUrl,
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
    public async Task<ActionResult<UpdateStadiumCommandResponse>> UpdateStadium(int id, [FromForm] UpdateStadiumDto stadiumDto)
    {
        if (id != stadiumDto.Id)
            return BadRequest(new { error = "ID in URL does not match ID in request body" });
            
        // Get existing stadium to check if we need to replace the image
        var getQuery = new GetStadiumByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);
        
        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);
            
        // Handle file upload if present
        string? imageUrl = stadiumDto.ImageUrl;
        if (stadiumDto.Image != null)
        {
            // Delete old image if it exists
            if (!string.IsNullOrEmpty(existingResult.Stadium.ImageUrl))
            {
                await _fileStorageService.DeleteImageAsync(existingResult.Stadium.ImageUrl, CONTAINER_NAME);
            }
            
            // Upload new image
            imageUrl = await _fileStorageService.UploadImageAsync(stadiumDto.Image, CONTAINER_NAME);
        }
        
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
            ImageUrl = imageUrl,
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
        // Get existing stadium to delete the image
        var getQuery = new GetStadiumByIdQuery { Id = id };
        var existingResult = await _mediator.Send(getQuery);
        
        if (!existingResult.Succeeded || existingResult.NotFound)
            return NotFound(existingResult);
            
        // Delete image if it exists
        if (!string.IsNullOrEmpty(existingResult.Stadium.ImageUrl))
        {
            await _fileStorageService.DeleteImageAsync(existingResult.Stadium.ImageUrl, CONTAINER_NAME);
        }
        
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
