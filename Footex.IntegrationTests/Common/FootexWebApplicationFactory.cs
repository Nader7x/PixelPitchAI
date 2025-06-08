using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
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
            .WithDatabase("footex_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null) await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FootballDbContext>));

            if (descriptor != null) services.Remove(descriptor);

            // Add a database context using PostgreSQL test container
            if (_postgreSqlContainer != null)
                services.AddDbContext<FootballDbContext>(options =>
                {
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
                });

            // Disable logging during tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }
}