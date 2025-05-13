using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Players.Commands;

public class UpdatePlayerCommand : IRequest<UpdatePlayerCommandResponse>
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(50)]
    public string Nationality { get; set; }
    
    [StringLength(50)]
    public string Position { get; set; }
    
    [StringLength(20)]
    public string PreferredFoot { get; set; }
    
    public int? Height { get; set; }
    
    public int? Weight { get; set; }
    
    [StringLength(500)]
    public string PhotoUrl { get; set; }
    
    public int? TeamId { get; set; }
    
    public int? ShirtNumber { get; set; }
    
    public decimal? MarketValue { get; set; }
    
    [StringLength(50)]
    public string ContractStatus { get; set; }
    
    public DateTime? ContractExpiryDate { get; set; }
    
    public int? StatsBombPlayerId { get; set; }
}

public class UpdatePlayerCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class UpdatePlayerCommandHandler : IRequestHandler<UpdatePlayerCommand, UpdatePlayerCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public UpdatePlayerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<UpdatePlayerCommandResponse> Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if player exists
            var player = await _unitOfWork.Players.GetByIdAsync(request.Id);
            if (player == null)
            {
                return new UpdatePlayerCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found"
                };
            }
            
            // Check for name conflicts
            if (player.FullName != request.FullName)
            {
                var existingPlayer = await _unitOfWork.Players.GetByFullNameAsync(request.FullName);
                if (existingPlayer != null && existingPlayer.Id != request.Id)
                {
                    return new UpdatePlayerCommandResponse
                    {
                        Succeeded = false,
                        Error = $"Player with name '{request.FullName}' already exists"
                    };
                }
            }
            
            // Update player properties
            player.FullName = request.FullName;
            player.Nationality = request.Nationality;
            player.PreferredFoot = request.PreferredFoot;
            player.PhotoUrl = request.PhotoUrl;
            player.TeamId = request.TeamId;
            player.ShirtNumber = request.ShirtNumber;
            player.StatsBombPlayerId = request.StatsBombPlayerId;
            
            _unitOfWork.Players.UpdateAsync(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
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
