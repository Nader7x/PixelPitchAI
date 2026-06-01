using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class ClearanceEventProcessorTests
{
    private readonly Match _match = new()
    {
        Id = 1,
        HomeTeamId = 1,
        AwayTeamId = 2,
        HomeTeamInMatchName = "Home Team",
        AwayTeamInMatchName = "Away Team",
        CreatorId = "null",
        MatchStatistics = new MatchStatistics { MatchId = 1 },
    };

    private readonly ClearanceEventProcessor _processor = new();

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForClearanceAction()
    {
        var matchEvent = new FootballMatchEvent { action = "clearance" };
        Assert.True(_processor.CanProcess(matchEvent));
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForOtherAction()
    {
        var matchEvent = new FootballMatchEvent { action = "goal" };
        Assert.False(_processor.CanProcess(matchEvent));
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementHomeTeamClearances()
    {
        var matchEvent = new FootballMatchEvent { team = "Home Team", action = "clearance" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamClearances);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementAwayTeamClearances()
    {
        var matchEvent = new FootballMatchEvent { team = "Away Team", action = "clearance" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamClearances);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalClearances()
    {
        var matchEvent = new FootballMatchEvent { action = "clearance" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalClearances);
    }
}
