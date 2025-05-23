
using Application.Dtos;
using Application.Mappers;
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
    public string? error { get; set; }
    public List<TeamDto> Teams { get; set; }
}



public class GetAllTeamsQueryHandler : IRequestHandler<GetAllTeamsQuery, GetAllTeamsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TeamMapper _teamMapper;
    
    public GetAllTeamsQueryHandler(IUnitOfWork unitOfWork, TeamMapper teamMapper)
    {
        _unitOfWork = unitOfWork;
        _teamMapper = teamMapper;
    }
    
    public async Task<GetAllTeamsQueryResponse> Handle(GetAllTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await _unitOfWork.Teams.GetAllAsync();
        
        // Apply filters if provided
        if (!string.IsNullOrEmpty(request.Country))
        {
            teams = teams.Where(t => t.Country == request.Country).ToList();
        }
        
        if (!string.IsNullOrEmpty(request.League))
        {
            teams = teams.Where(t => t.League == request.League).ToList();
        }
        
        return new GetAllTeamsQueryResponse
        {
            Teams = _teamMapper.ToDtoList(teams)
        };
    }
}
