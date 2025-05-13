using Domain.Interfaces;
using Domain.Repositories;
using Infrastructure.Identity;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IPlayerSeasonStatsRepository, PlayerSeasonStatsRepository>();
        services.AddScoped<ITeamSeasonStatsRepository, TeamSeasonStatsRepository>();
        services.AddScoped<IMatchEventsRepository, MatchEventsRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<ICoachRepository,CoachRepository>();
        // Register identity services
        services.AddScoped<IIdentityService, IdentityService>();

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