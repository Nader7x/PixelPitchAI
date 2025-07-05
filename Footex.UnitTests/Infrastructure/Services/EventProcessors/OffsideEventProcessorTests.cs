using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class OffsideEventProcessorTests
    {
        private readonly OffsideEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user")
        {
            HomeTeamInMatchName = "Team A",
            AwayTeamInMatchName = "Team B",
        };

        [Theory]
        [InlineData("offside", null, true)]
        [InlineData("pass", "Pass Offside", true)]
        [InlineData("pass", "Complete", false)]
        [InlineData("goal", null, false)]
        public void CanProcess_ShouldReturnCorrectValue(
            string action,
            string? outcome,
            bool expected
        )
        {
            var matchEvent = new FootballMatchEvent { action = action, outcome = outcome };
            var result = _processor.CanProcess(matchEvent);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ProcessMatchEvent_HomeTeamOffside_ShouldIncrementHomeTeamOffsides()
        {
            _match.ResetStatistics(); // Initialize statistics
            var matchEvent = new FootballMatchEvent { action = "offside", team = "Team A" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(1, _match.HomeTeamOffsides);
            Assert.Equal(0, _match.AwayTeamOffsides);
        }

        [Fact]
        public void ProcessMatchEvent_AwayTeamOffside_ShouldIncrementAwayTeamOffsides()
        {
            _match.ResetStatistics(); // Initialize statistics
            var matchEvent = new FootballMatchEvent { action = "offside", team = "Team B" };
            _processor.ProcessMatchEvent(matchEvent, _match);
            Assert.Equal(0, _match.HomeTeamOffsides);
            Assert.Equal(1, _match.AwayTeamOffsides);
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalOffsides()
        {
            var matchEvent = new FootballMatchEvent { action = "offside" };
            var matchEvents = new MatchEvents();
            var initialOffsides = matchEvents.TotalOffsides;

            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);

            Assert.Equal(initialOffsides + 1, matchEvents.TotalOffsides);
        }
    }
}
