namespace Infrastructure.Configuration;

/// <summary>
///     Configuration options for match event ingestion pipeline selection.
/// </summary>
public sealed class EventIngestionOptions
{
    public const string SectionName = "EventIngestion";

    /// <summary>
    ///     Ingestion mode: "RabbitMQ" (Pipeline A, default) or "GrpcStream" (Pipeline B).
    /// </summary>
    public string Mode { get; set; } = "RabbitMQ";

    /// <summary>
    ///     Maximum concurrent gRPC match simulation streams allowed simultaneously.
    /// </summary>
    public int MaxConcurrentStreams { get; set; } = 10;
}
