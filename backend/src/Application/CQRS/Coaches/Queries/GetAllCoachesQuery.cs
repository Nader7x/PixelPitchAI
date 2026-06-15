using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Coaches.Queries;

public class GetAllCoachesQuery : IRequest<GetAllCoachesQueryResponse>
{
    public string? Nationality { get; init; }
    public int? TeamId { get; set; }
}

public class GetAllCoachesQueryResponse
{
    public bool Succeeded { get; init; }
    public List<CoachDto>? Coaches { get; init; }
    public string? Error { get; init; }
}

public class GetAllCoachesQueryHandler(IUnitOfWork unitOfWork, ICoachMapper coachMapper)
    : IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse>
{
    public async Task<GetAllCoachesQueryResponse> Handle(
        GetAllCoachesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var coaches = unitOfWork.Coaches.GetQueryable();
            if (!string.IsNullOrWhiteSpace(request.Nationality))
                coaches = coaches.Where(c =>
                    c.Nationality.ToLower() == request.Nationality.ToLower()
                );
            if (request.TeamId.HasValue)
                coaches = coaches.Where(c => c.TeamId == request.TeamId.Value);

            var coachDtoS = coachMapper.ToDtoList(
                await coaches.ToListAsync(cancellationToken: cancellationToken)
            );
            return new GetAllCoachesQueryResponse { Succeeded = true, Coaches = coachDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllCoachesQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
