using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Matches.Queries;

public class GetAllMatchesQuery : IRequest<GetAllMatchesQueryResponse>
{
    // Optional parameters for filtering
    public int? HomeSeasonId { get; init; }
    public int? AwaySeasonId { get; init; }
    public int? TeamId { get; set; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MatchWeek { get; init; }
}

public class GetAllMatchesQueryResponse
{
    public bool Succeeded { get; init; }
    public List<MatchDto?>? Matches { get; init; }
    public string? Error { get; init; }
}

public class GetAllMatchesQueryHandler(IUnitOfWork unitOfWork, IMatchMapper matchMapper)
    : IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>
{
    public async Task<GetAllMatchesQueryResponse> Handle(
        GetAllMatchesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var query = unitOfWork.Matches.GetQueryable();

            if (request.HomeSeasonId.HasValue)
                query = query.Where(m => m.HomeTeamSeasonId == request.HomeSeasonId.Value);

            if (request.AwaySeasonId.HasValue)
                query = query.Where(m => m.AwayTeamSeasonId == request.AwaySeasonId.Value);

            if (request.TeamId.HasValue)
                if (request.TeamId.Value != 0)
                    query = query.Where(m =>
                        m.HomeTeamId == request.TeamId.Value || m.AwayTeamId == request.TeamId.Value
                    );

            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(m => m.MatchStatus == request.Status);

            if (request.FromDate.HasValue)
                query = query.Where(m => m.ScheduledDateTimeUtc >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(m => m.ScheduledDateTimeUtc <= request.ToDate.Value);

            if (request.MatchWeek.HasValue)
                query = query.Where(m => m.MatchWeek == request.MatchWeek.Value);
            var matches = await query.ToListAsync(cancellationToken);

            var matchDtoS = matchMapper.ToDtoList(matches);

            return new GetAllMatchesQueryResponse { Succeeded = true, Matches = matchDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllMatchesQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
