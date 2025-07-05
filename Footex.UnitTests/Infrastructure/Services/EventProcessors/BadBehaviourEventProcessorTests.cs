using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class BadBehaviourEventProcessorTests
    {
        private readonly BadBehaviourEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamInMatchName = "Home Team",
            AwayTeamInMatchName = "Away Team",
        };

        [Fact]
        public void CanProcess_ShouldReturnTrue_ForBadBehaviourAction()
        {
            var matchEvent = new FootballMatchEvent { action = "bad behaviour" };
            Assert.True(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void CanProcess_ShouldReturnFalse_ForOtherAction()
        {
            var matchEvent = new FootballMatchEvent { action = "goal" };
            Assert.False(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementHomeTeamYellowCards()
        {
            var matchEvent = new FootballMatchEvent { team = "Home Team", card = "Yellow Card" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamYellowCards);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamYellowCards()
        {
            var matchEvent = new FootballMatchEvent { team = "Away Team", card = "Yellow Card" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamYellowCards);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementHomeTeamRedCards()
        {
            var matchEvent = new FootballMatchEvent { team = "Home Team", card = "Red Card" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamRedCards);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamRedCards()
        {
            var matchEvent = new FootballMatchEvent { team = "Away Team", card = "Red Card" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamRedCards);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementCardCounters_ForYellowCard()
        {
            var matchEvent = new FootballMatchEvent { card = "Yellow Card" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalCards);
            Assert.Equal(1, matchEvents.TotalYellowCards);
            Assert.Equal(0, matchEvents.TotalRedCards);
            Assert.Equal(1, matchEvents.TotalEvents);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementCardCounters_ForRedCard()
        {
            var matchEvent = new FootballMatchEvent { card = "Red Card" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalCards);
            Assert.Equal(0, matchEvents.TotalYellowCards);
            Assert.Equal(1, matchEvents.TotalRedCards);
            Assert.Equal(1, matchEvents.TotalEvents);
        }

        [Fact]
        public void ProcessEventCounters_ShouldNotIncrementCardCounters_ForNoCard()
        {
            var matchEvent = new FootballMatchEvent { card = "No Card" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(0, matchEvents.TotalCards);
            Assert.Equal(0, matchEvents.TotalYellowCards);
            Assert.Equal(0, matchEvents.TotalRedCards);
            Assert.Equal(1, matchEvents.TotalEvents);
        }
    }
}
