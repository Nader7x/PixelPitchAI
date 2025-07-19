using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Commands;

public class DeleteMatchCommand : IRequest<DeleteMatchCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteMatchCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }
}

public class DeleteMatchCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMatchCommand, DeleteMatchCommandResponse>
{
    public async Task<DeleteMatchCommandResponse> Handle(
        DeleteMatchCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var match = await unitOfWork.Matches.GetByIdAsync(request.Id, cancellationToken);
            if (match == null)
                return new DeleteMatchCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found",
                };

            if (match.MatchStatus is "Completed" or "InProgress")
                return new DeleteMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Cannot delete a match that is {match.MatchStatus}",
                };

            unitOfWork.Matches.Delete(match);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteMatchCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new DeleteMatchCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
