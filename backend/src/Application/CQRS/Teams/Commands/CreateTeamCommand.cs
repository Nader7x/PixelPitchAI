using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Application.CQRS;

namespace Application.CQRS.Teams.Commands;

public class CreateTeamCommand : IRequest<CreateTeamCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string? ShortName { get; set; }

    [StringLength(500)]
    public string? Logo { get; set; }

    [Required]
    [StringLength(50)]
    public string? Country { get; set; }

    [Required]
    [StringLength(100)]
    public string? City { get; set; }

    [Required]
    [StringLength(50)]
    public string? League { get; set; }

    public DateTime FoundationDate { get; set; }

    [StringLength(20)]
    public string? PrimaryColor { get; set; }

    [StringLength(20)]
    public string? SecondaryColor { get; set; }

    public int? StadiumId { get; set; }
    public int? CoachId { get; set; }
}

public class CreateTeamCommandResponse
{
    public bool Succeeded { get; init; }
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Error { get; init; }
}

public class CreateTeamCommandHandler(IUnitOfWork unitOfWork, ITeamMapper teamMapper)
    : IRequestHandler<CreateTeamCommand, CreateTeamCommandResponse>
{
    public async Task<CreateTeamCommandResponse> Handle(
        CreateTeamCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            Coach? teamCoach = null;
            var existingTeam = await unitOfWork.Teams.GetByNameAsync(request.Name);

            if (existingTeam != null)
                return new CreateTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with name '{request.Name}' already exists",
                };
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new CreateTeamCommandResponse
                {
                    Succeeded = false,
                    Error = "Team name cannot be empty",
                };
            }

            var existingShortNameTeam = await unitOfWork.Teams.FindAsync(
                t => t.ShortName == request.ShortName,
                cancellationToken
            );
            if (existingShortNameTeam != null)
                return new CreateTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with short name '{request.ShortName}' already exists",
                };

            if (request.CoachId != null)
            {
                teamCoach = await unitOfWork.Coaches.GetByIdAsync(
                    request.CoachId.Value,
                    cancellationToken
                );
                if (teamCoach == null)
                    return new CreateTeamCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Coach with ID '{request.CoachId}' does not exist",
                    };
            }

            var team = teamMapper.ToTeamfromCreate(request);

            await unitOfWork.Teams.AddAsync(team);
            if (teamCoach != null)
                teamCoach.Team = team;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTeamCommandResponse
            {
                Succeeded = true,
                Id = team.Id,
                Name = team.Name,
            };
        }
        catch (Exception ex)
        {
            var innermostException = ex;
            while (innermostException.InnerException != null)
                innermostException = innermostException.InnerException;
            return new CreateTeamCommandResponse
            {
                Succeeded = false,
                Error = innermostException.Message,
            };
        }
    }
}
