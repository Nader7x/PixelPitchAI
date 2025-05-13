using Application.Dtos;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Players.Queries;

public class GetPlayerByIdQuery : IRequest<GetPlayerByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetPlayerByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public PlayerDto Player { get; set; }
    public string Error { get; set; }
}

public class GetPlayerByIdQueryHandler : IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetPlayerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetPlayerByIdQueryResponse> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var player = await _unitOfWork.Players.GetByIdAsync(request.Id);
            if (player == null)
            {
                return new GetPlayerByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found"
                };
            }
            
            var playerDto = new PlayerDto
            {
                Id = player.Id,
                FullName = player.FullName,
                Nationality = player.Nationality,
                PreferredFoot = player.PreferredFoot,
                PhotoUrl = player.PhotoUrl,
                TeamId = player.TeamId,
                TeamName = player.Team?.Name,
                ShirtNumber = player.ShirtNumber,
                StatsBombPlayerId = player.StatsBombPlayerId
            };
            
            return new GetPlayerByIdQueryResponse
            {
                Succeeded = true,
                Player = playerDto
            };
        }
        catch (Exception ex)
        {
            return new GetPlayerByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
