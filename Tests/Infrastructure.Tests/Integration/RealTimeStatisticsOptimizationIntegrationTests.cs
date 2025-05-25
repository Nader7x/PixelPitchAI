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

namespace Infrastructure.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete real-time statistics optimization system.
    /// Tests the interaction between LiveMatchStatisticsService, PerformanceMonitoringService,
    /// and MatchEventRabbitMqClient for end-to-end optimization validation.
    /// </summary>
    public class RealTimeStatisticsOptimizationIntegrationTests
    {
        private readonly Mock<ILogger<LiveMatchStatisticsService>> _mockLiveMatchLogger;
        private readonly Mock<ILogger<PerformanceMonitoringService>> _mockPerfLogger;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IEventAnalysisService> _mockEventAnalysisService;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        
        private readonly PerformanceMonitoringService _performanceService;
        private readonly LiveMatchStatisticsService _liveMatchService;

        public RealTimeStatisticsOptimizationIntegrationTests()
        {
            _mockLiveMatchLogger = new Mock<ILogger<LiveMatchStatisticsService>>();
            _mockPerfLogger = new Mock<ILogger<PerformanceMonitoringService>>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
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

            _performanceService = new PerformanceMonitoringService(_mockPerfLogger.Object);
            _liveMatchService = new LiveMatchStatisticsService(
                _mockLiveMatchLogger.Object,
                _mockServiceScopeFactory.Object,
                _performanceService,
                _mockEventAnalysisService.Object);
        }

        [Fact]
        public async Task EndToEndOptimization_PreloadAndAccess_MeetsPerformanceRequirements()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Barcelona", "Real Madrid");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);

            // Act 1: Preload match (simulates first event received)
            var preloadStart = DateTime.UtcNow;
            var preloadedMatch = await _liveMatchService.PreloadMatchForLiveStatistics(matchId);
            var preloadEnd = DateTime.UtcNow;

            // Act 2: Access cached match multiple times (simulates real-time queries)
            var accessTimes = new List<double>();
            for (int i = 0; i < 100; i++)
            {
                var accessStart = DateTime.UtcNow;
                var cachedMatch = _liveMatchService.GetCachedLiveMatch(matchId);
                var accessEnd = DateTime.UtcNow;
                
                accessTimes.Add((accessEnd - accessStart).TotalMilliseconds);
                Assert.NotNull(cachedMatch);
            }

            // Assert Performance Requirements
            var avgAccessTime = accessTimes.Average();
            var maxAccessTime = accessTimes.Max();
            var preloadTime = (preloadEnd - preloadStart).TotalMilliseconds;

            // Performance assertions based on documented requirements
            Assert.True(avgAccessTime < 5.0, $"Average access time {avgAccessTime}ms exceeds 5ms requirement");
            Assert.True(maxAccessTime < 10.0, $"Max access time {maxAccessTime}ms exceeds 10ms threshold");
            Assert.True(preloadTime < 100.0, $"Preload time {preloadTime}ms exceeds 100ms threshold");

            // Verify database calls were minimized
            var dbCalls = _performanceService.GetPerformanceMetrics().DatabaseCalls;
            Assert.Equal(1, dbCalls.Count); // Only one DB call for preload
            Assert.Equal("PreloadMatch", dbCalls.First().OperationType);

            // Verify cache efficiency
            var cacheMetrics = _performanceService.GetPerformanceMetrics().CacheMetrics;
            Assert.True(cacheMetrics.HitRatio > 0.99, $"Cache hit ratio {cacheMetrics.HitRatio} is below 99%");
        }

        [Fact]
        public async Task HighConcurrency_MultipleMatchesSimultaneous_MaintainsPerformance()
        {
            // Arrange - 20 concurrent matches (stress test)
            var matchCount = 20;
            var matchIds = Enumerable.Range(1, matchCount).Select(i => i.ToString()).ToArray();
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }

            // Act 1: Concurrent preloading
            var preloadStart = DateTime.UtcNow;
            var preloadTasks = matchIds.Select(matchId => 
                _liveMatchService.PreloadMatchForLiveStatistics(matchId)
            ).ToArray();
            
            await Task.WhenAll(preloadTasks);
            var preloadEnd = DateTime.UtcNow;

            // Act 2: Concurrent access simulation (100 requests per match)
            var accessStart = DateTime.UtcNow;
            var accessTasks = new List<Task>();
            
            foreach (var matchId in matchIds)
            {
                for (int i = 0; i < 100; i++)
                {
                    accessTasks.Add(Task.Run(() => _liveMatchService.GetCachedLiveMatch(matchId)));
                }
            }
            
            var accessResults = await Task.WhenAll(accessTasks);
            var accessEnd = DateTime.UtcNow;

            // Assert
            var totalPreloadTime = (preloadEnd - preloadStart).TotalMilliseconds;
            var totalAccessTime = (accessEnd - accessStart).TotalMilliseconds;
            var avgAccessTimePerRequest = totalAccessTime / (matchCount * 100);

            // Performance requirements for high concurrency
            Assert.True(avgAccessTimePerRequest < 1.0, 
                $"Average access time per request {avgAccessTimePerRequest}ms exceeds 1ms under load");
            Assert.True(totalPreloadTime < 5000, 
                $"Total preload time {totalPreloadTime}ms exceeds 5s for {matchCount} matches");
            
            // Verify all requests succeeded
            Assert.All(accessResults, result => Assert.NotNull(result));
            
            // Verify database efficiency (should only have matchCount DB calls)
            var dbCallCount = _performanceService.GetPerformanceMetrics().DatabaseCalls.Count;
            Assert.Equal(matchCount, dbCallCount);
        }

        [Fact]
        public async Task CacheEviction_MatchEnded_FreesMemoryEfficiently()
        {
            // Arrange
            var activeMatchIds = new[] { "123", "124", "125" };
            var endedMatchIds = new[] { "126", "127" };
            var allMatchIds = activeMatchIds.Concat(endedMatchIds).ToArray();
            
            var matches = allMatchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < allMatchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(allMatchIds[i])))
                    .ReturnsAsync(matches[i]);
            }

            // Act 1: Preload all matches
            foreach (var matchId in allMatchIds)
            {
                await _liveMatchService.PreloadMatchForLiveStatistics(matchId);
            }

            var initialCacheStatus = _liveMatchService.GetCacheStatus();
            Assert.Equal(5, initialCacheStatus.TotalCachedMatches);

            // Act 2: Simulate match endings (remove from cache)
            foreach (var endedMatchId in endedMatchIds)
            {
                var removed = _liveMatchService.RemoveFromLiveCache(endedMatchId);
                Assert.True(removed);
            }

            // Assert
            var finalCacheStatus = _liveMatchService.GetCacheStatus();
            Assert.Equal(3, finalCacheStatus.TotalCachedMatches);
            
            // Verify only active matches remain
            foreach (var activeMatchId in activeMatchIds)
            {
                Assert.Contains(activeMatchId, finalCacheStatus.CachedMatchIds);
            }
            
            foreach (var endedMatchId in endedMatchIds)
            {
                Assert.DoesNotContain(endedMatchId, finalCacheStatus.CachedMatchIds);
                Assert.Null(_liveMatchService.GetCachedLiveMatch(endedMatchId));
            }
        }

        [Fact]
        public async Task DatabaseCallReduction_CompareOptimizedVsNonOptimized_ShowsSignificantImprovement()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Liverpool", "Manchester City");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);

            var requestCount = 100;

            // Act 1: Optimized approach (with caching)
            await _liveMatchService.PreloadMatchForLiveStatistics(matchId);
            
            var optimizedDbCallsBefore = _performanceService.GetPerformanceMetrics().DatabaseCalls.Count;
            
            for (int i = 0; i < requestCount; i++)
            {
                _liveMatchService.GetCachedLiveMatch(matchId);
            }
            
            var optimizedDbCallsAfter = _performanceService.GetPerformanceMetrics().DatabaseCalls.Count;
            var optimizedDbCalls = optimizedDbCallsAfter - optimizedDbCallsBefore;

            // Act 2: Simulate non-optimized approach (direct DB calls)
            var nonOptimizedDbCalls = requestCount; // Each request would require a DB call

            // Assert Performance Improvement
            var reductionPercentage = ((double)(nonOptimizedDbCalls - optimizedDbCalls) / nonOptimizedDbCalls) * 100;
            
            Assert.Equal(0, optimizedDbCalls); // No additional DB calls after preload
            Assert.True(reductionPercentage >= 90, 
                $"Database call reduction {reductionPercentage:F1}% is below 90% target");
            
            // Verify cache metrics
            var cacheMetrics = _performanceService.GetPerformanceMetrics().CacheMetrics;
            Assert.Equal(requestCount, cacheMetrics.TotalHits);
            Assert.Equal(0, cacheMetrics.TotalMisses);
            Assert.Equal(1.0, cacheMetrics.HitRatio);
        }

        [Fact]
        public async Task RealTimeUpdates_CacheConsistency_MaintainsDataIntegrity()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Arsenal", "Chelsea");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);

            // Act 1: Initial preload
            await _liveMatchService.PreloadMatchForLiveStatistics(matchId);
            var initialMatch = _liveMatchService.GetCachedLiveMatch(matchId);
            
            Assert.NotNull(initialMatch);
            Assert.Equal(0, initialMatch.HomeTeamScore);
            Assert.Equal(0, initialMatch.AwayTeamScore);

            // Act 2: Simulate real-time score updates
            var updates = new[]
            {
                new { HomeScore = 1, AwayScore = 0, Minute = 15 },
                new { HomeScore = 1, AwayScore = 1, Minute = 32 },
                new { HomeScore = 2, AwayScore = 1, Minute = 67 },
                new { HomeScore = 2, AwayScore = 2, Minute = 89 }
            };

            foreach (var update in updates)
            {
                var updatedMatch = CreateTestMatch(123, "Arsenal", "Chelsea");
                updatedMatch.HomeTeamScore = update.HomeScore;
                updatedMatch.AwayTeamScore = update.AwayScore;
                
                _liveMatchService.UpdateCachedMatch(matchId, updatedMatch);
                
                // Verify immediate consistency
                var currentMatch = _liveMatchService.GetCachedLiveMatch(matchId);
                Assert.NotNull(currentMatch);
                Assert.Equal(update.HomeScore, currentMatch.HomeTeamScore);
                Assert.Equal(update.AwayScore, currentMatch.AwayTeamScore);
            }

            // Assert final state
            var finalMatch = _liveMatchService.GetCachedLiveMatch(matchId);
            Assert.Equal(2, finalMatch?.HomeTeamScore);
            Assert.Equal(2, finalMatch?.AwayTeamScore);
        }

        [Fact]
        public void MemoryUsage_LargeCacheSize_RemainsEfficient()
        {
            // Arrange - Simulate large number of concurrent matches
            var largeMatchCount = 1000;
            var matchIds = Enumerable.Range(1, largeMatchCount).Select(i => i.ToString()).ToArray();
            var matches = matchIds.Select(id => CreateTestMatch(int.Parse(id), $"Home {id}", $"Away {id}")).ToArray();
            
            for (int i = 0; i < matchIds.Length; i++)
            {
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchIds[i])))
                    .ReturnsAsync(matches[i]);
            }

            // Act - Preload all matches
            var preloadTasks = matchIds.Select(matchId => 
                _liveMatchService.PreloadMatchForLiveStatistics(matchId)
            ).ToArray();
            
            Task.WhenAll(preloadTasks).GetAwaiter().GetResult();

            // Assert
            var cacheStatus = _liveMatchService.GetCacheStatus();
            Assert.Equal(largeMatchCount, cacheStatus.TotalCachedMatches);
            Assert.True(cacheStatus.IsMemoryEfficient);
            
            // Verify O(1) access time doesn't degrade with cache size
            var accessTimes = new List<double>();
            var random = new Random();
            
            for (int i = 0; i < 100; i++)
            {
                var randomMatchId = matchIds[random.Next(0, largeMatchCount)];
                var start = DateTime.UtcNow;
                var match = _liveMatchService.GetCachedLiveMatch(randomMatchId);
                var end = DateTime.UtcNow;
                
                Assert.NotNull(match);
                accessTimes.Add((end - start).TotalMilliseconds);
            }
            
            var avgAccessTime = accessTimes.Average();
            Assert.True(avgAccessTime < 5.0, 
                $"Average access time {avgAccessTime}ms with {largeMatchCount} cached matches exceeds 5ms");
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
                HomeTeamShots = 0,
                AwayTeamShots = 0,
                HomeTeamShotsOnTarget = 0,
                AwayTeamShotsOnTarget = 0,
                HomeTeamCorners = 0,
                AwayTeamCorners = 0,
                HomeTeamFouls = 0,
                AwayTeamFouls = 0,
                ScheduledDateTimeUtc = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
