using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class PassEventProcessorTests
    {
        private readonly PassEventProcessor _processor = new();
        private Match _match;

        public PassEventProcessorTests()
        {
            _match = new Match(1, "test-user")
            {
                HomeTeamId = 1,
                AwayTeamId = 2,
                HomeTeamInMatchName = "Team A",
                AwayTeamInMatchName = "Team B",
                HomeTeamPasses = 0,
                AwayTeamPasses = 0,
                HomeTeamPassesCompleted = 0,
                AwayTeamPassesCompleted = 0,
                HomeTeamCorners = 0,
                AwayTeamCorners = 0,
                HomeTeamOffsides = 0,
                AwayTeamOffsides = 0,
                HomeTeamRecoveries = 0,
                AwayTeamRecoveries = 0,
                HomeTeamGoalKicks = 0,
                AwayTeamGoalKicks = 0,
            };
        }

        [Theory]
        [InlineData("pass", true)]
        [InlineData("shot", false)]
        public void CanProcess_ShouldReturnCorrectValue(string action, bool expected)
        {
            var matchEvent = new FootballMatchEvent { action = action };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_HomeTeamPass_ShouldIncrementHomeTeamPasses()
        {
            var matchEvent = new FootballMatchEvent { action = "pass", team = "Team A" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamPasses);
        }

        [Fact]
        public void ProcessMatchEvent_AwayTeamPass_ShouldIncrementAwayTeamPasses()
        {
            var matchEvent = new FootballMatchEvent { action = "pass", team = "Team B" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamPasses);
        }

        [Fact]
        public void ProcessMatchEvent_HomeTeamCompletedPass_ShouldIncrementCompletedPasses()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "pass",
                team = "Team A",
                outcome = "Complete",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamPassesCompleted);
        }

        [Fact]
        public void ProcessMatchEvent_HomeTeamCorner_ShouldIncrementCorners()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "pass",
                team = "Team A",
                type = "Corner",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamCorners);
        }

        [Fact]
        public void ProcessMatchEvent_AwayTeamPassOffside_ShouldIncrementOffsides()
        {
            var matchEvent = new FootballMatchEvent
            {
                action = "pass",
                team = "Team B",
                outcome = "Pass Offside",
            };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.AwayTeamOffsides);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalPasses()
        {
            var matchEvent = new FootballMatchEvent { action = "pass" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalPasses);
        }
    }
}
