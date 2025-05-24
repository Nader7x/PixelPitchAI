using MediatR;
using System.ComponentModel.DataAnnotations;
using Application.Mappers;
using Domain.Interfaces;

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
    public string PreferredFoot { get; set; }
    
    [StringLength(500)]
    public string? PhotoUrl { get; set; }
    
    public int? TeamId { get; set; }
    
    public int? ShirtNumber { get; set; }
    public string? Position { get; set; }
    
}

public class CreatePlayerCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string Error { get; set; }
}

public class CreatePlayerCommandHandler(IUnitOfWork unitOfWork, PlayerMapper playerMapper)
    : IRequestHandler<CreatePlayerCommand, CreatePlayerCommandResponse>
{
    public async Task<CreatePlayerCommandResponse> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if player with the same name already exists
            var existingPlayer = await unitOfWork.Players.GetByFullNameAsync(request.FullName);
            if (existingPlayer != null)
            {
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = $"Player with name '{request.FullName}' already exists"
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
                FullName = player.FullName
            };
        }
        catch (Exception ex)
        {
            return new CreatePlayerCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
