using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class FoulEventProcessorTests
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
            HomeTeamFouls = 0,
            AwayTeamFouls = 0,
            HomeTeamYellowCards = 0,
            AwayTeamRedCards = 0,
            HomeTeamFreeKicks = 0,
            AwayTeamFreeKicks = 0,
        },
    };

    private readonly FoulEventProcessor _processor = new();

    [Theory]
    [InlineData("foul committed", true)]
    [InlineData("foul won", true)]
    [InlineData("pass", false)]
    public void CanProcess_ShouldReturnCorrectValue_ForAction(string action, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_FoulCommitted_ShouldIncrementHomeTeamFouls()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "foul committed" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamFouls);
    }

    [Fact]
    public void ProcessMatchEvent_FoulCommitted_ShouldIncrementAwayTeamFouls()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "foul committed" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamFouls);
    }

    [Fact]
    public void ProcessMatchEvent_FoulCommitted_ShouldIncrementHomeTeamYellowCards()
    {
        var matchEvent = new FootballMatchEvent
        {
            team = "Home Team",
            action = "foul committed",
            card = "Yellow Card",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamYellowCards);
    }

    [Fact]
    public void ProcessMatchEvent_FoulCommitted_ShouldIncrementAwayTeamRedCards()
    {
        var matchEvent = new FootballMatchEvent
        {
            team = "Away Team",
            action = "foul committed",
            card = "Red Card",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamRedCards);
    }

    [Fact]
    public void ProcessMatchEvent_FoulWon_ShouldIncrementHomeTeamFreeKicks()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "foul won" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamFreeKicks);
    }

    [Fact]
    public void ProcessMatchEvent_FoulWon_ShouldIncrementAwayTeamFreeKicks()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "foul won" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamFreeKicks);
    }

    [Fact]
    public void ProcessEventCounters_FoulCommitted_ShouldIncrementTotalFouls()
    {
        var matchEvent = new FootballMatchEvent { action = "foul committed" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalFouls);
    }

    [Fact]
    public void ProcessEventCounters_FoulCommitted_ShouldIncrementTotalPenalties()
    {
        var matchEvent = new FootballMatchEvent { action = "foul committed", outcome = "Penalty" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalPenalties);
    }

    [Fact]
    public void ProcessEventCounters_FoulCommitted_ShouldIncrementCardCounters()
    {
        var matchEvent = new FootballMatchEvent { action = "foul committed", card = "Yellow Card" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalCards);
        Assert.Equal(1, matchEvents.TotalYellowCards);
    }

    [Fact]
    public void ProcessEventCounters_FoulWon_ShouldIncrementTotalFreeKicks()
    {
        var matchEvent = new FootballMatchEvent { action = "foul won" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalFreeKicks);
    }
}
