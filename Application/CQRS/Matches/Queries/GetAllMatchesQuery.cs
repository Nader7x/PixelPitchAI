using Application.Dtos;
using MediatR;
using Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Application.CQRS.Matches.Queries;

public class GetAllMatchesQuery : IRequest<GetAllMatchesQueryResponse>
{
    // Optional parameters for filtering
    public int? SeasonId { get; set; }
    public int? TeamId { get; set; }
    public string Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MatchWeek { get; set; }
}

public class GetAllMatchesQueryResponse
{
    public bool Succeeded { get; set; }
    public List<MatchDto> Matches { get; set; }
    public string Error { get; set; }
}

public class GetAllMatchesQueryHandler : IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetAllMatchesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetAllMatchesQueryResponse> Handle(GetAllMatchesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Domain.Models.Match> matches;
            
            // Build filter expression based on provided parameters
            if (request.SeasonId.HasValue || request.TeamId.HasValue || !string.IsNullOrEmpty(request.Status) ||
                request.FromDate.HasValue || request.ToDate.HasValue || request.MatchWeek.HasValue)
            {
                // Apply combined filters
                matches = await _unitOfWork.Matches.GetAllAsync(m => 
                    (!request.SeasonId.HasValue || m.SeasonId == request.SeasonId.Value) &&
                    (!request.TeamId.HasValue || m.HomeTeamId == request.TeamId.Value || m.AwayTeamId == request.TeamId.Value) &&
                    (string.IsNullOrEmpty(request.Status) || m.MatchStatus == request.Status) &&
                    (!request.FromDate.HasValue || m.ScheduledDateTimeUTC >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || m.ScheduledDateTimeUTC <= request.ToDate.Value) &&
                    (!request.MatchWeek.HasValue || m.MatchWeek == request.MatchWeek.Value)
                );
            }
            else
            {
                // Get all matches if no filters provided
                matches = await _unitOfWork.Matches.GetAllWithDetailsAsync();
            }
            
            var matchDtos = new List<MatchDto>();
            foreach (var match in matches)
            {
                matchDtos.Add(new MatchDto
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
                });
            }
            
            return new GetAllMatchesQueryResponse
            {
                Succeeded = true,
                Matches = matchDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllMatchesQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
