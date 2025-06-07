using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Commands;

public class UpdateMatchStatusCommand : IRequest<UpdateMatchStatusCommandResponse>
{
    public int MatchId { get; set; }
    public string NewStatus { get; set; } = string.Empty; // New property to hold the new status
}

public class UpdateMatchStatusCommandResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public string? Status { get; set; } // New property to hold the status of the match
}

public class UpdateMatchStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMatchStatusCommand, UpdateMatchStatusCommandResponse>
{
    public async Task<UpdateMatchStatusCommandResponse> Handle(UpdateMatchStatusCommand request,
        CancellationToken cancellationToken)
    {
        var match = await unitOfWork.Matches.GetByIdAsync(request.MatchId);
        if (match == null)
            return new UpdateMatchStatusCommandResponse
            {
                Succeeded = false,
                NotFound = true,
                Error = "Match not found"
            };
        if (string.IsNullOrEmpty(request.NewStatus))
            // Update the match status
            match.MatchStatus = "Completed"; // Example status update
        else
            match.MatchStatus = request.NewStatus; // Update to the new status provided
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateMatchStatusCommandResponse
        {
            Succeeded = true,
            Status = match.MatchStatus
        };
    }
}