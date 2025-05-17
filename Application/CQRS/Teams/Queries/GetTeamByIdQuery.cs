
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
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Logo { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? League { get; set; }
    public DateTime FoundationDate { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public int? StadiumId { get; set; }
    public string? StadiumName { get; set; }
}

public class GetTeamByIdQueryHandler(IUnitOfWork unitOfWork, TeamMapper teamMapper)
    : IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse>
{
    private readonly TeamMapper _teamMapper = teamMapper;


    public async Task<GetTeamByIdQueryResponse> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await unitOfWork.Teams.GetByIdAsyncWithStadium(request.Id);
        
        if (team == null)
            return null;
        var teamResponse = _teamMapper.ToTeamByIdQueryResponse(team);
            
        return teamResponse;
    }
}
