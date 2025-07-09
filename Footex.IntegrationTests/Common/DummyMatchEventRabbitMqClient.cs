using Application.Interfaces;
using Application.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Footex.IntegrationTests.Common
{
    public class DummyMatchEventRabbitMqClient(
        Microsoft.AspNetCore.SignalR.IHubContext<MatchHub, IMatchHub> hubContext,
        ILogger<MatchEventRabbitMqClient> logger,
        Microsoft.Extensions.Options.IOptions<Infrastructure.Configuration.RabbitMqOptions> rabbitMqOptions,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory,
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

        public override void Dispose()
        {
            new Logger<DummyMatchEventRabbitMqClient>(new LoggerFactory()).LogInformation(
                "DummyMatchEventRabbitMqClient disposing for tests."
            );
        }

        protected override Task CloseChannelAndConnectionAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task CloseConnectionAsync()
        {
            return Task.CompletedTask;
        }
    }
}
