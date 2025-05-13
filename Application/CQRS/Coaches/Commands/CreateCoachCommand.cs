using Domain.Models;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Coaches.Commands;

public class CreateCoachCommand : IRequest<CreateCoachCommandResponse>
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(50)]
    public string Nationality { get; set; }
    
    [StringLength(50)]
    public string Role { get; set; }
    
    public int? TeamId { get; set; }
    
    public DateTime? ContractStartDate { get; set; }
    
    public DateTime? ContractEndDate { get; set; }
    
    [StringLength(500)]
    public string PhotoUrl { get; set; }
    
    [StringLength(50)]
    public string PreferredFormation { get; set; }
    
    [StringLength(100)]
    public string CoachingStyle { get; set; }
}

public class CreateCoachCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class CreateCoachCommandHandler : IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateCoachCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<CreateCoachCommandResponse> Handle(CreateCoachCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create full name to check for duplicates
            string fullName = $"{request.FirstName} {request.LastName}";
            
            // Check if coach with the same name already exists
            var existingCoach = await _unitOfWork.Coaches.FindAsync(c => c.FirstName == request.FirstName && c.LastName == request.LastName);
                
            if (existingCoach != null)
            {
                return new CreateCoachCommandResponse
                {
                    Succeeded = false,
                    Error = $"Coach with name '{fullName}' already exists"
                };
            }
            
            // Create new coach
            var coach = new Coach
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Nationality = request.Nationality,
                Role = request.Role,
                TeamId = request.TeamId,
                ContractStartDate = request.ContractStartDate,
                ContractEndDate = request.ContractEndDate,
                PhotoUrl = request.PhotoUrl,
                PreferredFormation = request.PreferredFormation,
                CoachingStyle = request.CoachingStyle
            };
            
            await _unitOfWork.Coaches.AddAsync(coach);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new CreateCoachCommandResponse
            {
                Succeeded = true,
                Id = coach.Id,
                FullName = fullName
            };
        }
        catch (Exception ex)
        {
            return new CreateCoachCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
