using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Coaches.Commands;

public class DeleteCoachCommand : IRequest<DeleteCoachCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteCoachCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
}

public class DeleteCoachCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCoachCommand, DeleteCoachCommandResponse>
{
    public async Task<DeleteCoachCommandResponse> Handle(DeleteCoachCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var coach = await unitOfWork.Coaches.GetByIdAsync(request.Id);
            if (coach == null)
                return new DeleteCoachCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Coach with ID {request.Id} not found"
                };

            // Check if coach is currently assigned to a team
            if (coach.TeamId != null)
                return new DeleteCoachCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete coach as they are currently assigned to a team"
                };

            // Check if coach is assigned to any matches
            var matchesAsHomeCoach = await unitOfWork.Matches.FindAsync(m => m.HomeCoachId == request.Id, cancellationToken);
            var matchesAsAwayCoach = await unitOfWork.Matches.FindAsync(m => m.AwayCoachId == request.Id, cancellationToken);

            if (matchesAsHomeCoach != null || matchesAsAwayCoach != null)
                return new DeleteCoachCommandResponse
                {
                    Succeeded = false,
                    Error = "Cannot delete coach as they are assigned to one or more matches"
                };

            unitOfWork.Coaches.DeleteAsync(coach);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteCoachCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new DeleteCoachCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}