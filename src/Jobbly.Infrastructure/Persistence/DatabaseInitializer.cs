using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        // seed provider table with configured sources that have a registered connector
        await SeedProvidersAsync(scope.ServiceProvider, context, logger, cancellationToken);

        // seed Identity roles so registration can always assign the default role
        await SeedRolesAsync(scope.ServiceProvider, logger, cancellationToken);
    }
    

    // seed provider table with configured sources that have a registered connector, 
    // so a fresh database is ready for ingestion on first startup.
    // Idempotent - providers already present are left untouched.
    private static async Task SeedProvidersAsync(
        IServiceProvider serviceProvider,
        JobblyDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var sources = serviceProvider.GetRequiredService<IOptions<ProvidersOptions>>().Value.Sources;

        // Only sources with a working connector (by slug) become DB rows, so no
        // phantom rows or silent no-op scheduled jobs for unbuilt connectors.
        var connectedSlugs = serviceProvider
            .GetRequiredService<IEnumerable<IJobConnector>>()
            .Select(c => c.ProviderSlug)
            .ToHashSet();

        var existingSlugs = await context.Providers
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken);

        var anyAdded = false;
        foreach (var (slug, config) in sources)
        {
            if (!connectedSlugs.Contains(slug) || existingSlugs.Contains(slug))
            {
                continue;
            }

            context.Providers.Add(Provider.Create(
                config.Name,
                slug,
                IntegrationType.PublicApi,
                config.BaseUrl,
                config.RefreshIntervalMinutes));
            anyAdded = true;
        }

        if (anyAdded)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} provider(s).", sources.Count(s => connectedSlugs.Contains(s.Key)));
        }
    }

    // Seed Identity roles. Idempotent - roles already present are left untouched,
    // so every registration can assign the default role unconditionally.
    private static async Task SeedRolesAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in new[] { ApplicationRoles.User, ApplicationRoles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    logger.LogWarning("Could not seed role '{Role}': {Errors}.",
                        role, string.Join(';', result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
