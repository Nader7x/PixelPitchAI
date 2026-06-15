using System.ComponentModel.DataAnnotations;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Players.Commands;

public class UpdatePlayerCommand : IRequest<UpdatePlayerCommandResponse>
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string? FullName { get; set; }

    public string? KnownName { get; set; }

    [StringLength(50)]
    public string? Nationality { get; set; }

    [StringLength(50)]
    public string? Position { get; set; }

    [StringLength(20)]
    public string? PreferredFoot { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public int? TeamId { get; set; }

    public int? ShirtNumber { get; set; }
}

public class UpdatePlayerCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public int Id { get; init; }
    public string? FullName { get; init; }
    public string? Error { get; init; }
}

public class UpdatePlayerCommandHandler(IUnitOfWork unitOfWork, IPlayerMapper playerMapper)
    : IRequestHandler<UpdatePlayerCommand, UpdatePlayerCommandResponse>
{
    public async Task<UpdatePlayerCommandResponse> Handle(
        UpdatePlayerCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var player = await unitOfWork.Players.GetByIdAsync(request.Id, cancellationToken);
            if (player == null)
                return new UpdatePlayerCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found",
                };

            if (player.FullName != request.FullName)
            {
                var existingPlayer = await unitOfWork.Players.GetByFullNameAsync(request.FullName);
                if (existingPlayer != null && existingPlayer.Id != request.Id)
                    return new UpdatePlayerCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Player with name '{request.FullName}' already exists",
                    };
            }

            playerMapper.ToPlayerFromUpdate(request, player);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdatePlayerCommandResponse
            {
                Succeeded = true,
                Id = player.Id,
                FullName = player.FullName,
            };
        }
        catch (Exception ex)
        {
            return new UpdatePlayerCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
