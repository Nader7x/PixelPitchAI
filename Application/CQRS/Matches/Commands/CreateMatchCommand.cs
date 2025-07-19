using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Helpers;
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
    public bool IsLive { get; init; }
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
            if (
                request.HomeTeamId == request.AwayTeamId
                && request.HomeSeasonId == request.AwaySeasonId
            )
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = "Home and Away teams Must be different",
                };
            }
            var (homeSeason, awaySeason) = await unitOfWork.Seasons.GetByIdsAsync(
                [request.HomeSeasonId, request.AwaySeasonId],
                cancellationToken
            );

            if (request.HomeSeasonId == request.AwaySeasonId)
                homeSeason = awaySeason = homeSeason ?? awaySeason;
            if (homeSeason == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.HomeSeasonId} not found",
                };
            if (awaySeason == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.AwaySeasonId} not found",
                };

            var (homeTeam, awayTeam) = await unitOfWork.Teams.GetByIdsAsync(
                [request.HomeTeamId, request.AwayTeamId],
                cancellationToken
            );
            if (homeTeam == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Home Team with ID {request.HomeTeamId} not found",
                };

            if (awayTeam == null)
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Away Team with ID {request.AwayTeamId} not found",
                };

            if (request.StadiumId.HasValue)
            {
                var stadium = await unitOfWork.Stadiums.GetByIdAsync(
                    request.StadiumId.Value,
                    cancellationToken
                );
                if (stadium == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with ID {request.StadiumId} not found",
                    };
            }

            if (request is { HomeCoachId: not null, AwayCoachId: not null })
            {
                if (request.HomeCoachId == request.AwayCoachId)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = "Home and Away coaches cannot be the same",
                    };
                var (homeCoach, awayCoach) = await unitOfWork.Coaches.GetByIdsAsync(
                    [request.HomeCoachId.Value, request.AwayCoachId.Value],
                    cancellationToken
                );

                if (homeCoach == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Home Coach with ID {request.HomeCoachId} not found",
                    };

                if (awayCoach == null)
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Coach with ID {request.AwayCoachId} not found",
                    };
            }

            if (string.IsNullOrWhiteSpace(request.HomeTeamInMatchName))
                request.HomeTeamInMatchName =
                    $"{homeTeam.Name?.Replace(" ", "_")}_{homeSeason.Name.Split("/")[1]}";
            if (string.IsNullOrWhiteSpace(request.AwayTeamInMatchName))
                request.AwayTeamInMatchName =
                    $"{awayTeam.Name?.Replace(" ", "_")}_{awaySeason.Name.Split("/")[1]}";

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
