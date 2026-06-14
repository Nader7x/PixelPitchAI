using System.Net.Http.Headers;
using System.Reflection;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        var projectDir = GetProjectDirectory();
        Console.WriteLine($"Current directory : {projectDir}");
        builder.UseContentRoot(
           projectDir 
        );
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
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

            // Remove Redis connections and cache services to prevent connection timeouts
            var redisConnDescriptor = services.SingleOrDefault(d =>
                d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("IConnectionMultiplexer")
            );
            if (redisConnDescriptor != null)
                services.Remove(redisConnDescriptor);

            var distCacheDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache)
            );
            if (distCacheDescriptor != null)
                services.Remove(distCacheDescriptor);

            var cacheServiceDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ICacheService)
            );
            if (cacheServiceDescriptor != null)
                services.Remove(cacheServiceDescriptor);

            // Register in-memory cache implementations
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();

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

    private static string GetProjectDirectory()
    {
        // Get the directory of the currently executing assembly (the benchmark runner or test assembly)
        // This will typically be within your project's bin/Release/net8.0 or a BenchmarkDotNet temp folder.
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var currentDirectory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation) ?? @"d:\programming\GitHub\PixelPitchAI\backend");

        Console.WriteLine(
            $"[DEBUG] Starting project directory search from assembly location: {currentDirectory.FullName}");

        // Traverse up the directory tree to find the solution root (which contains the .sln file)
        // This is a robust way to find the top-level directory of your solution.
        while (currentDirectory != null && Directory.GetFiles(currentDirectory.FullName, "*.sln").Length == 0)
        {
            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory == null)
        {
            throw new InvalidOperationException("Could not find the solution root directory (.sln file not found). " +
                                                 "Ensure the test is run from within the solution folder structure.");
        }

        // Now, currentDirectory.FullName is the solution root (e.g., D:\programming\GitHub\Footex\)
        var solutionRoot = currentDirectory.FullName;
        Console.WriteLine($"[DEBUG] Found solution root: {solutionRoot}");

        // Construct the path to your main Footex project folder relative to the solution root.
        var footexProjectDirectory = Path.Combine(solutionRoot, "src", "Footex");

        Console.WriteLine($"[DEBUG] Calculated Footex Project Directory: {footexProjectDirectory}");
        Console.WriteLine(
            $"[DEBUG] Does Calculated Footex Project Directory exist: {Directory.Exists(footexProjectDirectory)}");

        if (!Directory.Exists(footexProjectDirectory))
        {
            throw new InvalidOperationException(
                $"Calculated Footex project directory does not exist: {footexProjectDirectory}. " +
                "Verify the 'Footex' folder exists directly under the solution root.");
        }

        return footexProjectDirectory;
    }

    private class InMemoryCacheService(Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache) : ICacheService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _keys = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            return Task.FromResult(memoryCache.Get<T>(key));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            _keys.TryAdd(key, 0);
            var options = new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _keys.TryRemove(key, out _);
            memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (memoryCache.TryGetValue(key, out object? cachedObj) && cachedObj is T cachedValue)
            {
                return cachedValue;
            }

            var value = await factory();
            await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            var normalizedPattern = pattern.Replace("*", "");
            foreach (var key in _keys.Keys)
            {
                if (key.Contains(normalizedPattern))
                {
                    _keys.TryRemove(key, out _);
                    memoryCache.Remove(key);
                }
            }
            return Task.CompletedTask;
        }
    }
}