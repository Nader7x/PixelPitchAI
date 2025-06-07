using Application.Dtos;
using Application.Mappers;
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

public class GetAllSeasonsQueryHandler : IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>
{
    private readonly SeasonMapper _seasonMapper;
    private readonly IUnitOfWork _unitOfWork;

    public GetAllSeasonsQueryHandler(IUnitOfWork unitOfWork, SeasonMapper seasonMapper)
    {
        _unitOfWork = unitOfWork;
        _seasonMapper = seasonMapper;
    }

    public async Task<GetAllSeasonsQueryResponse> Handle(GetAllSeasonsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Season> seasons;

            // Apply filters if provided
            if (!string.IsNullOrEmpty(request.LeagueName) && !string.IsNullOrEmpty(request.Country) &&
                request.IsActive.HasValue)
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.Country.ToLower().Contains(request.Country.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            else if (!string.IsNullOrEmpty(request.LeagueName) && !string.IsNullOrEmpty(request.Country))
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.Country.ToLower().Contains(request.Country.ToLower()));
            else if (!string.IsNullOrEmpty(request.LeagueName) && request.IsActive.HasValue)
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            else if (!string.IsNullOrEmpty(request.Country) && request.IsActive.HasValue)
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.Country.ToLower().Contains(request.Country.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            else if (!string.IsNullOrEmpty(request.LeagueName))
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()));
            else if (!string.IsNullOrEmpty(request.Country))
                seasons = await _unitOfWork.Seasons.GetAllAsync(s =>
                    s.Country.ToLower().Contains(request.Country.ToLower()));
            else if (request.IsActive.HasValue)
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => s.IsActive == request.IsActive.Value);
            else
                seasons = await _unitOfWork.Seasons.GetAllAsync();

            var seasonDtos = _seasonMapper.ToDtoList(seasons);

            return new GetAllSeasonsQueryResponse
            {
                Succeeded = true,
                Seasons = seasonDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllSeasonsQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}