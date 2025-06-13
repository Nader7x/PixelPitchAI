using Application.Dtos;
using Application.Mappers;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Queries;

public class GetUserMatchesQuery : IRequest<GetUserMatchesQueryResponse>
{
    public required string UserId { get; init; }
}

public class GetUserMatchesQueryResponse
{
    public List<UserMatchDto?>? Matches { get; init; }
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
}

public class GetUserMatchesQueryHandler(IUnitOfWork unitOfWork, MatchMapper matchMapper)
    : IRequestHandler<GetUserMatchesQuery, GetUserMatchesQueryResponse>
{
    private readonly MatchMapper _matchMapper = matchMapper;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<GetUserMatchesQueryResponse> Handle(GetUserMatchesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userMatches = await _unitOfWork.Matches.GetMatchesByUserIdAsync(request.UserId);
            var matchesDtos = _matchMapper.ToUserMatchDto(userMatches);
            return new GetUserMatchesQueryResponse
            {
                Succeeded = true,
                Matches = matchesDtos.OrderByDescending(md => md?.ScheduledDateTimeUtc ?? default).ToList()
            };
        }
        catch (Exception ex)
        {
            return new GetUserMatchesQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}