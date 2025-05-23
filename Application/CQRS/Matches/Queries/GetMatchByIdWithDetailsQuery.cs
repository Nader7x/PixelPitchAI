using Application.Dtos;
using Application.Mappers;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Queries;

public class GetMatchByIdWithDetailsQuery : IRequest<GetMatchByIdWithDetailsQueryResponse>
{
    public int MatchId { get; set; }
}

public class GetMatchByIdWithDetailsQueryResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public MatchDetailsDto? Match { get; set; }
    public string? Error { get; set; }
}

public class GetMatchByIdWithDetailsQueryHandler(MatchMapper matchMapper, IUnitOfWork unitOfWork)
    : IRequestHandler<GetMatchByIdWithDetailsQuery, GetMatchByIdWithDetailsQueryResponse>
{
    public async Task<GetMatchByIdWithDetailsQueryResponse> Handle(GetMatchByIdWithDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var match = await unitOfWork.Matches.GetByIdWithDetailsAsync(request.MatchId);
            if (match == null)
            {
                return new GetMatchByIdWithDetailsQueryResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"Match with ID {request.MatchId} not found"
                };
            }

            var matchDto = matchMapper.ToDetailsFromMatch(match);
            return new GetMatchByIdWithDetailsQueryResponse
            {
                Succeeded = true,
                Match = matchDto
            };
        }
        catch (Exception ex)
        {
            return new GetMatchByIdWithDetailsQueryResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}


