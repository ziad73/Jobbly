using Jobbly.Domain.Enums;

namespace Jobbly.Domain.Entities;

public sealed class SavedJob
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CanonicalJobId { get; private set; }

    public SavedJobStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? AppliedAt { get; private set; }
    public DateTime? FollowUpAt { get; private set; }

    public DateTime SavedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SavedJob()
    {
    }

    public static SavedJob Create(Guid userId, Guid canonicalJobId)
    {
        var now = DateTime.UtcNow;

        return new SavedJob
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CanonicalJobId = canonicalJobId,
            Status = SavedJobStatus.Saved,
            SavedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        SavedJobStatus? status = null,
        string? notes = null,
        DateTime? followUpAt = null)
    {
        var now = DateTime.UtcNow;

        if (status is { } newStatus)
        {
            Status = newStatus;
            AppliedAt ??= newStatus == SavedJobStatus.Applied ? now : AppliedAt;
        }

        Notes = notes ?? Notes;
        FollowUpAt = followUpAt ?? FollowUpAt;
        UpdatedAt = now;
    }
}