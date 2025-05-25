using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Services
{
    public class LiveMatchStatisticsServiceTests
    {
        private readonly Mock<ILogger<LiveMatchStatisticsService>> _mockLogger;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IPerformanceMonitoringService> _mockPerformanceMonitoringService;
        private readonly Mock<IEventAnalysisService> _mockEventAnalysisService;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly LiveMatchStatisticsService _service;

        public LiveMatchStatisticsServiceTests()
        {
            _mockLogger = new Mock<ILogger<LiveMatchStatisticsService>>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockPerformanceMonitoringService = new Mock<IPerformanceMonitoringService>();
            _mockEventAnalysisService = new Mock<IEventAnalysisService>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            // Setup service scope factory chain
            _mockServiceScopeFactory.Setup(f => f.CreateScope())
                .Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(s => s.ServiceProvider)
                .Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(p => p.GetRequiredService<IUnitOfWork>())
                .Returns(_mockUnitOfWork.Object);

            _service = new LiveMatchStatisticsService(
                _mockLogger.Object,
                _mockServiceScopeFactory.Object,
                _mockPerformanceMonitoringService.Object,
                _mockEventAnalysisService.Object);
        }

        [Fact]
        public async Task PreloadMatchForLiveStatistics_ValidMatchId_ReturnsMatchAndCachesIt()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Test Home", "Test Away");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);

            // Act
            var result = await _service.PreloadMatchForLiveStatistics(matchId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(match.Id, result.Id);
            
            // Verify the match is cached
            var cachedMatch = _service.GetCachedLiveMatch(matchId);
            Assert.NotNull(cachedMatch);
            Assert.Equal(match.Id, cachedMatch.Id);
            
            // Verify performance monitoring was called
            _mockPerformanceMonitoringService.Verify(p => 
                p.RecordDatabaseCall("PreloadMatch", It.IsAny<double>()), Times.Once);
        }

        [Fact]
        public async Task PreloadMatchForLiveStatistics_InvalidMatchId_ReturnsNull()
        {
            // Arrange
            var matchId = "999";
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(999))
                .ReturnsAsync((Match?)null);

            // Act
            var result = await _service.PreloadMatchForLiveStatistics(matchId);

            // Assert
            Assert.Null(result);
            
            // Verify no cache entry was created
            var cachedMatch = _service.GetCachedLiveMatch(matchId);
            Assert.Null(cachedMatch);
        }

        [Fact]
        public async Task PreloadMultipleMatchesForLiveStatistics_ValidMatchIds_ReturnsCorrectCount()
        {
            // Arrange
            var matchIds = new[] { "123", "124", "125" };
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }

            // Act
            var result = await _service.PreloadMultipleMatchesForLiveStatistics(matchIds);

            // Assert
            Assert.Equal(3, result);
            
            // Verify all matches are cached
            foreach (var matchId in matchIds)
            {
                var cachedMatch = _service.GetCachedLiveMatch(matchId);
                Assert.NotNull(cachedMatch);
            }
        }

        [Fact]
        public void GetCachedLiveMatch_ExistingMatch_ReturnsMatchAndRecordsCacheHit()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Test Home", "Test Away");
            
            // Preload the match first
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);
            
            _service.PreloadMatchForLiveStatistics(matchId).GetAwaiter().GetResult();

            // Act
            var result = _service.GetCachedLiveMatch(matchId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(match.Id, result.Id);
            
            // Verify cache hit was recorded
            _mockPerformanceMonitoringService.Verify(p => 
                p.RecordCacheHit("GetLiveMatch"), Times.Once);
        }

        [Fact]
        public void GetCachedLiveMatch_NonExistingMatch_ReturnsNullAndRecordsCacheMiss()
        {
            // Arrange
            var matchId = "999";

            // Act
            var result = _service.GetCachedLiveMatch(matchId);

            // Assert
            Assert.Null(result);
            
            // Verify cache miss was recorded
            _mockPerformanceMonitoringService.Verify(p => 
                p.RecordCacheMiss("GetLiveMatch"), Times.Once);
        }

        [Fact]
        public void GetAllLiveMatches_WithCachedMatches_ReturnsAllMatches()
        {
            // Arrange
            var matchIds = new[] { "123", "124", "125" };
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            // Preload matches
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }
            
            foreach (var matchId in matchIds)
            {
                _service.PreloadMatchForLiveStatistics(matchId).GetAwaiter().GetResult();
            }

            // Act
            var result = _service.GetAllLiveMatches();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(matchIds, matchId => Assert.True(result.ContainsKey(matchId)));
        }

        [Fact]
        public void UpdateCachedMatch_ExistingMatch_UpdatesSuccessfully()
        {
            // Arrange
            var matchId = "123";
            var originalMatch = CreateTestMatch(123, "Test Home", "Test Away");
            originalMatch.HomeTeamScore = 0;
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(originalMatch);
            
            _service.PreloadMatchForLiveStatistics(matchId).GetAwaiter().GetResult();

            // Create updated match
            var updatedMatch = CreateTestMatch(123, "Test Home", "Test Away");
            updatedMatch.HomeTeamScore = 2;

            // Act
            _service.UpdateCachedMatch(matchId, updatedMatch);

            // Assert
            var cachedMatch = _service.GetCachedLiveMatch(matchId);
            Assert.NotNull(cachedMatch);
            Assert.Equal(2, cachedMatch.HomeTeamScore);
        }

        [Fact]
        public void RemoveFromLiveCache_ExistingMatch_RemovesSuccessfully()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Test Home", "Test Away");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);
            
            _service.PreloadMatchForLiveStatistics(matchId).GetAwaiter().GetResult();
            
            // Verify it's cached
            Assert.NotNull(_service.GetCachedLiveMatch(matchId));

            // Act
            var result = _service.RemoveFromLiveCache(matchId);

            // Assert
            Assert.True(result);
            Assert.Null(_service.GetCachedLiveMatch(matchId));
        }

        [Fact]
        public void RemoveFromLiveCache_NonExistingMatch_ReturnsFalse()
        {
            // Arrange
            var matchId = "999";

            // Act
            var result = _service.RemoveFromLiveCache(matchId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCacheStatus_WithCachedMatches_ReturnsCorrectStatus()
        {
            // Arrange
            var matchIds = new[] { "123", "124" };
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }
            
            foreach (var matchId in matchIds)
            {
                _service.PreloadMatchForLiveStatistics(matchId).GetAwaiter().GetResult();
            }

            // Act
            var status = _service.GetCacheStatus();

            // Assert
            Assert.Equal(2, status.TotalCachedMatches);
            Assert.True(status.IsMemoryEfficient);
            Assert.Contains("123", status.CachedMatchIds);
            Assert.Contains("124", status.CachedMatchIds);
        }

        [Fact]
        public async Task ConcurrentAccess_MultipleThreads_HandledSafely()
        {
            // Arrange
            var matchIds = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }

            // Act - simulate concurrent preloading
            var tasks = matchIds.Select(matchId => 
                Task.Run(async () => await _service.PreloadMatchForLiveStatistics(matchId))
            ).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            var allLiveMatches = _service.GetAllLiveMatches();
            Assert.Equal(10, allLiveMatches.Count);
            
            // Verify concurrent access to cached data
            var concurrentTasks = matchIds.Select(matchId =>
                Task.Run(() => _service.GetCachedLiveMatch(matchId))
            ).ToArray();

            var results = await Task.WhenAll(concurrentTasks);
            Assert.All(results, result => Assert.NotNull(result));
        }

        private static Match CreateTestMatch(int id, string homeTeam, string awayTeam)
        {
            return new Match
            {
                Id = id,
                HomeTeam = new Team { Id = id * 10, Name = homeTeam },
                AwayTeam = new Team { Id = id * 10 + 1, Name = awayTeam },
                HomeTeamScore = 0,
                AwayTeamScore = 0,
                MatchStatus = "Live",
                IsLive = true,
                HomeTeamPossession = 50,
                AwayTeamPossession = 50,
                ScheduledDateTimeUtc = DateTime.UtcNow
            };
        }
    }
}
