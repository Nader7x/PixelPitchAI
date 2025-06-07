using Application.Dtos;
using Application.Mappers;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

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
    public List<MatchDto?>? Matches { get; set; }
    public string? Error { get; set; }
}

public class GetAllMatchesQueryHandler(IUnitOfWork unitOfWork, MatchMapper matchMapper)
    : IRequestHandler<GetAllMatchesQuery, GetAllMatchesQueryResponse>
{
    public async Task<GetAllMatchesQueryResponse> Handle(GetAllMatchesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Match> matches;

            // Build filter expression based on provided parameters
            if (request.HomeSeasonId.HasValue || request.TeamId.HasValue || !string.IsNullOrEmpty(request.Status) ||
                request.FromDate.HasValue || request.ToDate.HasValue || request.MatchWeek.HasValue)
                // Apply combined filters
                matches = await unitOfWork.Matches.GetAllAsync(m =>
                    (!request.HomeSeasonId.HasValue || m.HomeTeamSeasonId == request.HomeSeasonId.Value) &&
                    (!request.AwaySeasonId.HasValue || m.AwayTeamSeasonId == request.AwaySeasonId.Value) &&
                    (!request.TeamId.HasValue || m.HomeTeamId == request.TeamId.Value ||
                     m.AwayTeamId == request.TeamId.Value) &&
                    (string.IsNullOrEmpty(request.Status) || m.MatchStatus == request.Status) &&
                    (!request.FromDate.HasValue || m.ScheduledDateTimeUtc >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || m.ScheduledDateTimeUtc <= request.ToDate.Value) &&
                    (!request.MatchWeek.HasValue || m.MatchWeek == request.MatchWeek.Value)
                );
            else
                // Get all matches if no filters provided
                matches = await unitOfWork.Matches.GetAllWithDetailsAsync();

            var matchDtos = matchMapper.ToDtoList(matches);


            return new GetAllMatchesQueryResponse
            {
                Succeeded = true,
                Matches = matchDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllMatchesQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}