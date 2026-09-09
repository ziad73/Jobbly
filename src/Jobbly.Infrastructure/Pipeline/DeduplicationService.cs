using Jobbly.Application.Common;
using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Pipeline;

public sealed class DeduplicationService : IDeduplicationService
{
    // Minimum trigram similarity for the fuzzy second pass. Calibrated against
    // live titles: 0.8 merged distinct jobs ("Product Manager, Payments" vs
    // "Staff Product Manager, Payments" score 0.800), while every sampled pair
    // at >= 0.95 is a true repost/reword ("Enterprise Account Executive
    // -Grower" variants score 1.000). Conservative by design: a missed merge
    // just means two listings, a false merge loses a real job.
    private const double SimilarityThreshold = 0.95;

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

        if (match?.CanonicalJobId is Guid canonicalId)
        {
            return new DedupResult(true, canonicalId);
        }

        return await ResolveFuzzyAsync(job, cancellationToken);
    }

    // Second pass: same-company title similarity for reposts and rewordings
    // the exact fingerprint misses (same-board reposts carry distinct external
    // ids, so pass 1 never sees them). Same-company only - never merge across
    // employers. Requires the pg_trgm extension (EnablePgTrgm migration).
    private async Task<DedupResult> ResolveFuzzyAsync(Job job, CancellationToken cancellationToken)
    {
        var company = job.CompanyName.ToLower();
        var fuzzy = await _dbContext.Jobs
            .Where(j => j.Id != job.Id
                && j.CompanyName.ToLower() == company
                && EF.Functions.TrigramsSimilarity(j.Title, job.Title) >= SimilarityThreshold)
            .OrderByDescending(j => EF.Functions.TrigramsSimilarity(j.Title, job.Title))
            .ThenBy(j => j.IngestedAt)
            .Select(j => new { j.CanonicalJobId })
            .FirstOrDefaultAsync(cancellationToken);

        return fuzzy?.CanonicalJobId is Guid canonicalId
            ? new DedupResult(true, canonicalId)
            : new DedupResult(false);
    }
}
