using Application.CQRS.Matches.Commands;
using Application.Dtos;
using AutoFixture;
using Domain.Models;

namespace Footex.UnitTests.Common;

public static class TestDataBuilder
{
    private static readonly Fixture _fixture = new();

    static TestDataBuilder()
    {
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public static CreateMatchDto CreateValidCreateMatchDto()
    {
        return new CreateMatchDto
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeSeasonId = 1,
            AwaySeasonId = 2,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            CreatorId = Guid.NewGuid().ToString()
        };
    }

    public static UpdateMatchDto CreateValidUpdateMatchDto(int matchId)
    {
        return new UpdateMatchDto
        {
            Id = matchId,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchWeek = 1,
            ScheduledDateTimeUTC = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled"
        };
    }

    public static SimulateMatchDto CreateValidSimulateMatchDto()
    {
        return new SimulateMatchDto
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamName = "Arsenal",
            AwayTeamName = "Chelsea",
            HomeTeamSeason = "2023/24",
            AwayTeamSeason = "2023/24",
            HomeSeasonId = 7,
            AwaySeasonId = 7,
        };
    }

    public static Match CreateValidMatch(int? id = null)
    {
        return new Match
        {
            Id = id ?? _fixture.Create<int>(),
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamSeasonId = 1,
            AwayTeamSeasonId = 2,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            CreatorId = Guid.NewGuid().ToString(),
            HomeTeamInMatchName = "Arsenal_2024",
            AwayTeamInMatchName = "Chelsea_2024"
        };
    }

    public static Team CreateValidTeam(int? id = null, string? name = null)
    {
        return new Team
        {
            Id = id ?? _fixture.Create<int>(),
            Name = name ?? _fixture.Create<string>(),
            FoundationDate = DateTime.UtcNow.AddYears(-50),
            Country = "England"
        };
    }

    public static Season CreateValidSeason(int? id = null, string? name = null)
    {
        return new Season
        {
            Id = id ?? _fixture.Create<int>(),
            Name = name ?? "2023/24",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            LeagueName = "Premier League",
            Country = "England"
        };
    }

    public static UpdateMatchCommand CreateValidUpdateMatchCommand()
    {
        return new UpdateMatchCommand
        {
            Id = _fixture.Create<int>(),
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchWeek = 1,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled"
        };
    }

    public static CreateMatchCommand CreateValidCreateMatchCommand()
    {
        return new CreateMatchCommand
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeSeasonId = 1,
            AwaySeasonId = 2,
            ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(7),
            MatchStatus = "Scheduled",
            CreatorId = Guid.NewGuid().ToString()
        };
    }
}