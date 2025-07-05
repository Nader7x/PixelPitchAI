using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Services;
using Infrastructure.Services.EventProcessors;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services
{
    public class EventAnalysisServiceTests
    {
        private readonly Mock<IEventProcessor> _eventProcessorMock;
        private readonly EventAnalysisService _eventAnalysisService;

        public EventAnalysisServiceTests()
        {
            _eventProcessorMock = new Mock<IEventProcessor>();
            var eventProcessors = new List<IEventProcessor> { _eventProcessorMock.Object };
            _eventAnalysisService = new EventAnalysisService(eventProcessors);
        }

        [Fact]
        public async Task UpdateMatchStatistics_WithCounters_ShouldProcessEventAndCounters()
        {
            // Arrange
            var matchEvent = new FootballMatchEvent { action = "pass" };
            var matchEventsEntity = new MatchEvents();
            var match = new Match { CreatorId = "0" }; // Provide a value for the required member

            _eventProcessorMock.Setup(p => p.CanProcess(matchEvent)).Returns(true);

            // Act
            var result = await _eventAnalysisService.UpdateMatchStatistics(
                matchEvent,
                matchEventsEntity,
                match
            );

            // Assert
            _eventProcessorMock.Verify(p => p.ProcessMatchEvent(matchEvent, match), Times.Once);
            _eventProcessorMock.Verify(
                p => p.ProcessEventCounters(matchEvent, matchEventsEntity, match),
                Times.Once
            );
            Assert.Equal(1, result.TotalEvents);
        }

        [Fact]
        public async Task UpdateMatchStatistics_WithoutCounters_ShouldOnlyProcessEvent()
        {
            // Arrange
            var matchEvent = new FootballMatchEvent { action = "pass" };
            var matchEventsEntity = new MatchEvents();
            var match = new Match { CreatorId = "0" }; // Provide a value for the required member

            _eventProcessorMock.Setup(p => p.CanProcess(matchEvent)).Returns(true);

            // Act
            var result = await _eventAnalysisService.UpdateMatchStatistics(
                matchEvent,
                matchEventsEntity,
                match,
                withCounters: false
            );

            // Assert
            _eventProcessorMock.Verify(p => p.ProcessMatchEvent(matchEvent, match), Times.Once);
            _eventProcessorMock.Verify(
                p => p.ProcessEventCounters(matchEvent, matchEventsEntity, match),
                Times.Never
            );
            Assert.Equal(0, result.TotalEvents);
        }

        [Fact]
        public async Task UpdateMatchStatistics_WithNullMatch_ShouldThrowArgumentNullException()
        {
            // Arrange
            var matchEvent = new FootballMatchEvent();
            var matchEventsEntity = new MatchEvents();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _eventAnalysisService.UpdateMatchStatistics(
                    matchEvent,
                    matchEventsEntity,
                    (Match?)null
                )
            );
        }

        [Fact]
        public async Task UpdateMatchStatistics_WithNullMatchEventsEntity_ShouldThrowArgumentNullException()
        {
            // Arrange
            var matchEvent = new FootballMatchEvent();
            var match = new Match { CreatorId = "0" }; // Provide a value for the required member

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _eventAnalysisService.UpdateMatchStatistics(matchEvent, (MatchEvents?)null, match)
            );
        }
    }
}
