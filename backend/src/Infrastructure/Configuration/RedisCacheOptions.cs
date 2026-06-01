namespace Infrastructure.Configuration;

public class RedisCacheOptions
{
    public const string SectionName = "RedisCache";

    public string LocalConnectionString { get; set; } = "localhost:6379";
    public string RemoteConnectionString { get; set; } = "";
    public string InstanceName { get; set; } = "Footex_";

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromSeconds(30);

    // Circuit breaker options
    public TimeSpan CircuitResetInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int FailureThreshold { get; set; } = 3;
}
