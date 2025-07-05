using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

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
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
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
            // Check if team with the same name already exists
            var existingTeam = await unitOfWork.Teams.GetByNameAsync(request.Name);
            if (request.CoachId != null)
            {
                teamCoach = await unitOfWork.Coaches.GetByIdAsync(request.CoachId.Value);
                if (teamCoach == null)
                    return new CreateTeamCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Coach with ID '{request.CoachId}' does not exist",
                    };
            }

            if (existingTeam != null)
                return new CreateTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with name '{request.Name}' already exists",
                };

            // Create new team
            var team = teamMapper.ToTeamfromCreate(request);

            await unitOfWork.Teams.AddAsync(team);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            if (teamCoach != null)
            {
                teamCoach.TeamId = team.Id;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new CreateTeamCommandResponse
            {
                Succeeded = true,
                Id = team.Id,
                Name = team.Name,
            };
        }
        catch (Exception ex)
        {
            return new CreateTeamCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
