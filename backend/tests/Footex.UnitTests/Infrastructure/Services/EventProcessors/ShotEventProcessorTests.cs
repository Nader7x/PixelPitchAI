using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class ShotEventProcessorTests
{
    private readonly Match _match = new()
    {
        Id = 1,
        HomeTeamId = 1,
        AwayTeamId = 2,
        HomeTeamInMatchName = "Team A",
        AwayTeamInMatchName = "Team B",
        HomeTeamScore = 0,
        AwayTeamScore = 0,
        CreatorId = "null",
        MatchStatistics = new MatchStatistics
        {
            MatchId = 1,
            HomeTeamShots = 0,
            AwayTeamShots = 0,
            HomeTeamShotsOnTarget = 0,
            AwayTeamShotsOnTarget = 0,
            HomeTeamSaves = 0,
        },
    };

    private readonly ShotEventProcessor _processor = new();

    [Theory]
    [InlineData("shot", true)]
    [InlineData("pass", false)]
    public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamShot_ShouldIncrementShots()
    {
        var matchEvent = new FootballMatchEvent { action = "shot", team = "Team A" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamShots);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamShot_ShouldIncrementShots()
    {
        var matchEvent = new FootballMatchEvent { action = "shot", team = "Team B" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamShots);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamGoal_ShouldIncrementScoreAndShotsOnTarget()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "shot",
            team = "Team A",
            outcome = "Goal",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.HomeTeamScore);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamShotsOnTarget);
        Assert.Equal(_match.HomeTeamId, _match.WinningTeamId);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamSavedShot_ShouldIncrementSavesAndShotsOnTarget()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "shot",
            team = "Team B",
            outcome = "Saved",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamShotsOnTarget);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamSaves);
    }

    [Fact]
    public void ProcessEventCounters_Goal_ShouldIncrementCountersCorrectly()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "shot",
            team = "Team A",
            outcome = "Goal",
        };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalShots);
        Assert.Equal(1, matchEvents.TotalGoals);
        Assert.Equal(1, matchEvents.GoalsHomeTeam);
    }
}
