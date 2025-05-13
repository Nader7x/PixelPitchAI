using Application.Dtos;
using MediatR;
using Domain.Interfaces;
using System.Collections.Generic;

namespace Application.CQRS.Seasons.Queries;

public class GetAllSeasonsQuery : IRequest<GetAllSeasonsQueryResponse>
{
    // Optional parameters for filtering
    public string LeagueName { get; set; }
    public string Country { get; set; }
    public bool? IsActive { get; set; }
}

public class GetAllSeasonsQueryResponse
{
    public bool Succeeded { get; set; }
    public List<SeasonDto> Seasons { get; set; }
    public string Error { get; set; }
}

public class GetAllSeasonsQueryHandler : IRequestHandler<GetAllSeasonsQuery, GetAllSeasonsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetAllSeasonsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetAllSeasonsQueryResponse> Handle(GetAllSeasonsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Domain.Models.Season> seasons;
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(request.LeagueName) && !string.IsNullOrEmpty(request.Country) && request.IsActive.HasValue)
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.Country.ToLower().Contains(request.Country.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            }
            else if (!string.IsNullOrEmpty(request.LeagueName) && !string.IsNullOrEmpty(request.Country))
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.Country.ToLower().Contains(request.Country.ToLower()));
            }
            else if (!string.IsNullOrEmpty(request.LeagueName) && request.IsActive.HasValue)
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            }
            else if (!string.IsNullOrEmpty(request.Country) && request.IsActive.HasValue)
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.Country.ToLower().Contains(request.Country.ToLower()) &&
                    s.IsActive == request.IsActive.Value);
            }
            else if (!string.IsNullOrEmpty(request.LeagueName))
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.LeagueName.ToLower().Contains(request.LeagueName.ToLower()));
            }
            else if (!string.IsNullOrEmpty(request.Country))
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => 
                    s.Country.ToLower().Contains(request.Country.ToLower()));
            }
            else if (request.IsActive.HasValue)
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync(s => s.IsActive == request.IsActive.Value);
            }
            else
            {
                seasons = await _unitOfWork.Seasons.GetAllAsync();
            }
            
            var seasonDtos = new List<SeasonDto>();
            foreach (var season in seasons)
            {
                // Get match count for this season
                var matches = await _unitOfWork.Matches.FindAsync(m => m.SeasonId == season.Id);
                
                seasonDtos.Add(new SeasonDto
                {
                    Id = season.Id,
                    Name = season.Name,
                    LeagueName = season.LeagueName,
                    Country = season.Country,
                    CurrentRound = season.CurrentRound,
                    TotalRounds = season.TotalRounds,
                    IsActive = season.IsActive,
                    IsCompleted = season.IsCompleted,
                });
            }
            
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
