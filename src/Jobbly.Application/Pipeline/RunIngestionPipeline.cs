using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Pipeline;

// The pipeline has five jobs to do: fetch from a source, normalize, deduplicate across sources, enrich, and save. 
// Each of those four steps is defined as a port (an interface) — the orchestrator coordinates them, but doesn't
// know how they work internally. 

// each provider has its own:
//  - connector implementation: call the API then map its raw JSON into RawJobDto
//  - normalizer implementation:  turns RawJobDto into a Job entity

// The orchestrator handles the full flow including: skip-and-update when the same source posts the same job twice, 
// record a run in the pipeline_runs table even on failure, and never crash the scheduler if one source breaks.

public sealed class RunIngestionPipeline
{
    private readonly IJobblyDbContext _dbContext;
    private readonly IEnumerable<IJobConnector> _connectors;
    private readonly IJobNormalizer _normalizer;
    private readonly IDeduplicationService _deduplicationService;
    private readonly IEnrichmentService _enrichmentService;

    public RunIngestionPipeline(
        IJobblyDbContext dbContext,
        IEnumerable<IJobConnector> connectors,
        IJobNormalizer normalizer,
        IDeduplicationService deduplicationService,
        IEnrichmentService enrichmentService)
    {
        _dbContext = dbContext;
        _connectors = connectors;
        _normalizer = normalizer;
        _deduplicationService = deduplicationService;
        _enrichmentService = enrichmentService;
    }

    // Orchestrates one end-to-end ingestion run for a single provider:
    // fetch -> normalize -> (within-provider update | cross-provider dedup) -> enrich -> persist.
    // A failure is recorded on the run + provider and returned, never thrown - one
    // broken provider never cascades to the scheduler or other providers.
    public async Task<PipelineRunResult?> ExecuteAsync(string providerSlug, CancellationToken cancellationToken = default)
    {
        var provider = await _dbContext.Providers
            .SingleOrDefaultAsync(p => p.Slug == providerSlug, cancellationToken);

        if (provider is null || !provider.IsActive)
        {
            return null;
        }

        var connector = _connectors.FirstOrDefault(c => c.ProviderSlug == providerSlug);
        if (connector is null)
        {
            return null;
        }

        var run = PipelineRun.Start(provider.Id, provider.Slug);
        _dbContext.PipelineRuns.Add(run);

        try
        {
            // 1. Fetch raw jobs from the provider's source
            var rawJobs = await connector.FetchAsync(cancellationToken);
            run.RecordFetch(rawJobs.Count);

            var created = 0;
            var updated = 0;
            var deduplicated = 0;

            // New canonicals are created alongside their primary job. Each
            // references the other (PrimaryJobId -> Job, Job.CanonicalJobId ->
            // CanonicalJob), so they can't be inserted in the same SaveChanges
            // (circular FK dependency). Collect them and insert in a second save,
            // after the jobs rows exist.
            var newCanonicals = new List<(Job job, CanonicalJob canonical)>();

            foreach (var raw in rawJobs)
            {
                var existing = await _dbContext.Jobs
                    .SingleOrDefaultAsync(
                        j => j.ProviderId == provider.Id && j.ExternalId == raw.ExternalId,
                        cancellationToken);

                if (existing is not null)
                {
                    _normalizer.Update(existing, raw);
                    _enrichmentService.Enrich(existing);
                    updated++;
                    continue;
                }
                // 2. Normalize and update the existing job
                var job = _normalizer.Normalize(raw, provider.Id);
                _dbContext.Jobs.Add(job);
                // 3. Deduplicate across providers
                var dedup = await _deduplicationService.ResolveAsync(job, cancellationToken);

                if (dedup.IsDuplicate && dedup.CanonicalJobId is Guid canonicalId)
                {
                    var canonical = await _dbContext.CanonicalJobs
                        .SingleAsync(c => c.Id == canonicalId, cancellationToken);

                    job.AttachToCanonical(canonical.Id);
                    canonical.LinkSource(1.0f);
                    deduplicated++;
                }
                else
                {
                    var canonical = CanonicalJob.Create(job);
                    // The canonical's PrimaryJobId references the new job, and the
                    // job's CanonicalJobId references the new canonical. The
                    // back-link (AttachToCanonical) is applied in the second save.
                    newCanonicals.Add((job, canonical));
                    created++;
                }
                // 4. Enrich
                _enrichmentService.Enrich(job);
            }

            run.Complete(created, updated, deduplicated);
            provider.MarkSynced(DateTime.UtcNow);
            // 5. Persist to the database - jobs/updates first, canonicals second
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var (job, canonical) in newCanonicals)
            {
                _dbContext.CanonicalJobs.Add(canonical);
                job.AttachToCanonical(canonical.Id);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToResult(run);
        }
        catch (Exception ex)
        {
            run.Fail(ex.Message);
            provider.MarkFailed(ex.Message, DateTime.UtcNow);

            // May partially persist jobs added before the failure; the run status
            // records the failure so the next scheduled run reconciles them.
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToResult(run);
        }
    }

    private static PipelineRunResult ToResult(PipelineRun run) => new(
        run.ProviderSlug,
        run.Status,
        run.JobsFetched,
        run.JobsCreated,
        run.JobsUpdated,
        run.JobsDeduplicated,
        run.StartedAt,
        run.FinishedAt,
        run.ErrorMessage);
}
