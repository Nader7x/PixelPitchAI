using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Teams.Queries;

public class GetAllTeamsQuery : IRequest<GetAllTeamsQueryResponse>
{
    // Optional filter parameters
    public string? Country { get; set; }
    public string? League { get; set; }
}

public class GetAllTeamsQueryResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
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
            var teams = await unitOfWork.Teams.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(request.Country))
                teams = teams.Where(t => t.Country == request.Country).ToList();

            if (!string.IsNullOrWhiteSpace(request.League))
                teams = teams.Where(t => t.League == request.League).ToList();

            return new GetAllTeamsQueryResponse
            {
                Succeeded = true,
                Teams = teamMapper.ToDtoList(teams),
            };
        }
        catch (Exception e)
        {
            return new GetAllTeamsQueryResponse { Succeeded = false, Error = e.Message };
        }
    }
}
