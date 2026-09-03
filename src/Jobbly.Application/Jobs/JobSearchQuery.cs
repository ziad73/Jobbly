using Jobbly.Domain.Enums;

namespace Jobbly.Application.Jobs;

// Describes a search over the deduplicated job feed. Every field is optional;
// omitted filters are ignored. Bound from the GET /api/jobs query string.
public sealed record JobSearchQuery
{
    public string? Q { get; init; }
    // string[] so ASP.NET Minimal API can bind repeated ?tags=x&tags=y from the query string.
    public string[]? Tags { get; init; }
    public SeniorityLevel? Seniority { get; init; }
    public RemoteType? Remote { get; init; }
    public string? Location { get; init; }
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
    public string? SalaryCurrency { get; init; }
    public JobSearchSort? Sort { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public static class PageSizeOptions
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
