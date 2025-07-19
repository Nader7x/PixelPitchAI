using System.Net.Http.Headers;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Infrastructure.Configuration;
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
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Footex.IntegrationTests.Common;

public class FootexWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;
    private string _testUserToken = "";

    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("Footex_Api")
            .WithUsername("postgres")
            .WithPassword("0000")
            .WithPortBinding(0, 5432)
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();

        using var scope = Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var dbContext = scopedServices.GetRequiredService<FootballDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
        // var dataSeeder = scopedServices.GetRequiredService<DataSeeder>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));
        // await dataSeeder.SeedAllAsync();
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
            (_, config) =>
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
            var rabbitMqDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(RabbitMqOptions)
            );
            if (rabbitMqDescriptor != null)
                services.Remove(rabbitMqDescriptor);

            services.AddSingleton<IHostedService, DummyMatchEventRabbitMqClient>(sp =>
            {
                var hubContext = sp.GetRequiredService<IHubContext<MatchHub, IMatchHub>>();
                var logger = sp.GetRequiredService<ILogger<MatchEventRabbitMqClient>>();
                var rabbitMqOptions = sp.GetRequiredService<IOptions<RabbitMqOptions>>();
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

            services.AddLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Warning));
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
        if (string.IsNullOrWhiteSpace(_testUserToken))
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var user = new ApplicationUser
            {
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                FirstName = "Test",
                LastName = "User",
            };
            await userManager.CreateAsync(user, "Password123!");
            await userManager.AddToRoleAsync(user, "Admin");

            var (token, _) = await tokenService.GenerateTokenAsync(user, "0.0.0.0");
            _testUserToken = token;
        }
        if (string.IsNullOrWhiteSpace(_testUserToken))
            throw new InvalidOperationException("Test user token is not set.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _testUserToken
        );

        return client;
    }
}
