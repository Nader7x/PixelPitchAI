using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventProcessorTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMatchRepository> _mockMatchRepo;
    private readonly Mock<IMatchEventsRepository> _mockMatchEventsRepo;
    private readonly Mock<IEventAnalysisService> _mockEventAnalysisService;
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceMonitoring;
    private readonly Mock<ILiveMatchStatisticsService> _mockLiveMatchStatistics;
    private readonly Mock<IMatchEventBroadcaster> _mockBroadcaster;
    private readonly Mock<ILogger<MatchEventProcessor>> _mockLogger;
    private readonly MatchEventProcessor _processor;

    public MatchEventProcessorTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMatchRepo = new Mock<IMatchRepository>();
        _mockMatchEventsRepo = new Mock<IMatchEventsRepository>();
        _mockEventAnalysisService = new Mock<IEventAnalysisService>();
        _mockPerformanceMonitoring = new Mock<IPerformanceMonitoringService>();
        _mockLiveMatchStatistics = new Mock<ILiveMatchStatisticsService>();
        _mockBroadcaster = new Mock<IMatchEventBroadcaster>();
        _mockLogger = new Mock<ILogger<MatchEventProcessor>>();

        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEventAnalysisService))).Returns(_mockEventAnalysisService.Object);
        _mockUnitOfWork.Setup(u => u.Matches).Returns(_mockMatchRepo.Object);
        _mockUnitOfWork.Setup(u => u.MatchEvents).Returns(_mockMatchEventsRepo.Object);

        _processor = new MatchEventProcessor(
            _mockScopeFactory.Object,
            _mockPerformanceMonitoring.Object,
            _mockLiveMatchStatistics.Object,
            _mockLogger.Object,
            _mockBroadcaster.Object,
            hubContext: null
        );
    }

    [Fact]
    public async Task GetOrLoadMatchEntityAsync_LoadsFromDbAndCaches()
    {
        var match = new Match { Id = 42, CreatorId = "user1", HomeTeamScore = 0, AwayTeamScore = 0 };
        _mockMatchRepo.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(match);

        // First call loads from DB
        var result1 = await _processor.GetOrLoadMatchEntityAsync("42");
        Assert.NotNull(result1);
        Assert.Equal(42, result1.Id);
        _mockMatchRepo.Verify(r => r.GetByIdWithDetailsAsync(42), Times.Once);

        // Second call hits in-memory cache
        var result2 = await _processor.GetOrLoadMatchEntityAsync("42");
        Assert.Same(result1, result2);
        _mockMatchRepo.Verify(r => r.GetByIdWithDetailsAsync(42), Times.Once);
    }

    [Fact]
    public void ScoreTracking_InitializesAndUpdatesCorrectly()
    {
        var match = new Match { Id = 10, CreatorId = "user1", HomeTeamScore = 1, AwayTeamScore = 2 };
        var (home, away) = _processor.GetOrInitMatchScore("10", match);
        Assert.Equal(1, home);
        Assert.Equal(2, away);

        _processor.UpdateMatchScore("10", 3, 2);
        var (updatedHome, updatedAway) = _processor.GetOrInitMatchScore("10", null);
        Assert.Equal(3, updatedHome);
        Assert.Equal(2, updatedAway);
    }

    [Fact]
    public async Task ProcessEventAsync_UpdatesScoresAndBroadcastsEvent()
    {
        var match = new Match { Id = 15, CreatorId = "user1", HomeTeamScore = 0, AwayTeamScore = 0 };
        var matchEvent = new FootballMatchEvent
        {
            match_id = "15",
            event_index = 1,
            action = "goal",
            Score = new Score { Home = 1, Away = 0 }
        };

        await _processor.ProcessEventAsync(matchEvent, match);

        Assert.Equal(1, match.HomeTeamScore);
        Assert.Equal(0, match.AwayTeamScore);
        _mockBroadcaster.Verify(b => b.BroadcastEventAsync("15", matchEvent, default), Times.Once);

        var cachedEvents = _processor.GetCachedEvents("15");
        Assert.Single(cachedEvents);
        Assert.Equal("goal", cachedEvents[0].action);
    }

    [Fact]
    public async Task FlushAndPersistMatchAsync_NormalEnd_MarksCompletedAndSavesDb()
    {
        var match = new Match { Id = 20, CreatorId = "user1", MatchStatus = "SimulationInProgress", IsLive = true };
        _mockMatchRepo.Setup(r => r.GetByIdWithDetailsAsync(20)).ReturnsAsync(match);

        var ev1 = new FootballMatchEvent { match_id = "20", event_index = 1, action = "pass", time_seconds = 10 };
        var ev2 = new FootballMatchEvent { match_id = "20", event_index = 2, action = "match_end", event_type = "match_end", time_seconds = 5400 };

        await _processor.ProcessEventAsync(ev1, match);
        await _processor.ProcessEventAsync(ev2, match);

        await _processor.FlushAndPersistMatchAsync("20", isNormalEnd: true);

        Assert.False(match.IsLive);
        Assert.Equal("Completed", match.MatchStatus);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        _mockBroadcaster.Verify(b => b.ClearReplayBuffer("20"), Times.Once);

        // Cache is cleaned up
        var remaining = _processor.GetCachedEvents("20");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task FlushAndPersistMatchAsync_PrematureEnd_MarksTruncated()
    {
        var match = new Match { Id = 30, CreatorId = "user1", MatchStatus = "SimulationInProgress", IsLive = true };
        _mockMatchRepo.Setup(r => r.GetByIdWithDetailsAsync(30)).ReturnsAsync(match);

        var ev1 = new FootballMatchEvent { match_id = "30", event_index = 1, action = "pass", time_seconds = 10 };
        await _processor.ProcessEventAsync(ev1, match);

        await _processor.FlushAndPersistMatchAsync("30", isNormalEnd: false);

        Assert.False(match.IsLive);
        Assert.Equal("Truncated", match.MatchStatus);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
