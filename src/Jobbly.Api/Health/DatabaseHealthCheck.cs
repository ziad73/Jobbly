using Jobbly.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jobbly.Api.Health;

/// <summary>
/// Readiness signal for Postgres: can EF open a connection to the database?
/// Composition-root check (Api is the only layer allowed to touch the
/// Infrastructure DbContext directly).
/// </summary>
public sealed class DatabaseHealthCheck(JobblyDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Postgres reachable.")
            : HealthCheckResult.Unhealthy("Postgres unreachable.");
    }
}