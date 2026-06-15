using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Players.Queries;

public class GetAllPlayersQuery : IRequest<GetAllPlayersQueryResponse>
{
    public string? Nationality { get; set; }

    public string? PreferredFoot { get; set; }
    public int? TeamId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public class GetAllPlayersQueryResponse
{
    public bool Succeeded { get; init; }
    public List<PlayerDto>? Players { get; init; }
    public string? Error { get; init; }
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
            var players = unitOfWork.Players.GetQueryable();

            if (!string.IsNullOrWhiteSpace(request.Nationality))
                players = players.Where(p =>
                    p.Nationality!.ToLower() == request.Nationality.ToLower()
                );
            if (!string.IsNullOrWhiteSpace(request.PreferredFoot))
                players = players.Where(p =>
                    p.PreferredFoot!.ToLower() == request.PreferredFoot.ToLower()
                );
            players = request.TeamId is > 0
                ? players.Where(p => p.TeamId == request.TeamId)
                : players.OrderBy(p => p.Id);
            var playerDtoS = _playerMapper.ToDtoList(await players.ToListAsync(cancellationToken));
            if (request is { PageNumber: not null, PageSize: not null })
            {
                playerDtoS = playerDtoS
                    .Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                    .Take(request.PageSize.Value)
                    .ToList();
            }
            return new GetAllPlayersQueryResponse { Succeeded = true, Players = playerDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllPlayersQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
