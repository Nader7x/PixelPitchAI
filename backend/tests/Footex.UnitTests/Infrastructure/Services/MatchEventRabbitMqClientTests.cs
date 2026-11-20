using System.Text;
using System.Text.Json;
using Application.Dtos.V2;
using Domain.Models;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventRabbitMqClientTests
{
    [Fact]
    public async Task StartAsync_DeclaresV2ExchangeQueueAndDeadLetterQueue()
    {
        var mockFactory = new Mock<IConnectionFactory>();
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChannel.Object);
        mockFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConnection.Object);

        var services = new ServiceCollection();
        services.AddDbContext<FootballDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        using var provider = services.BuildServiceProvider();

        var client = new MatchEventRabbitMqClient(
            Mock.Of<ILogger<MatchEventRabbitMqClient>>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RabbitMqOptions()),
            mockFactory.Object
        );

        await client.StartAsync(CancellationToken.None);

        mockChannel.Verify(
            c => c.ExchangeDeclareAsync(
                MatchEventRabbitMqClient.DeadLetterExchangeName,
                ExchangeType.Direct,
                true,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        mockChannel.Verify(
            c => c.ExchangeDeclareAsync(
                MatchEventRabbitMqClient.ExchangeName,
                ExchangeType.Topic,
                true,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        mockChannel.Verify(
            c => c.QueueDeclareAsync(
                MatchEventRabbitMqClient.ExperimentQueueName,
                true,
                false,
                false,
                It.Is<IDictionary<string, object?>>(d =>
                    d != null && Equals(d["x-dead-letter-exchange"], MatchEventRabbitMqClient.DeadLetterExchangeName)
                ),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ValidExperimentCompleted_UpdatesDbBroadcastsAndAcks()
    {
        var mockFactory = new Mock<IConnectionFactory>();
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChannel.Object);
        mockFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConnection.Object);

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<FootballDbContext>(o => o.UseInMemoryDatabase(dbName));
        using var provider = services.BuildServiceProvider();

        using (var seedScope = provider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<FootballDbContext>();
            db.TacticalExperiments.Add(
                new TacticalExperimentRecord
                {
                    Id = "exp_123",
                    UserId = "user-1",
                    Title = "Test",
                    Status = "running",
                }
            );
            await db.SaveChangesAsync();
        }

        var expBroadcaster = new Mock<IExperimentBroadcaster>();
        var notifBroadcaster = new Mock<INotificationsBroadcaster>();

        var client = new MatchEventRabbitMqClient(
            Mock.Of<ILogger<MatchEventRabbitMqClient>>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RabbitMqOptions()),
            mockFactory.Object,
            expBroadcaster.Object,
            notifBroadcaster.Object
        );

        await client.StartAsync(CancellationToken.None);

        var resultDto = new MonteCarloExperimentResponseDto(
            "exp_123",
            "completed",
            "local_cuda",
            100,
            1200,
            []
        );
        var resultJson = JsonSerializer.Serialize(resultDto, MatchEventJsonContext.Default.MonteCarloExperimentResponseDto);
        var payload = new ExperimentCompletedRabbitMqEventDto(
            "exp_123",
            "user-1",
            "completed",
            "local_cuda",
            1200,
            resultJson,
            null,
            null,
            null
        );
        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(payload, MatchEventJsonContext.Default.ExperimentCompletedRabbitMqEventDto)
        );
        var ea = new BasicDeliverEventArgs(
            "tag",
            42,
            false,
            MatchEventRabbitMqClient.ExchangeName,
            MatchEventRabbitMqClient.ExperimentRoutingKey,
            new BasicProperties(),
            body
        );

        await client.HandleReceivedMessageAsync(ea, CancellationToken.None);

        mockChannel.Verify(c => c.BasicAckAsync(42, false, It.IsAny<CancellationToken>()), Times.Once);
        expBroadcaster.Verify(
            b => b.BroadcastCompletedAsync("exp_123", It.Is<MonteCarloExperimentResponseDto>(r => r.ExperimentId == "exp_123"), It.IsAny<CancellationToken>()),
            Times.Once
        );
        notifBroadcaster.Verify(
            b => b.BroadcastNotificationAsync("user-1", It.Is<NotificationV2Dto>(n => n.Type == "experiment_completed"), It.IsAny<CancellationToken>()),
            Times.Once
        );

        using var verifyScope = provider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var updated = await verifyDb.TacticalExperiments.FindAsync("exp_123");
        updated.Should().NotBeNull();
        updated!.Status.Should().Be("completed");
        updated.ExecutionTier.Should().Be("local_cuda");
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_InvalidJson_NacksWithoutRequeueToDlq()
    {
        var mockFactory = new Mock<IConnectionFactory>();
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IChannel>();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockChannel.Object);
        mockFactory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConnection.Object);

        var services = new ServiceCollection();
        services.AddDbContext<FootballDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        using var provider = services.BuildServiceProvider();

        var client = new MatchEventRabbitMqClient(
            Mock.Of<ILogger<MatchEventRabbitMqClient>>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RabbitMqOptions()),
            mockFactory.Object
        );

        await client.StartAsync(CancellationToken.None);

        var ea = new BasicDeliverEventArgs(
            "tag",
            99,
            false,
            MatchEventRabbitMqClient.ExchangeName,
            MatchEventRabbitMqClient.ExperimentRoutingKey,
            new BasicProperties(),
            Encoding.UTF8.GetBytes("{invalid-json")
        );

        await client.HandleReceivedMessageAsync(ea, CancellationToken.None);

        mockChannel.Verify(c => c.BasicNackAsync(99, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
