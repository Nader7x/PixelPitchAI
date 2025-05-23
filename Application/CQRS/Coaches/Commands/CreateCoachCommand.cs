using MediatR;
using System.ComponentModel.DataAnnotations;
using Application.Mappers;
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
    
    [StringLength(500)]
    public string? PhotoUrl { get; set; }
    
    [StringLength(50)]
    public string PreferredFormation { get; set; }
    
    [StringLength(100)]
    public string CoachingStyle { get; set; }
    public string Biography { get; set; }
    public int? YearsOfExperience { get; set; }
}

public class CreateCoachCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class CreateCoachCommandHandler(IUnitOfWork unitOfWork, CoachMapper coachMapper)
    : IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>
{
    public async Task<CreateCoachCommandResponse> Handle(CreateCoachCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create full name to check for duplicates
            string fullName = $"{request.FirstName} {request.LastName}";
            
            // Check if coach with the same name already exists
            var existingCoach = await unitOfWork.Coaches.FindAsync(c => c.FirstName == request.FirstName && c.LastName == request.LastName);
                
            if (existingCoach != null)
            {
                return new CreateCoachCommandResponse
                {
                    Succeeded = false,
                    Error = $"Coach with name '{fullName}' already exists"
                };
            }
            
            // Create new coach
            var coach = coachMapper.ToCoachFromCreate(request);
            
            await unitOfWork.Coaches.AddAsync(coach);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
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
