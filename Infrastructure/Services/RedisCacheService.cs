using System.Text.Json;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly DistributedCacheEntryOptions _options;
    private readonly RedisCacheOptions _cacheOptions;
    
    // Circuit breaker pattern properties
    private bool _circuitOpen = false;
    private DateTime _circuitResetTime = DateTime.MinValue;
    private int _failureCount = 0;
    private readonly object _circuitLock = new object();

    public RedisCacheService(
        IDistributedCache cache, 
        ILogger<RedisCacheService> logger,
        IOptions<RedisCacheOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _cacheOptions = options.Value;
        _options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheOptions.DefaultExpiration,
            SlidingExpiration = _cacheOptions.DefaultSlidingExpiration
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Attempt to get cache value with null or empty key");
            return null;
        }

        if (IsCircuitOpen())
        {
            _logger.LogDebug("Circuit breaker open - skipping cache GET for key: {Key}", key);
            return null;
        }

        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);
            
            if (cachedValue is null)
            {
                return null;
            }

            ResetCircuitBreaker(); // Successful operation
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (Exception ex)
        {
            IncrementFailureCount();
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Attempt to set cache value with null or empty key");
            return;
        }

        if (value == null)
        {
            _logger.LogWarning("Attempt to set null value in cache for key: {Key}", key);
            return;
        }

        if (IsCircuitOpen())
        {
            _logger.LogDebug("Circuit breaker open - skipping cache SET for key: {Key}", key);
            return;
        }

        try
        {
            var options = expiration.HasValue 
                ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }
                : _options;
                
            var serializedValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);
            
            ResetCircuitBreaker(); // Successful operation
        }
        catch (Exception ex)
        {
            IncrementFailureCount();
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Attempt to remove cache value with null or empty key");
            return;
        }

        if (IsCircuitOpen())
        {
            _logger.LogDebug("Circuit breaker open - skipping cache REMOVE for key: {Key}", key);
            return;
        }

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            ResetCircuitBreaker(); // Successful operation
        }
        catch (Exception ex)
        {
            IncrementFailureCount();
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("Attempt to get or create cache value with null or empty key");
            return await factory();
        }

        // Try to get from cache only if circuit is closed
        var cachedValue = !IsCircuitOpen() 
            ? await GetAsync<T>(key, cancellationToken) 
            : null;
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        // Generate value from factory
        var newValue = await factory();
        
        // Only cache non-null values
        if (newValue != null && !IsCircuitOpen())
        {
            await SetAsync(key, newValue, expiration, cancellationToken);
        }
        
        return newValue;
    }
    
    // Circuit breaker pattern methods
    private bool IsCircuitOpen()
    {
        lock (_circuitLock)
        {
            // If circuit is open, check if reset interval has elapsed
            if (_circuitOpen && DateTime.UtcNow > _circuitResetTime)
            {
                // Move to half-open state by allowing a trial operation
                _logger.LogInformation("Circuit breaker reset interval elapsed, attempting to close circuit");
                _circuitOpen = false;
                _failureCount = 0;
                return false;
            }
            
            return _circuitOpen;
        }
    }
    
    private void IncrementFailureCount()
    {
        lock (_circuitLock)
        {
            _failureCount++;
            
            // If failure threshold reached, open the circuit
            if (_failureCount >= _cacheOptions.FailureThreshold && !_circuitOpen)
            {
                _circuitOpen = true;
                _circuitResetTime = DateTime.UtcNow.Add(_cacheOptions.CircuitResetInterval);
                _logger.LogWarning(
                    "Circuit breaker opened after {FailureCount} failures. Will reset at {ResetTime}", 
                    _failureCount, 
                    _circuitResetTime);
            }
        }
    }
    
    private void ResetCircuitBreaker()
    {
        lock (_circuitLock)
        {
            if (_circuitOpen)
            {
                _logger.LogInformation("Circuit breaker closed after successful operation");
            }
            
            _circuitOpen = false;
            _failureCount = 0;
        }
    }
}
