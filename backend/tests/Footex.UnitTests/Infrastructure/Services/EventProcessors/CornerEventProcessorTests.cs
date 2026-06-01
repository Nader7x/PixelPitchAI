using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class CornerEventProcessorTests
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
            HomeTeamCorners = 0,
            AwayTeamCorners = 0,
        },
    };

    private readonly CornerEventProcessor _processor = new();

    [Theory]
    [InlineData("pass", "Corner", true)]
    [InlineData("pass", "Regular", false)]
    [InlineData("shot", "Corner", false)]
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
    public void ProcessMatchEvent_ShouldIncrementHomeTeamCorners()
    {
        var matchEvent = new FootballMatchEvent
        {
            team = "Home Team",
            action = "pass",
            type = "Corner",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamCorners);
    }

    [Fact]
    public void ProcessMatchEvent_ShouldIncrementAwayTeamCorners()
    {
        var matchEvent = new FootballMatchEvent
        {
            team = "Away Team",
            action = "pass",
            type = "Corner",
        };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamCorners);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalCorners()
    {
        var matchEvent = new FootballMatchEvent { action = "pass", type = "Corner" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalCorners);
    }
}
