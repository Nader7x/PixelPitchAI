using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

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

public class GetSeasonByIdQueryHandler(IUnitOfWork unitOfWork, ISeasonMapper seasonMapper)
    : IRequestHandler<GetSeasonByIdQuery, GetSeasonByIdQueryResponse>
{
    public async Task<GetSeasonByIdQueryResponse> Handle(
        GetSeasonByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var season = await unitOfWork.Seasons.GetByIdAsync(request.Id);
            if (season == null)
                return new GetSeasonByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Season with ID {request.Id} not found",
                };

            // Get team standings summary for the season
            var teamStats = await unitOfWork.TeamSeasons.GetAllAsync(ts =>
                ts.SeasonId == request.Id
            );

            // Count the matches for this season
            var seasonDto = seasonMapper.ToDto(season);

            return new GetSeasonByIdQueryResponse { Succeeded = true, Season = seasonDto };
        }
        catch (Exception ex)
        {
            return new GetSeasonByIdQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
