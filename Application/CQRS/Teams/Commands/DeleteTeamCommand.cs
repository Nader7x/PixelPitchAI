
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
    public string Error { get; set; }
}

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand, DeleteTeamCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteTeamCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<DeleteTeamCommandResponse> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if team exists
            var team = await _unitOfWork.Teams.GetByIdAsync(request.Id);
            if (team == null)
            {
                return new DeleteTeamCommandResponse
                {
                    Succeeded = false,
                    Error = $"Team with ID {request.Id} not found"
                };
            }
            
            // Begin transaction to ensure atomic operation
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                // Delete team
                 _unitOfWork.Teams.DeleteAsync(team);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                // Commit transaction
                await _unitOfWork.CommitTransactionAsync();
                
                return new DeleteTeamCommandResponse
                {
                    Succeeded = true
                };
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Failed to delete team: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            return new DeleteTeamCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
