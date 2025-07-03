using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Coaches.Queries;

public class GetAllCoachesQuery : IRequest<GetAllCoachesQueryResponse>
{
    // Optional parameters for filtering
    public string? Nationality { get; set; }
    public int? TeamId { get; set; }
}

public class GetAllCoachesQueryResponse
{
    public bool Succeeded { get; set; }
    public List<CoachDto>? Coaches { get; set; }
    public string? Error { get; set; }
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
            IEnumerable<Coach> coaches;

            switch (string.IsNullOrEmpty(request.Nationality))
            {
                // Apply filters if provided
                case false when request.TeamId.HasValue:
                    coaches = await unitOfWork.Coaches.GetAllAsync(c =>
                        c.Nationality.ToLower() == request.Nationality.ToLower()
                        && c.TeamId == request.TeamId.Value
                    );
                    break;
                case false:
                    coaches = await unitOfWork.Coaches.GetAllAsync(c =>
                        c.Nationality.ToLower() == request.Nationality.ToLower()
                    );
                    break;
                default:
                {
                    if (request.TeamId.HasValue)
                        coaches = await unitOfWork.Coaches.GetAllAsync(c =>
                            c.TeamId == request.TeamId.Value
                        );
                    else
                        coaches = await unitOfWork.Coaches.GetAllAsync();
                    break;
                }
            }

            var coachDtos = coachMapper.ToDtoList(coaches);
            return new GetAllCoachesQueryResponse { Succeeded = true, Coaches = coachDtos };
        }
        catch (Exception ex)
        {
            return new GetAllCoachesQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
