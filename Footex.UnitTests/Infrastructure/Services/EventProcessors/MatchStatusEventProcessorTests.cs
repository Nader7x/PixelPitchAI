using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class MatchStatusEventProcessorTests
    {
        private readonly MatchStatusEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user");

        [Theory]
        [InlineData("match_start", true)]
        [InlineData("match_end", true)]
        [InlineData("first_half_end", true)]
        [InlineData("second_half_start", true)]
        [InlineData("goal", false)]
        public void CanProcess_ShouldReturnCorrectValue_ForAction(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_MatchStart_ShouldUpdateStatusToInProgressAndLive()
        {
            var matchEvent = new FootballMatchEvent { action = "match_start" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal("In Progress", _match.MatchStatus);
            Assert.True(_match.IsLive);
        }

        [Fact]
        public void ProcessMatchEvent_MatchEnd_ShouldUpdateStatusToCompletedAndNotLive()
        {
            var matchEvent = new FootballMatchEvent { action = "match_end" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal("Completed", _match.MatchStatus);
            Assert.False(_match.IsLive);
        }

        [Fact]
        public void ProcessMatchEvent_FirstHalfEnd_ShouldUpdateStatusToHalfTimeAndLive()
        {
            var matchEvent = new FootballMatchEvent { action = "first_half_end" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal("Half Time", _match.MatchStatus);
            Assert.True(_match.IsLive);
        }

        [Fact]
        public void ProcessMatchEvent_SecondHalfStart_ShouldUpdateStatusToInProgressAndLive()
        {
            var matchEvent = new FootballMatchEvent { action = "second_half_start" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal("In Progress", _match.MatchStatus);
            Assert.True(_match.IsLive);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalEvents()
        {
            var matchEvent = new FootballMatchEvent { action = "match_start" };
            var matchEvents = new MatchEvents();
            var initialTotalEvents = matchEvents.TotalEvents;

            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);

            Assert.Equal(initialTotalEvents + 1, matchEvents.TotalEvents);
        }
    }
}
