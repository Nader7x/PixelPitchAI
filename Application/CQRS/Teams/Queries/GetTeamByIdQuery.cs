
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
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Logo { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string League { get; set; }
    public DateTime FoundationDate { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public int? StadiumId { get; set; }
    public string StadiumName { get; set; }
}

public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, GetTeamByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetTeamByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetTeamByIdQueryResponse> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _unitOfWork.Teams.GetByIdAsync(request.Id);
        
        if (team == null)
            return null;
            
        return new GetTeamByIdQueryResponse
        {
            Id = team.Id,
            Name = team.Name,
            ShortName = team.ShortName,
            Logo = team.Logo,
            Country = team.Country,
            City = team.City,
            League = team.League,
            FoundationDate = team.FoundationDate,
            PrimaryColor = team.PrimaryColor,
            SecondaryColor = team.SecondaryColor,
            StadiumId = team.StadiumId,
            StadiumName = team.Stadium?.Name
        };
    }
}
