using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Teams.Queries;

public class GetTeamByIdQuery : IRequest<GetTeamByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetTeamByIdQueryResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public bool NotFound { get; init; }
    public TeamDto? Team { get; init; }
}

public class GetTeamByIdQueryHandler(IUnitOfWork unitOfWork, ITeamMapper teamMapper)
    : IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse>
{
    private readonly ITeamMapper _teamMapper = teamMapper;

    public async Task<GetTeamByIdQueryResponse> Handle(
        GetTeamByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var team = await unitOfWork.Teams.GetByIdAsyncWithStadium(request.Id);

            if (team == null)
                return new GetTeamByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = "Team not found",
                };
            var teamDto = _teamMapper.ToTeamDto(team);

            return new GetTeamByIdQueryResponse { Succeeded = true, Team = teamDto };
        }
        catch (Exception ex)
        {
            var innerMostException = ex.GetBaseException();
            return new GetTeamByIdQueryResponse
            {
                Succeeded = false,
                Error = innerMostException.Message,
            };
        }
    }
}
