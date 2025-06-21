using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Footex.Extensions;

public static class MigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FootballDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting database migration...");
            db.Database.EnsureCreated(); // This will create the database if it doesn't exist
            db.Database.Migrate();
            logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw; // Re-throw if you want the application to fail on migration error
        }

        return app;
    }
}