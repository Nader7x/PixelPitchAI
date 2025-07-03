using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Commands;

public class CreateMatchCommand : IRequest<CreateMatchCommandResponse>
{
    [Required]
    public int HomeSeasonId { get; set; }

    public int AwaySeasonId { get; set; }

    [Required]
    public int HomeTeamId { get; set; }

    [Required]
    public int AwayTeamId { get; set; }

    public string? HomeTeamInMatchName { get; set; }
    public string? AwayTeamInMatchName { get; set; }

    [Required]
    public DateTime ScheduledDateTimeUtc { get; set; }

    public int? StadiumId { get; set; }

    public int? MatchWeek { get; set; }

    public int? HomeCoachId { get; set; }

    public int? AwayCoachId { get; set; }

    [StringLength(50)]
    public string? MatchStatus { get; set; } = "Scheduled";

    public required string CreatorId { get; init; }
    public DateTime? ModelSimulationStartTimeUtc { get; init; }
    public bool IsLive { get; init; } = false;
}

public class CreateMatchCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; init; }
    public string? HomeTeamName { get; init; }
    public string? AwayTeamName { get; init; }
    public string? Error { get; set; }
    public StartMatchResponse? ApiResponse { get; set; }
}

public class CreateMatchCommandHandler(IUnitOfWork unitOfWork, IMatchMapper matchMapper)
    : IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>
{
    public async Task<CreateMatchCommandResponse> Handle(
        CreateMatchCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate Season exists
            var homeSeason = await unitOfWork.Seasons.GetByIdAsync(request.HomeSeasonId);
            if (homeSeason == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.HomeSeasonId} not found",
                };
            var awaySeason = await unitOfWork.Seasons.GetByIdAsync(request.AwaySeasonId);
            if (awaySeason == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.AwaySeasonId} not found",
                };

            // Validate HomeTeam exists
            var homeTeam = await unitOfWork.Teams.GetByIdAsync(request.HomeTeamId);
            if (homeTeam == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Home Team with ID {request.HomeTeamId} not found",
                };

            // Validate AwayTeam exists
            var awayTeam = await unitOfWork.Teams.GetByIdAsync(request.AwayTeamId);
            if (awayTeam == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Away Team with ID {request.AwayTeamId} not found",
                };

            // Validate Stadium if provided
            if (request.StadiumId.HasValue)
            {
                var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.StadiumId.Value);
                if (stadium == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with ID {request.StadiumId} not found",
                    };
            }

            // Validate HomeCoach if provided
            if (request.HomeCoachId.HasValue)
            {
                var homeCoach = await unitOfWork.Coaches.GetByIdAsync(request.HomeCoachId.Value);
                if (homeCoach == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Home Coach with ID {request.HomeCoachId} not found",
                    };
            }

            // Validate AwayCoach if provided
            if (request.AwayCoachId.HasValue)
            {
                var awayCoach = await unitOfWork.Coaches.GetByIdAsync(request.AwayCoachId.Value);
                if (awayCoach == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Coach with ID {request.AwayCoachId} not found",
                    };
            }

            if (string.IsNullOrEmpty(request.HomeTeamInMatchName))
                request.HomeTeamInMatchName =
                    $"{homeTeam.Name?.Replace(" ", "_")}_{homeSeason.Name.Split("/")[1]}";
            if (string.IsNullOrEmpty(request.AwayTeamInMatchName))
                request.AwayTeamInMatchName =
                    $"{awayTeam.Name?.Replace(" ", "_")}_{awaySeason.Name.Split("/")[1]}";

            // Create new match
            var match = matchMapper.ToMatchFromCreate(request);

            await unitOfWork.Matches.AddAsync(match);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateMatchCommandResponse
            {
                Succeeded = true,
                Id = match.Id,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
            };
        }
        catch (Exception ex)
        {
            return new CreateMatchCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}

public class StartMatchResponse
{
    [JsonPropertyName("match_id")]
    public int MatchId { get; init; }

    [JsonPropertyName("home_team_name")]
    public string HomeTeamName { get; init; } = string.Empty;

    [JsonPropertyName("away_team_name")]
    public string AwayTeamName { get; init; } = string.Empty;

    [JsonPropertyName("home_team_season")]
    public string HomeTeamSeason { get; init; } = string.Empty;

    [JsonPropertyName("away_team_season")]
    public string AwayTeamSeason { get; init; } = string.Empty;

    [JsonPropertyName("events_count")]
    public int EventsCount { get; init; } = 0;

    [JsonPropertyName("execution_time")]
    public double ExecutionTime { get; init; } = 0.0;

    [JsonPropertyName("preview")]
    public string Preview { get; init; } = "";

    [JsonPropertyName("simulation_id")]
    public string SimulationId { get; init; } = "";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "pending";
}
