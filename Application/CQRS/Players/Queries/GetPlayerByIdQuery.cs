using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

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
    private readonly IPlayerMapper _playerMapper;
    private readonly IUnitOfWork _unitOfWork;

    public GetPlayerByIdQueryHandler(IUnitOfWork unitOfWork, IPlayerMapper playerMapper)
    {
        _unitOfWork = unitOfWork;
        _playerMapper = playerMapper;
    }

    public async Task<GetPlayerByIdQueryResponse> Handle(GetPlayerByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var player = await _unitOfWork.Players.GetByIdAsync(request.Id);
            if (player == null)
                return new GetPlayerByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found"
                };

            var playerDto = _playerMapper.ToDto(player);

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
