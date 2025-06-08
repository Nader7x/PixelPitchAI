using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Footex.PerformanceTests.Common;

namespace Footex.PerformanceTests.Common;

public static class TestConfigurationHelper
{
    private static IConfiguration? _configuration;
    private static PerformanceTestSettings? _settings;

    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            return _configuration;
        }
    }

    public static PerformanceTestSettings Settings
    {
        get
        {
            if (_settings == null)
            {
                _settings = new PerformanceTestSettings();
                Configuration.GetSection(PerformanceTestSettings.SectionName).Bind(_settings);
            }
            return _settings;
        }
    }

    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        services.AddSingleton(Configuration);
        services.AddSingleton(Settings);
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        return services.BuildServiceProvider();
    }

    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static string GetResultsDirectory(string testType)
    {
        var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
        var testDir = Path.Combine(baseDir, testType, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        EnsureDirectoryExists(testDir);
        return testDir;
    }

    public static void LogTestStart(string testName, ILogger logger)
    {
        logger.LogInformation("=== Starting Performance Test: {TestName} ===", testName);
        logger.LogInformation("Test started at: {StartTime}", DateTime.Now);
        logger.LogInformation("Configuration: {@Settings}", Settings);
    }

    public static void LogTestEnd(string testName, ILogger logger, TimeSpan duration)
    {
        logger.LogInformation("=== Completed Performance Test: {TestName} ===", testName);
        logger.LogInformation("Test completed at: {EndTime}", DateTime.Now);
        logger.LogInformation("Test duration: {Duration}", duration);
    }
}
