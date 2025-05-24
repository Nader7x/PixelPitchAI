using MediatR;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Coaches.Commands;

public class UpdateCoachCommand : IRequest<UpdateCoachCommandResponse>
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(50)]
    public string? Nationality { get; set; }
    
    [StringLength(50)]
    public string Role { get; set; }
    
    public int? TeamId { get; set; }
    
    [StringLength(500)]
    public string? PhotoUrl { get; set; }
    
    [StringLength(50)]
    public string? PreferredFormation { get; set; }
    
    [StringLength(100)]
    public string? CoachingStyle { get; set; }
    [StringLength(500)]
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    
}

public class UpdateCoachCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class UpdateCoachCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>
{
    public async Task<UpdateCoachCommandResponse> Handle(UpdateCoachCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if coach exists
            var coach = await unitOfWork.Coaches.GetByIdAsync(request.Id);
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
                var existingCoach = (await unitOfWork.Coaches.GetAllAsync(c => 
                    c.FirstName == request.FirstName && c.LastName == request.LastName)).FirstOrDefault();
                if (existingCoach != null && existingCoach.Id != request.Id)
                {
                    return new UpdateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Coach with name '{request.FirstName} {request.LastName}' already exists"
                    };
                }
            }
            // Check if team exists
            if (request.TeamId.HasValue)
            {
                var team = await unitOfWork.Teams.GetByIdAsync(request.TeamId.Value);
                if (team == null)
                {
                    return new UpdateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Team with ID {request.TeamId} not found"
                    };
                }
            }
            
            // Update coach properties
            if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != coach.FirstName)
            {
                coach.FirstName = request.FirstName;
            }
            if (!string.IsNullOrEmpty(request.LastName) && request.LastName != coach.LastName)
            {
                coach.LastName = request.LastName;
            }

            if (request.DateOfBirth != null && request.DateOfBirth != coach.DateOfBirth)
            {
                coach.DateOfBirth = request.DateOfBirth;
            }

            if (!string.IsNullOrEmpty(request.Nationality))
            {
                coach.Nationality = request.Nationality;
            }
            if (!string.IsNullOrEmpty(request.Role))
            {
                coach.Role = request.Role;
            }
            if (!string.IsNullOrEmpty(request.PhotoUrl))
            {
                coach.PhotoUrl = request.PhotoUrl;
            }
            if (!string.IsNullOrEmpty(request.TeamId.ToString()) && request.TeamId != coach.TeamId)
            {
                coach.TeamId = request.TeamId;
            }
            if (!string.IsNullOrEmpty(request.PreferredFormation))
            {
                coach.PreferredFormation = request.PreferredFormation;
            }
            if (!string.IsNullOrEmpty(request.CoachingStyle))
            {
                coach.CoachingStyle = request.CoachingStyle;
            }
            if (!string.IsNullOrEmpty(request.Biography))
            {
                coach.Biography = request.Biography;
            }
            if (request.YearsOfExperience != null && request.YearsOfExperience != coach.YearsOfExperience)
            {
                coach.YearsOfExperience = request.YearsOfExperience;
            }
            // Update the coach in the database
            unitOfWork.Coaches.UpdateAsync(coach);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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





