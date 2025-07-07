using System; // Added for TimeSpan, EventId, Exception, Func
using System.Collections.Generic; // Added for Dictionary
using System.Net.Sockets; // Added for SocketException
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks; // Added for Task
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Domain.Repositories;
using Footex.UnitTests.Common;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions; // Added for BrokerUnreachableException
using Xunit;
using Match = Domain.Models.Match;

namespace Footex.UnitTests.Infrastructure.Services;

public class MatchEventRabbitMqClientTests : IDisposable
{
    private readonly Mock<IHubContext<MatchHub, IMatchHub>> _mockHubContext;
    private readonly Mock<ILogger<MatchEventRabbitMqClient>> _mockLogger;
    private readonly Mock<IOptions<RabbitMqOptions>> _mockRabbitMqOptions;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IChannel> _mockChannel;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEventAnalysisService> _mockEventAnalysisService;
    private readonly Mock<IPerformanceMonitoringService> _mockPerformanceMonitoringService;
    private readonly Mock<ILiveMatchStatisticsService> _mockLiveMatchStatisticsService;
    private readonly Mock<IMatchHub> _mockMatchHub;
    private readonly Mock<IHubCallerClients<IMatchHub>> _mockClients;
    private readonly Mock<IMatchRepository> _mockMatchRepository;
    private readonly Mock<IConnectionFactory> _mockConnectionFactory; // NEW: Declare mock for IConnectionFactory
    private readonly MatchEventRabbitMqClient _sut;

    public MatchEventRabbitMqClientTests()
    {
        _mockLogger = new Mock<ILogger<MatchEventRabbitMqClient>>();
        _mockHubContext = new Mock<IHubContext<MatchHub, IMatchHub>>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockRabbitMqOptions = new Mock<IOptions<RabbitMqOptions>>();
        _mockConnection = new Mock<IConnection>();
        _mockChannel = new Mock<IChannel>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEventAnalysisService = new Mock<IEventAnalysisService>();
        _mockPerformanceMonitoringService = new Mock<IPerformanceMonitoringService>();
        _mockLiveMatchStatisticsService = new Mock<ILiveMatchStatisticsService>();
        _mockMatchHub = new Mock<IMatchHub>();
        _mockClients = new Mock<IHubCallerClients<IMatchHub>>();
        _mockMatchRepository = new Mock<IMatchRepository>();
        _mockConnectionFactory = new Mock<IConnectionFactory>(); // NEW: Initialize the mock

        var rabbitMqOptions = new RabbitMqOptions
        {
            HostName = "localhost",
            QueueName = "test_queue",
            UserName = "test_user",
            Password = "test_password",
            VirtualHost = "/",
            Port = 5672,
        };
        _mockRabbitMqOptions.Setup(o => o.Value).Returns(rabbitMqOptions);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);
        serviceProvider
            .Setup(x => x.GetService(typeof(IEventAnalysisService)))
            .Returns(_mockEventAnalysisService.Object);

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        serviceScope.Setup(x => x.Dispose());

        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);

        _mockConnection
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), default))
            .ReturnsAsync(_mockChannel.Object);
        _mockConnection.Setup(c => c.IsOpen).Returns(true);
        _mockChannel.Setup(c => c.IsOpen).Returns(true);

        _mockUnitOfWork.Setup(x => x.Matches).Returns(_mockMatchRepository.Object);

        // Setup SignalR mocks
        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockMatchHub.Object);

        _sut = new MatchEventRabbitMqClient(
            _mockLogger.Object,
            _mockHubContext.Object,
            _mockServiceScopeFactory.Object,
            _mockPerformanceMonitoringService.Object,
            _mockRabbitMqOptions.Object,
            _mockLiveMatchStatisticsService.Object,
            _mockConnectionFactory.Object // NEW: Pass the mocked connection factory
        );
    }

    [Theory]
    [InlineData("shot", "shot", true, "goal")]
    [InlineData("shot", "shot", true, null)]
    [InlineData("card", "yellow_card", true, null)]
    [InlineData("substitution", "substitution", true, null)]
    [InlineData("match_start", "match_start", true, null)]
    [InlineData("pass", "pass", false, null)]
    [InlineData(null, "some_event", false, null)]
    public void IsSignificantEvent_ShouldReturnExpectedResult(
        string action,
        string eventType,
        bool expected,
        string? outcome = null
    )
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            action = action,
            event_type = eventType,
            outcome = outcome,
        };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSignificantEvent_WithNullEvent_ShouldReturnFalse()
    {
        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            (FootballMatchEvent?)null
        );

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("free kick")]
    [InlineData("corner")]
    [InlineData("kick off")]
    public void IsSignificantEvent_WithSetPieces_ShouldReturnTrue(string type)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            action = "some_action",
            event_type = "some_event",
            type = type,
        };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("yellow")]
    [InlineData("red")]
    [InlineData("card")]
    public void IsSignificantEvent_WithCardActions_ShouldReturnTrue(string action)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = action, event_type = "some_event" };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSignificantEvent_WithLongPass_ShouldReturnTrue()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            action = "pass",
            event_type = "pass",
            long_pass = true,
        };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("substitution")]
    [InlineData("sub")]
    public void IsSignificantEvent_WithSubstitutions_ShouldReturnTrue(string action)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = action, event_type = "some_event" };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("match_start")]
    [InlineData("match_end")]
    [InlineData("first_half_end")]
    [InlineData("second_half_start")]
    [InlineData("stoppage_time_start")]
    public void IsSignificantEvent_WithMatchStateChanges_ShouldReturnTrue(string action)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = action, event_type = "some_event" };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("penalty")]
    [InlineData("penalty_kick")]
    public void IsSignificantEvent_WithPenalties_ShouldReturnTrue(string action)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = action, event_type = "some_event" };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSignificantEvent_WithOwnGoal_ShouldReturnTrue()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = "own goal", event_type = "goal" };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("shot")]
    [InlineData("duel")]
    [InlineData("foul committed")]
    [InlineData("foul won")]
    [InlineData("dribble")]
    [InlineData("interception")]
    [InlineData("clearance")]
    [InlineData("block")]
    [InlineData("carry")]
    [InlineData("ball_recovery")]
    public void IsSignificantEvent_WithSignificantEventTypes_ShouldReturnTrue(string eventType)
    {
        // Arrange
        var matchEvent = new FootballMatchEvent { action = "some_action", event_type = eventType };

        // Act
        var result = TestUtils.InvokeStaticMethod<MatchEventRabbitMqClient, bool>(
            "IsSignificantEvent",
            matchEvent
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StartAsync_WhenInitializationSucceeds_ShouldStartSuccessfully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Setup the mocked connection factory to return the mocked connection
        // The SUT now uses this mock to create the connection
        _mockConnectionFactory
            .Setup(cf => cf.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockConnection.Object);

        // Set up the connection and channel to appear open and valid
        _mockConnection.Setup(c => c.IsOpen).Returns(true);
        _mockConnection
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), default))
            .ReturnsAsync(_mockChannel.Object);
        _mockChannel.Setup(c => c.IsOpen).Returns(true);

        // Mock the topology declarations
        _mockChannel
            .Setup(c =>
                c.ExchangeDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _mockChannel
            .Setup(c =>
                c.QueueDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                Task.FromResult(
                    new QueueDeclareOk(It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>())
                )
            );

        _mockChannel
            .Setup(c =>
                c.QueueBindAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _mockChannel
            .Setup(c =>
                c.BasicQosAsync(
                    It.IsAny<uint>(),
                    It.IsAny<ushort>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        await _sut.StartAsync(cancellationToken);

        // Assert
        // Verify that CreateConnectionAsync was called on the MOCKED factory
        _mockConnectionFactory.Verify(
            cf => cf.CreateConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify that a channel was created
        _mockConnection.Verify(
            c =>
                c.CreateChannelAsync(
                    It.IsAny<CreateChannelOptions>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify logging
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("MatchEventRabbitMqClient starting")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Connection to RabbitMQ established.")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Channel created successfully.")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains(
                                    "Successfully connected to RabbitMQ and declared topology"
                                )
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _mockChannel.Verify(
            c =>
                c.ExchangeDeclareAsync(
                    "match_events",
                    ExchangeType.Topic,
                    true,
                    false,
                    It.IsAny<IDictionary<string, object?>>(),
                    false,
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockChannel.Verify(
            c =>
                c.QueueDeclareAsync(
                    _mockRabbitMqOptions.Object.Value.QueueName, // Use the actual queue name from options
                    true,
                    false,
                    false,
                    It.IsAny<IDictionary<string, object?>>(),
                    false,
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockChannel.Verify(
            c =>
                c.QueueBindAsync(
                    _mockRabbitMqOptions.Object.Value.QueueName, // Use the actual queue name from options
                    "match_events",
                    "match.events",
                    It.IsAny<IDictionary<string, object?>>(),
                    false,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _mockChannel.Verify(
            c => c.BasicQosAsync(0, 1, false, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Act
        await _sut.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("MatchEventRabbitMqClient stopped")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesAndSuppressFinalize()
    {
        // Act
        _sut.Dispose();

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("MatchEventRabbitMqClient disposed")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetOrLoadMatchEntity_WithValidMatchId_ShouldReturnMatch()
    {
        // Arrange
        var matchId = "123";
        var expectedMatch = new Match
        {
            Id = 123,
            HomeTeamInMatchName = "Team A",
            AwayTeamInMatchName = "Team B",
            CreatorId = "0", // Set to a valid test value
        };

        _mockMatchRepository.Setup(x => x.GetByIdWithDetailsAsync(123)).ReturnsAsync(expectedMatch);

        // Act
        var result = await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Match?>(
            _sut,
            "GetOrLoadMatchEntity",
            matchId
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        _mockMatchRepository.Verify(x => x.GetByIdWithDetailsAsync(123), Times.Once);
        _mockPerformanceMonitoringService.Verify(
            x => x.RecordDatabaseCall("LoadMatchEntity", It.IsAny<double>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetOrLoadMatchEntity_WithInvalidMatchId_ShouldReturnNull()
    {
        // Arrange
        var matchId = "invalid";

        // Act
        var result = await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Match?>(
            _sut,
            "GetOrLoadMatchEntity",
            matchId
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrLoadMatchEntity_WithNonExistentMatch_ShouldReturnNull()
    {
        // Arrange
        var matchId = "999";

        _mockMatchRepository.Setup(x => x.GetByIdWithDetailsAsync(999)).ReturnsAsync((Match?)null);

        // Act
        var result = await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Match?>(
            _sut,
            "GetOrLoadMatchEntity",
            matchId
        );

        // Assert
        Assert.Null(result);
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Match with ID 999 not found")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task BroadcastEventToClients_WithValidEvent_ShouldBroadcastSuccessfully()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            match_id = "123",
            event_index = 1,
            action = "goal",
            event_type = "shot",
        };

        // Act
        await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Task>(
            _sut,
            "BroadcastEventToClients",
            matchEvent
        );

        // Assert
        _mockMatchHub.Verify(
            x => x.SendMatchEventAsync("match_event", 123, matchEvent),
            Times.Once
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Broadcasted match event 1 for match 123")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task BroadcastMatchStatistics_WithValidData_ShouldBroadcastSuccessfully()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            match_id = "123",
            timestamp = DateTime.UtcNow.ToString(),
            minute = 45,
            action = "goal",
            team = "home",
        };

        var matchEntity = new Match
        {
            Id = 123,
            HomeTeamInMatchName = "Team A",
            AwayTeamInMatchName = "Team B",
            HomeTeamScore = 1,
            AwayTeamScore = 0,
            MatchStatus = "Live",
            IsLive = true,
            CreatorId = "0", // Set to a valid test value
        };

        var mockStatisticsGroup = new Mock<IMatchHub>();
        _mockClients
            .Setup(x => x.Group($"MatchStatistics-{matchEntity.Id}"))
            .Returns(mockStatisticsGroup.Object);

        // Act
        await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Task>(
            _sut,
            "BroadcastMatchStatistics",
            matchEvent,
            matchEntity
        );

        // Assert
        mockStatisticsGroup.Verify(
            x =>
                x.SendMatchStatisticsAsync(
                    "match_statistics_update",
                    matchEntity.Id,
                    It.IsAny<object>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CacheMatchEvent_WithValidEvent_ShouldCacheSuccessfully()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            match_id = "123",
            event_index = 1,
            action = "goal",
        };

        // Act
        await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Task>(
            _sut,
            "CacheMatchEvent",
            matchEvent
        );

        // Assert - No exception should be thrown and method should complete successfully
        // Since this is a private method that caches internally, we can't directly verify the cache
        // but we can ensure no errors occurred during caching
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessMatchEventWithEntity_WithValidData_ShouldProcessSuccessfully()
    {
        // Arrange
        var matchEvent = new FootballMatchEvent
        {
            match_id = "123",
            event_index = 1,
            action = "goal",
        };

        var matchEntity = new Match
        {
            Id = 123,
            HomeTeamInMatchName = "Team A",
            AwayTeamInMatchName = "Team B",
            CreatorId = "0", // Set to a valid test value
        };

        // Act
        await TestUtils.InvokePrivateMethodAsync<MatchEventRabbitMqClient, Task>(
            _sut,
            "ProcessMatchEventWithEntity",
            matchEvent,
            matchEntity
        );

        // Assert
        _mockEventAnalysisService.Verify(
            x => x.UpdateMatchStatistics(matchEvent, It.IsAny<MatchEvents>(), matchEntity, false),
            Times.Once
        );
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}
