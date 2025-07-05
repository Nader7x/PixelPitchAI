using Domain.Models;
using Infrastructure.Services.EventProcessors;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services.EventProcessors
{
    public class BallReceiptEventProcessorTests
    {
        private readonly BallReceiptEventProcessor _processor = new();
        private readonly Match _match = new(1, "test-user");

        [Fact]
        public void CanProcess_ShouldReturnTrue_ForBallReceiptAction()
        {
            var matchEvent = new FootballMatchEvent { action = "ball receipt*" };
            Assert.True(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void CanProcess_ShouldReturnFalse_ForOtherAction()
        {
            var matchEvent = new FootballMatchEvent { action = "goal" };
            Assert.False(_processor.CanProcess(matchEvent));
        }

        [Fact]
        public void ProcessEventCounters_ShouldIncrementTotalEvents()
        {
            var matchEvent = new FootballMatchEvent { action = "ball receipt*" };
            var matchEvents = new MatchEvents();
            _processor.ProcessEventCounters(matchEvent, matchEvents, _match);
            Assert.Equal(1, matchEvents.TotalEvents);
        }
    }
}
