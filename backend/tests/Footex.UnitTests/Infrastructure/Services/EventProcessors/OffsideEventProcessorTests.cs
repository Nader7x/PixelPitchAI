using Domain.Models;
using Footex.UnitTests.Common;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class OffsideEventProcessorTests
{
    private readonly OffsideEventProcessor _processor = new();

    [Theory]
    [InlineData("offside", null, true)]
    [InlineData("pass", "Pass Offside", true)]
    [InlineData("pass", "Complete", false)]
    [InlineData("goal", null, false)]
    public void CanProcess_ShouldReturnCorrectValue(string action, string? outcome, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action, outcome = outcome };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamOffside_ShouldIncrementHomeTeamOffsides()
    {
        var match = TestDataBuilder.CreateValidMatch();
        match.HomeTeamInMatchName = "Team A";
        var matchEvent = new FootballMatchEvent { action = "offside", team = "Team A" };
        _processor.ProcessMatchEvent(matchEvent, match);
        Assert.Equal(1, match.MatchStatistics?.HomeTeamOffsides);
        Assert.Null(match.MatchStatistics?.AwayTeamOffsides);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamOffside_ShouldIncrementAwayTeamOffsides()
    {
        var match = TestDataBuilder.CreateValidMatch();
        var matchEvent = new FootballMatchEvent { action = "offside", team = "Team B" };
        _processor.ProcessMatchEvent(matchEvent, match);
        Assert.Null(match.MatchStatistics?.HomeTeamOffsides);
        Assert.Equal(1, match.MatchStatistics?.AwayTeamOffsides);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalOffsides()
    {
        var match = TestDataBuilder.CreateValidMatch();
        var matchEvent = new FootballMatchEvent { action = "offside" };
        var matchEvents = new MatchEvents();
        var initialOffsides = matchEvents.TotalOffsides;

        _processor.ProcessEventCounters(matchEvent, matchEvents, match);

        Assert.Equal(initialOffsides + 1, matchEvents.TotalOffsides);
    }
}
