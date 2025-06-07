using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Seasons.Queries;

public class GetSeasonTeamsQuery : IRequest<GetSeasonTeamsQueryResponse>
{
    public int SeasonId { get; set; }
}

public class GetSeasonTeamsQueryResponse
{
    public bool Succeeded { get; set; }
    public string? error { get; set; }
    public IReadOnlyList<TeamSeasons>? TeamSeasons { get; set; }
}

public class GetSeasonTeamsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSeasonTeamsQuery, GetSeasonTeamsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<GetSeasonTeamsQueryResponse> Handle(GetSeasonTeamsQuery request,
        CancellationToken cancellationToken)
    {
        var seasonTeams = await _unitOfWork.TeamSeasons.GetTeamsBySeasonIdAsync(request.SeasonId);
        if (seasonTeams.Count == 0)
            return new GetSeasonTeamsQueryResponse
            {
                Succeeded = false,
                error = "Season not found"
            };
        return new GetSeasonTeamsQueryResponse
        {
            Succeeded = true,
            TeamSeasons = seasonTeams
        };
    }
}