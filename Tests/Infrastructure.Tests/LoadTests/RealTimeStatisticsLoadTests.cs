using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Infrastructure.Tests.LoadTests
{
    /// <summary>
    /// Load tests for validating performance under high concurrent load scenarios.
    /// These tests verify that the optimization meets performance requirements
    /// when handling hundreds of concurrent matches and thousands of requests.
    /// </summary>
    public class RealTimeStatisticsLoadTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ILogger<LiveMatchStatisticsService>> _mockLiveMatchLogger;
        private readonly Mock<ILogger<PerformanceMonitoringService>> _mockPerfLogger;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IEventAnalysisService> _mockEventAnalysisService;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        
        private readonly PerformanceMonitoringService _performanceService;
        private readonly LiveMatchStatisticsService _liveMatchService;

        public RealTimeStatisticsLoadTests(ITestOutputHelper output)
        {
            _output = output;
            _mockLiveMatchLogger = new Mock<ILogger<LiveMatchStatisticsService>>();
            _mockPerfLogger = new Mock<ILogger<PerformanceMonitoringService>>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockEventAnalysisService = new Mock<IEventAnalysisService>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            SetupMocks();

            _performanceService = new PerformanceMonitoringService(_mockPerfLogger.Object);
            _liveMatchService = new LiveMatchStatisticsService(
                _mockLiveMatchLogger.Object,
                _mockServiceScopeFactory.Object,
                _performanceService,
                _mockEventAnalysisService.Object);
        }

        private void SetupMocks()
        {
            _mockServiceScopeFactory.Setup(f => f.CreateScope())
                .Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(s => s.ServiceProvider)
                .Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(p => p.GetRequiredService<IUnitOfWork>())
                .Returns(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task HighLoad_1000ConcurrentMatches_MaintainsPerformance()
        {
            // Arrange
            var matchCount = 1000;
            var requestsPerMatch = 100;
            var totalRequests = matchCount * requestsPerMatch;
            
            _output.WriteLine($"Load Test: {matchCount} matches, {requestsPerMatch} requests each = {totalRequests} total requests");

            var matchIds = Enumerable.Range(1, matchCount).Select(i => i.ToString()).ToArray();
            SetupMatchData(matchIds);

            // Act - Phase 1: Preload all matches
            var preloadStopwatch = Stopwatch.StartNew();
            var preloadTasks = matchIds.Select(matchId => 
                _liveMatchService.PreloadMatchForLiveStatistics(matchId)
            ).ToArray();
            
            await Task.WhenAll(preloadTasks);
            preloadStopwatch.Stop();

            _output.WriteLine($"Preload completed: {preloadStopwatch.ElapsedMilliseconds}ms for {matchCount} matches");

            // Act - Phase 2: Concurrent access load test
            var accessStopwatch = Stopwatch.StartNew();
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 4); // Limit concurrency
            var accessTasks = new List<Task<double>>();
            var successCount = 0;
            var errorCount = 0;

            foreach (var matchId in matchIds)
            {
                for (int i = 0; i < requestsPerMatch; i++)
                {
                    accessTasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var requestStart = Stopwatch.GetTimestamp();
                            var match = _liveMatchService.GetCachedLiveMatch(matchId);
                            var requestEnd = Stopwatch.GetTimestamp();
                            
                            if (match != null)
                            {
                                Interlocked.Increment(ref successCount);
                                return (double)(requestEnd - requestStart) / Stopwatch.Frequency * 1000; // Convert to ms
                            }
                            else
                            {
                                Interlocked.Increment(ref errorCount);
                                return -1;
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
            }

            var accessTimes = await Task.WhenAll(accessTasks);
            accessStopwatch.Stop();

            // Assert Performance Requirements
            var validAccessTimes = accessTimes.Where(t => t >= 0).ToArray();
            var avgAccessTime = validAccessTimes.Average();
            var p95AccessTime = validAccessTimes.OrderBy(t => t).Skip((int)(validAccessTimes.Length * 0.95)).First();
            var maxAccessTime = validAccessTimes.Max();
            var throughput = (double)totalRequests / accessStopwatch.Elapsed.TotalSeconds;

            _output.WriteLine($"Access completed: {accessStopwatch.ElapsedMilliseconds}ms total");
            _output.WriteLine($"Success rate: {successCount}/{totalRequests} ({(double)successCount/totalRequests*100:F2}%)");
            _output.WriteLine($"Average access time: {avgAccessTime:F3}ms");
            _output.WriteLine($"95th percentile: {p95AccessTime:F3}ms");
            _output.WriteLine($"Max access time: {maxAccessTime:F3}ms");
            _output.WriteLine($"Throughput: {throughput:F0} requests/second");

            // Performance Assertions
            Assert.True(successCount == totalRequests, $"Not all requests succeeded: {successCount}/{totalRequests}");
            Assert.True(avgAccessTime < 5.0, $"Average access time {avgAccessTime:F3}ms exceeds 5ms requirement");
            Assert.True(p95AccessTime < 10.0, $"95th percentile {p95AccessTime:F3}ms exceeds 10ms requirement");
            Assert.True(throughput > 10000, $"Throughput {throughput:F0} req/s is below 10,000 req/s target");
            
            // Memory efficiency check
            var cacheStatus = _liveMatchService.GetCacheStatus();
            Assert.Equal(matchCount, cacheStatus.TotalCachedMatches);
            Assert.True(cacheStatus.IsMemoryEfficient);
        }

        [Fact]
        public async Task StressTest_ContinuousLoad_30Seconds()
        {
            // Arrange
            var testDuration = TimeSpan.FromSeconds(30);
            var concurrentMatches = 100;
            var matchIds = Enumerable.Range(1, concurrentMatches).Select(i => i.ToString()).ToArray();
            
            SetupMatchData(matchIds);
            
            // Preload matches
            await Task.WhenAll(matchIds.Select(id => _liveMatchService.PreloadMatchForLiveStatistics(id)));
            
            _output.WriteLine($"Stress Test: {testDuration.TotalSeconds}s duration, {concurrentMatches} matches");

            // Act - Continuous load for specified duration
            var cancellationToken = new CancellationTokenSource(testDuration);
            var requestCount = 0;
            var errorCount = 0;
            var accessTimes = new ConcurrentBag<double>();
            var random = new Random();

            var loadTasks = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(_ =>
                Task.Run(async () =>
                {
                    while (!cancellationToken.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var matchId = matchIds[random.Next(0, matchIds.Length)];
                            var start = Stopwatch.GetTimestamp();
                            
                            var match = _liveMatchService.GetCachedLiveMatch(matchId);
                            
                            var end = Stopwatch.GetTimestamp();
                            var timeMs = (double)(end - start) / Stopwatch.Frequency * 1000;
                            
                            if (match != null)
                            {
                                accessTimes.Add(timeMs);
                                Interlocked.Increment(ref requestCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref errorCount);
                            }
                            
                            // Small delay to prevent CPU spinning
                            await Task.Delay(1, cancellationToken.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                }, cancellationToken.Token)
            ).ToArray();

            await Task.WhenAll(loadTasks);

            // Assert Results
            var totalRequests = requestCount + errorCount;
            var successRate = (double)requestCount / totalRequests * 100;
            var avgAccessTime = accessTimes.Average();
            var throughput = (double)requestCount / testDuration.TotalSeconds;
            
            _output.WriteLine($"Stress test completed:");
            _output.WriteLine($"Total requests: {totalRequests:N0}");
            _output.WriteLine($"Successful requests: {requestCount:N0}");
            _output.WriteLine($"Success rate: {successRate:F2}%");
            _output.WriteLine($"Average access time: {avgAccessTime:F3}ms");
            _output.WriteLine($"Sustained throughput: {throughput:F0} req/s");

            Assert.True(successRate >= 99.5, $"Success rate {successRate:F2}% is below 99.5%");
            Assert.True(avgAccessTime < 5.0, $"Average access time {avgAccessTime:F3}ms exceeds 5ms");
            Assert.True(throughput > 1000, $"Sustained throughput {throughput:F0} req/s is below 1000 req/s");
        }

        [Fact]
        public async Task MemoryPressure_GrowingCacheSize_HandlesGracefully()
        {
            // Arrange - Simulate growing number of matches throughout the day
            var phases = new[]
            {
                new { MatchCount = 50, PhaseName = "Morning" },
                new { MatchCount = 200, PhaseName = "Afternoon" },
                new { MatchCount = 500, PhaseName = "Peak Evening" },
                new { MatchCount = 800, PhaseName = "Late Night" }
            };

            var allMatchIds = new List<string>();
            var phaseResults = new List<object>();

            foreach (var phase in phases)
            {
                _output.WriteLine($"Testing {phase.PhaseName} phase: {phase.MatchCount} matches");

                // Add new matches for this phase
                var newMatchIds = Enumerable.Range(allMatchIds.Count + 1, 
                    phase.MatchCount - allMatchIds.Count).Select(i => i.ToString()).ToArray();
                allMatchIds.AddRange(newMatchIds);

                SetupMatchData(newMatchIds);

                // Act - Preload new matches
                var preloadStart = Stopwatch.StartNew();
                await Task.WhenAll(newMatchIds.Select(id => _liveMatchService.PreloadMatchForLiveStatistics(id)));
                preloadStart.Stop();

                // Test access performance
                var accessTimes = new List<double>();
                var random = new Random();
                
                for (int i = 0; i < 1000; i++)
                {
                    var randomMatchId = allMatchIds[random.Next(0, allMatchIds.Count)];
                    var start = Stopwatch.GetTimestamp();
                    var match = _liveMatchService.GetCachedLiveMatch(randomMatchId);
                    var end = Stopwatch.GetTimestamp();
                    
                    Assert.NotNull(match);
                    accessTimes.Add((double)(end - start) / Stopwatch.Frequency * 1000);
                }

                var avgAccessTime = accessTimes.Average();
                var cacheStatus = _liveMatchService.GetCacheStatus();

                var phaseResult = new
                {
                    Phase = phase.PhaseName,
                    MatchCount = phase.MatchCount,
                    PreloadTimeMs = preloadStart.ElapsedMilliseconds,
                    AvgAccessTimeMs = avgAccessTime,
                    CachedMatches = cacheStatus.TotalCachedMatches,
                    IsMemoryEfficient = cacheStatus.IsMemoryEfficient
                };

                phaseResults.Add(phaseResult);

                _output.WriteLine($"  Preload time: {phaseResult.PreloadTimeMs}ms");
                _output.WriteLine($"  Avg access time: {phaseResult.AvgAccessTimeMs:F3}ms");
                _output.WriteLine($"  Cached matches: {phaseResult.CachedMatches}");
                _output.WriteLine($"  Memory efficient: {phaseResult.IsMemoryEfficient}");

                // Assert performance doesn't degrade significantly
                Assert.True(avgAccessTime < 5.0, 
                    $"Access time {avgAccessTime:F3}ms in {phase.PhaseName} phase exceeds 5ms");
                Assert.True(cacheStatus.IsMemoryEfficient, 
                    $"Memory efficiency lost in {phase.PhaseName} phase");
                Assert.Equal(phase.MatchCount, cacheStatus.TotalCachedMatches);
            }

            // Assert overall scalability
            var performanceDegradation = phaseResults.Cast<dynamic>()
                .Select(r => (double)r.AvgAccessTimeMs)
                .Zip(phaseResults.Cast<dynamic>().Skip(1).Select(r => (double)r.AvgAccessTimeMs), 
                     (prev, curr) => curr / prev)
                .Max();

            Assert.True(performanceDegradation < 2.0, 
                $"Performance degraded by {performanceDegradation:F2}x as cache size increased");
        }

        [Fact]
        public async Task ConcurrentUpdates_HighFrequency_MaintainsConsistency()
        {
            // Arrange
            var matchId = "123";
            var match = CreateTestMatch(123, "Test Home", "Test Away");
            
            _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(123))
                .ReturnsAsync(match);

            await _liveMatchService.PreloadMatchForLiveStatistics(matchId);

            // Act - Simulate high-frequency updates (like real match events)
            var updateCount = 1000;
            var concurrentReaders = 50;
            var consistencyErrors = 0;
            var readValues = new ConcurrentBag<int>();

            var updateTask = Task.Run(async () =>
            {
                for (int i = 1; i <= updateCount; i++)
                {
                    var updatedMatch = CreateTestMatch(123, "Test Home", "Test Away");
                    updatedMatch.HomeTeamScore = i % 10; // Score changes 0-9
                    updatedMatch.AwayTeamScore = (i * 2) % 10;
                    
                    _liveMatchService.UpdateCachedMatch(matchId, updatedMatch);
                    
                    // Small delay to simulate realistic update frequency
                    await Task.Delay(1);
                }
            });

            var readerTasks = Enumerable.Range(0, concurrentReaders).Select(_ =>
                Task.Run(() =>
                {
                    var random = new Random();
                    while (!updateTask.IsCompleted)
                    {
                        try
                        {
                            var cachedMatch = _liveMatchService.GetCachedLiveMatch(matchId);
                            if (cachedMatch != null)
                            {
                                // Verify data consistency (scores should be valid)
                                if (cachedMatch.HomeTeamScore < 0 || cachedMatch.HomeTeamScore > 9 ||
                                    cachedMatch.AwayTeamScore < 0 || cachedMatch.AwayTeamScore > 9)
                                {
                                    Interlocked.Increment(ref consistencyErrors);
                                }
                                
                                readValues.Add(cachedMatch.HomeTeamScore ?? 0);
                            }
                        }
                        catch
                        {
                            Interlocked.Increment(ref consistencyErrors);
                        }
                        
                        Thread.Sleep(random.Next(1, 5)); // Vary read frequency
                    }
                })
            ).ToArray();

            await Task.WhenAll(new[] { updateTask }.Concat(readerTasks));

            // Assert
            Assert.Equal(0, consistencyErrors);
            Assert.True(readValues.Count > 0, "No reads completed during test");
            
            // Verify final state is consistent
            var finalMatch = _liveMatchService.GetCachedLiveMatch(matchId);
            Assert.NotNull(finalMatch);
            Assert.True(finalMatch.HomeTeamScore >= 0 && finalMatch.HomeTeamScore <= 9);
            Assert.True(finalMatch.AwayTeamScore >= 0 && finalMatch.AwayTeamScore <= 9);

            _output.WriteLine($"Completed {updateCount} updates with {readValues.Count} concurrent reads");
            _output.WriteLine($"Consistency errors: {consistencyErrors}");
        }

        private void SetupMatchData(IEnumerable<string> matchIds)
        {
            foreach (var matchId in matchIds)
            {
                var match = CreateTestMatch(int.Parse(matchId), $"Home {matchId}", $"Away {matchId}");
                _mockUnitOfWork.Setup(uow => uow.Matches.GetByIdWithDetailsAsync(int.Parse(matchId)))
                    .ReturnsAsync(match);
            }
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
