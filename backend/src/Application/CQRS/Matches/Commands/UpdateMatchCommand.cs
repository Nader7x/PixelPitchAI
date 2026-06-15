using System.ComponentModel.DataAnnotations;
using Application.Helpers;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Matches.Commands;

public class UpdateMatchCommand : IRequest<UpdateMatchCommandResponse>
{
    public required int Id { get; set; }

    public int? HomeSeasonId { get; set; }

    public int? AwaySeasonId { get; set; }

    public int? HomeTeamId { get; set; }

    public int? AwayTeamId { get; set; }

    public DateTime ScheduledDateTimeUtc { get; set; }

    public int? StadiumId { get; set; }

    public int? MatchWeek { get; set; }

    public int? HomeCoachId { get; set; }

    public int? AwayCoachId { get; set; }

    // Match result data
    public int? HomeTeamScore { get; set; }

    public int? AwayTeamScore { get; set; }

    public int? WinningTeamId { get; set; }

    public int? LosingTeamId { get; set; }

    public bool IsDraw { get; set; }

    [StringLength(50)]
    public string? MatchStatus { get; set; }
}

public class UpdateMatchCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int Id { get; init; }
    public string? HomeTeamName { get; init; }
    public string? AwayTeamName { get; init; }
    public DateTime ScheduledDateTime { get; init; }
    public string? Error { get; init; }
}

public class UpdateMatchCommandHandler(IUnitOfWork unitOfWork, IMatchMapper matchMapper)
    : IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>
{
    private readonly IMatchMapper _matchMapper = matchMapper;

    public async Task<UpdateMatchCommandResponse> Handle(
        UpdateMatchCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (
                request is
                {
                    HomeTeamId: not null,
                    AwayTeamId: not null,
                    HomeSeasonId: not null,
                    AwaySeasonId: not null
                }
            )
                if (
                    request.HomeTeamId == request.AwayTeamId
                    && request.HomeSeasonId == request.AwaySeasonId
                )
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            "Home team and away team must be either different Teams or Same Teams With a different Seasons",
                    };

            var match = await unitOfWork.Matches.GetByIdAsync(request.Id, cancellationToken);
            if (match == null)
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found",
                };
            if (request is { HomeSeasonId: not null, AwaySeasonId: not null })
            {
                var seasons = await unitOfWork.Seasons.GetByIdsAsync(
                    [request.HomeSeasonId.Value, request.AwaySeasonId.Value],
                    cancellationToken
                );
                if (seasons.Count != 2)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = "The Team Seasons or one of them Does not exist",
                    };
            }

            if (request is { HomeTeamId: not null, AwayTeamId: not null })
            {
                var (homeTeam, awayTeam) = await unitOfWork.Teams.GetByIdsAsync(
                    [request.HomeTeamId.Value, request.AwayTeamId.Value],
                    cancellationToken
                );
                if (homeTeam == null)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Home Team with ID {request.HomeTeamId} not found",
                    };

                if (awayTeam == null)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Team with ID {request.AwayTeamId} not found",
                    };
            }

            if (request.StadiumId.HasValue)
            {
                var stadium = await unitOfWork.Stadiums.GetByIdAsync(
                    request.StadiumId.Value,
                    cancellationToken
                );
                if (stadium == null)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with ID {request.StadiumId} not found",
                    };
            }

            if (request is { HomeCoachId: not null, AwayCoachId: not null })
            {
                var (homeCoach, awayCoach) = await unitOfWork.Coaches.GetByIdsAsync(
                    [request.HomeCoachId.Value, request.AwayCoachId.Value],
                    cancellationToken
                );

                if (homeCoach == null)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Home Coach with ID {request.HomeCoachId} not found",
                    };

                if (awayCoach == null)
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Coach with ID {request.AwayCoachId} not found",
                    };
            }

            _matchMapper.UpdateMatchFromCommand(request, match);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateMatchCommandResponse
            {
                Succeeded = true,
                Id = match.Id,
                HomeTeamName = match.HomeTeamInMatchName,
                AwayTeamName = match.AwayTeamInMatchName,
                ScheduledDateTime = match.ScheduledDateTimeUtc,
            };
        }
        catch (Exception ex)
        {
            return new UpdateMatchCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
