using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Queries;

public class GetMatchByIdWithDetailsQuery : IRequest<GetMatchByIdWithDetailsQueryResponse>
{
    public int MatchId { get; set; }
}

public class GetMatchByIdWithDetailsQueryResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public MatchDetailsDto? Match { get; init; }
    public string? Error { get; init; }
}

public class GetMatchByIdWithDetailsQueryHandler(
    IMatchMapper matchMapper,
    IUnitOfWork unitOfWork,
    ILiveMatchStatisticsService liveMatchStatisticsService
) : IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>
{
    public async Task<GetMatchByIdWithDetailsQueryResponse> Handle(
        GetMatchByIdWithDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(request.MatchId);
            if (match == null)
                return new GetMatchByIdWithDetailsQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.MatchId} not found",
                };
            var liveStatistics = liveMatchStatisticsService.GetCachedLiveMatch(
                request.MatchId.ToString()
            );
            var matchDto = matchMapper.ToDetailsFromMatch(liveStatistics ?? match);
            return new GetMatchByIdWithDetailsQueryResponse { Succeeded = true, Match = matchDto };
        }
        catch (Exception ex)
        {
            return new GetMatchByIdWithDetailsQueryResponse
            {
                Succeeded = false,
                Error = ex.Message,
            };
        }
    }
}
