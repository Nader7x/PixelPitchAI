using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Repositories;
using Infrastructure.Identity;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Serilog.Sinks.PostgreSQL;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection
        AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
    {
        // Register DbContext with PostgreSQL
        services.AddDbContext<FootballDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamSeasonsRepository, TeamSeasonsRepository>();
        services.AddScoped<IMatchEventsRepository, MatchEventsRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<ICoachRepository,CoachRepository>();
        services.AddScoped<IStadiumsRepository, StadiumsRepository>();
        // Register identity services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISearchService, SearchService>();
        // Register EventAnalysis service
        services.AddScoped<IEventAnalysisService ,EventAnalysisService>();
        
        // Register RabbitMQ connection and client
        services.AddHostedService<MatchEventRabbitMqClient>();
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Serilog column mappings for PostgreSQL
        if (configuration.GetSection("SerilogColumnOptions").Exists())
        {
            // Create dictionary of column writers for PostgreSQL
            IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
            {
                { "message", new RenderedMessageColumnWriter() },
                { "message_template", new MessageTemplateColumnWriter() },
                { "level", new LevelColumnWriter() },
                { "timestamp", new TimestampColumnWriter() },
                { "exception", new ExceptionColumnWriter() },
                { "properties", new PropertiesColumnWriter() },
                { "source_context", new SinglePropertyColumnWriter("SourceContext") }
            };

            // Register the column writers for use in Program.cs
            services.AddSingleton(columnWriters);
        }
   

        return services;
    }
}
