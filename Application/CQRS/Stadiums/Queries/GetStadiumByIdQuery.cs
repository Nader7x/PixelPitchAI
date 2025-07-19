using Application.Dtos;
using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Stadiums.Queries;

public class GetStadiumByIdQuery : IRequest<GetStadiumByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetStadiumByIdQueryResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public StadiumDto? Stadium { get; init; }
    public string? Error { get; init; }
}

public class GetStadiumByIdQueryHandler(IUnitOfWork unitOfWork, IStadiumMapper stadiumMapper)
    : IRequestHandler<GetStadiumByIdQuery, GetStadiumByIdQueryResponse>
{
    public async Task<GetStadiumByIdQueryResponse> Handle(
        GetStadiumByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var stadium = await unitOfWork.Stadiums.GetByIdAsync(request.Id, cancellationToken);
            if (stadium == null)
                return new GetStadiumByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Stadium with ID {request.Id} not found",
                };

            var stadiumDto = stadiumMapper.ToDto(stadium);

            return new GetStadiumByIdQueryResponse { Succeeded = true, Stadium = stadiumDto };
        }
        catch (Exception ex)
        {
            return new GetStadiumByIdQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
