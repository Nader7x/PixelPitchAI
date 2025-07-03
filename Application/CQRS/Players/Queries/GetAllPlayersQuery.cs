using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Players.Queries;

public class GetAllPlayersQuery : IRequest<GetAllPlayersQueryResponse>
{
    // Optional parameters for filtering
    public string? Nationality { get; set; }

    public string? PreferredFoot { get; set; }
    public int? TeamId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public class GetAllPlayersQueryResponse
{
    public bool Succeeded { get; set; }
    public List<PlayerDto>? Players { get; set; }
    public string? Error { get; set; }
}

public class GetAllPlayersQueryHandler(IUnitOfWork unitOfWork, IPlayerMapper playerMapper)
    : IRequestHandler<GetAllPlayersQuery, GetAllPlayersQueryResponse>
{
    private readonly IPlayerMapper _playerMapper = playerMapper;

    public async Task<GetAllPlayersQueryResponse> Handle(
        GetAllPlayersQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            IEnumerable<Player> players;

            // Apply filters if provided
            if (!string.IsNullOrWhiteSpace(request.Nationality))
                players = await unitOfWork.Players.GetByNationalityAsync(request.Nationality);
            else if (!string.IsNullOrWhiteSpace(request.PreferredFoot))
                players = await unitOfWork.Players.GetByPreferredFootAsync(request.PreferredFoot);
            else if (request.TeamId is > 0)
                players = await unitOfWork.Players.GetAllAsync(p =>
                    p.TeamId == request.TeamId.Value
                );
            else
                players = await unitOfWork.Players.GetAllAsync(
                    request.PageNumber,
                    request.PageSize
                );
            var playerDtoS = _playerMapper.ToDtoList(players);
            return new GetAllPlayersQueryResponse { Succeeded = true, Players = playerDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllPlayersQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
