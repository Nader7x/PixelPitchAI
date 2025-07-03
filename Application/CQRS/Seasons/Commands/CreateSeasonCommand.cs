using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Seasons.Commands;

public class CreateSeasonCommand : IRequest<CreateSeasonCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LeagueName { get; set; }

    [Required]
    [StringLength(50)]
    public string Country { get; set; }

    public int TotalRounds { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class CreateSeasonCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
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

            // Check if season with the same name already exists
            var existingSeason = await unitOfWork.Seasons.FindAsync(s => s.Name == request.Name);
            if (existingSeason != null)
                return new CreateSeasonCommandResponse
                {
                    Succeeded = false,
                    Error = $"Season with name '{request.Name}' already exists",
                };

            // Check if active season already exists for the same league
            if (request.IsActive)
            {
                var activeSeasons = await unitOfWork.Seasons.FindAsync(s =>
                    s.LeagueName == request.LeagueName && s.Country == request.Country && s.IsActive
                );

                if (activeSeasons != null)
                    return new CreateSeasonCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"An active season for {request.LeagueName} in {request.Country} already exists",
                    };
            }

            // Create new season
            var season = new Season
            {
                Name = request.Name,
                LeagueName = request.LeagueName,
                Country = request.Country,
                CurrentRound = 1, // Default to first round
                IsActive = request.IsActive,
                IsCompleted = false, // A new season is not completed
                StartDate = request.StartDate,
                EndDate = request.EndDate,
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
            return new CreateSeasonCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
