using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class BlockEventProcessorTests
{
    private readonly Match _match = new() { CreatorId = "null" };

    private readonly BlockEventProcessor _processor = new();

    [Theory]
    [InlineData("block", "any", true)]
    [InlineData("shot", "Blocked", true)]
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
    public void ProcessEventCounters_ShouldIncrementTotalBlocks()
    {
        var matchEvent = new FootballMatchEvent { action = "block" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalBlocks);
    }
}
