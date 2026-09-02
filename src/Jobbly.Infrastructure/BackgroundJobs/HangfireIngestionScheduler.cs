using Hangfire;
using Jobbly.Application;
using Jobbly.Application.Common;
using Jobbly.Application.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobbly.Infrastructure.BackgroundJobs;

// Recurring Hangfire job entry point for ingestion. Lives in Infrastructure so
// Hangfire concerns never leak into the Application layer. Resolved by
// Hangfire's AspNetCoreJobActivator within a per-execution DI scope, which is
// why RunIngestionPipeline (and its scoped deps: DbContext, normalizer, ...)
// are constructor-injected here rather than obtained from a static provider.
public sealed class HangfireIngestionScheduler
{
    private readonly RunIngestionPipeline _pipeline;

    public HangfireIngestionScheduler(RunIngestionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task ExecuteAsync(string providerSlug)
    {
        await _pipeline.ExecuteAsync(providerSlug, CancellationToken.None);
    }

    // Registers one recurring job per active Provider, keyed by slug so
    // re-registration on startup is an idempotent upsert. Runs after migrations
    // are applied so the provider data is queryable.
    //
    // Uses the DI-resolved IRecurringJobManager rather than the static
    // RecurringJob helper: at startup the static JobStorage.Current has not been
    // set yet, which throws InvalidOperationException. IRecurringJobManager is
    // resolved from the service provider and works regardless.
    public static void RegisterRecurringJobs(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IJobblyDbContext>();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        var providers = dbContext.Providers
            .Where(p => p.IsActive)
            .ToListAsync()
            .GetAwaiter()
            .GetResult();
        // for each active provider, register a recurring job -Run Ingestion Pipeline-
        foreach (var provider in providers)
        {
            recurringJobs.AddOrUpdate<HangfireIngestionScheduler>(
                provider.Slug,
                job => job.ExecuteAsync(provider.Slug),
                BuildCronInterval(provider.RefreshIntervalMinutes),
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }

    // Converts a refresh interval in whole minutes into a valid 5-field CRON
    // expression. A bare "*/N" is only valid while N stays within the field's
    // range, so hour-aligned intervals (e.g. 180 min = every 3 hours) are
    // expanded to "0 */H * * *". Falls back to minute-granularity when the
    // value fits a minute step.
    private static string BuildCronInterval(int refreshIntervalMinutes)
    {
        if (refreshIntervalMinutes > 0 && refreshIntervalMinutes % 60 == 0)
        {
            var hours = refreshIntervalMinutes / 60;
            if (24 % hours == 0)
            {
                return $"0 */{hours} * * *";
            }
        }

        var step = Math.Clamp(refreshIntervalMinutes, 1, 60);
        return $"*/{step} * * * *";
    }
}
