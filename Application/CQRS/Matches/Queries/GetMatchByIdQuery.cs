using Application.Dtos;
using Application.Mappers;
using MediatR;
using Domain.Interfaces;

namespace Application.CQRS.Matches.Queries;

public class GetMatchByIdQuery : IRequest<GetMatchByIdQueryResponse>
{
    public int Id { get; set; }
}

public class GetMatchByIdQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public MatchDto Match { get; set; }
    public string Error { get; set; }
}

public class GetMatchByIdQueryHandler(IUnitOfWork unitOfWork, MatchMapper matchMapper)
    : IRequestHandler<GetMatchByIdQuery, GetMatchByIdQueryResponse>
{
    public async Task<GetMatchByIdQueryResponse> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(request.Id);
            if (match == null)
            {
                return new GetMatchByIdQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.Id} not found"
                };
            }
            
            var matchDto = matchMapper.ToDto(match);
            return new GetMatchByIdQueryResponse
            {
                Succeeded = true,
                Match = matchDto
            };
        }
        catch (Exception ex)
        {
            return new GetMatchByIdQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
