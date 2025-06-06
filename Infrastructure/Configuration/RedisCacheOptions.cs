using System;

namespace Infrastructure.Configuration;

public class RedisCacheOptions
{
    public const string SectionName = "RedisCache";
    
    public string LocalConnectionString { get; set; } = "localhost:6379";
    public string RemoteConnectionString { get; set; } = "";
    public string InstanceName { get; set; } = "Footex_";
    
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(2);
    
    // Circuit breaker options
    public TimeSpan CircuitResetInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int FailureThreshold { get; set; } = 3;
}
