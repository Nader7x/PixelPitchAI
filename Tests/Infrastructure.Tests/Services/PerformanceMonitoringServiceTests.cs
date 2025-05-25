using System.Diagnostics;
using Infrastructure.Services;
using Xunit;

namespace Infrastructure.Tests.Services;

public class PerformanceMonitoringServiceTests
{
    [Fact]
    public void RecordDatabaseCall_ShouldTrackCallsCorrectly()
    {
        // Arrange
        var service = new PerformanceMonitoringService();
        
        // Act
        service.RecordDatabaseCall("TestOperation", 100.5);
        service.RecordDatabaseCall("TestOperation", 200.5);
        service.RecordDatabaseCall("AnotherOperation", 50.0);
        
        // Assert
        var metrics = service.GetMetrics();
        
        Assert.Equal(2, metrics.DatabaseCalls["TestOperation"].Count);
        Assert.Equal(1, metrics.DatabaseCalls["AnotherOperation"].Count);
        Assert.Equal(150.5, metrics.DatabaseCalls["TestOperation"].AverageDurationMs, 1);
        Assert.Equal(100.5, metrics.DatabaseCalls["TestOperation"].MinDurationMs);
        Assert.Equal(200.5, metrics.DatabaseCalls["TestOperation"].MaxDurationMs);
    }
    
    [Fact]
    public void RecordCacheOperations_ShouldTrackHitsAndMisses()
    {
        // Arrange
        var service = new PerformanceMonitoringService();
        
        // Act
        service.RecordCacheHit("GetMatch");
        service.RecordCacheHit("GetMatch");
        service.RecordCacheHit("GetMatch");
        service.RecordCacheMiss("GetMatch");
        service.RecordCacheMiss("GetMatch");
        
        // Assert
        var metrics = service.GetMetrics();
        
        Assert.Equal(3, metrics.CacheHits["GetMatch"]);
        Assert.Equal(2, metrics.CacheMisses["GetMatch"]);
        Assert.Equal(0.6, metrics.CacheHitRatio, 1);
    }
    
    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var service = new PerformanceMonitoringService();
        service.RecordDatabaseCall("TestOperation", 100.0);
        service.RecordCacheHit("GetMatch");
        service.RecordCacheMiss("GetMatch");
        
        // Act
        service.Reset();
        
        // Assert
        var metrics = service.GetMetrics();
        
        Assert.Empty(metrics.DatabaseCalls);
        Assert.Empty(metrics.CacheHits);
        Assert.Empty(metrics.CacheMisses);
        Assert.Equal(0.0, metrics.CacheHitRatio);
    }
    
    [Fact]
    public void GetMetrics_WithNoCacheOperations_ShouldReturnZeroHitRatio()
    {
        // Arrange
        var service = new PerformanceMonitoringService();
        service.RecordDatabaseCall("TestOperation", 100.0);
        
        // Act
        var metrics = service.GetMetrics();
        
        // Assert
        Assert.Equal(0.0, metrics.CacheHitRatio);
    }
}
