using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Seasons.Queries;

public class GetAllSeasonsQuery : IRequest<GetAllSeasonsQueryResponse>
{
    // Optional parameters for filtering
    public string? LeagueName { get; set; }
    public string? Country { get; set; }
    public bool? IsActive { get; set; }
}

public class GetAllSeasonsQueryResponse
{
    public bool Succeeded { get; set; }
    public List<SeasonDto>? Seasons { get; set; }
    public string? Error { get; set; }
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
            IEnumerable<Season> seasons;
            // Apply filters if provided
            if (
                !string.IsNullOrWhiteSpace(request.LeagueName)
                || !string.IsNullOrWhiteSpace(request.Country)
                || request.IsActive.HasValue
            )
            {
                seasons = await unitOfWork.Seasons.GetAllAsync(s =>
                    (
                        string.IsNullOrWhiteSpace(request.LeagueName)
                        || (
                            s.LeagueName != null
                            && s.LeagueName.Contains(
                                request.LeagueName,
                                StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                    )
                    && (
                        string.IsNullOrWhiteSpace(request.Country)
                        || (
                            s.Country != null
                            && s.Country.Contains(
                                request.Country,
                                StringComparison.CurrentCultureIgnoreCase
                            )
                        )
                    )
                    && (!request.IsActive.HasValue || s.IsActive == request.IsActive.Value)
                );
            }
            else
            {
                seasons = await unitOfWork.Seasons.GetAllAsync();
            }

            var seasonDtos = seasonMapper.ToDtoList(seasons);

            return new GetAllSeasonsQueryResponse { Succeeded = true, Seasons = seasonDtos };
        }
        catch (Exception ex)
        {
            return new GetAllSeasonsQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
