using Jobbly.Application.Common;
using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Pipeline;

public sealed class DeduplicationService : IDeduplicationService
{
    private readonly IJobblyDbContext _dbContext;

    public DeduplicationService(IJobblyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    // Dedup by comparing the dedup fingerprint of the job
    public async Task<DedupResult> ResolveAsync(Job job, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Jobs
            .Where(j => j.DedupFingerprint == job.DedupFingerprint
                     && j.ProviderId != job.ProviderId)
            .OrderBy(j => j.IngestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
            return new DedupResult(false);

        return match.CanonicalJobId is Guid canonicalId
            ? new DedupResult(true, canonicalId)
            : new DedupResult(false);
    }
}
