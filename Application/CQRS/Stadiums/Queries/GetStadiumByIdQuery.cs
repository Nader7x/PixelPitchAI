using Application.Dtos;
using Application.Mappers;
using Domain.Interfaces;
using MediatR;

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
    private readonly StadiumMapper _stadiumMapper;
    private readonly IUnitOfWork _unitOfWork;

    public GetStadiumByIdQueryHandler(IUnitOfWork unitOfWork, StadiumMapper stadiumMapper)
    {
        _unitOfWork = unitOfWork;
        _stadiumMapper = stadiumMapper;
    }

    public async Task<GetStadiumByIdQueryResponse> Handle(GetStadiumByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stadium = await _unitOfWork.Stadiums.GetByIdAsync(request.Id);
            if (stadium == null)
                return new GetStadiumByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found"
                };

            var stadiumDto = _stadiumMapper.ToDto(stadium);

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