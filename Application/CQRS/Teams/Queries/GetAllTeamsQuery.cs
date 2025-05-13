
using Domain.Interfaces;
using Domain.Models;
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
    public List<TeamDto> Teams { get; set; } = new();
}

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Logo { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string League { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    
    // Static mapper method
    public static TeamDto FromTeam(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortName = team.ShortName,
            Logo = team.Logo,
            Country = team.Country,
            City = team.City,
            League = team.League,
            PrimaryColor = team.PrimaryColor,
            SecondaryColor = team.SecondaryColor
        };
    }
}

public class GetAllTeamsQueryHandler : IRequestHandler<GetAllTeamsQuery, GetAllTeamsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetAllTeamsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
            Teams = teams.Select(TeamDto.FromTeam).ToList()
        };
    }
}
