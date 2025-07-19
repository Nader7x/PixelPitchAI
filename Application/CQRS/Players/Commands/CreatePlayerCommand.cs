using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Players.Commands;

public class CreatePlayerCommand : IRequest<CreatePlayerCommandResponse>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? FullName { get; set; }

    public string? KnownName { get; set; }

    [StringLength(50)]
    public string? Nationality { get; set; }

    [StringLength(20)]
    public string? PreferredFoot { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public int? TeamId { get; set; }

    [Range(1, 99)]
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
}

public class CreatePlayerCommandResponse
{
    public bool Succeeded { get; init; }
    public int Id { get; init; }
    public string? FullName { get; init; }
    public string? Error { get; init; }
}

public class CreatePlayerCommandHandler(IUnitOfWork unitOfWork, IPlayerMapper playerMapper)
    : IRequestHandler<CreatePlayerCommand, CreatePlayerCommandResponse>
{
    public async Task<CreatePlayerCommandResponse> Handle(
        CreatePlayerCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = "Full name is required",
                };
            if (request.FullName.Length is < 2 or > 100)
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = "Full name must be between 2 and 100 characters",
                };
            if (string.IsNullOrWhiteSpace(request.KnownName) && request.KnownName?.Length > 25)
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = "Known name must be up to 25 characters",
                };
            if (request.ShirtNumber is 0)
                return new CreatePlayerCommandResponse()
                {
                    Succeeded = false,
                    Error = "Shirt number must be between 1 and 99",
                };
            if (request.TeamId is not null)
            {
                // Check if team exists
                var team = await unitOfWork.Teams.GetByIdAsync(
                    request.TeamId.Value,
                    cancellationToken
                );
                if (team == null)
                    return new CreatePlayerCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Team with ID {request.TeamId} not found",
                    };
            }
            // Check if player with the same name already exists
            var existingPlayer = await unitOfWork.Players.GetByFullNameAsync(request.FullName);
            if (existingPlayer != null)
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = $"Player with name '{request.FullName}' already exists",
                };
            // Check for Duplicate Shirt Number in the same team
            if (request is { ShirtNumber: not null, TeamId: not null })
            {
                var existingShirtNumber = await unitOfWork.Players.GetByShirtNumberAndTeamAsync(
                    request.ShirtNumber.Value,
                    request.TeamId.Value
                );
                if (existingShirtNumber != null)
                    return new CreatePlayerCommandResponse
                    {
                        Succeeded = false,
                        Error =
                            $"Jersey number {request.ShirtNumber} already exists for team ID {request.TeamId}",
                    };
            }

            // Create new player
            var player = playerMapper.ToPlayerFromCreate(request);

            await unitOfWork.Players.AddAsync(player);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreatePlayerCommandResponse
            {
                Succeeded = true,
                Id = player.Id,
                FullName = player.FullName,
            };
        }
        catch (Exception ex)
        {
            return new CreatePlayerCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
