using Application.Dtos;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Seasons.Queries;

public class GetSeasonByIdQuery : IRequest<GetSeasonByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetSeasonByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public SeasonDto Season { get; set; }
    public string Error { get; set; }
}

public class GetSeasonByIdQueryHandler : IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetSeasonByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetSeasonByIdQueryResponse> Handle(GetSeasonByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var season = await _unitOfWork.Seasons.GetByIdAsync(request.Id);
            if (season == null)
            {
                return new GetSeasonByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found"
                };
            }
            
            // Get team standings summary for the season
            var teamStats = await _unitOfWork.TeamSeasonStats.GetAllAsync(ts => ts.SeasonId == request.Id);
            var standings = teamStats.Select(ts => new TeamStandingDto
            {
                TeamId = ts.TeamId,
                TeamName = ts.Team?.Name,
                Position = ts.Position,
                Played = ts.MatchesPlayed,
                Won = ts.Wins,
                Drawn = ts.Draws,
                Lost = ts.Losses,
                GoalsFor = ts.GoalsScored,
                GoalsAgainst = ts.GoalsConceded,
                GoalDifference = ts.GoalDifference,
                Points = ts.Points
            }).OrderBy(s => s.Position).ToList();
            
            // Count the matches for this season
            var matches = await _unitOfWork.Matches.GetAllAsync(m => m.SeasonId == request.Id);
            
            var seasonDto = new SeasonDto
            {
                Id = season.Id,
                Name = season.Name,
                LeagueName = season.LeagueName,
                Country = season.Country,
                CurrentRound = season.CurrentRound,
                TotalRounds = season.TotalRounds,
                IsActive = season.IsActive,
                IsCompleted = season.IsCompleted,
                MatchCount = matches.Count(),
                TeamStandings = standings
            };
            
            return new GetSeasonByIdQueryResponse
            {
                Succeeded = true,
                Season = seasonDto
            };
        }
        catch (Exception ex)
        {
            return new GetSeasonByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
