namespace Jobbly.Application.Common;

// Port for invalidating cached job-search responses. Implemented at the
// composition root (Api) over OutputCache; the orchestrator calls it after a
// successful ingestion so scheduled and manual runs both stay fresh.
public interface ICacheInvalidator
{
    Task EvictJobsAsync(CancellationToken cancellationToken = default);
}