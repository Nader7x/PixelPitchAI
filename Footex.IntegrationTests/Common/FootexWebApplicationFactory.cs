using System.Net.Http.Headers;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Footex.IntegrationTests.Common;

public class FootexWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;

    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("Footex_Api")
            .WithUsername("postgres")
            .WithPassword("0000")
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();
        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var dbContext = scopedServices.GetRequiredService<FootballDbContext>();
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
        var dataSeeder = scopedServices.GetRequiredService<DataSeeder>();

        await dbContext.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        await dataSeeder.SeedAllAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
            await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(
            Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Footex")
            )
        );
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddUserSecrets<Program>();
            }
        );
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<FootballDbContext>)
            );

            if (descriptor != null)
                services.Remove(descriptor);

            if (_postgreSqlContainer != null)
                services.AddDbContext<FootballDbContext>(options =>
                {
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
                });

            services.AddLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Warning));
            var rabbitMqClientDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(MatchEventRabbitMqClient)
            );
            if (rabbitMqClientDescriptor != null)
            {
                services.Remove(rabbitMqClientDescriptor);
            }
            services.AddSingleton<IHostedService>(sp =>
            {
                var hubContext = sp.GetRequiredService<IHubContext<MatchHub, IMatchHub>>();
                var logger = sp.GetRequiredService<ILogger<MatchEventRabbitMqClient>>();
                var rabbitMqOptions =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Infrastructure.Configuration.RabbitMqOptions>>();
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var performanceMonitoringService =
                    sp.GetRequiredService<IPerformanceMonitoringService>();
                var liveMatchStatisticsService =
                    sp.GetRequiredService<ILiveMatchStatisticsService>();

                return new DummyMatchEventRabbitMqClient(
                    hubContext,
                    logger,
                    rabbitMqOptions,
                    serviceScopeFactory,
                    performanceMonitoringService,
                    liveMatchStatisticsService
                );
            });
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost:7082"),
            }
        );
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
        };
        await userManager.CreateAsync(user, "Password123!");

        var (token, _) = await tokenService.GenerateTokenAsync(user, "0.0.0.0");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
