using System.ComponentModel.DataAnnotations;

namespace Jobbly.Infrastructure.Config;

public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    /// <summary>Fallback schedule for providers that don't define their own interval.</summary>
    [Range(1, 10_080)]
    public int DefaultRefreshIntervalMinutes { get; init; } = 180;

    /// <summary>Polly retry ceiling per connector call (TECHNICAL-DESIGN §3.4).</summary>
    [Range(0, 20)]
    public int MaxRetryAttempts { get; init; } = 5;

    /// <summary>Stale-listing soft-delete SLA in days (§3.5).</summary>
    [Range(1, 365)]
    public int StaleArchiveAfterDays { get; init; } = 30;
}
