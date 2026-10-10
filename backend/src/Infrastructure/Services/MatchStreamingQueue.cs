using System.Threading.Channels;
using Infrastructure.Protos;

namespace Infrastructure.Services;

/// <summary>
///     Queue interface for buffering Pipeline B gRPC match simulation streaming requests.
/// </summary>
public interface IMatchStreamingQueue
{
    ValueTask EnqueueAsync(SimulateMatchRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SimulateMatchRequest> ReadAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     High-performance channel-backed queue decoupling HTTP simulation requests from long-running gRPC streaming tasks.
/// </summary>
public sealed class MatchStreamingQueue : IMatchStreamingQueue
{
    private readonly Channel<SimulateMatchRequest> _channel;

    public MatchStreamingQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<SimulateMatchRequest>(options);
    }

    public ValueTask EnqueueAsync(SimulateMatchRequest request, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<SimulateMatchRequest> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
