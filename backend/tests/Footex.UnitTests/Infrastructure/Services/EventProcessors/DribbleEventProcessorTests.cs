using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class DribbleEventProcessorTests
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
            HomeTeamDribbles = 0,
            AwayTeamDribbles = 0,
        },
    };

    private readonly DribbleEventProcessor _processor = new();

    [Theory]
    [InlineData("dribble", true)]
    [InlineData("carry", true)]
    [InlineData("pass", false)]
    public void CanProcess_ShouldReturnCorrectValue_ForAction(string action, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementHomeTeamDribbles()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "dribble" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamDribbles);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementAwayTeamDribbles_ForDribble()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "dribble" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamDribbles);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementAwayTeamDribbles_ForCarry()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "carry" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamDribbles);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalDribbles()
    {
        var matchEvent = new FootballMatchEvent { action = "dribble" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalDribbles);
    }
}
