using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class ClearanceEventProcessorTests
    {
        private readonly ClearanceEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamInMatchName = "Home Team",
            AwayTeamInMatchName = "Away Team",
        };

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
            Assert.Equal(1, _match.HomeTeamClearances);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamClearances()
        {
            var matchEvent = new FootballMatchEvent { team = "Away Team", action = "clearance" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamClearances);
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
}
