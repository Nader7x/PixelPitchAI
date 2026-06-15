using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;
using Domain.Models;
using Application.CQRS;

namespace Application.CQRS.Seasons.Commands;

public class CreateSeasonCommand : IRequest<CreateSeasonCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? LeagueName { get; set; }
    public int CompetitionId { get; set; }

    [Required]
    [StringLength(50)]
    public string? Country { get; set; }

    public int TotalRounds { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class CreateSeasonCommandResponse
{
    public bool Succeeded { get; init; }
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Error { get; init; }
}

public class CreateSeasonCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSeasonCommand, CreateSeasonCommandResponse>
{
    public async Task<CreateSeasonCommandResponse> Handle(
        CreateSeasonCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Validate date range
            if (request.EndDate <= request.StartDate)
                return new CreateSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = "End date must be after start date",
                };

            // Check if a season with the same name already exists
            var existingSeason = await unitOfWork.Seasons.FindAsync(
                s => s.Name.ToLower() == request.Name.ToLower(),
                cancellationToken
            );
            if (existingSeason != null)
                return new CreateSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with name '{request.Name}' already exists",
                };

            // Check if an active season already exists for the same league
            if (request.IsActive)
            {
                var activeSeason = await unitOfWork.Seasons.FindAsync(
                    s =>
                        s.LeagueName == request.LeagueName
                        && s.Country == request.Country
                        && s.IsActive,
                    cancellationToken
                );

                if (activeSeason != null)
                    return new CreateSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"An active season for {request.LeagueName} in {request.Country} already exists",
                    };
            }

            // Create a new season
            var season = new Season
            {
                Name = request.Name,
                LeagueName = request.LeagueName,
                Country = request.Country,
                CurrentRound = 1,
                IsActive = request.IsActive,
                IsCompleted = false,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompetitionId = request.CompetitionId,
            };

            await unitOfWork.Seasons.AddAsync(season);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateSeasonCommandResponse
            {
                Succeeded = true,
                Id = season.Id,
                Name = season.Name,
            };
        }
        catch (Exception ex)
        {
            var innerException = ex.InnerException?.Message ?? ex.Message;
            return new CreateSeasonCommandResponse { Succeeded = false, Error = innerException };
        }
    }
}
