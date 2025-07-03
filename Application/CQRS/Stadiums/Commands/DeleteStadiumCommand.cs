using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Stadiums.Commands;

public class DeleteStadiumCommand : IRequest<DeleteStadiumCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteStadiumCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
}

public class DeleteStadiumCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteStadiumCommand, DeleteStadiumCommandResponse>
{
    public async Task<DeleteStadiumCommandResponse> Handle(
        DeleteStadiumCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.Id);
            if (stadium == null)
                return new DeleteStadiumCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found",
                };

            // Check if stadium is being used by matches
            var matches = await unitOfWork.Matches.GetAllAsync(m => m.StadiumId == request.Id);
            var matchesEnumerator = matches as Match[] ?? matches.ToArray();
            if (matchesEnumerator.Length != 0)
                return new DeleteStadiumCommandResponse
                {
                    Succeeded = false,
                    Error =
                        $"Cannot delete stadium as it is being used by {matchesEnumerator.Count()} match(es)",
                };

            unitOfWork.Stadiums.DeleteAsync(stadium);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteStadiumCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new DeleteStadiumCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
