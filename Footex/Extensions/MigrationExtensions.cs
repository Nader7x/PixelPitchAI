using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Footex.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FootballDbContext>();

        dbContext.Database.Migrate();
    }
}
