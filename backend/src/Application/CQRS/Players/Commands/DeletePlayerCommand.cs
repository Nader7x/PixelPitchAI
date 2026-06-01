using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Players.Commands;

public class DeletePlayerCommand : IRequest<DeletePlayerCommandResponse>
{
    public int Id { get; set; }
}

public class DeletePlayerCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }
}

public class DeletePlayerCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>
{
    public async Task<DeletePlayerCommandResponse> Handle(
        DeletePlayerCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var player = await unitOfWork.Players.GetByIdAsync(request.Id, cancellationToken);
            if (player == null)
                return new DeletePlayerCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found",
                };

            unitOfWork.Players.Delete(player);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeletePlayerCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new DeletePlayerCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
