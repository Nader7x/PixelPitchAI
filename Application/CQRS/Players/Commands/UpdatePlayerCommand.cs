using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Players.Commands;

public class UpdatePlayerCommand : IRequest<UpdatePlayerCommandResponse>
{
    [Required] public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? FullName { get; set; }

    public string? KnownName { get; set; }

    [StringLength(50)] public string? Nationality { get; set; }

    [StringLength(50)] public string? Position { get; set; }

    [StringLength(20)] public string? PreferredFoot { get; set; }

    [StringLength(500)] public string? PhotoUrl { get; set; }

    public int? TeamId { get; set; }

    public int? ShirtNumber { get; set; }
}

public class UpdatePlayerCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Error { get; set; }
}

public class UpdatePlayerCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePlayerCommand, UpdatePlayerCommandResponse>
{
    public async Task<UpdatePlayerCommandResponse> Handle(UpdatePlayerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if player exists
            var player = await unitOfWork.Players.GetByIdAsync(request.Id);
            if (player == null)
                return new UpdatePlayerCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found"
                };

            // Check for name conflicts
            if (player.FullName != request.FullName)
            {
                var existingPlayer = await unitOfWork.Players.GetByFullNameAsync(request.FullName);
                if (existingPlayer != null && existingPlayer.Id != request.Id)
                    return new UpdatePlayerCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Player with name '{request.FullName}' already exists"
                    };
            }

            // Update player properties
            player.FullName = request.FullName;
            player.Nationality = request.Nationality;
            player.PreferredFoot = request.PreferredFoot;
            player.Position = request.Position;
            if (!string.IsNullOrEmpty(request.PhotoUrl)) player.PhotoUrl = request.PhotoUrl;
            player.TeamId = request.TeamId;
            player.ShirtNumber = request.ShirtNumber;

            unitOfWork.Players.UpdateAsync(player);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdatePlayerCommandResponse
            {
                Succeeded = true,
                Id = player.Id,
                FullName = player.FullName
            };
        }
        catch (Exception ex)
        {
            return new UpdatePlayerCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}