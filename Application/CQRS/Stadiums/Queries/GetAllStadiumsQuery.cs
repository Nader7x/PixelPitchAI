using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.CQRS.Stadiums.Queries;

public class GetAllStadiumsQuery : IRequest<GetAllStadiumsQueryResponse>
{
    // Optional parameters for filtering
    public string? Country { get; set; }
    public string? City { get; set; }
}

public class GetAllStadiumsQueryResponse
{
    public bool Succeeded { get; init; }
    public List<StadiumDto>? Stadiums { get; init; }
    public string? Error { get; init; }
}

public class GetAllStadiumsQueryHandler(IUnitOfWork unitOfWork, IStadiumMapper stadiumMapper)
    : IRequestHandler<GetAllStadiumsQuery, GetAllStadiumsQueryResponse>
{
    public async Task<GetAllStadiumsQueryResponse> Handle(
        GetAllStadiumsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var query = unitOfWork.Stadiums.GetQueryable();
            // Apply filters if provided
            if (
                !string.IsNullOrWhiteSpace(request.Country)
                && !string.IsNullOrWhiteSpace(request.City)
            )
                query = query.Where(s =>
                    s.Country != null
                    && s.Country.ToLower() == request.Country.ToLower()
                    && s.City != null
                    && s.City.ToLower() == request.City.ToLower()
                );
            else if (!string.IsNullOrWhiteSpace(request.Country))
                query = query.Where(s =>
                    s.Country != null && s.Country.ToLower() == request.Country.ToLower()
                );
            else if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(s =>
                    s.City != null && s.City.ToLower() == request.City.ToLower()
                );
            var stadiums = await query.ToListAsync(cancellationToken: cancellationToken);
            var stadiumDtoS = stadiumMapper.ToDtoList(stadiums);

            return new GetAllStadiumsQueryResponse { Succeeded = true, Stadiums = stadiumDtoS };
        }
        catch (Exception ex)
        {
            return new GetAllStadiumsQueryResponse
            {
                Succeeded = false,
                Error = ex.Message,
                Stadiums = [],
            };
        }
    }
}
