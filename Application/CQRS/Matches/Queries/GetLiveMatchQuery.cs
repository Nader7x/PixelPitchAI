using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Matches.Queries;

public class GetLiveMatchQuery : IRequest<GetLiveMatchQueryResponse>
{
    public required string UserId { get; set; }
}

public class GetLiveMatchQueryResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public int MatchId { get; set; }
}

public class GetLiveMatchQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLiveMatchQuery, GetLiveMatchQueryResponse>
{
    public async Task<GetLiveMatchQueryResponse> Handle(GetLiveMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await unitOfWork.Matches.GetLiveMatchAsync(request.UserId);
        if (match == 0 || match == null)
            return new GetLiveMatchQueryResponse
            {
                Succeeded = false,
                Error = "No live match found for the user."
            };

        return new GetLiveMatchQueryResponse
        {
            Succeeded = true,
            MatchId = match
        };
    }
}