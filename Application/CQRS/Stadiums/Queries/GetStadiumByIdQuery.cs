using Application.Dtos;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Stadiums.Queries;

public class GetStadiumByIdQuery : IRequest<GetStadiumByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetStadiumByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public StadiumDto Stadium { get; set; }
    public string Error { get; set; }
}

public class GetStadiumByIdQueryHandler : IRequestHandler<GetStadiumByIdQuery, GetStadiumByIdQueryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetStadiumByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetStadiumByIdQueryResponse> Handle(GetStadiumByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stadium = await _unitOfWork.Stadiums.GetByIdAsync(request.Id);
            if (stadium == null)
            {
                return new GetStadiumByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found"
                };
            }
            
            var stadiumDto = new StadiumDto
            {
                Id = stadium.Id,
                Name = stadium.Name,
                City = stadium.City,
                Country = stadium.Country,
                Capacity = stadium.Capacity,
                SurfaceType = stadium.SurfaceType,
                Address = stadium.Address,
                Latitude = stadium.Latitude,
                Longitude = stadium.Longitude,
                ImageUrl = stadium.ImageUrl,
                Description = stadium.Description,
                Facilities = stadium.Facilities,
                BuiltDate = stadium.BuiltDate
            };
            
            return new GetStadiumByIdQueryResponse
            {
                Succeeded = true,
                Stadium = stadiumDto
            };
        }
        catch (Exception ex)
        {
            return new GetStadiumByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
