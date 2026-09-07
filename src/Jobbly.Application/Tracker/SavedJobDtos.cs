using System.ComponentModel.DataAnnotations;
using Jobbly.Domain.Enums;

namespace Jobbly.Application.Tracker;

public sealed record SaveJobRequest(
    [property: Required] Guid CanonicalJobId);

public sealed record PatchSavedJobRequest(
    [property: EnumDataType(typeof(SavedJobStatus))] SavedJobStatus? Status,
    [property: MaxLength(2000)] string? Notes,
    DateTime? FollowUpAt);

public sealed record SavedJobItemDto(
    Guid CanonicalJobId,
    string Title,
    string Company,
    string? Location,
    RemoteType RemoteType,
    SeniorityLevel Seniority,
    string? SourceUrl);

public sealed record SavedJobDto(
    Guid Id,
    Guid CanonicalJobId,
    SavedJobStatus Status,
    string? Notes,
    DateTime? AppliedAt,
    DateTime? FollowUpAt,
    DateTime SavedAt,
    DateTime UpdatedAt,
    SavedJobItemDto? Job);

public sealed record SavedJobListResponse
{
    public IReadOnlyList<SavedJobDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public enum SaveJobOutcome
{
    Created,
    Duplicate,
    CanonicalNotFound
}

public sealed record SaveJobAttempt(SaveJobOutcome Outcome, SavedJobDto? SavedJob)
{
    public static SaveJobAttempt Created(SavedJobDto savedJob) => new(SaveJobOutcome.Created, savedJob);

    public static SaveJobAttempt Duplicate() => new(SaveJobOutcome.Duplicate, null);

    public static SaveJobAttempt CanonicalNotFound() => new(SaveJobOutcome.CanonicalNotFound, null);
}