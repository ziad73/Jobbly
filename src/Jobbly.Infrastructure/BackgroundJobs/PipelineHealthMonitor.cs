using Jobbly.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobbly.Infrastructure.BackgroundJobs;

// Hourly watchdog over ingestion health. Reads provider state maintained by
// the orchestrator (MarkSynced/MarkFailed) and logs alerts - console for now,
// a real sink (Seq/Sentry) later. No notifications leave the process in v1;
// the point is failures stop being silent rows in pipeline_runs.
public sealed class PipelineHealthMonitor(IJobblyDbContext dbContext, ILogger<PipelineHealthMonitor> logger)
{
    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var providers = await dbContext.Providers
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        if (providers.Count == 0)
        {
            logger.LogWarning("Pipeline health: no active providers registered.");
            return;
        }

        foreach (var provider in providers)
        {
            if (provider.ConsecutiveFailures > 0)
            {
                logger.LogWarning(
                    "Pipeline health: '{Slug}' failed {Count} time(s) in a row. Last error: {Error}",
                    provider.Slug, provider.ConsecutiveFailures, provider.LastError ?? "unknown");
            }

            var staleAfter = TimeSpan.FromMinutes(provider.RefreshIntervalMinutes * 2L);
            if (provider.LastSyncedAt is null || now - provider.LastSyncedAt > staleAfter)
            {
                logger.LogWarning(
                    "Pipeline health: '{Slug}' has no run in the last {Elapsed} (interval {Interval} min).",
                    provider.Slug,
                    provider.LastSyncedAt is null ? "never" : $"{now - provider.LastSyncedAt.Value:g} ago",
                    provider.RefreshIntervalMinutes);
            }
        }

        var failing = providers.Count(p => p.ConsecutiveFailures > 0);
        logger.LogInformation(
            "Pipeline health: checked {Total} provider(s), {Failing} failing.",
            providers.Count, failing);
    }
}