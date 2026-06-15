using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Seasons.Commands;

public class UpdateSeasonCommand : IRequest<UpdateSeasonCommandResponse>
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? LeagueName { get; set; }

    [Required]
    [StringLength(50)]
    public string? Country { get; set; }

    public int? CurrentRound { get; set; }

    public int TotalRounds { get; set; }

    public bool IsActive { get; set; }

    public bool IsCompleted { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }
}

public class UpdateSeasonCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int? Id { get; init; }
    public string? Name { get; init; }
    public string? Error { get; init; }
}

public class UpdateSeasonCommandHandler(IUnitOfWork unitOfWork, ISeasonMapper seasonMapper)
    : IRequestHandler<UpdateSeasonCommand, UpdateSeasonCommandResponse>
{
    public async Task<UpdateSeasonCommandResponse> Handle(
        UpdateSeasonCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Check if season exists
            var season = await unitOfWork.Seasons.GetByIdAsync(request.Id, cancellationToken);
            if (season == null)
                return new UpdateSeasonCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found",
                };

            // Validate date range
            if (request.EndDate <= request.StartDate)
                return new UpdateSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "End date must be after start date",
                };

            if (season.Name != request.Name)
            {
                var existingSeason = await unitOfWork.Seasons.FindAsync(
                    s => s.Name == request.Name,
                    cancellationToken
                );
                if (existingSeason != null && existingSeason.Id != request.Id)
                    return new UpdateSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Season with name '{request.Name}' already exists",
                    };
            }

            // Check if active season already exists for the same league (if this one is being set to active)
            if (request.IsActive && !season.IsActive)
            {
                var activeSeasons = await unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName == request.LeagueName && s.Country == request.Country && s.IsActive
                );

                if (activeSeasons.Any(s => s.Id != request.Id))
                    return new UpdateSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"An active season for {request.LeagueName} in {request.Country} already exists",
                    };
            }

            // Validate current round against total rounds
            if (request.CurrentRound.HasValue && request.CurrentRound.Value > request.TotalRounds)
                return new UpdateSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "Current round cannot be greater than total rounds",
                };

            // Update season properties
            seasonMapper.UpdateSeasonFromCommand(request, season);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateSeasonCommandResponse
            {
                Succeeded = true,
                Id = season.Id,
                Name = season.Name,
            };
        }
        catch (Exception ex)
        {
            return new UpdateSeasonCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
