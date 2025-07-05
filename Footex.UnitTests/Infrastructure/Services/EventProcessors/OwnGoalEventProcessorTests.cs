using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class OwnGoalEventProcessorTests
    {
        private readonly OwnGoalEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamInMatchName = "Team A",
            AwayTeamInMatchName = "Team B",
            HomeTeamScore = 0,
            AwayTeamScore = 0,
        };

        [Theory]
        [InlineData("own goal against", true)]
        [InlineData("goal", false)]
        [InlineData("shot", false)]
        public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_HomeTeamOwnGoal_ShouldIncrementAwayTeamScore()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "own goal against",
                team = "Team A",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(0, _match.HomeTeamScore);
            Assert.Equal(1, _match.AwayTeamScore);
            Assert.Equal(_match.AwayTeamId, _match.WinningTeamId);
            Assert.False(_match.IsDraw);
        }

        [Fact]
        public void ProcessMatchEvent_AwayTeamOwnGoal_ShouldIncrementHomeTeamScore()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "own goal against",
                team = "Team B",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamScore);
            Assert.Equal(0, _match.AwayTeamScore);
            Assert.Equal(_match.HomeTeamId, _match.WinningTeamId);
            Assert.False(_match.IsDraw);
        }

        [Fact]
        public void ProcessEventCounters_HomeTeamOwnGoal_ShouldIncrementCountersCorrectly()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "own goal against",
                team = "Team A",
            };
            var matchEvents = new MatchEvents();

            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);

            Assert.Equal(1, matchEvents.TotalGoals);
            Assert.Equal(1, matchEvents.GoalsAwayTeam);
            Assert.Equal(0, matchEvents.GoalsHomeTeam);
        }

        [Fact]
        public void ProcessEventCounters_AwayTeamOwnGoal_ShouldIncrementCountersCorrectly()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "own goal against",
                team = "Team B",
            };
            var matchEvents = new MatchEvents();

            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);

            Assert.Equal(1, matchEvents.TotalGoals);
            Assert.Equal(1, matchEvents.GoalsHomeTeam);
            Assert.Equal(0, matchEvents.GoalsAwayTeam);
        }
    }
}
