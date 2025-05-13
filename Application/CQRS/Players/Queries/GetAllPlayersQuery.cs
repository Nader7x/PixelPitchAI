using Application.Dtos;
using MediatR;
using Domain.Interfaces;
using Domain.Models;

namespace Application.CQRS.Players.Queries;

public class GetAllPlayersQuery : IRequest<GetAllPlayersQueryResponse>
{
    // Optional parameters for filtering
    public string Nationality { get; set; }
    public string Position { get; set; }
    public string PreferredFoot { get; set; }
    public int? TeamId { get; set; }
}

public class GetAllPlayersQueryResponse
{
    public bool Succeeded { get; set; }
    public List<PlayerDto> Players { get; set; }
    public string Error { get; set; }
}

public class GetAllPlayersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllPlayersQuery, GetAllPlayersQueryResponse>
{
    public async Task<GetAllPlayersQueryResponse> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<Player> players;
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(request.Nationality))
            {
                players = await unitOfWork.Players.GetByNationalityAsync(request.Nationality);
            }
            else if (!string.IsNullOrEmpty(request.PreferredFoot))
            {
                players = await unitOfWork.Players.GetByPreferredFootAsync(request.PreferredFoot);
            }
            else if (request.TeamId.HasValue)
            {
                players = await unitOfWork.Players.FindAsync(p => p.TeamId == request.TeamId.Value);
            }
            else
            {
                players = (IReadOnlyList<Player>)await unitOfWork.Players.GetAllAsync();
            }
            
            var playerDtos = players.Select(p => new PlayerDto
            {
                Id = p.Id,
                FullName = p.FullName,
                Nationality = p.Nationality,
                PreferredFoot = p.PreferredFoot,
                PhotoUrl = p.PhotoUrl,
                TeamId = p.TeamId,
                TeamName = p.Team?.Name,
                ShirtNumber = p.ShirtNumber,
                StatsBombPlayerId = p.StatsBombPlayerId
            }).ToList();
            
            return new GetAllPlayersQueryResponse
            {
                Succeeded = true,
                Players = playerDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllPlayersQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
