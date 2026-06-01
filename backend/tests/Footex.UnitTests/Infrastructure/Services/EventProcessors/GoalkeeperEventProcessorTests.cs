using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class GoalkeeperEventProcessorTests
{
    private readonly Match _match = new()
    {
        HomeTeamId = 1,
        AwayTeamId = 2,
        HomeTeamInMatchName = "Home Team",
        AwayTeamInMatchName = "Away Team",
        CreatorId = "null",
        MatchStatistics = new MatchStatistics
        {
            MatchId = 1,
            HomeTeamSaves = 0,
            AwayTeamSaves = 0,
            HomeTeamShotsOnTarget = 0,
        },
    };

    private readonly GoalkeeperEventProcessor _processor = new();

    [Theory]
    [InlineData("Save", "any", true)]
    [InlineData("goal keeper", "any", true)]
    [InlineData("shot", "Saved", true)]
    [InlineData("shot", "Goal", false)]
    [InlineData("pass", "any", false)]
    public void CanProcess_ShouldReturnCorrectValue_ForActionAndOutcome(
        string action,
        string outcome,
        bool expected
    )
    {
        var matchEvent = new FootballMatchEvent { action = action, outcome = outcome };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_SaveAction_ShouldIncrementHomeTeamSaves()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "Save" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamSaves);
    }

    [Fact]
    public void ProcessMatchEvent_GoalkeeperAction_ShouldIncrementAwayTeamSaves()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "goal keeper" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamSaves);
    }

    [Fact]
    public void ProcessMatchEvent_ShotSaved_ShouldIncrementOpposingTeamSavesAndShootingTeamShotsOnTarget()
    {
        var matchEvent = new FootballMatchEvent
        {
            team = "Home Team",
            action = "shot",
            outcome = "Saved",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamSaves);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamShotsOnTarget);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalGoalkeeperSaves()
    {
        var matchEvent = new FootballMatchEvent { action = "Save" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalGoalkeeperSaves);
    }
}
