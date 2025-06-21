using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Stadiums.Commands;

public class CreateStadiumCommand : IRequest<CreateStadiumCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [Required] [StringLength(100)] public string? City { get; set; }

    [Required] [StringLength(50)] public string? Country { get; set; }

    [Required] public int Capacity { get; set; }

    [StringLength(50)] public string? SurfaceType { get; set; }

    [StringLength(200)] public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [StringLength(500)] public string? ImageUrl { get; set; }

    [StringLength(2000)] public string? Description { get; set; }

    [StringLength(1000)] public string? Facilities { get; set; }

    public DateTime BuiltDate { get; set; }
}

public class CreateStadiumCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

public class CreateStadiumCommandHandler(IUnitOfWork unitOfWork, IStadiumMapper stadiumMapper)
    : IRequestHandler<CreateStadiumCommand, CreateStadiumCommandResponse>
{
    public async Task<CreateStadiumCommandResponse> Handle(CreateStadiumCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if stadium with the same name already exists
            var existingStadium = await unitOfWork.Stadiums.FindAsync(s => s.Name == request.Name, cancellationToken);
            if (existingStadium != null)
                return new CreateStadiumCommandResponse
                {
                    Succeeded = false,
                    Error = $"Stadium with name '{request.Name}' already exists"
                };

            // Create new stadium
            var stadium = stadiumMapper.ToStadiumFromCreate(request);

            await unitOfWork.Stadiums.AddAsync(stadium);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateStadiumCommandResponse
            {
                Succeeded = true,
                Id = stadium.Id,
                Name = stadium.Name
            };
        }
        catch (Exception ex)
        {
            return new CreateStadiumCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
