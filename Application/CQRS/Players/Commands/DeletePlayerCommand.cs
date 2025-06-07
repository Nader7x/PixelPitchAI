using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Players.Commands;

public class DeletePlayerCommand : IRequest<DeletePlayerCommandResponse>
{
    public int Id { get; set; }
}

public class DeletePlayerCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
}

public class DeletePlayerCommandHandler : IRequestHandler<DeletePlayerCommand, DeletePlayerCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeletePlayerCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePlayerCommandResponse> Handle(DeletePlayerCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var player = await _unitOfWork.Players.GetByIdAsync(request.Id);
            if (player == null)
                return new DeletePlayerCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Player with ID {request.Id} not found"
                };

            _unitOfWork.Players.DeleteAsync(player);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeletePlayerCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new DeletePlayerCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}