using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Commands;

public class UpdateMatchCommand : IRequest<UpdateMatchCommandResponse>
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    public int SeasonId { get; set; }
    
    [Required]
    public int HomeTeamId { get; set; }
    
    [Required]
    public int AwayTeamId { get; set; }
    
    [Required]
    public DateTime ScheduledDateTimeUTC { get; set; }
    
    public int? StadiumId { get; set; }
    
    public int? MatchWeek { get; set; }
    
    public int? HomeCoachId { get; set; }
    
    public int? AwayCoachId { get; set; }
    
    // Match result data
    public int? HomeTeamScore { get; set; }
    
    public int? AwayTeamScore { get; set; }
    
    public int? WinningTeamId { get; set; }
    
    public int? LosingTeamId { get; set; }
    
    public bool IsDraw { get; set; }
    
    // Match status
    [StringLength(50)]
    public string MatchStatus { get; set; }
    
    // Match statistics
    public int? HomeTeamPossession { get; set; }
    public int? AwayTeamPossession { get; set; }
    public int? HomeTeamShots { get; set; }
    public int? AwayTeamShots { get; set; }
    public int? HomeTeamShotsOnTarget { get; set; }
    public int? AwayTeamShotsOnTarget { get; set; }
    public int? HomeTeamCorners { get; set; }
    public int? AwayTeamCorners { get; set; }
    public int? HomeTeamFouls { get; set; }
    public int? AwayTeamFouls { get; set; }
    public int? HomeTeamYellowCards { get; set; }
    public int? AwayTeamYellowCards { get; set; }
    public int? HomeTeamRedCards { get; set; }
    public int? AwayTeamRedCards { get; set; }
}

public class UpdateMatchCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string HomeTeamName { get; set; }
    public string AwayTeamName { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public string Error { get; set; }
}

public class UpdateMatchCommandHandler : IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public UpdateMatchCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<UpdateMatchCommandResponse> Handle(UpdateMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if match exists
            var match = await _unitOfWork.Matches.GetByIdAsync(request.Id);
            if (match == null)
            {
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found"
                };
            }
            
            // Validate Season exists
            var season = await _unitOfWork.Seasons.GetByIdAsync(request.SeasonId);
            if (season == null)
            {
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.SeasonId} not found"
                };
            }
            
            // Validate HomeTeam exists
            var homeTeam = await _unitOfWork.Teams.GetByIdAsync(request.HomeTeamId);
            if (homeTeam == null)
            {
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Home Team with ID {request.HomeTeamId} not found"
                };
            }
            
            // Validate AwayTeam exists
            var awayTeam = await _unitOfWork.Teams.GetByIdAsync(request.AwayTeamId);
            if (awayTeam == null)
            {
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Away Team with ID {request.AwayTeamId} not found"
                };
            }
            
            // Validate Stadium if provided
            if (request.StadiumId.HasValue)
            {
                var stadium = await _unitOfWork.Stadiums.GetByIdAsync(request.StadiumId.Value);
                if (stadium == null)
                {
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Stadium with ID {request.StadiumId} not found"
                    };
                }
            }
            
            // Validate HomeCoach if provided
            if (request.HomeCoachId.HasValue)
            {
                var homeCoach = await _unitOfWork.Coaches.GetByIdAsync(request.HomeCoachId.Value);
                if (homeCoach == null)
                {
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Home Coach with ID {request.HomeCoachId} not found"
                    };
                }
            }
            
            // Validate AwayCoach if provided
            if (request.AwayCoachId.HasValue)
            {
                var awayCoach = await _unitOfWork.Coaches.GetByIdAsync(request.AwayCoachId.Value);
                if (awayCoach == null)
                {
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Coach with ID {request.AwayCoachId} not found"
                    };
                }
            }
            
            // Validate teams are different
            if (request.HomeTeamId == request.AwayTeamId)
            {
                return new UpdateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = "Home team and away team must be different"
                };
            }
            
            // Check if match already exists for these teams on the same date (excluding current match)
            if (match.HomeTeamId != request.HomeTeamId || match.AwayTeamId != request.AwayTeamId || 
                match.ScheduledDateTimeUTC.Date != request.ScheduledDateTimeUTC.Date)
            {
                var existingMatches = await _unitOfWork.Matches.FindAsync(m => 
                    m.Id != request.Id &&
                    m.SeasonId == request.SeasonId &&
                    ((m.HomeTeamId == request.HomeTeamId && m.AwayTeamId == request.AwayTeamId) ||
                    (m.HomeTeamId == request.AwayTeamId && m.AwayTeamId == request.HomeTeamId)) &&
                    m.ScheduledDateTimeUTC.Date == request.ScheduledDateTimeUTC.Date);
                    
                if (existingMatches != null)
                {
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"A match between {homeTeam.Name} and {awayTeam.Name} already exists on {request.ScheduledDateTimeUTC.ToShortDateString()}"
                    };
                }
            }
            
            // Validate match statistics
            if (request.HomeTeamPossession.HasValue && request.AwayTeamPossession.HasValue)
            {
                if (request.HomeTeamPossession.Value + request.AwayTeamPossession.Value != 100)
                {
                    return new UpdateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = "Home team and away team possession must sum to 100%"
                    };
                }
            }
            
            // Update match properties
            match.SeasonId = request.SeasonId;
            match.HomeTeamId = request.HomeTeamId;
            match.AwayTeamId = request.AwayTeamId;
            match.ScheduledDateTimeUTC = request.ScheduledDateTimeUTC;
            match.StadiumId = request.StadiumId;
            match.MatchWeek = request.MatchWeek;
            match.HomeCoachId = request.HomeCoachId;
            match.AwayCoachId = request.AwayCoachId;
            match.HomeTeamScore = request.HomeTeamScore;
            match.AwayTeamScore = request.AwayTeamScore;
            match.WinningTeamId = request.WinningTeamId;
            match.LosingTeamId = request.LosingTeamId;
            match.IsDraw = request.IsDraw;
            match.MatchStatus = request.MatchStatus;
            match.HomeTeamPossession = request.HomeTeamPossession;
            match.AwayTeamPossession = request.AwayTeamPossession;
            match.HomeTeamShots = request.HomeTeamShots;
            match.AwayTeamShots = request.AwayTeamShots;
            match.HomeTeamShotsOnTarget = request.HomeTeamShotsOnTarget;
            match.AwayTeamShotsOnTarget = request.AwayTeamShotsOnTarget;
            match.HomeTeamCorners = request.HomeTeamCorners;
            match.AwayTeamCorners = request.AwayTeamCorners;
            match.HomeTeamFouls = request.HomeTeamFouls;
            match.AwayTeamFouls = request.AwayTeamFouls;
            match.HomeTeamYellowCards = request.HomeTeamYellowCards;
            match.AwayTeamYellowCards = request.AwayTeamYellowCards;
            match.HomeTeamRedCards = request.HomeTeamRedCards;
            match.AwayTeamRedCards = request.AwayTeamRedCards;
            
            _unitOfWork.Matches.UpdateAsync(match);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new UpdateMatchCommandResponse
            {
                Succeeded = true,
                Id = match.Id,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
                ScheduledDateTime = match.ScheduledDateTimeUTC
            };
        }
        catch (Exception ex)
        {
            return new UpdateMatchCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
