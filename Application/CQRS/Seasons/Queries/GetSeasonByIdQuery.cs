using Application.Dtos;
using Application.Mappers;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Seasons.Queries;

public class GetSeasonByIdQuery : IRequest<GetSeasonByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetSeasonByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public SeasonDto? Season { get; set; }
    public string? Error { get; set; }
}

public class GetSeasonByIdQueryHandler(IUnitOfWork unitOfWork, SeasonMapper seasonMapper)
    : IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>
{
    public async Task<GetSeasonByIdQueryResponse> Handle(GetSeasonByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var season = await unitOfWork.Seasons.GetByIdAsync(request.Id);
            if (season == null)
            {
                return new GetSeasonByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found"
                };
            }
            
            // Get team standings summary for the season
            var teamStats = await unitOfWork.TeamSeasonStats.GetAllAsync(ts => ts.SeasonId == request.Id);
            var standings = seasonMapper.ToTeamStandingDtoList(teamStats).OrderBy(s => s.Position).ToList();
            
            // Count the matches for this season
            var matches = await unitOfWork.Matches.GetAllAsync(m => m.SeasonId == request.Id);
            var seasonDto = seasonMapper.ToDto(season);
            seasonDto.MatchCount = matches.Count();
            seasonDto.TeamStandings = standings;
            
            return new GetSeasonByIdQueryResponse
            {
                Succeeded = true,
                Season = seasonDto
            };
        }
        catch (Exception ex)
        {
            return new GetSeasonByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
