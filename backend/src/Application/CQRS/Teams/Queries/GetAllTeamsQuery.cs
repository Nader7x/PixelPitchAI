using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Teams.Queries;

public class GetAllTeamsQuery : IRequest<GetAllTeamsQueryResponse>
{
    public string? Country { get; set; }
    public string? League { get; set; }
}

public class GetAllTeamsQueryResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public List<TeamDto>? Teams { get; init; } = [];
}

public class GetAllTeamsQueryHandler(IUnitOfWork unitOfWork, ITeamMapper teamMapper)
    : IRequestHandler<GetAllTeamsQuery, GetAllTeamsQueryResponse>
{
    public async Task<GetAllTeamsQueryResponse> Handle(
        GetAllTeamsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var teams = unitOfWork.Teams.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Country))
                teams = teams.Where(t => t.Country == request.Country);

            if (!string.IsNullOrWhiteSpace(request.League))
                teams = teams.Where(t => t.League == request.League);
            
            var teamDtoS = teamMapper.ToDtoList(await teams.ToListAsync(cancellationToken));

            return new GetAllTeamsQueryResponse
            {
                Succeeded = true,
                Teams = teamDtoS
            };
        }
        catch (Exception e)
        {
            return new GetAllTeamsQueryResponse { Succeeded = false, Error = e.InnerException?.Message };
        }
    }
}
