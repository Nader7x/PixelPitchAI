using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Coaches.Commands;

public class CreateCoachCommand : IRequest<CreateCoachCommandResponse>
{
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
    public string? Role { get; set; }

    public int? TeamId { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(50)]
    public string? PreferredFormation { get; set; }

    [StringLength(100)]
    public string? CoachingStyle { get; set; }

    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
}

public class CreateCoachCommandResponse
{
    public bool Succeeded { get; init; }
    public int Id { get; init; }
    public string? FullName { get; init; }
    public string? Error { get; init; }
}

public class CreateCoachCommandHandler(IUnitOfWork unitOfWork, ICoachMapper coachMapper)
    : IRequestHandler<CreateCoachCommand, CreateCoachCommandResponse>
{
    public async Task<CreateCoachCommandResponse> Handle(
        CreateCoachCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request.TeamId is < 0)
            {
                var coachTeam = await unitOfWork.Teams.GetByIdAsync(
                    request.TeamId.Value,
                    cancellationToken
                );
                if (coachTeam is null)
                    return new CreateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error = "Team does not exist",
                    };
            }

            if (
                string.IsNullOrWhiteSpace(request.FirstName)
                || string.IsNullOrWhiteSpace(request.LastName)
            )
                return new CreateCoachCommandResponse
                {
                    Succeeded = false,
                    Error = "Coach Name Can not Be Null or White Space",
                };
            var fullName = $"{request.FirstName} {request.LastName}";

            var existingCoach = await unitOfWork.Coaches.FindAsync(
                c => c.FirstName == request.FirstName && c.LastName == request.LastName,
                cancellationToken
            );

            if (existingCoach != null)
                return new CreateCoachCommandResponse
                {
                    Succeeded = false,
                    Error = $"Coach with name '{fullName}' already exists",
                };

            var coach = coachMapper.ToCoachFromCreate(request);

            await unitOfWork.Coaches.AddAsync(coach);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateCoachCommandResponse
            {
                Succeeded = true,
                Id = coach.Id,
                FullName = fullName,
            };
        }
        catch (Exception ex)
        {
            // return new CreateCoachCommandResponse { Succeeded = false, Error = ex.Message };
            var innermostException = ex;
            while (innermostException.InnerException != null)
                innermostException = innermostException.InnerException;

            return new CreateCoachCommandResponse
            {
                Succeeded = false,
                Error = innermostException.Message,
            };
        }
    }
}
