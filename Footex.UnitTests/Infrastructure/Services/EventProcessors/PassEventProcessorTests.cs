using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class PassEventProcessorTests
{
    private readonly Match _match = new()
    {
        Id = 1,
        HomeTeamId = 1,
        AwayTeamId = 2,
        HomeTeamInMatchName = "Team A",
        AwayTeamInMatchName = "Team B",
        CreatorId = "null",
        MatchStatistics = new MatchStatistics
        {
            MatchId = 1,
            HomeTeamPasses = 0,
            AwayTeamPasses = 0,
            HomeTeamPassesCompleted = 0,
            AwayTeamOffsides = 0,
            HomeTeamCorners = 0,
        },
    };

    private readonly PassEventProcessor _processor = new();

    [Theory]
    [InlineData("pass", true)]
    [InlineData("shot", false)]
    public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamPass_ShouldIncrementHomeTeamPasses()
    {
        var matchEvent = new FootballMatchEvent { action = "pass", team = "Team A" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamPasses);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamPass_ShouldIncrementAwayTeamPasses()
    {
        var matchEvent = new FootballMatchEvent { action = "pass", team = "Team B" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamPasses);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamCompletedPass_ShouldIncrementCompletedPasses()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "pass",
            team = "Team A",
            outcome = "Complete",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamPassesCompleted);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamCorner_ShouldIncrementCorners()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "pass",
            team = "Team A",
            type = "Corner",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamCorners);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamPassOffside_ShouldIncrementOffsides()
    {
        var matchEvent = new FootballMatchEvent
        {
            action = "pass",
            team = "Team B",
            outcome = "Pass Offside",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamOffsides);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalPasses()
    {
        var matchEvent = new FootballMatchEvent { action = "pass" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalPasses);
    }
}
