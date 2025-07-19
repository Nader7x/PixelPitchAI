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
    public bool HasLiveMatch { get; init; }
    public LiveMatchDto? LiveMatch { get; init; }
}

public class LiveMatchDto
{
    public int Id { get; set; }
    public bool IsLive { get; set; }
    public LiveTeamDto? HomeTeam { get; set; }
    public LiveTeamDto? AwayTeam { get; set; }
    public int HomeTeamScore { get; set; }
    public int AwayTeamScore { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public int HomeTeamPossession { get; set; }
    public int AwayTeamPossession { get; set; }
    public int HomeTeamShots { get; set; }
    public int AwayTeamShots { get; set; }
    public int HomeTeamShotsOnTarget { get; set; }
    public int AwayTeamShotsOnTarget { get; set; }
    public int HomeTeamCorners { get; set; }
    public int AwayTeamCorners { get; set; }
    public int HomeTeamFouls { get; set; }
    public int AwayTeamFouls { get; set; }
    public int HomeTeamYellowCards { get; set; }
    public int AwayTeamYellowCards { get; set; }
    public int HomeTeamRedCards { get; set; }
    public int AwayTeamRedCards { get; set; }
}

public class LiveTeamDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Logo { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? League { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public DateTime FoundationDate { get; set; }
}

public class GetLiveMatchQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLiveMatchQuery, GetLiveMatchQueryResponse>
{
    public async Task<GetLiveMatchQueryResponse> Handle(
        GetLiveMatchQuery request,
        CancellationToken cancellationToken
    )
    {
        var match = await unitOfWork.Matches.GetLiveMatchAsync(request.UserId);
        if (match is not { IsLive: true })
            return new GetLiveMatchQueryResponse
            {
                Succeeded = true,
                HasLiveMatch = false,
                Error = "No live match found for the user.",
            };

        return new GetLiveMatchQueryResponse
        {
            Succeeded = true,
            HasLiveMatch = true,
            LiveMatch = new LiveMatchDto
            {
                Id = match.Id,
                IsLive = match.IsLive,
                HomeTeam = new LiveTeamDto
                {
                    Id = match.HomeTeam?.Id ?? 0,
                    Name = match.HomeTeam?.Name,
                    ShortName = match.HomeTeam?.ShortName,
                    Logo = match.HomeTeam?.Logo,
                    Country = match.HomeTeam?.Country,
                    City = match.HomeTeam?.City,
                    League = match.HomeTeam?.League,
                    PrimaryColor = match.HomeTeam?.PrimaryColor,
                    SecondaryColor = match.HomeTeam?.SecondaryColor,
                    FoundationDate = match.HomeTeam?.FoundationDate ?? DateTime.MinValue,
                },
                AwayTeam = new LiveTeamDto
                {
                    Id = match.AwayTeam?.Id ?? 0,
                    Name = match.AwayTeam?.Name,
                    ShortName = match.AwayTeam?.ShortName,
                    Logo = match.AwayTeam?.Logo,
                    Country = match.AwayTeam?.Country,
                    City = match.AwayTeam?.City,
                    League = match.AwayTeam?.League,
                    PrimaryColor = match.AwayTeam?.PrimaryColor,
                    SecondaryColor = match.AwayTeam?.SecondaryColor,
                    FoundationDate = match.AwayTeam?.FoundationDate ?? DateTime.MinValue,
                },
                HomeTeamScore = match.HomeTeamScore ?? 0,
                AwayTeamScore = match.AwayTeamScore ?? 0,
                ScheduledDateTimeUtc = match.ScheduledDateTimeUtc,
                MatchStatus = match.MatchStatus ?? "Scheduled",
                HomeTeamPossession = match.MatchStatistics?.HomeTeamPossession ?? 0,
                AwayTeamPossession = match.MatchStatistics?.AwayTeamPossession ?? 0,
                HomeTeamShots = match.MatchStatistics?.HomeTeamShots ?? 0,
                AwayTeamShots = match.MatchStatistics?.AwayTeamShots ?? 0,
                HomeTeamShotsOnTarget = match.MatchStatistics?.HomeTeamShotsOnTarget ?? 0,
                AwayTeamShotsOnTarget = match.MatchStatistics?.AwayTeamShotsOnTarget ?? 0,
                HomeTeamCorners = match.MatchStatistics?.HomeTeamCorners ?? 0,
                AwayTeamCorners = match.MatchStatistics?.AwayTeamCorners ?? 0,
                HomeTeamFouls = match.MatchStatistics?.HomeTeamFouls ?? 0,
                AwayTeamFouls = match.MatchStatistics?.AwayTeamFouls ?? 0,
                HomeTeamYellowCards = match.MatchStatistics?.HomeTeamYellowCards ?? 0,
                AwayTeamYellowCards = match.MatchStatistics?.AwayTeamYellowCards ?? 0,
                HomeTeamRedCards = match.MatchStatistics?.HomeTeamRedCards ?? 0,
                AwayTeamRedCards = match.MatchStatistics?.AwayTeamRedCards ?? 0,
            },
        };
    }
}
