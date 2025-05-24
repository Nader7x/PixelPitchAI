using Application.Dtos;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Teams.Queries;

public class GetTeamSeasonsQuery : IRequest<GetTeamSeasonsQueryResponse>
{
   public int TeamId { get; set; }
}
public class GetTeamSeasonsQueryResponse
{
    public bool Succeeded { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public IReadOnlyList<TeamSeasonsDto>? Seasons { get; set; }
    public string? Error { get; set; }
}
public class GetTeamSeasonsQueryResponseHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTeamSeasonsQuery, GetTeamSeasonsQueryResponse>
{
    public async Task<GetTeamSeasonsQueryResponse> Handle(GetTeamSeasonsQuery request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Teams.GetByIdAsync(request.TeamId);
        if (team == null)
        {
            return new GetTeamSeasonsQueryResponse()
            {
                Succeeded = false,
                Error = "Team not found"
            };
        }

        var seasons = await unitOfWork.TeamSeasons.GetSeasonsByTeamIdAsync(team.Id);
        var teamSeasonsDtos = seasons.Select(ts => new TeamSeasonsDto
        {
            SeasonId = ts.SeasonId,
            SeasonName = ts.Season.Name
        });

        return new GetTeamSeasonsQueryResponse
        {
            TeamId = team.Id,
            TeamName = team.Name,
            Succeeded = true,
            Seasons = teamSeasonsDtos.ToList()
        };
    }
}

