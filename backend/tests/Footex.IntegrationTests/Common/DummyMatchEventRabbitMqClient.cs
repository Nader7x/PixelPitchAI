using Application.Interfaces;
using Application.Services;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Footex.IntegrationTests.Common;

public class DummyMatchEventRabbitMqClient(
    IHubContext<MatchHub, IMatchHub> hubContext,
    ILogger<MatchEventRabbitMqClient> logger,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory serviceScopeFactory,
    IPerformanceMonitoringService performanceMonitoringService,
    ILiveMatchStatisticsService liveMatchStatisticsService
)
    : MatchEventRabbitMqClient(
        logger,
        hubContext,
        serviceScopeFactory,
        performanceMonitoringService,
        rabbitMqOptions,
        liveMatchStatisticsService
    )
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        new Logger<DummyMatchEventRabbitMqClient>(new LoggerFactory()).LogInformation(
            "DummyMatchEventRabbitMqClient starting for tests."
        );
        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        new Logger<DummyMatchEventRabbitMqClient>(new LoggerFactory()).LogInformation(
            "DummyMatchEventRabbitMqClient stopping for tests."
        );
        return Task.CompletedTask;
    }

    public override void Dispose() =>
        new Logger<DummyMatchEventRabbitMqClient>(new LoggerFactory()).LogInformation(
            "DummyMatchEventRabbitMqClient disposing for tests."
        );

    protected override Task CloseChannelAndConnectionAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task CloseConnectionAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task CloseChannelAsync()
    {
        return Task.CompletedTask;
    }
}
