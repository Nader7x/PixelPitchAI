using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class DuelEventProcessorTests
    {
        private readonly DuelEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamInMatchName = "Home Team",
            AwayTeamInMatchName = "Away Team",
        };

        [Theory]
        [InlineData("duel", true)]
        [InlineData("50/50", true)]
        [InlineData("shield", true)]
        [InlineData("pass", false)]
        public void CanProcess_ShouldReturnCorrectValue_ForAction(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementHomeTeamDuels()
        {
            var matchEvent = new FootballMatchEvent { team = "Home Team", action = "duel" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamDuels);
            Assert.Null(_match.HomeTeamDuelsWon);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementHomeTeamDuelsWon_ForWonOutcome()
        {
            var matchEvent = new FootballMatchEvent
            {
                team = "Home Team",
                action = "duel",
                outcome = "won",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamDuels);
            Assert.Equal(1, _match.HomeTeamDuelsWon);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementHomeTeamDuelsWon_ForSuccessOutcome()
        {
            var matchEvent = new FootballMatchEvent
            {
                team = "Home Team",
                action = "duel",
                outcome = "Success",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamDuels);
            Assert.Equal(1, _match.HomeTeamDuelsWon);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamDuels()
        {
            var matchEvent = new FootballMatchEvent { team = "Away Team", action = "duel" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamDuels);
            Assert.Null(_match.AwayTeamDuelsWon);
        }

        [Fact]
        public void ProcessMatchEvent_ShouldIncrementAwayTeamDuelsWon()
        {
            var matchEvent = new FootballMatchEvent
            {
                team = "Away Team",
                action = "duel",
                outcome = "won",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamDuels);
            Assert.Equal(1, _match.AwayTeamDuelsWon);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalDuels()
        {
            var matchEvent = new FootballMatchEvent { action = "duel" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalDuels);
        }
    }
}
