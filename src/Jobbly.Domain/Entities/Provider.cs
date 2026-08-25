using Jobbly.Domain.Enums;

namespace Jobbly.Domain.Entities;

public sealed class Provider
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public IntegrationType IntegrationType { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public int RefreshIntervalMinutes { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public string? LastError { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Provider()
    {
    }

    public static Provider Create(
        string name,
        string slug,
        IntegrationType integrationType,
        string baseUrl,
        int refreshIntervalMinutes)
    {
        return new Provider
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug,
            IntegrationType = integrationType,
            BaseUrl = baseUrl,
            IsActive = true,
            RefreshIntervalMinutes = refreshIntervalMinutes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkSynced(DateTime syncedAtUtc)
    {
        LastSyncedAt = syncedAtUtc;
        LastError = null;
        ConsecutiveFailures = 0;
    }

    public void MarkFailed(string error, DateTime failedAtUtc)
    {
        LastError = error;
        ConsecutiveFailures++;
        LastSyncedAt = failedAtUtc;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
