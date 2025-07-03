using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

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

public class GetCoachByIdQueryHandler(IUnitOfWork unitOfWork, ICoachMapper coachMapper)
    : IRequestHandler<GetCoachByIdQuery, GetCoachByIdQueryResponse>
{
    public async Task<GetCoachByIdQueryResponse> Handle(
        GetCoachByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var coach = await unitOfWork.Coaches.GetByIdAsync(request.Id);
            if (coach == null)
                return new GetCoachByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = "Coach not found",
                };
            var coachDto = coachMapper.ToDto(coach);

            return new GetCoachByIdQueryResponse { Succeeded = true, Coach = coachDto };
        }
        catch (Exception ex)
        {
            return new GetCoachByIdQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
