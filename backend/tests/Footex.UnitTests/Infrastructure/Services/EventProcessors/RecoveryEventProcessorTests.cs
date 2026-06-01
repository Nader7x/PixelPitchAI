using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors;

public class RecoveryEventProcessorTests
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
            HomeTeamRecoveries = 0,
            AwayTeamRecoveries = 0,
            HomeTeamPossessionWon = 0,
            AwayTeamPossessionWon = 0,
        },
    };

    private readonly RecoveryEventProcessor _processor = new();

    [Theory]
    [InlineData("ball recovery", null, true)]
    [InlineData("pass", "Recovery", true)]
    [InlineData("pass", "Complete", false)]
    [InlineData("shot", null, false)]
    public void CanProcess_ShouldReturnCorrectValue(string action, string? type, bool expected)
    {
        var matchEvent = new FootballMatchEvent { action = action, type = type };
        var result = _processor.CanProcess(matchEvent);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProcessMatchEvent_HomeTeamRecovery_ShouldIncrementRecoveriesAndPossessionWon()
    {
        var matchEvent = new FootballMatchEvent { action = "ball recovery", team = "Team A" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamRecoveries);
        Assert.Equal(1, _match.MatchStatistics?.HomeTeamPossessionWon);
    }

    [Fact]
    public void ProcessMatchEvent_AwayTeamRecovery_ShouldIncrementRecoveriesAndPossessionWon()
    {
        var matchEvent = new FootballMatchEvent { action = "ball recovery", team = "Team B" };
        _processor.ProcessMatchEvent(matchEvent, _match);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamRecoveries);
        Assert.Equal(1, _match.MatchStatistics?.AwayTeamPossessionWon);
    }

    [Fact]
    public void ProcessEventCounters_ShouldIncrementTotalPossessionWon()
    {
        var matchEvent = new FootballMatchEvent { action = "ball recovery" };
        var matchEvents = new MatchEvents();
        _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
        Assert.Equal(1, matchEvents.TotalPossessionWon);
    }
}
