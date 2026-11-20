using Grpc.Core;
using Grpc.Net.Client;
using Infrastructure.Protos.V2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public interface ISimulationGrpcClientV2
{
    IAsyncEnumerable<MatchStreamFrameV2> StreamMatchV2Async(
        SimulateMatchV2Request request,
        CancellationToken cancellationToken = default
    );

    Task<LiveTacticalOverrideAck?> ApplyLiveTacticalOverrideAsync(
        LiveTacticalOverrideRequest request,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<MonteCarloProgressFrame> RunMonteCarloExperimentAsync(
        MonteCarloExperimentRequest request,
        CancellationToken cancellationToken = default
    );

    Task<HealthV2Response?> GetHealthV2Async(
        bool includeCheckpoints = false,
        CancellationToken cancellationToken = default
    );
}

public sealed class SimulationGrpcClientV2 : ISimulationGrpcClientV2, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly SimulationServiceV2.SimulationServiceV2Client _client;
    private readonly ILogger<SimulationGrpcClientV2> _logger;

    public SimulationGrpcClientV2(
        IOptions<SimulationGrpcOptions> options,
        ILogger<SimulationGrpcClientV2> logger
    )
    {
        _logger = logger;
        var address = !string.IsNullOrWhiteSpace(options.Value?.Address)
            ? options.Value.Address
            : options.Value?.ServerUrl ?? "http://localhost:50051";
        _channel = GrpcChannel.ForAddress(
            address,
            new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 32 * 1024 * 1024,
                MaxSendMessageSize = 16 * 1024 * 1024,
            }
        );
        _client = new SimulationServiceV2.SimulationServiceV2Client(_channel);
    }

    public async IAsyncEnumerable<MatchStreamFrameV2> StreamMatchV2Async(
        SimulateMatchV2Request request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Opening SimulationServiceV2.StreamMatchV2 for MatchId {MatchId}", request.MatchId);
        AsyncServerStreamingCall<MatchStreamFrameV2>? call = null;
        try
        {
            call = _client.StreamMatchV2(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate StreamMatchV2 for MatchId {MatchId}", request.MatchId);
            yield break;
        }

        using (call)
        {
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                yield return call.ResponseStream.Current;
            }
        }
    }

    public async Task<LiveTacticalOverrideAck?> ApplyLiveTacticalOverrideAsync(
        LiveTacticalOverrideRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _client.ApplyLiveTacticalOverrideAsync(request, cancellationToken: cancellationToken);
        }
        catch (RpcException rpcEx)
        {
            _logger.LogError(rpcEx, "RpcException in ApplyLiveTacticalOverride for MatchId {MatchId}: {Status}", request.MatchId, rpcEx.Status);
            return new LiveTacticalOverrideAck
            {
                MatchId = request.MatchId,
                Applied = false,
                Message = rpcEx.Status.Detail,
            };
        }
    }

    public async IAsyncEnumerable<MonteCarloProgressFrame> RunMonteCarloExperimentAsync(
        MonteCarloExperimentRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        AsyncServerStreamingCall<MonteCarloProgressFrame>? call = null;
        try
        {
            call = _client.RunMonteCarloExperiment(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate RunMonteCarloExperiment for ExperimentId {ExperimentId}", request.ExperimentId);
            yield break;
        }

        using (call)
        {
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                yield return call.ResponseStream.Current;
            }
        }
    }

    public async Task<HealthV2Response?> GetHealthV2Async(
        bool includeCheckpoints = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _client.GetHealthV2Async(
                new HealthV2Request { IncludeCheckpoints = includeCheckpoints },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SimulationServiceV2 health check failed");
            return new HealthV2Response { Healthy = false, DetailsJson = ex.Message };
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
