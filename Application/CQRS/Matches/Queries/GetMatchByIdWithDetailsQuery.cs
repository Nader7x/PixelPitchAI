using Application.Dtos;
using Application.Interfaces;
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
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public MatchDetailsDto? Match { get; set; }
    public string? Error { get; set; }
}

public class GetMatchByIdWithDetailsQueryHandler(
    IMatchMapper matchMapper,
    IUnitOfWork unitOfWork,
    ILiveMatchStatisticsService liveMatchStatisticsService
) : IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>
{
    private readonly ILiveMatchStatisticsService _liveMatchStatisticsService =
        liveMatchStatisticsService;

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
            var liveStatistics = _liveMatchStatisticsService.GetCachedLiveMatch(
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
