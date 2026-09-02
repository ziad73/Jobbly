using Jobbly.Domain.Entities;

namespace Jobbly.Application.Pipeline;

// Rule-based enrichment in place: tech stack tags, seniority level, remote
// classification, salary normalization. No I/O - pure computation.
public interface IEnrichmentService
{
    void Enrich(Job job);
}