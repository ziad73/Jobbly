using Jobbly.Domain.Enums;

namespace Jobbly.Application.Jobs;

// Describes a search over the deduplicated job feed. Every field is optional;
// omitted filters are ignored. Bound from the GET /api/jobs query string.
public sealed record JobSearchQuery
{
    public string? Q { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public SeniorityLevel? Seniority { get; init; }
    public RemoteType? Remote { get; init; }
    public string? Location { get; init; }
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
    public string? SalaryCurrency { get; init; }
    public JobSearchSort Sort { get; init; } = JobSearchSort.Relevance;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PageSizeOptions.DefaultPageSize;
}

public static class PageSizeOptions
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
