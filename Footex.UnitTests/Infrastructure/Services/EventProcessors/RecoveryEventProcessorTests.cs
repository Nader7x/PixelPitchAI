using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class RecoveryEventProcessorTests
    {
        private readonly RecoveryEventProcessor _processor = new();
        private Match _match;

        public RecoveryEventProcessorTests()
        {
            _match = new Match(1, "test-user")
            {
                HomeTeamId = 1,
                AwayTeamId = 2,
                HomeTeamInMatchName = "Team A",
                AwayTeamInMatchName = "Team B",
                HomeTeamRecoveries = 0,
                AwayTeamRecoveries = 0,
                HomeTeamPossessionWon = 0,
                AwayTeamPossessionWon = 0,
            };
        }

        [Theory]
        [InlineData("ball recovery", null, true)]
        [InlineData("pass", "Recovery", true)]
        [InlineData("pass", "Complete", false)]
        [InlineData("shot", null, false)]
        public void CanProcess_ShouldReturnCorrectValue(string action, string type, bool expected)
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
            Assert.Equal(1, _match.HomeTeamRecoveries);
            Assert.Equal(1, _match.HomeTeamPossessionWon);
        }

        [Fact]
        public void ProcessMatchEvent_AwayTeamRecovery_ShouldIncrementRecoveriesAndPossessionWon()
        {
            var matchEvent = new FootballMatchEvent { action = "ball recovery", team = "Team B" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamRecoveries);
            Assert.Equal(1, _match.AwayTeamPossessionWon);
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
}
