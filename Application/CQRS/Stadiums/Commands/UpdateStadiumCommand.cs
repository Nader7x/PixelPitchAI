using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;
using MediatR;

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
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

public class UpdateStadiumCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStadiumCommand, UpdateStadiumCommandResponse>
{
    public async Task<UpdateStadiumCommandResponse> Handle(
        UpdateStadiumCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Check if stadium exists
            var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.Id);
            if (stadium == null)
                return new UpdateStadiumCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found",
                };

            // Check for name conflicts
            if (stadium.Name != request.Name)
            {
                var existingStadium = await unitOfWork.Stadiums.GetAllAsync(s =>
                    s.Name == request.Name
                );
                if (existingStadium.First().Id != request.Id)
                    return new UpdateStadiumCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with name '{request.Name}' already exists",
                    };
            }

            // Update stadium properties
            stadium.Name = request.Name;
            stadium.City = request.City;
            stadium.Country = request.Country;
            stadium.Capacity = request.Capacity;
            stadium.SurfaceType = request.SurfaceType;
            stadium.Address = request.Address;
            stadium.Latitude = request.Latitude;
            stadium.Longitude = request.Longitude;
            stadium.ImageUrl = request.ImageUrl;
            stadium.Description = request.Description;
            stadium.Facilities = request.Facilities;
            stadium.BuiltDate = request.BuiltDate;

            unitOfWork.Stadiums.UpdateAsync(stadium);
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
