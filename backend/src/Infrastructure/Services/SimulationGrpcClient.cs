using Grpc.Core;
using Grpc.Net.Client;
using Infrastructure.Protos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class SimulationGrpcOptions
{
    public const string SectionName = "SimulationGrpc";
    public string ServerUrl { get; set; } = "http://localhost:50051";
}

public interface ISimulationGrpcClient
{
    Task<StartMatchResponse?> StartMatchAsync(SimulateMatchRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MatchEventRaw> StreamMatchEventsAsync(SimulateMatchRequest request, CancellationToken cancellationToken = default);
    Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default);
}

public sealed class SimulationGrpcClient : ISimulationGrpcClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly SimulationService.SimulationServiceClient _client;
    private readonly ILogger<SimulationGrpcClient> _logger;

    public SimulationGrpcClient(IOptions<SimulationGrpcOptions> options, ILogger<SimulationGrpcClient> logger)
    {
        _logger = logger;
        var address = options.Value?.ServerUrl ?? "http://localhost:50051";
        _logger.LogInformation("Initializing SimulationGrpcClient pointing to {Address}", address);
        
        _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024,
            MaxSendMessageSize = 16 * 1024 * 1024
        });
        _client = new SimulationService.SimulationServiceClient(_channel);
    }

    public async Task<StartMatchResponse?> StartMatchAsync(SimulateMatchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invoking StartMatchSimulation gRPC for MatchId {MatchId}", request.MatchId);
            return await _client.StartMatchSimulationAsync(request, cancellationToken: cancellationToken);
        }
        catch (RpcException rpcEx)
        {
            _logger.LogError(rpcEx, "RpcException in StartMatchSimulation for MatchId {MatchId}: {Status}", request.MatchId, rpcEx.Status);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception in StartMatchSimulation for MatchId {MatchId}", request.MatchId);
            return null;
        }
    }

    public async IAsyncEnumerable<MatchEventRaw> StreamMatchEventsAsync(
        SimulateMatchRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opening direct StartMatchSimulationStream gRPC for MatchId {MatchId}", request.MatchId);
        AsyncServerStreamingCall<MatchEventRaw>? call = null;
        try
        {
            call = _client.StartMatchSimulationStream(request, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate StartMatchSimulationStream for MatchId {MatchId}", request.MatchId);
            yield break;
        }

        while (await call.ResponseStream.MoveNext(cancellationToken))
        {
            yield return call.ResponseStream.Current;
        }
    }

    public async Task<HealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.GetHealthAsync(new HealthRequest { ClientId = "FootexBackend" }, cancellationToken: cancellationToken);
        }
        catch (RpcException rpcEx)
        {
            _logger.LogWarning("Simulation gRPC service health check failed: {Detail}", rpcEx.Status.Detail);
            return new HealthResponse { Status = false, Message = rpcEx.Status.Detail };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Simulation gRPC health check error: {Message}", ex.Message);
            return new HealthResponse { Status = false, Message = ex.Message };
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
