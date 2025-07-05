using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class InjuryEventProcessorTests
    {
        private readonly InjuryEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user");

        [Fact]
        public void CanProcess_ShouldReturnTrue_ForInjuryStoppageAction()
        {
            var matchEvent = new FootballMatchEvent { action = "injury stoppage" };
            Assert.True(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void CanProcess_ShouldReturnFalse_ForOtherAction()
        {
            var matchEvent = new FootballMatchEvent { action = "goal" };
            Assert.False(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalInjuries()
        {
            var matchEvent = new FootballMatchEvent { action = "injury stoppage" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalInjuries);
        }
    }
}
