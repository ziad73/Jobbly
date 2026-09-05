using System.ComponentModel.DataAnnotations;
using Jobbly.Application.Validation;
using Jobbly.Domain.Enums;

namespace Jobbly.Application.Jobs;

// Describes a search over the deduplicated job feed. Every field is optional;
// omitted filters are ignored. Bound from the GET /api/jobs query string.
public sealed record JobSearchQuery
{
    [MaxLength(200)]
    public string? Q { get; init; }
    // string[] so ASP.NET Minimal API can bind repeated ?tags=x&tags=y from the query string.
    [MaxItems(20)]
    public string[]? Tags { get; init; }
    [EnumDataType(typeof(SeniorityLevel))]
    public SeniorityLevel? Seniority { get; init; }
    [EnumDataType(typeof(RemoteType))]
    public RemoteType? Remote { get; init; }
    [MaxLength(100)]
    public string? Location { get; init; }
    [Range(0, 1_000_000)]
    public int? SalaryMin { get; init; }
    [Range(0, 1_000_000)]
    public int? SalaryMax { get; init; }
    [StringLength(3)]
    public string? SalaryCurrency { get; init; }
    public JobSearchSort? Sort { get; init; }
    [Range(1, 10_000)]
    public int? Page { get; init; }
    [Range(1, 100)]
    public int? PageSize { get; init; }
}

public static class PageSizeOptions
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
