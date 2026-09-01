using Jobbly.Domain.Enums;

namespace Jobbly.Application.Pipeline;

// run summary DTO (status, counts, timestamps, error)
public sealed record PipelineRunResult(
    string ProviderSlug,
    PipelineRunStatus Status,
    int JobsFetched,
    int JobsCreated,
    int JobsUpdated,
    int JobsDeduplicated,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? ErrorMessage = null);
