using Application.Dtos;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Coaches.Queries;

public class GetCoachByIdQuery : IRequest<GetCoachByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetCoachByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public CoachDto Coach { get; set; }
    public string Error { get; set; }
}

public class GetCoachByIdQueryHandler : IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetCoachByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetCoachByIdQueryResponse> Handle(GetCoachByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var coach = await _unitOfWork.Coaches.GetByIdAsync(request.Id);
            if (coach == null)
            {
                return new GetCoachByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Coach with ID {request.Id} not found"
                };
            }
            
            var coachDto = new CoachDto
            {
                Id = coach.Id,
                FirstName = coach.FirstName,
                LastName = coach.LastName,
                DateOfBirth = coach.DateOfBirth,
                Nationality = coach.Nationality,
                Role = coach.Role,
                TeamId = coach.TeamId,
                TeamName = coach.Team?.Name,
                ContractStartDate = coach.ContractStartDate,
                ContractEndDate = coach.ContractEndDate,
                PhotoUrl = coach.PhotoUrl,
                PreferredFormation = coach.PreferredFormation,
                CoachingStyle = coach.CoachingStyle
            };
            
            return new GetCoachByIdQueryResponse
            {
                Succeeded = true,
                Coach = coachDto
            };
        }
        catch (Exception ex)
        {
            return new GetCoachByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
