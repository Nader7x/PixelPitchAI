using Domain.Models;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Application.Mappers;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Commands;

public class CreateMatchCommand : IRequest<CreateMatchCommandResponse>
{
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
    
    [StringLength(50)]
    public string MatchStatus { get; set; } = "Scheduled";
}

public class CreateMatchCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string? HomeTeamName { get; set; }
    public string? AwayTeamName { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public string Error { get; set; }
}

public class CreateMatchCommandHandler : IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MatchMapper _matchMapper;
    
    public CreateMatchCommandHandler(IUnitOfWork unitOfWork, MatchMapper matchMapper)
    {
        _unitOfWork = unitOfWork;
        _matchMapper = matchMapper;
    }
    
    public async Task<CreateMatchCommandResponse> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate Season exists
            var season = await _unitOfWork.Seasons.GetByIdAsync(request.SeasonId);
            if (season == null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.SeasonId} not found"
                };
            }
            
            // Validate HomeTeam exists
            var homeTeam = await _unitOfWork.Teams.GetByIdAsync(request.HomeTeamId);
            if (homeTeam == null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Home Team with ID {request.HomeTeamId} not found"
                };
            }
            
            // Validate AwayTeam exists
            var awayTeam = await _unitOfWork.Teams.GetByIdAsync(request.AwayTeamId);
            if (awayTeam == null)
            {
                return new CreateMatchCommandResponse
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
                    return new CreateMatchCommandResponse
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
                    return new CreateMatchCommandResponse
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
                    return new CreateMatchCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Away Coach with ID {request.AwayCoachId} not found"
                    };
                }
            }
            
            // Validate teams are different
            if (request.HomeTeamId == request.AwayTeamId)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = "Home team and away team must be different"
                };
            }
            
            // Check if match already exists for these teams on the same date
            var existingMatches = await _unitOfWork.Matches.FindAsync(m => 
                m.SeasonId == request.SeasonId &&
                ((m.HomeTeamId == request.HomeTeamId && m.AwayTeamId == request.AwayTeamId) ||
                (m.HomeTeamId == request.AwayTeamId && m.AwayTeamId == request.HomeTeamId)) &&
                m.ScheduledDateTimeUTC.Date == request.ScheduledDateTimeUTC.Date);
                
            if (existingMatches!=null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"A match between {homeTeam.Name} and {awayTeam.Name} already exists on {request.ScheduledDateTimeUTC.ToShortDateString()}"
                };
            }
            
            // Create new match
            var match = _matchMapper.ToMatchFromCreate(request);
            
            await _unitOfWork.Matches.AddAsync(match);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new CreateMatchCommandResponse
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
            return new CreateMatchCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
