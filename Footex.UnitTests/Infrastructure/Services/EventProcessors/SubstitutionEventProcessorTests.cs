using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class SubstitutionEventProcessorTests
    {
        private readonly SubstitutionEventProcessor _processor = new();
        private readonly Match _match;
        private readonly MatchEvents _matchEvents;

        public SubstitutionEventProcessorTests()
        {
            _match = new Match(1, "test-user");
            _matchEvents = new MatchEvents();
        }

        [Theory]
        [InlineData("substitution", true)]
        [InlineData("player on", true)]
        [InlineData("player off", true)]
        [InlineData("pass", false)]
        public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_DoesNothing()
        {
            var matchEvent = new FootballMatchEvent { action = "substitution" };
            var initialMatch = new Match(1, "test-user");
            _processor.ProcessMatchEvent(matchEvent, _match);
            // Assert that the match object is unchanged
            Assert.Equal(initialMatch.HomeTeamScore, _match.HomeTeamScore);
            Assert.Equal(initialMatch.AwayTeamScore, _match.AwayTeamScore);
        }

        [Fact]
        public void ProcessEventCounters_SubstitutionAction_ShouldIncrementTotalSubstitutions()
        {
            var matchEvent = new FootballMatchEvent { action = "substitution" };
            _processor.ProcessEventCounters(matchEvent, _matchEvents, _match);
            Assert.Equal(1, _matchEvents.TotalSubstitutions);
        }

        [Fact]
        public void ProcessEventCounters_PlayerOnAction_ShouldNotIncrementTotalSubstitutions()
        {
            var matchEvent = new FootballMatchEvent { action = "player on" };
            _processor.ProcessEventCounters(matchEvent, _matchEvents, _match);
            Assert.Equal(0, _matchEvents.TotalSubstitutions);
        }

        [Fact]
        public void ProcessEventCounters_PlayerOffAction_ShouldNotIncrementTotalSubstitutions()
        {
            var matchEvent = new FootballMatchEvent { action = "player off" };
            _processor.ProcessEventCounters(matchEvent, _matchEvents, _match);
            Assert.Equal(0, _matchEvents.TotalSubstitutions);
        }
    }
}
