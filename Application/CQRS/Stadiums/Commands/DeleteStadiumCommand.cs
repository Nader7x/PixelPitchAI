using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Stadiums.Commands;

public class DeleteStadiumCommand : IRequest<DeleteStadiumCommandResponse>
{
    public int Id { get; set; }
}

public class DeleteStadiumCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
}

public class DeleteStadiumCommandHandler : IRequestHandler<DeleteStadiumCommand, DeleteStadiumCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteStadiumCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<DeleteStadiumCommandResponse> Handle(DeleteStadiumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var stadium = await _unitOfWork.Stadiums.GetByIdAsync(request.Id);
            if (stadium == null)
            {
                return new DeleteStadiumCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found"
                };
            }
            
            // Check if stadium is being used by matches
            var matches = await _unitOfWork.Matches.GetAllAsync(m => m.StadiumId == request.Id);
            if (matches.Any())
            {
                return new DeleteStadiumCommandResponse
                {
                    Succeeded = false,
                    Error = $"Cannot delete stadium as it is being used by {matches.Count()} match(es)"
                };
            }
            
            _unitOfWork.Stadiums.DeleteAsync(stadium);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return new DeleteStadiumCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new DeleteStadiumCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
