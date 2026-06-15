using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;

namespace Application.CQRS.Coaches.Queries;

public class GetCoachByIdQuery : IRequest<GetCoachByIdQueryResponse>
{
    public int Id { get; init; }
}

public class GetCoachByIdQueryResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public CoachDto? Coach { get; init; }
    public string? Error { get; init; }
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
            var coach = await unitOfWork.Coaches.GetByIdAsync(request.Id, cancellationToken);
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
