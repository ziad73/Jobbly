namespace Jobbly.Domain.Entities;

public sealed class CanonicalJob
{
    public Guid Id { get; private set; }
    public Guid PrimaryJobId { get; private set; }
    public Job PrimaryJob { get; private set; } = null!;
    public ICollection<Job> Jobs { get; private set; } = [];

    public int SourceCount { get; private set; }
    public float DedupConfidence { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private CanonicalJob()
    {
    }

    public static CanonicalJob Create(Job primaryJob, float dedupConfidence = 1.0f)
    {
        var now = DateTime.UtcNow;

        return new CanonicalJob
        {
            Id = Guid.CreateVersion7(),
            PrimaryJobId = primaryJob.Id,
            PrimaryJob = primaryJob,
            SourceCount = 1,
            DedupConfidence = dedupConfidence,
            IsArchived = false,
            FirstSeenAt = now,
            LastSeenAt = now
        };
    }

    public void LinkSource(float dedupConfidence)
    {
        SourceCount++;
        LastSeenAt = DateTime.UtcNow;
        DedupConfidence = Math.Max(DedupConfidence, dedupConfidence);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }
}
