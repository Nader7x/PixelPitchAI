using MediatR;
using System.ComponentModel.DataAnnotations;
using Application.Mappers;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Commands;

public class CreateMatchCommand : IRequest<CreateMatchCommandResponse>
{
    [Required]
    public int HomeSeasonId { get; set; }
    public int AwaySeasonId { get; set; }

    [Required]
    public int HomeTeamId { get; set; }
    
    [Required]
    public int AwayTeamId { get; set; }
    public string? HomeTeamInMatchName { get; init; }
    public string? AwayTeamInMatchName { get; init; }
    
    [Required]
    public DateTime ScheduledDateTimeUtc { get; set; }
    
    public int? StadiumId { get; set; }
    
    public int? MatchWeek { get; set; }
    
    public int? HomeCoachId { get; set; }
    
    public int? AwayCoachId { get; set; }
    
    [StringLength(50)]
    public string? MatchStatus { get; set; } = "Scheduled";
    public required string CreatorId { get; init; }
    public DateTime? ModelSimulationStartTimeUtc { get; init; }
    public bool IsLive { get; init; } = false;

}

public class CreateMatchCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; init; }
    public string? HomeTeamName { get; init; }
    public string? AwayTeamName { get; init; }
    public string? Error { get; set; }
    public StartMatchResponse? ApiResponse { get; set; }
}

public class CreateMatchCommandHandler(IUnitOfWork unitOfWork, MatchMapper matchMapper)
    : IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>
{
    public async Task<CreateMatchCommandResponse> Handle(CreateMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate Season exists
            var season = await unitOfWork.Seasons.GetByIdAsync(request.HomeSeasonId);
            if (season == null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with ID {request.HomeSeasonId} not found"
                };
            }
            
            // Validate HomeTeam exists
            var homeTeam = await unitOfWork.Teams.GetByIdAsync(request.HomeTeamId);
            if (homeTeam == null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Home Team with ID {request.HomeTeamId} not found"
                };
            }
            
            // Validate AwayTeam exists
            var awayTeam = await unitOfWork.Teams.GetByIdAsync(request.AwayTeamId);
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
                var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.StadiumId.Value);
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
                var homeCoach = await unitOfWork.Coaches.GetByIdAsync(request.HomeCoachId.Value);
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
                var awayCoach = await unitOfWork.Coaches.GetByIdAsync(request.AwayCoachId.Value);
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
            var existingMatches = await unitOfWork.Matches.FindAsync(m =>
                m.HomeTeamSeasonId == request.HomeSeasonId &&
                m.AwayTeamSeasonId == request.AwaySeasonId &&
                ((m.HomeTeamId == request.HomeTeamId && m.AwayTeamId == request.AwayTeamId) ||
                (m.HomeTeamId == request.AwayTeamId && m.AwayTeamId == request.HomeTeamId)) &&
                m.ScheduledDateTimeUtc.Date == request.ScheduledDateTimeUtc.Date);

            if (existingMatches!=null)
            {
                return new CreateMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"A match between {homeTeam.Name} and {awayTeam.Name} already exists on {request.ScheduledDateTimeUtc.ToShortDateString()}"
                };
            }
            
            // Create new match
            var match = matchMapper.ToMatchFromCreate(request);

            await unitOfWork.Matches.AddAsync(match);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateMatchCommandResponse
            {
                Succeeded = true,
                Id = match.Id,
                HomeTeamName = homeTeam.Name,
                AwayTeamName = awayTeam.Name,
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

public abstract class StartMatchResponse
{
    public int MatchId { get; init; }
    public string? HomeTeamName { get; init; }
    public string? AwayTeamName { get; init; }
    public string? HomeTeamSeason { get; init; }
    public string? AwayTeamSeason { get; init; }
    public int? EventsCount { get; init; }
    public double? ExecutionTime { get; init; } 
    public string? Preview { get; init; }
    public string? SimulationId { get; init; }
    public string? Status { get; init; }
}
