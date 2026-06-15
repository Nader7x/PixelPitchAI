using Domain.Interfaces;
using Domain.Models;
using Application.CQRS;

namespace Application.CQRS.Seasons.Queries;

public class GetSeasonTeamsQuery : IRequest<GetSeasonTeamsQueryResponse>
{
    public int SeasonId { get; set; }
}

public class GetSeasonTeamsQueryResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<TeamSeason>? TeamSeasons { get; init; }
}

public class GetSeasonTeamsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<GetSeasonTeamsQueryResponse> Handle(
        GetSeasonTeamsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var seasonTeams = await _unitOfWork.TeamSeasons.GetTeamsBySeasonIdAsync(
                request.SeasonId
            );
            // if (seasonTeams.Count == 0)
            //     return new GetSeasonTeamsQueryResponse
            //     {
            //         Succeeded = false,
            //         Error = "Season not found",
            //     };
            return new GetSeasonTeamsQueryResponse { Succeeded = true, TeamSeasons = seasonTeams };
        }
        catch (Exception ex)
        {
            return new GetSeasonTeamsQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
