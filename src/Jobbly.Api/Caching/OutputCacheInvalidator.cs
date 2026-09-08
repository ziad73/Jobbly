using Jobbly.Application.Common;
using Microsoft.AspNetCore.OutputCaching;

namespace Jobbly.Api.Caching;

/// <summary>
/// ICacheInvalidator over the OutputCache store. Evicts everything tagged
/// "jobs" (currently just GET /api/jobs variants).
/// </summary>
public sealed class OutputCacheInvalidator(IOutputCacheStore store) : ICacheInvalidator
{
    public async Task EvictJobsAsync(CancellationToken cancellationToken = default)
        => await store.EvictByTagAsync("jobs", cancellationToken);
}