using System.Collections.Concurrent;

namespace Infrastructure.Services;

public interface IPerformanceMonitoringService
{
    void RecordDatabaseCall(string operation, double durationMs);
    void RecordCacheHit(string operation);
    void RecordCacheMiss(string operation);
    PerformanceMetrics GetMetrics();
    PerformanceMetrics GetPerformanceMetrics(); // Alternative method name for compatibility
    DetailedPerformanceMetrics GetDetailedMetrics();
    void Reset();
}

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ConcurrentDictionary<string, long> _cacheHits = new();
    private readonly ConcurrentDictionary<string, long> _cacheMisses = new();
    private readonly ConcurrentDictionary<string, PerformanceCounter> _databaseCalls = new();
    private readonly object _lockObject = new();
    private DateTime _startTime = DateTime.UtcNow;

    public void RecordDatabaseCall(string operation, double durationMs)
    {
        _databaseCalls.AddOrUpdate(
            operation,
            new PerformanceCounter
            {
                Count = 1,
                TotalDurationMs = durationMs,
                MinDurationMs = durationMs,
                MaxDurationMs = durationMs,
            },
            (key, existing) =>
            {
                existing.Count++;
                existing.TotalDurationMs += durationMs;
                existing.MinDurationMs = Math.Min(existing.MinDurationMs, durationMs);
                existing.MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs);
                return existing;
            }
        );
    }

    public void RecordCacheHit(string operation)
    {
        _cacheHits.AddOrUpdate(operation, 1, (key, existing) => existing + 1);
    }

    public void RecordCacheMiss(string operation)
    {
        _cacheMisses.AddOrUpdate(operation, 1, (key, existing) => existing + 1);
    }

    public PerformanceMetrics GetMetrics()
    {
        var totalCacheHits = _cacheHits.Values.Sum();
        var totalCacheMisses = _cacheMisses.Values.Sum();
        var totalCacheOperations = totalCacheHits + totalCacheMisses;
        var cacheHitRatio =
            totalCacheOperations > 0 ? (double)totalCacheHits / totalCacheOperations : 0;

        return new PerformanceMetrics
        {
            StartTime = _startTime,
            CurrentTime = DateTime.UtcNow,
            DatabaseCalls = _databaseCalls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            CacheHits = _cacheHits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            CacheMisses = _cacheMisses.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            CacheHitRatio = cacheHitRatio,
            TotalDatabaseCalls = _databaseCalls.Values.Sum(pc => pc.Count),
            TotalCacheHits = totalCacheHits,
            TotalCacheMisses = totalCacheMisses,
        };
    }

    public PerformanceMetrics GetPerformanceMetrics()
    {
        return GetMetrics();
    }

    public DetailedPerformanceMetrics GetDetailedMetrics()
    {
        var baseMetrics = GetMetrics();
        var upTime = DateTime.UtcNow - _startTime;

        return new DetailedPerformanceMetrics
        {
            StartTime = _startTime,
            CurrentTime = DateTime.UtcNow,
            UpTime = upTime,
            DatabaseCalls = baseMetrics
                .DatabaseCalls.Select(kvp => new DatabaseCallMetric
                {
                    OperationType = kvp.Key,
                    Count = kvp.Value.Count,
                    TotalDurationMs = kvp.Value.TotalDurationMs,
                    AverageDurationMs = kvp.Value.AverageDurationMs,
                    MinDurationMs = kvp.Value.MinDurationMs,
                    MaxDurationMs = kvp.Value.MaxDurationMs,
                    CallsPerSecond =
                        upTime.TotalSeconds > 0 ? kvp.Value.Count / upTime.TotalSeconds : 0,
                })
                .ToList(),
            CacheMetrics = new CacheMetrics
            {
                TotalHits = baseMetrics.TotalCacheHits,
                TotalMisses = baseMetrics.TotalCacheMisses,
                HitRatio = baseMetrics.CacheHitRatio,
                MissRatio = 1.0 - baseMetrics.CacheHitRatio,
                TotalOperations = baseMetrics.TotalCacheHits + baseMetrics.TotalCacheMisses,
                HitsPerSecond =
                    upTime.TotalSeconds > 0 ? baseMetrics.TotalCacheHits / upTime.TotalSeconds : 0,
                OperationsPerSecond =
                    upTime.TotalSeconds > 0
                        ? (baseMetrics.TotalCacheHits + baseMetrics.TotalCacheMisses)
                            / upTime.TotalSeconds
                        : 0,
                OperationBreakdown = baseMetrics
                    .CacheHits.Select(kvp => new CacheOperationMetric
                    {
                        OperationType = kvp.Key,
                        Hits = kvp.Value,
                        Misses = baseMetrics.CacheMisses.GetValueOrDefault(kvp.Key, 0),
                        HitRatio =
                            kvp.Value + baseMetrics.CacheMisses.GetValueOrDefault(kvp.Key, 0) > 0
                                ? (double)kvp.Value
                                    / (
                                        kvp.Value
                                        + baseMetrics.CacheMisses.GetValueOrDefault(kvp.Key, 0)
                                    )
                                : 0,
                    })
                    .ToList(),
            },
            SystemMetrics = new SystemMetrics
            {
                TotalDatabaseCallsPerSecond =
                    upTime.TotalSeconds > 0
                        ? baseMetrics.TotalDatabaseCalls / upTime.TotalSeconds
                        : 0,
                AverageDatabaseCallDuration = baseMetrics.DatabaseCalls.Values.Any()
                    ? baseMetrics.DatabaseCalls.Values.Average(pc => pc.AverageDurationMs)
                    : 0,
                DatabaseEfficiencyRatio =
                    baseMetrics.TotalCacheHits + baseMetrics.TotalDatabaseCalls > 0
                        ? (double)baseMetrics.TotalCacheHits
                            / (baseMetrics.TotalCacheHits + baseMetrics.TotalDatabaseCalls)
                        : 0,
            },
        };
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _databaseCalls.Clear();
            _cacheHits.Clear();
            _cacheMisses.Clear();
            _startTime = DateTime.UtcNow;
        }
    }
}

public class PerformanceCounter
{
    public long Count { get; set; }
    public double TotalDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double AverageDurationMs => Count > 0 ? TotalDurationMs / Count : 0;
}

public class PerformanceMetrics
{
    public DateTime StartTime { get; set; }
    public DateTime CurrentTime { get; set; }
    public Dictionary<string, PerformanceCounter> DatabaseCalls { get; set; } = new();
    public Dictionary<string, long> CacheHits { get; set; } = new();
    public Dictionary<string, long> CacheMisses { get; set; } = new();
    public double CacheHitRatio { get; set; }
    public long TotalDatabaseCalls { get; set; }
    public long TotalCacheHits { get; set; }
    public long TotalCacheMisses { get; set; }
    public TimeSpan UpTime => CurrentTime - StartTime;
}

public class DetailedPerformanceMetrics
{
    public DateTime StartTime { get; set; }
    public DateTime CurrentTime { get; set; }
    public TimeSpan UpTime { get; set; }
    public List<DatabaseCallMetric> DatabaseCalls { get; set; } = new();
    public CacheMetrics CacheMetrics { get; set; } = new();
    public SystemMetrics SystemMetrics { get; set; } = new();
}

public class DatabaseCallMetric
{
    public string OperationType { get; set; } = string.Empty;
    public long Count { get; set; }
    public double TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double CallsPerSecond { get; set; }
}

public class CacheMetrics
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRatio { get; set; }
    public double MissRatio { get; set; }
    public long TotalOperations { get; set; }
    public double HitsPerSecond { get; set; }
    public double OperationsPerSecond { get; set; }
    public List<CacheOperationMetric> OperationBreakdown { get; set; } = new();
}

public class CacheOperationMetric
{
    public string OperationType { get; set; } = string.Empty;
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRatio { get; set; }
}

public class SystemMetrics
{
    public double TotalDatabaseCallsPerSecond { get; set; }
    public double AverageDatabaseCallDuration { get; set; }
    public double DatabaseEfficiencyRatio { get; set; }
}
