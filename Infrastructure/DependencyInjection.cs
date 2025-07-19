using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Repositories;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.EventProcessors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.PostgreSQL;
using StackExchange.Redis;

// Add this using

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure Redis Cache
        services.Configure<RedisCacheOptions>(options =>
        {
            configuration.GetSection(RedisCacheOptions.SectionName).Bind(options);

            // Set the remote connection string from Connection Strings section if available
            if (
                string.IsNullOrEmpty(options.RemoteConnectionString)
                && !string.IsNullOrEmpty(configuration.GetConnectionString("RemoteRedisConnection"))
            )
                options.RemoteConnectionString =
                    configuration.GetConnectionString("RemoteRedisConnection") ?? string.Empty;

            // Use the configured local connection string or default to localhost
            if (string.IsNullOrEmpty(options.LocalConnectionString))
                options.LocalConnectionString = "localhost:6379";
        });

        // Create the Redis connection directly without relying on the service provider
        // to avoid the circular dependency that causes "Logger already frozen" errors
        var redisOptions = new RedisCacheOptions();
        configuration.GetSection(RedisCacheOptions.SectionName).Bind(redisOptions);

        var remoteConnectionString = redisOptions.RemoteConnectionString;
        var remoteRedisPassword = configuration.GetConnectionString("RemoteRedisPassword") ?? "";
        if (string.IsNullOrEmpty(remoteConnectionString))
            remoteConnectionString =
                configuration.GetConnectionString("RemoteRedisConnection") ?? "";

        var localConnectionString = redisOptions.LocalConnectionString;
        if (string.IsNullOrEmpty(localConnectionString))
            localConnectionString = "localhost:6379";

        // Create the multiplexer directly
        ConnectionMultiplexer redis;
        try
        {
            // Try remote connection first
            if (!string.IsNullOrEmpty(remoteConnectionString))
                redis = ConnectionMultiplexer.Connect(
                    new ConfigurationOptions
                    {
                        EndPoints = { { remoteConnectionString, 17264 } },
                        User = "default",
                        Password = remoteRedisPassword,
                        AbortOnConnectFail = false,
                        ConnectRetry = 3,
                        ConnectTimeout = 5000,
                    }
                );
            else
                // Fall back to local immediately if no remote configured
                throw new Exception("No remote connection string available");
        }
        catch
        {
            // Fall back to local
            redis = ConnectionMultiplexer.Connect(
                new ConfigurationOptions
                {
                    EndPoints = { localConnectionString },
                    AbortOnConnectFail = false,
                    ConnectRetry = 2,
                    ConnectTimeout = 3000,
                }
            );
        }

        // Register Redis connection multiplexer as singleton
        services.AddSingleton<IConnectionMultiplexer>(redis);

        // Register the DataSeeder
        services.AddScoped<DataSeeder>();

        // Register the SmtpClientWrapper
        services.AddScoped<ISmtpClient, SmtpClientWrapper>();

        // Configure Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName =
                configuration.GetValue<string>($"{RedisCacheOptions.SectionName}:InstanceName")
                ?? "Footex_";
            options.ConnectionMultiplexerFactory = () =>
                Task.FromResult<IConnectionMultiplexer>(redis);
        });

        // Register Redis cache service implementation
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Also register DbContext for services that need direct context access
        services.AddDbContext<FootballDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // Register repositories
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamSeasonsRepository, TeamSeasonsRepository>();
        services.AddScoped<IMatchEventsRepository, MatchEventsRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<ICoachRepository, CoachRepository>();
        services.AddScoped<IStadiumsRepository, StadiumsRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        // Register identity services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailService, EmailService>();

        // Register the unified search service with both interfaces
        services.AddScoped<ISearchService, UnifiedSearchService>();
        services.AddScoped<IAdvancedSearchService>(provider =>
            (UnifiedSearchService)provider.GetRequiredService<ISearchService>()
        );

        // Configure RabbitMQ options
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        // Register EventAnalysis service
        services.AddSingleton<IEventAnalysisService, EventAnalysisService>();

        // Register the MatchEventRabbitMqClient as a hosted service
        services.AddSingleton<MatchEventRabbitMqClient>();
        services.AddHostedService(provider =>
            provider.GetRequiredService<MatchEventRabbitMqClient>()
        );

        // Register performance monitoring service
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Register live match statistics service
        services.AddSingleton<ILiveMatchStatisticsService, LiveMatchStatisticsService>();
        // Register Event Processors
        services.AddSingleton<IEventProcessor, PassEventProcessor>();
        services.AddSingleton<IEventProcessor, ShotEventProcessor>();
        services.AddSingleton<IEventProcessor, FoulEventProcessor>();
        services.AddSingleton<IEventProcessor, CornerEventProcessor>();
        services.AddSingleton<IEventProcessor, OffsideEventProcessor>();
        services.AddSingleton<IEventProcessor, GoalkeeperEventProcessor>();
        services.AddSingleton<IEventProcessor, DuelEventProcessor>();
        services.AddSingleton<IEventProcessor, ClearanceEventProcessor>();
        services.AddSingleton<IEventProcessor, RecoveryEventProcessor>();
        services.AddSingleton<IEventProcessor, DribbleEventProcessor>();
        services.AddSingleton<IEventProcessor, OwnGoalEventProcessor>();
        services.AddSingleton<IEventProcessor, InterceptionEventProcessor>();
        services.AddSingleton<IEventProcessor, BlockEventProcessor>();
        services.AddSingleton<IEventProcessor, SubstitutionEventProcessor>();
        services.AddSingleton<IEventProcessor, MatchStatusEventProcessor>();
        services.AddSingleton<IEventProcessor, BadBehaviourEventProcessor>();
        services.AddSingleton<IEventProcessor, BallLossEventProcessor>();
        services.AddSingleton<IEventProcessor, PressureEventProcessor>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Serilog column mappings for PostgreSQL
        if (configuration.GetSection("SerilogColumnOptions").Exists())
        {
            // Create dictionary of column writers for PostgreSQL
            IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<
                string,
                ColumnWriterBase
            >
            {
                { "message", new RenderedMessageColumnWriter() },
                { "message_template", new MessageTemplateColumnWriter() },
                { "level", new LevelColumnWriter() },
                { "timestamp", new TimestampColumnWriter() },
                { "exception", new ExceptionColumnWriter() },
                { "properties", new PropertiesColumnWriter() },
                { "source_context", new SinglePropertyColumnWriter("SourceContext") },
            };

            // Register the column writers for use in Program.cs
            services.AddSingleton(columnWriters);
        }

        return services;
    }
}
