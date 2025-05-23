using Application.Dtos;
using Application.Mappers;
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
    public CoachDto? Coach { get; set; }
    public string? Error { get; set; }
}

public class GetCoachByIdQueryHandler : IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CoachMapper _coachMapper;
    
    public GetCoachByIdQueryHandler(IUnitOfWork unitOfWork, CoachMapper coachMapper)
    {
        _unitOfWork = unitOfWork;
        _coachMapper = coachMapper;
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
                    Error = "Coach not found"
                };
            }
            var coachDto = _coachMapper.ToDto(coach);
            
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
