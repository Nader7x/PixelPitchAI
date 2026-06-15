using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Players.Queries;

public class GetPlayerByIdQuery : IRequest<GetPlayerByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetPlayerByIdQueryResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public PlayerDto? Player { get; init; }
    public string? Error { get; init; }
}

public class GetPlayerByIdQueryHandler(IUnitOfWork unitOfWork, IPlayerMapper playerMapper)
    : IRequestHandler<GetPlayerByIdQuery, GetPlayerByIdQueryResponse>
{
    public async Task<GetPlayerByIdQueryResponse> Handle(
        GetPlayerByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var player = await unitOfWork.Players.GetByIdAsync(request.Id, cancellationToken);
            if (player == null)
                return new GetPlayerByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found",
                };

            var playerDto = playerMapper.ToDto(player);

            return new GetPlayerByIdQueryResponse { Succeeded = true, Player = playerDto };
        }
        catch (Exception ex)
        {
            return new GetPlayerByIdQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
