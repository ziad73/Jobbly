using Jobbly.Domain.Enums;

namespace Jobbly.Application.Jobs;

// Full detail for a single canonical listing. Extends the search card with the
// description and the parsed requirements/nice-to-haves.
public sealed record JobDetail
{
    public Guid CanonicalJobId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string? Location { get; init; }
    public RemoteType RemoteType { get; init; }
    public SeniorityLevel Seniority { get; init; }
    public EmploymentType EmploymentType { get; init; }
    public IReadOnlyList<string> TechStack { get; init; } = [];
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
    public string? SalaryCurrency { get; init; }
    public string? SalaryPeriod { get; init; }
    public DateTime? PostedAtUtc { get; init; }
    public int SourceCount { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
    public string? Overview { get; init; }
    public IReadOnlyList<string> Requirements { get; init; } = [];
    public IReadOnlyList<string> NiceToHaves { get; init; } = [];
    public IReadOnlyDictionary<string, string>? Description { get; init; }
}
