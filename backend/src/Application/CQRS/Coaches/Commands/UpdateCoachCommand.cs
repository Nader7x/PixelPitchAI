using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Coaches.Commands;

public class UpdateCoachCommand : IRequest<UpdateCoachCommandResponse>
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; init; }

    public DateTime? DateOfBirth { get; set; }

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

    [StringLength(500)]
    public string? Biography { get; set; }

    public int? YearsOfExperience { get; set; }
}

public class UpdateCoachCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int Id { get; init; }
    public string? FullName { get; init; }
    public string? Error { get; init; }
}

public class UpdateCoachCommandHandler(IUnitOfWork unitOfWork, ICoachMapper coachMapper)
    : IRequestHandler<UpdateCoachCommand, UpdateCoachCommandResponse>
{
    public async Task<UpdateCoachCommandResponse> Handle(
        UpdateCoachCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var coach = await unitOfWork.Coaches.GetByIdAsync(request.Id, cancellationToken);
            if (coach == null)
                return new UpdateCoachCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Coach with ID {request.Id} not found",
                };

            if (coach.FirstName != request.FirstName || coach.LastName != request.LastName)
            {
                var newNameExists = await unitOfWork.Coaches.AnyAsync(
                    c =>
                        c.Id != request.Id
                        && c.FirstName == request.FirstName
                        && c.LastName == request.LastName,
                    cancellationToken
                );

                if (newNameExists)
                    return new UpdateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"Coach with name '{request.FirstName} {request.LastName}' already exists",
                    };
            }

            if (request.TeamId.HasValue)
            {
                var teamExists = await unitOfWork.Teams.AnyAsync(
                    t => t.Id == request.TeamId.Value,
                    cancellationToken
                );
                if (!teamExists)
                    return new UpdateCoachCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Team with ID {request.TeamId} not found",
                    };
            }

            coachMapper.ToCoachFromUpdate(request, coach);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateCoachCommandResponse
            {
                Succeeded = true,
                Id = coach.Id,
                FullName = $"{coach.FirstName} {coach.LastName}",
            };
        }
        catch (Exception ex)
        {
            var innermostException = ex;
            while (innermostException.InnerException != null)
                innermostException = innermostException.InnerException;
            return new UpdateCoachCommandResponse
            {
                Succeeded = false,
                Error = innermostException.Message,
            };
        }
    }
}
