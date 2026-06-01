using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Teams.Commands;

public class DeleteTeamCommand : IRequest<DeleteTeamCommandResponse>
{
    public int Id { get; init; }
}

public class DeleteTeamCommandResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
}

public class DeleteTeamCommandHandler(
    IUnitOfWork unitOfWork,
    ILogger<DeleteTeamCommandHandler> logger
) : IRequestHandler<DeleteTeamCommand, DeleteTeamCommandResponse>
{
    public async Task<DeleteTeamCommandResponse> Handle(
        DeleteTeamCommand request,
        CancellationToken cancellationToken
    )
    {
        var team = await unitOfWork.Teams.GetByIdAsync(request.Id);
        if (team == null)
            return new DeleteTeamCommandResponse
            {
                Succeeded = false,
                Error = $"Team with ID {request.Id} not found.",
            };

        await unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Unlink related coaches
            var coaches = await unitOfWork.Coaches.GetAllAsync(c => c.TeamId == team.Id);
            foreach (var coach in coaches)
                coach.TeamId = null;
            // Save changes to coaches
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 2. Delete the team
            unitOfWork.Teams.Delete(team);

            // 3. Save the final deletion
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. If all successful, commit the transaction
            await unitOfWork.CommitTransactionAsync();

            return new DeleteTeamCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            // If anything fails, roll back the entire transaction
            await unitOfWork.RollbackTransactionAsync();

            logger.LogError(ex, "Failed to delete team with ID {TeamId}", request.Id);

            return new DeleteTeamCommandResponse
            {
                Succeeded = false,
                Error = $"An error occurred while deleting the team: {ex.Message}",
            };
        }
    }
}
