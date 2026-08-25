using Jobbly.Domain.Enums;

namespace Jobbly.Domain.Entities;

public sealed class PipelineRun
{
    public Guid Id { get; private set; }
    public Guid ProviderId { get; private set; }
    public Provider Provider { get; private set; } = null!;
    public string ProviderSlug { get; private set; } = null!;
    public PipelineRunStatus Status { get; private set; }

    public int JobsFetched { get; private set; }
    public int JobsCreated { get; private set; }
    public int JobsUpdated { get; private set; }
    public int JobsDeduplicated { get; private set; }
    public int RetryCount { get; private set; }

    public string? ErrorMessage { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private PipelineRun()
    {
    }

    public static PipelineRun Start(Guid providerId, string providerSlug)
    {
        return new PipelineRun
        {
            Id = Guid.CreateVersion7(),
            ProviderId = providerId,
            ProviderSlug = providerSlug,
            Status = PipelineRunStatus.Running,
            StartedAt = DateTime.UtcNow
        };
    }

    public void RecordFetch(int fetchedCount) => JobsFetched = fetchedCount;

    public void RecordRetry() => RetryCount++;

    public void Complete(int createdCount, int updatedCount, int deduplicatedCount)
    {
        JobsCreated = createdCount;
        JobsUpdated = updatedCount;
        JobsDeduplicated = deduplicatedCount;
        Status = PipelineRunStatus.Succeeded;
        FinishedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = PipelineRunStatus.Failed;
        FinishedAt = DateTime.UtcNow;
    }
}
