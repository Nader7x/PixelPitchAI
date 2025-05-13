using Application.Dtos;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Queries;

public class GetMatchByIdQuery : IRequest<GetMatchByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetMatchByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public MatchDto Match { get; set; }
    public string Error { get; set; }
}

public class GetMatchByIdQueryHandler : IRequestHandler<GetMatchByIdQuery, GetMatchByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetMatchByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetMatchByIdQueryResponse> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var match = await _unitOfWork.Matches.GetByIdWithDetailsAsync(request.Id);
            if (match == null)
            {
                return new GetMatchByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found"
                };
            }
            
            var matchDto = new MatchDto
            {
                Id = match.Id,
                SeasonId = match.SeasonId,
                SeasonName = match.Season?.Name,
                HomeTeamId = match.HomeTeamId,
                HomeTeamName = match.HomeTeam?.Name,
                AwayTeamId = match.AwayTeamId,
                AwayTeamName = match.AwayTeam?.Name,
                ScheduledDateTimeUTC = match.ScheduledDateTimeUTC,
                StadiumId = match.StadiumId,
                StadiumName = match.Stadium?.Name,
                MatchWeek = match.MatchWeek,
                HomeCoachId = match.HomeCoachId,
                AwayCoachId = match.AwayCoachId,
                HomeTeamScore = match.HomeTeamScore,
                AwayTeamScore = match.AwayTeamScore,
                WinningTeamId = match.WinningTeamId,
                LosingTeamId = match.LosingTeamId,
                IsDraw = match.IsDraw,
                MatchStatus = match.MatchStatus,
                HomeTeamPossession = match.HomeTeamPossession,
                AwayTeamPossession = match.AwayTeamPossession,
                HomeTeamShots = match.HomeTeamShots,
                AwayTeamShots = match.AwayTeamShots,
                HomeTeamShotsOnTarget = match.HomeTeamShotsOnTarget,
                AwayTeamShotsOnTarget = match.AwayTeamShotsOnTarget,
                HomeTeamCorners = match.HomeTeamCorners,
                AwayTeamCorners = match.AwayTeamCorners,
                HomeTeamFouls = match.HomeTeamFouls,
                AwayTeamFouls = match.AwayTeamFouls,
                HomeTeamYellowCards = match.HomeTeamYellowCards,
                AwayTeamYellowCards = match.AwayTeamYellowCards,
                HomeTeamRedCards = match.HomeTeamRedCards,
                AwayTeamRedCards = match.AwayTeamRedCards
            };
            
            return new GetMatchByIdQueryResponse
            {
                Succeeded = true,
                Match = matchDto
            };
        }
        catch (Exception ex)
        {
            return new GetMatchByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
