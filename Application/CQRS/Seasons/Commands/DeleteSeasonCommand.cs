using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Seasons.Commands;

public class DeleteSeasonCommand : IRequest<DeleteSeasonCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteSeasonCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
}

public class DeleteSeasonCommandHandler : IRequestHandler<DeleteSeasonCommand, DeleteSeasonCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteSeasonCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<DeleteSeasonCommandResponse> Handle(DeleteSeasonCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var season = await _unitOfWork.Seasons.GetByIdAsync(request.Id);
            if (season == null)
            {
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found"
                };
            }
            
            // Check if there are matches associated with this season
            var matches = await _unitOfWork.Matches.FindAsync(m => m.SeasonId == request.Id);
            if (matches != null)
            {
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated matches"
                };
            }
            
            // Check if there are team statistics associated with this season
            var teamStats = await _unitOfWork.TeamSeasonStats.FindAsync(ts => ts.SeasonId == request.Id);
            if (teamStats != null)
            {
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated team statistics"
                };
            }
            
            // Check if this is the only active season for its league
            if (season.IsActive)
            {
                var activeSeasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.LeagueName == season.LeagueName && 
                    s.Country == season.Country && 
                    s.IsActive);
                    
                if (activeSeasons.Count() == 1)
                {
                    return new DeleteSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error = "Cannot delete the only active season for this league"
                    };
                }
            }
            
            _unitOfWork.Seasons.DeleteAsync(season);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new DeleteSeasonCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new DeleteSeasonCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
