using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Commands;

public class DeleteMatchCommand : IRequest<DeleteMatchCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteMatchCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
}

public class DeleteMatchCommandHandler : IRequestHandler<DeleteMatchCommand, DeleteMatchCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteMatchCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<DeleteMatchCommandResponse> Handle(DeleteMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var match = await _unitOfWork.Matches.GetByIdAsync(request.Id);
            if (match == null)
            {
                return new DeleteMatchCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found"
                };
            }
            
            // Check if match can be deleted (e.g. if it's already completed, maybe don't allow deletion)
            if (match.MatchStatus == "Completed" || match.MatchStatus == "InProgress")
            {
                return new DeleteMatchCommandResponse
                {
                    Succeeded = false,
                    Error = $"Cannot delete a match that is {match.MatchStatus}"
                };
            }
            
            // Check for related events or data that would be affected by deletion
            // Example: Check for match events, match statistics, etc.
            
            _unitOfWork.Matches.DeleteAsync(match);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new DeleteMatchCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new DeleteMatchCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
