using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Application.CQRS;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Seasons.Queries;

public class GetAllSeasonsQuery : IRequest<GetAllSeasonsQueryResponse>
{
    // Optional parameters for filtering
    public string? LeagueName { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }
}

public class GetAllSeasonsQueryResponse
{
    public bool Succeeded { get; init; }
    public List<SeasonDto>? Seasons { get; init; }
    public string? Error { get; init; }
}

public class GetAllSeasonsQueryHandler(IUnitOfWork unitOfWork, ISeasonMapper seasonMapper)
    : IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>
{
    public async Task<GetAllSeasonsQueryResponse> Handle(
        GetAllSeasonsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var seasons = unitOfWork.Seasons.GetQueryable();
            // Apply filters if provided
            if (!string.IsNullOrWhiteSpace(request.LeagueName))
                seasons = seasons.Where(s =>
                    s.LeagueName != null && s.LeagueName.ToLower() == request.LeagueName.ToLower()
                );

            if (!string.IsNullOrWhiteSpace(request.Country))
                seasons = seasons.Where(s =>
                    s.Country != null && s.Country.ToLower() == request.Country.ToLower()
                );

            if (request.IsActive)
                seasons = seasons.Where(s => s.IsActive == request.IsActive);

            var seasonDtoS = seasonMapper.ToDtoList(await seasons.ToListAsync(cancellationToken));

            return new GetAllSeasonsQueryResponse { Succeeded = true, Seasons = seasonDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllSeasonsQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
