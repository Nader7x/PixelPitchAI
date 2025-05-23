
using Application.Dtos;
using Application.Mappers;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Teams.Queries;

public class GetTeamByIdQuery : IRequest<GetTeamByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetTeamByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public string? error { get; set; }
    public TeamDto? Team { get; set; }
}

public class GetTeamByIdQueryHandler(IUnitOfWork unitOfWork, TeamMapper teamMapper)
    : IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse>
{
    private readonly TeamMapper _teamMapper = teamMapper;


    public async Task<GetTeamByIdQueryResponse> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Teams.GetByIdAsyncWithStadium(request.Id);
        
        if (team == null)
            return new GetTeamByIdQueryResponse
            {
                Succeeded = false,
                error = "Team not found"
            };
        var teamDto = _teamMapper.ToTeamDto(team);
            
        return new GetTeamByIdQueryResponse
        {
            Succeeded = true,
            Team = teamDto
        };
    }
}
