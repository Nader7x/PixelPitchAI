using Domain.Interfaces;
using Domain.Models;
using Application.CQRS;

namespace Application.CQRS.Seasons.Commands;

public class DeleteSeasonCommand : IRequest<DeleteSeasonCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteSeasonCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }
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
            var season = await unitOfWork.Seasons.GetByIdAsync(request.Id, cancellationToken);
            if (season == null)
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found",
                };

            // Check if there are matches associated with this season
            var matches = await unitOfWork.Matches.GetAllAsync(m =>
                m.HomeTeamSeasonId == request.Id || m.AwayTeamSeasonId == request.Id
            );
            if (matches.Any())
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated matches",
                };

            // Check if there are team statistics associated with this season
            var teamSeasons = await unitOfWork.TeamSeasons.GetAllAsync(ts =>
                ts.SeasonId == request.Id
            );
            if (teamSeasons.Any())
                return new DeleteSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete season as it has associated team seasons",
                };

            // Check if this is the only active season for its league
            if (season.IsActive)
            {
                var activeSeasons = await unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName == season.LeagueName && s.Country == season.Country && s.IsActive
                );

                var seasons = activeSeasons as Season[] ?? activeSeasons.ToArray();
                if (seasons.Length != 0 && seasons.Length == 1)
                    return new DeleteSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error = "Cannot delete the only active season for this league",
                    };
            }

            unitOfWork.Seasons.Delete(season);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteSeasonCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new DeleteSeasonCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
