using System.ComponentModel.DataAnnotations;
using Application.Helpers;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Stadiums.Commands;

public class UpdateStadiumCommand : IRequest<UpdateStadiumCommandResponse>
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [Required]
    [StringLength(100)]
    public string? City { get; set; }

    [Required]
    [StringLength(50)]
    public string? Country { get; set; }

    [Required]
    public int Capacity { get; set; }

    [StringLength(50)]
    public string? SurfaceType { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(1000)]
    public string? Facilities { get; set; }

    public DateTime BuiltDate { get; set; }
}

public class UpdateStadiumCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Error { get; init; }
}

public class UpdateStadiumCommandHandler(IUnitOfWork unitOfWork, IStadiumMapper stadiumMapper)
    : IRequestHandler<UpdateStadiumCommand, UpdateStadiumCommandResponse>
{
    public async Task<UpdateStadiumCommandResponse> Handle(
        UpdateStadiumCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate required fields
            if (
                StringExtensions.AreAnyNullOrWhiteSpace(request.Name, request.Country, request.City)
            )
            {
                return new UpdateStadiumCommandResponse
                {
                    Succeeded = false,
                    Error = "Name, Country, and City are can not be white Spaces",
                };
            }
            if (request.Capacity is <= 0)
            {
                return new UpdateStadiumCommandResponse
                {
                    Succeeded = false,
                    Error = "Capacity must be a positive number",
                };
            }
            // Check if stadium exists
            var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.Id, cancellationToken);
            if (stadium == null)
                return new UpdateStadiumCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found",
                };

            // Check for name conflicts
            if (
                !StringExtensions.AreAnyNullOrWhiteSpace(stadium.Name, request.Name)
                && !string.Equals(
                    stadium.Name,
                    request.Name,
                    StringComparison.CurrentCultureIgnoreCase
                )
            )
            {
                var existingStadium = await unitOfWork.Stadiums.AnyAsync(
                    s =>
                        s.Name != null
                        && s.Name.ToLower() == request.Name.ToLower()
                        && s.Id != request.Id,
                    cancellationToken
                );
                if (existingStadium)
                    return new UpdateStadiumCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with name '{request.Name}' already exists",
                    };
            }

            stadiumMapper.UpdateStadiumFromCommand(request, stadium);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateStadiumCommandResponse
            {
                Succeeded = true,
                Id = stadium.Id,
                Name = stadium.Name,
            };
        }
        catch (Exception ex)
        {
            return new UpdateStadiumCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
