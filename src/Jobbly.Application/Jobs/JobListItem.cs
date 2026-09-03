using Jobbly.Domain.Enums;

namespace Jobbly.Application.Jobs;

// One deduplicated listing in the search results. Identified by the canonical
// job id; provider-specific raw job ids never leak to clients.
public sealed record JobListItem
{
    public Guid CanonicalJobId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string? Location { get; init; }
    public RemoteType RemoteType { get; init; }
    public SeniorityLevel Seniority { get; init; }
    public IReadOnlyList<string> TechStack { get; init; } = [];
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
    public string? SalaryCurrency { get; init; }
    public string? SalaryPeriod { get; init; }
    public DateTime? PostedAtUtc { get; init; }
    public int SourceCount { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
}
