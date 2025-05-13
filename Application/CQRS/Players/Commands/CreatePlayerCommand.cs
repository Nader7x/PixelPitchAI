using Domain.Models;
using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Domain.Interfaces;

namespace Application.CQRS.Players.Commands;

public class CreatePlayerCommand : IRequest<CreatePlayerCommandResponse>
{
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

public class CreatePlayerCommandResponse
{
    public bool Succeeded { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Error { get; set; }
}

public class CreatePlayerCommandHandler : IRequestHandler<CreatePlayerCommand, CreatePlayerCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreatePlayerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<CreatePlayerCommandResponse> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if player with the same name already exists
            var existingPlayer = await _unitOfWork.Players.GetByFullNameAsync(request.FullName);
            if (existingPlayer != null)
            {
                return new CreatePlayerCommandResponse
                {
                    Succeeded = false,
                    Error = $"Player with name '{request.FullName}' already exists"
                };
            }
            
            // Create new player
            var player = new Player
            {
                FullName = request.FullName,
                Nationality = request.Nationality,
                PreferredFoot = request.PreferredFoot,
                PhotoUrl = request.PhotoUrl,
                TeamId = request.TeamId,
                ShirtNumber = request.ShirtNumber,
                StatsBombPlayerId = request.StatsBombPlayerId
            };
            
            await _unitOfWork.Players.AddAsync(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
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
