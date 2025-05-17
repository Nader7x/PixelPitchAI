using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using RabbitMQ.Client.Events;


namespace Infrastructure.Services
{
    public class MatchEventRabbitMqClient : BackgroundService
    {
        private readonly ILogger<MatchEventRabbitMqClient> _logger;
        private readonly RabbitMqSettings _settings;
        private readonly IHubContext<MatchHub> _hubContext;
        private IConnection _connection;
        private IChannel _channel;
        private int _eventSequence;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Application.Interfaces.IEventAnalysisService _eventAnalysisService;


        public MatchEventRabbitMqClient(
            ILogger<MatchEventRabbitMqClient> logger,
            IOptions<RabbitMqSettings> settings,
            IHubContext<MatchHub> hubContext, IConnection connection, IChannel channel, IUnitOfWork unitOfWork,
            EventAnalysisService eventAnalysisService)
        {
            _logger = logger;
            _settings = settings.Value;
            _hubContext = hubContext;
            _connection = connection;
            _channel = channel;
            _unitOfWork = unitOfWork;
            _eventAnalysisService = eventAnalysisService;

            _ = InitializeRabbitMq();
        }

        private async Task InitializeRabbitMq()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    Port = _settings.Port
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: "match_events",
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);
                await _channel.QueueDeclareAsync(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                );
                await _channel.QueueBindAsync(
                    queue: _settings.QueueName,
                    exchange: "match_events",
                    routingKey: "match.events");

                _logger.LogInformation(
                    "RabbitMQ connected to {SettingsHostName}:{SettingsPort}/{SettingsVirtualHost}", _settings.HostName,
                    _settings.Port, _settings.VirtualHost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not initialize RabbitMQ connection");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Received match event: {Message}", message);

                    // Deserialize directly from JSON
                    var matchEvent = JsonSerializer.Deserialize<FootballMatchEvent>(message);

                    if (matchEvent != null)
                    {
                        await StoreMatchEvent(matchEvent).WaitAsync(stoppingToken);
                        await BroadcastEventToClients(matchEvent).WaitAsync(stoppingToken);
                    }

                    // Acknowledge message
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing match event");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            _channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer, cancellationToken: stoppingToken);

            return Task.CompletedTask;
        }


        private async Task StoreMatchEvent(FootballMatchEvent matchEvent)
        {
            try
            {
                // Process the event with analysis service first
                await _eventAnalysisService.ProcessEventAsync(matchEvent);

                // Get the match events entity
                var matchEventsEntity = await _unitOfWork.MatchEvents.GetByMatchIdAsync(matchEvent.match_id);

                if (matchEventsEntity == null)
                {
                    // Create new match events entity
                    matchEventsEntity = new MatchEvents
                    {
                        MatchId = matchEvent.match_id,
                        EventsJson = "[]", // Start with empty array
                        LastUpdated = DateTime.UtcNow,
                        TotalEvents = 0
                    };
                    await _unitOfWork.MatchEvents.AddAsync(matchEventsEntity);
                }

                // Deserialize existing events
                var events = matchEventsEntity.GetEvents<List<FootballMatchEvent>>()
                             ?? new List<FootballMatchEvent>();

                // Add new event
                events.Add(matchEvent);

                // Update the event sequence for tracking
                _eventSequence = Math.Max(_eventSequence, matchEvent.event_index + 1);

                // Update match statistics based on the event
                await _eventAnalysisService.UpdateMatchStatistics(matchEvent, matchEventsEntity);
                // Save the updated events
                matchEventsEntity.SetEvents(events);
                matchEventsEntity.TotalEvents = events.Count;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Stored match event {EventIndex} for match {MatchId}",
                    matchEvent.event_index, matchEvent.match_id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing match event {EventIndex} for match {MatchId}",
                    matchEvent.event_index, matchEvent.match_id);
            }
        }

        private async Task BroadcastEventToClients(FootballMatchEvent matchEvent)
        {
            await _hubContext.Clients.Group(matchEvent.match_id.ToString())
                .SendAsync("ReceiveMatchEvent", matchEvent);

            _logger.LogInformation("Broadcasted match event {FootballMatchEvent} to clients", matchEvent);
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }
    

    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; } = 5672;
        public string QueueName { get; set; } = "match_events_queue";
    }
}