using Domain.Models;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Stadiums.Commands;

public class CreateStadiumCommand : IRequest<CreateStadiumCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(100)]
    public string City { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Country { get; set; }
    
    [Required]
    public int Capacity { get; set; }
    
    [StringLength(50)]
    public string SurfaceType { get; set; }
    
    [StringLength(200)]
    public string Address { get; set; }
    
    public decimal? Latitude { get; set; }
    
    public decimal? Longitude { get; set; }
    
    [StringLength(500)]
    public string ImageUrl { get; set; }
    
    [StringLength(2000)]
    public string Description { get; set; }
    
    [StringLength(1000)]
    public string Facilities { get; set; }
    
    public DateTime BuiltDate { get; set; }
}

public class CreateStadiumCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Error { get; set; }
}

public class CreateStadiumCommandHandler : IRequestHandler<CreateStadiumCommand, CreateStadiumCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateStadiumCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<CreateStadiumCommandResponse> Handle(CreateStadiumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if stadium with the same name already exists
            var existingStadium = await _unitOfWork.Stadiums.FindAsync(s => s.Name == request.Name);
            if (existingStadium != null)
            {
                return new CreateStadiumCommandResponse
                {
                    Succeeded = false,
                    Error = $"Stadium with name '{request.Name}' already exists"
                };
            }
            
            // Create new stadium
            var stadium = new Stadium
            {
                Name = request.Name,
                City = request.City,
                Country = request.Country,
                Capacity = request.Capacity,
                SurfaceType = request.SurfaceType,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                Facilities = request.Facilities,
                BuiltDate = request.BuiltDate
            };
            
            await _unitOfWork.Stadiums.AddAsync(stadium);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
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
