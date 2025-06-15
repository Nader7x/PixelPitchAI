using System.Text.Json;
using FluentAssertions;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Footex.UnitTests.Services;

public class RedisCacheServiceTests
{
    private readonly RedisCacheOptions _cacheOptions;
    private readonly RedisCacheService _cacheService;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly Mock<IOptions<RedisCacheOptions>> _optionsMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexer;

    public RedisCacheServiceTests()
    {
        _distributedCacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _optionsMock = new Mock<IOptions<RedisCacheOptions>>();

        _cacheOptions = new RedisCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(10),
            DefaultSlidingExpiration = TimeSpan.FromMinutes(2),
            FailureThreshold = 3,
            CircuitResetInterval = TimeSpan.FromMinutes(1)
        };

        _optionsMock.Setup(x => x.Value).Returns(_cacheOptions);

        _cacheService = new RedisCacheService(
            _distributedCacheMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _connectionMultiplexer.Object);
    }

    [Fact]
    public async Task GetAsync_WithValidKey_ReturnsDeserializedValue()
    {
        // Arrange
        const string key = "test-key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var serializedValue = JsonSerializer.Serialize(testObject);

        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testObject.Id);
        result.Name.Should().Be(testObject.Name);
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(null!);

        // Assert
        result.Should().BeNull();
        _distributedCacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WithEmptyKey_ReturnsNull()
    {
        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>("");

        // Assert
        result.Should().BeNull();
        _distributedCacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenCacheThrowsException_ReturnsNullAndIncrementsFailureCount()
    {
        // Arrange
        const string key = "test-key";
        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        // Act
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithValidData_CallsDistributedCache()
    {
        // Arrange
        const string key = "test-key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, testObject, expiration);

        // Assert
        _distributedCacheMock.Verify(x => x.SetStringAsync(
            key,
            It.Is<string>(s => s.Contains("Test")),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expiration),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithNullKey_DoesNotCallDistributedCache()
    {
        // Arrange
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };

        // Act
        await _cacheService.SetAsync(null!, testObject);

        // Assert
        _distributedCacheMock.Verify(x => x.SetStringAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_WithNullValue_DoesNotCallDistributedCache()
    {
        // Arrange
        const string key = "test-key";

        // Act
        await _cacheService.SetAsync<TestCacheObject>(key, null!);

        // Assert
        _distributedCacheMock.Verify(x => x.SetStringAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_CallsDistributedCache()
    {
        // Arrange
        const string key = "test-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _distributedCacheMock.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_DoesNotCallDistributedCache()
    {
        // Act
        await _cacheService.RemoveAsync(null!);

        // Assert
        _distributedCacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ReturnsFromCache()
    {
        // Arrange
        const string key = "test-key";
        var cachedObject = new TestCacheObject { Id = 1, Name = "Cached" };
        var serializedValue = JsonSerializer.Serialize(cachedObject);
        var factoryCalled = false;

        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestCacheObject { Id = 2, Name = "Factory" });
        });

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Cached");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_CallsFactoryAndCachesResult()
    {
        // Arrange
        const string key = "test-key";
        var factoryObject = new TestCacheObject { Id = 2, Name = "Factory" };
        var factoryCalled = false;

        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(factoryObject);
        });

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(2);
        result.Name.Should().Be("Factory");
        factoryCalled.Should().BeTrue();

        // Verify that the result was cached
        _distributedCacheMock.Verify(x => x.SetStringAsync(
            key,
            It.Is<string>(s => s.Contains("Factory")),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailureThreshold()
    {
        // Arrange
        const string key = "test-key";
        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache error"));

        // Act - Trigger failures to reach threshold
        for (var i = 0; i < _cacheOptions.FailureThreshold; i++) await _cacheService.GetAsync<TestCacheObject>(key);

        // Reset mock to return valid data
        _distributedCacheMock.Reset();
        _distributedCacheMock
            .Setup(x => x.GetStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("valid data");

        // Circuit should be open now, so this call should not reach the cache
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.Should().BeNull();
        _distributedCacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}