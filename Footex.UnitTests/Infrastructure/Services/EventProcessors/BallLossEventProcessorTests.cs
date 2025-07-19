using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class BallLossEventProcessorTests
{
    private readonly Match _match = new() { CreatorId = "null" };

    private readonly BallLossEventProcessor _processor = new();

    [Theory]
    [InlineData("miscontrol", true)]
    [InlineData("dispossessed", true)]
    [InlineData("error", true)]
    [InlineData("goal", false)]
    public void CanProcess_ShouldReturnCorrectValue_ForAction(string action, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalOuts_ForMiscontrol()
    {
        var matchEvent = new FootballMatchEvent { action = "miscontrol" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalOuts);
        Assert.Equal(1, matchEvents.TotalEvents);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalPossessionWon_ForDispossessed()
    {
        var matchEvent = new FootballMatchEvent { action = "dispossessed" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalPossessionWon);
        Assert.Equal(1, matchEvents.TotalEvents);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalErrors_ForError()
    {
        var matchEvent = new FootballMatchEvent { action = "error" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalErrors);
        Assert.Equal(1, matchEvents.TotalEvents);
    }
}
