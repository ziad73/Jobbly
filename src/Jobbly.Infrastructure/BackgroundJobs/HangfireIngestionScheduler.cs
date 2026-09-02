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
    public static void RegisterRecurringJobs(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IJobblyDbContext>();

        var providers = dbContext.Providers
            .Where(p => p.IsActive)
            .ToListAsync()
            .GetAwaiter()
            .GetResult();
        // for each active provider, register a recurring job -Run Ingestion Pipeline-
        foreach (var provider in providers)
        {
            RecurringJob.AddOrUpdate<HangfireIngestionScheduler>(
                provider.Slug,
                job => job.ExecuteAsync(provider.Slug),
                $"0 */{provider.RefreshIntervalMinutes} * * *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }
}
