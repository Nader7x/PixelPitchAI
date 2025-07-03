using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Seasons.Commands;

public class DeleteSeasonCommand : IRequest<DeleteSeasonCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteSeasonCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; set; }
}

public class DeleteSeasonCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSeasonCommand, DeleteSeasonCommandResponse>
{
    public async Task<DeleteSeasonCommandResponse> Handle(
        DeleteSeasonCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var season = await unitOfWork.Seasons.GetByIdAsync(request.Id);
            if (season == null)
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found",
                };

            // Check if there are matches associated with this season
            var matches = await unitOfWork.Matches.FindAsync(
                m => m.HomeTeamSeasonId == request.Id || m.AwayTeamSeasonId == request.Id,
                cancellationToken
            );
            if (matches != null)
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated matches",
                };

            // Check if there are team statistics associated with this season
            var teamStats = await unitOfWork.TeamSeasons.FindAsync(
                ts => ts.SeasonId == request.Id,
                cancellationToken
            );
            if (teamStats != null)
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated team statistics",
                };

            // Check if this is the only active season for its league
            if (season.IsActive)
            {
                var activeSeasons = await unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName == season.LeagueName && s.Country == season.Country && s.IsActive
                );

                if (activeSeasons.Count() == 1)
                    return new DeleteSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error = "Cannot delete the only active season for this league",
                    };
            }

            unitOfWork.Seasons.DeleteAsync(season);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteSeasonCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new DeleteSeasonCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
