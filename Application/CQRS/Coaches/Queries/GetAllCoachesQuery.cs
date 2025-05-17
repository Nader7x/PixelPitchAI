using Application.Dtos;
using MediatR;
using Domain.Interfaces;
using System.Collections.Generic;
using Application.Mappers;
using Domain.Models;

namespace Application.CQRS.Coaches.Queries;

public class GetAllCoachesQuery : IRequest<GetAllCoachesQueryResponse>
{
    // Optional parameters for filtering
    public string? Nationality { get; set; }
    public int? TeamId { get; set; }
}

public class GetAllCoachesQueryResponse
{
    public bool Succeeded { get; set; }
    public List<CoachDto> Coaches { get; set; }
    public string Error { get; set; }
}

public class GetAllCoachesQueryHandler : IRequestHandler<GetAllCoachesQuery, GetAllCoachesQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CoachMapper _coachMapper;
    
    public GetAllCoachesQueryHandler(IUnitOfWork unitOfWork, CoachMapper coachMapper)
    {
        _unitOfWork = unitOfWork;
        _coachMapper = coachMapper;
    }
    
    public async Task<GetAllCoachesQueryResponse> Handle(GetAllCoachesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Coach> coaches;
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(request.Nationality) && request.TeamId.HasValue)
            {
                coaches = await _unitOfWork.Coaches.GetAllAsync(c => 
                    c.Nationality.ToLower() == request.Nationality.ToLower() && 
                    c.TeamId == request.TeamId.Value);
            }
            else if (!string.IsNullOrEmpty(request.Nationality))
            {
                coaches = await _unitOfWork.Coaches.GetAllAsync(c => 
                    c.Nationality.ToLower() == request.Nationality.ToLower());
            }
            else if (request.TeamId.HasValue)
            {
                coaches = await _unitOfWork.Coaches.GetAllAsync(c => c.TeamId == request.TeamId.Value);
            }
            else
            {
                coaches = await _unitOfWork.Coaches.GetAllAsync();
            }

            var coachDtos = _coachMapper.ToDtoList(coaches);
            return new GetAllCoachesQueryResponse
            {
                Succeeded = true,
                Coaches = coachDtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllCoachesQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
