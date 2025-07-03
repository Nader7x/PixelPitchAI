using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Teams.Commands;

public class UpdateTeamCommand : IRequest<UpdateTeamCommandResponse>
{
    [Required]
    public int Id { get; set; }

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

public class UpdateTeamCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

public class UpdateTeamCommandHandler(IUnitOfWork unitOfWork, ITeamMapper teamMapper)
    : IRequestHandler<UpdateTeamCommand, UpdateTeamCommandResponse>
{
    private readonly ITeamMapper _teamMapper = teamMapper;

    public async Task<UpdateTeamCommandResponse> Handle(
        UpdateTeamCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Check if team exists
            var team = await unitOfWork.Teams.GetByIdAsync(request.Id);
            if (request.CoachId != null)
            {
                var teamCoach = await unitOfWork.Coaches.GetByIdAsync(request.CoachId.Value);
                if (teamCoach is { TeamId: not null } && teamCoach.TeamId != request.Id)
                    return new UpdateTeamCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"Coach with ID {request.CoachId} is already assigned to another team.",
                    };
                if (teamCoach != null)
                    teamCoach.TeamId = request.Id;
            }

            if (team == null)
                return new UpdateTeamCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Team with ID {request.Id} not found",
                };

            // Check for name conflicts
            if (team.Name != request.Name)
            {
                var existingTeam = await unitOfWork.Teams.GetByNameAsync(request.Name);
                if (existingTeam != null && existingTeam.Id != request.Id)
                    return new UpdateTeamCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Team with name '{request.Name}' already exists",
                    };
            }

            // Update team properties
            team.Name = request.Name;
            team.ShortName = request.ShortName;
            if (!string.IsNullOrEmpty(request.Logo))
                team.Logo = request.Logo;
            team.Country = request.Country;
            if (!string.IsNullOrEmpty(request.City))
                team.League = request.League;
            if (
                !string.IsNullOrEmpty(request.FoundationDate.ToString(CultureInfo.InvariantCulture))
            )
                team.FoundationDate = request.FoundationDate;
            team.PrimaryColor = request.PrimaryColor;
            team.SecondaryColor = request.SecondaryColor;
            team.StadiumId = request.StadiumId;
            team.City = request.City;

            unitOfWork.Teams.UpdateAsync(team);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateTeamCommandResponse
            {
                Succeeded = true,
                Id = team.Id,
                Name = team.Name,
            };
        }
        catch (Exception ex)
        {
            return new UpdateTeamCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
