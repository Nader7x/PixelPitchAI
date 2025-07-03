using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Teams.Commands;

public class DeleteTeamCommand : IRequest<DeleteTeamCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteTeamCommandResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; } // Nullable to avoid warnings
}

public class DeleteTeamCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTeamCommand, DeleteTeamCommandResponse>
{
    public async Task<DeleteTeamCommandResponse> Handle(
        DeleteTeamCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Check if team exists
            var team = await unitOfWork.Teams.GetByIdAsync(request.Id);
            if (team == null)
                return new DeleteTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with ID {request.Id} not found",
                };

            // Begin transaction to ensure atomic operation
            await unitOfWork.BeginTransactionAsync();

            try
            {
                // Set TeamId to null for all related coaches
                var coaches = await unitOfWork.Coaches.GetAllAsync(c => c.TeamId == team.Id);
                coaches.ToList().ForEach(c => c.TeamId = null);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Delete team
                unitOfWork.Teams.DeleteAsync(team);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Commit transaction
                await unitOfWork.CommitTransactionAsync();

                return new DeleteTeamCommandResponse { Succeeded = true };
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to delete team: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            return new DeleteTeamCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
