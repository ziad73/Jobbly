using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jobbly.Api.Health;

/// <summary>
/// Readiness signal for Hangfire's Postgres storage: is the hangfire schema
/// initialized and readable? Catches a running API whose background-job
/// storage is missing or unreachable.
/// </summary>
public sealed class HangfireStorageHealthCheck(JobblyDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 FROM hangfire.schema", cancellationToken);
            return HealthCheckResult.Healthy("Hangfire storage readable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Hangfire storage unreadable.", ex);
        }
    }
}