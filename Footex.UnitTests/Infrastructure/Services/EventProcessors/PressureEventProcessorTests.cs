using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class PressureEventProcessorTests
    {
        private readonly PressureEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user");

        [Theory]
        [InlineData("pressure", true)]
        [InlineData("shot", false)]
        public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldNotChangeMatchState()
        {
            var initialMatch = new Match(1, "test-user");
            var matchEvent = new FootballMatchEvent { action = "pressure" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(initialMatch.HomeTeamScore, _match.HomeTeamScore);
            Assert.Equal(initialMatch.AwayTeamScore, _match.AwayTeamScore);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalEvents()
        {
            var matchEvent = new FootballMatchEvent { action = "pressure" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalEvents);
        }
    }
}
