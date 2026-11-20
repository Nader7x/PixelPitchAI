using System.Text;
using System.Text.Json;
using Application.Dtos.V2;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Services;

/// <summary>
///     Event-driven RabbitMQ consumer scoped strictly to async experiment.completed.v2
///     notifications with Dead-Letter Exchange (DLX) / DLQ handling.
///     Live match streams run exclusively over gRPC SimulationServiceV2.
/// </summary>
public class MatchEventRabbitMqClient : BackgroundService
{
    public const string ExchangeName = "match_events_v2";
    public const string DeadLetterExchangeName = "match_events_dlx";
    public const string ExperimentQueueName = "experiment_results_v2_queue";
    public const string ExperimentDlqName = "experiment_results_v2_dlq";
    public const string ExperimentRoutingKey = "experiment.completed.v2";

    private readonly IConnectionFactory? _injectedConnectionFactory;
    private readonly ILogger<MatchEventRabbitMqClient> _logger;
    private readonly RabbitMqOptions _rabbitMqSettings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IExperimentBroadcaster? _experimentBroadcaster;
    private readonly INotificationsBroadcaster? _notificationsBroadcaster;

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public MatchEventRabbitMqClient(
        ILogger<MatchEventRabbitMqClient> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IConnectionFactory? injectedConnectionFactory = null,
        IExperimentBroadcaster? experimentBroadcaster = null,
        INotificationsBroadcaster? notificationsBroadcaster = null
    )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _rabbitMqSettings = rabbitMqOptions.Value;
        _injectedConnectionFactory = injectedConnectionFactory;
        _experimentBroadcaster = experimentBroadcaster;
        _notificationsBroadcaster = notificationsBroadcaster;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await TryInitializeRabbitMqAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is not { IsOpen: true })
        {
            _logger.LogWarning("RabbitMQ channel unavailable at ExecuteAsync. Async experiment consumer idle.");
            return;
        }

        await _channel.BasicQosAsync(0, 32, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            await HandleReceivedMessageAsync(ea, stoppingToken);
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: ExperimentQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation(
            "MatchEventRabbitMqClient started push consumer on {QueueName} ({RoutingKey})",
            ExperimentQueueName,
            ExperimentRoutingKey
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("MatchEventRabbitMqClient stopping.");
        }
    }

    public async Task HandleReceivedMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(ea.Body.Span);
            var evt = JsonSerializer.Deserialize(
                payloadJson,
                MatchEventJsonContext.Default.ExperimentCompletedRabbitMqEventDto
            );

            if (evt is null || string.IsNullOrWhiteSpace(evt.ExperimentId))
            {
                _logger.LogWarning("Malformed experiment.completed.v2 payload; routing to DLQ.");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }

            await ProcessExperimentCompletedAsync(evt, cancellationToken);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in experiment.completed.v2; sending to DLQ.");
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing experiment.completed.v2 message {DeliveryTag}; sending to DLQ.", ea.DeliveryTag);
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    private async Task ProcessExperimentCompletedAsync(
        ExperimentCompletedRabbitMqEventDto evt,
        CancellationToken cancellationToken
    )
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var record = await db.TacticalExperiments.FirstOrDefaultAsync(
            e => e.Id == evt.ExperimentId,
            cancellationToken
        );

        if (record != null)
        {
            record.Status = string.IsNullOrWhiteSpace(evt.Status) ? "completed" : evt.Status;
            record.ExecutionTier = evt.ExecutionTier;
            record.ResultJson = evt.ResultJson;
            record.CommentaryTelemetryJson = evt.CommentaryTelemetryJson;
            record.RepresentativeSeedsJson = evt.RepresentativeSeedsJson;
            record.ErrorMessage = evt.ErrorMessage;
            record.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        if (_experimentBroadcaster != null)
        {
            if (evt.Status == "failed")
            {
                await _experimentBroadcaster.BroadcastFailedAsync(
                    evt.ExperimentId,
                    new ExperimentProgressDto(evt.ExperimentId, "failed", 0, 0, 0, evt.ExecutionTier, evt.ErrorMessage),
                    cancellationToken
                );
            }
            else if (!string.IsNullOrWhiteSpace(evt.ResultJson))
            {
                var parsed = JsonSerializer.Deserialize(
                    evt.ResultJson,
                    MatchEventJsonContext.Default.MonteCarloExperimentResponseDto
                );
                if (parsed != null)
                {
                    await _experimentBroadcaster.BroadcastCompletedAsync(evt.ExperimentId, parsed, cancellationToken);
                }
            }
        }

        if (_notificationsBroadcaster != null && !string.IsNullOrWhiteSpace(evt.UserId))
        {
            await _notificationsBroadcaster.BroadcastNotificationAsync(
                evt.UserId,
                new NotificationV2Dto(
                    Guid.NewGuid().ToString(),
                    evt.UserId,
                    "experiment_completed",
                    "Counterfactual Experiment Completed",
                    $"Experiment {evt.ExperimentId} finished ({evt.Status}).",
                    DateTime.UtcNow,
                    ExperimentId: evt.ExperimentId
                ),
                cancellationToken
            );
        }
    }

    private async Task<bool> TryInitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        var factory = _injectedConnectionFactory ?? new ConnectionFactory
        {
            HostName = _rabbitMqSettings.HostName,
            UserName = _rabbitMqSettings.UserName,
            Password = _rabbitMqSettings.Password,
            VirtualHost = _rabbitMqSettings.VirtualHost,
            Port = _rabbitMqSettings.Port,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        try
        {
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                DeadLetterExchangeName,
                ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            );
            await _channel.QueueDeclareAsync(
                ExperimentDlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken
            );
            await _channel.QueueBindAsync(
                ExperimentDlqName,
                DeadLetterExchangeName,
                ExperimentRoutingKey,
                cancellationToken: cancellationToken
            );

            await _channel.ExchangeDeclareAsync(
                ExchangeName,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken
            );

            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = ExperimentRoutingKey,
            };

            await _channel.QueueDeclareAsync(
                ExperimentQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken
            );
            await _channel.QueueBindAsync(
                ExperimentQueueName,
                ExchangeName,
                ExperimentRoutingKey,
                cancellationToken: cancellationToken
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize RabbitMQ for experiment.completed.v2; running without RabbitMQ.");
            return false;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } && !string.IsNullOrEmpty(_consumerTag))
        {
            try
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            }
            catch
            {
                // Ignore shutdown race
            }
        }

        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }
}
