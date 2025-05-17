using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Coaches.Commands;

public class UpdateCoachCommand : IRequest<UpdateCoachCommandResponse>
{
    [Required]
    public int Id { get; set; }
    
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
    public string? PhotoUrl { get; set; }
    
    [StringLength(50)]
    public string PreferredFormation { get; set; }
    
    [StringLength(100)]
    public string CoachingStyle { get; set; }
}

public class UpdateCoachCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class UpdateCoachCommandHandler : IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public UpdateCoachCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<UpdateCoachCommandResponse> Handle(UpdateCoachCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if coach exists
            var coach = await _unitOfWork.Coaches.GetByIdAsync(request.Id);
            if (coach == null)
            {
                return new UpdateCoachCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Coach with ID {request.Id} not found"
                };
            }
            
            // Check for name conflicts (if name was changed)
            if (coach.FirstName != request.FirstName || coach.LastName != request.LastName)
            {
                var existingCoach = await _unitOfWork.Coaches.GetAllAsync(c => 
                    c.FirstName == request.FirstName && c.LastName == request.LastName);
                    
                if (existingCoach.First().Id != request.Id)
                {
                    return new UpdateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Coach with name '{request.FirstName} {request.LastName}' already exists"
                    };
                }
            }
            
            // Update coach properties
            coach.FirstName = request.FirstName;
            coach.LastName = request.LastName;
            coach.DateOfBirth = request.DateOfBirth;
            coach.Nationality = request.Nationality;
            coach.Role = request.Role;
            coach.TeamId = request.TeamId;
            
            _unitOfWork.Coaches.UpdateAsync(coach);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new UpdateCoachCommandResponse
            {
                Succeeded = true,
                Id = coach.Id,
                FullName = $"{coach.FirstName} {coach.LastName}"
            };
        }
        catch (Exception ex)
        {
            return new UpdateCoachCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
