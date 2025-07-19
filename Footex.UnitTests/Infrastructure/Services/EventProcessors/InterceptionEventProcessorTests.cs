using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class InterceptionEventProcessorTests
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
            HomeTeamPossessionWon = 0,
            AwayTeamPossessionWon = 0,
        },
    };

    private readonly InterceptionEventProcessor _processor = new();

    [Theory]
    [InlineData("interception", "any", true)]
    [InlineData("pass", "Interception", true)]
    [InlineData("pass", "Regular", false)]
    [InlineData("shot", "any", false)]
    public void CanProcess_ShouldReturnCorrectValue_ForActionAndType(
        string action,
        string type,
        bool expected
    )
    {
        var matchEvent = new FootballMatchEvent { action = action, type = type };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementHomeTeamPossessionWon()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "interception" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamPossessionWon);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementAwayTeamPossessionWon()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "interception" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamPossessionWon);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalInterceptions()
    {
        var matchEvent = new FootballMatchEvent { action = "interception" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalInterceptions);
    }
}
