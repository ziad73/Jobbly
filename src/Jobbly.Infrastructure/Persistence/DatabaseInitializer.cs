using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobbly.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobblyDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobblyDbContext>>();

        // On a fresh database EF logs an error reading "__EFMigrationsHistory"
        // before that table exists - it catches it and creates the schema. Expected.
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }
}
