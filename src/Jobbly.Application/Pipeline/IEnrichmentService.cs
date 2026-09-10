using Jobbly.Domain.Entities;

namespace Jobbly.Application.Pipeline;

// Rule-based enrichment in place: tech stack tags, seniority level, remote
// classification, salary normalization. No I/O - pure computation.
// remoteHint carries a provider-supplied workplace signal that takes
// precedence over text rules; null means infer from text.
public interface IEnrichmentService
{
    void Enrich(Job job, string? remoteHint = null);
}