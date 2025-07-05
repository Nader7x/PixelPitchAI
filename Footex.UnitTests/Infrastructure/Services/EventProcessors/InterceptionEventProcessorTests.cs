using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class InterceptionEventProcessorTests
    {
        private readonly InterceptionEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamInMatchName = "Home Team",
            AwayTeamInMatchName = "Away Team",
        };

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
            Assert.Equal(1, _match.HomeTeamPossessionWon);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamPossessionWon()
        {
            var matchEvent = new FootballMatchEvent { team = "Away Team", action = "interception" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamPossessionWon);
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
}
