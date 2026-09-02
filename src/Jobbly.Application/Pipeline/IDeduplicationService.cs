using Jobbly.Domain.Entities;

namespace Jobbly.Application.Pipeline;

// Cross-provider dedup: given a new job, finds whether an equivalent job from a
// different provider is already known (matching DedupFingerprint) so the new
// job can link to its canonical instead of creating a new one.
public interface IDeduplicationService
{
    Task<DedupResult> ResolveAsync(Job job, CancellationToken cancellationToken = default);
}