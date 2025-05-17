using Application.Dtos;
using MediatR;
using Domain.Interfaces;
using System.Collections.Generic;
using Application.Mappers;

namespace Application.CQRS.Stadiums.Queries;

public class GetAllStadiumsQuery : IRequest<GetAllStadiumsQueryResponse>
{
    // Optional parameters for filtering
    public string? Country { get; set; }
    public string? City { get; set; }
}

public class GetAllStadiumsQueryResponse
{
    public bool Succeeded { get; set; }
    public List<StadiumDto> Stadiums { get; set; }
    public string Error { get; set; }
}

public class GetAllStadiumsQueryHandler : IRequestHandler<GetAllStadiumsQuery, GetAllStadiumsQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly StadiumMapper _stadiumMapper;
    
    public GetAllStadiumsQueryHandler(IUnitOfWork unitOfWork, StadiumMapper stadiumMapper)
    {
        _unitOfWork = unitOfWork;
        _stadiumMapper = stadiumMapper;
    }
    
    public async Task<GetAllStadiumsQueryResponse> Handle(GetAllStadiumsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Domain.Models.Stadium> stadiums;
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(request.Country) && !string.IsNullOrEmpty(request.City))
            {
                stadiums = await _unitOfWork.Stadiums.GetAllAsync(s => 
                    s.Country.Equals(request.Country, StringComparison.CurrentCultureIgnoreCase) && 
                    s.City.Equals(request.City, StringComparison.CurrentCultureIgnoreCase));
            }
            else if (!string.IsNullOrEmpty(request.Country))
            {
                stadiums = await _unitOfWork.Stadiums.GetAllAsync(s => 
                    s.Country.ToLower() == request.Country.ToLower());
            }
            else if (!string.IsNullOrEmpty(request.City))
            {
                stadiums = await _unitOfWork.Stadiums.GetAllAsync(s => 
                    s.City.ToLower() == request.City.ToLower());
            }
            else
            {
                stadiums = await _unitOfWork.Stadiums.GetAllAsync();
            }
            
            var stadiumDtos = _stadiumMapper.ToDtoList(stadiums);
            
            return new GetAllStadiumsQueryResponse
            {
                Succeeded = true,
                Stadiums = stadiumDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllStadiumsQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
