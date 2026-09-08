namespace Jobbly.Infrastructure.Connectors;

// Response models for the public Ashby Job Board API (no auth):
//   GET {board base}  ->  { jobs: [...], apiVersion: "1" }.
// The board is site-specific (see Providers:Sources:ashby:BaseUrl); postings
// carry no company field, so the configured source Name is used instead.
// Unlisted postings (isListed == false) are direct-link only and skipped.
// Compensation is employer-opt-in and usually absent - parsed defensively.
public sealed class AshbyJobsResponse
{
    public List<AshbyJob> Jobs { get; init; } = [];
}

public sealed class AshbyJob
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Location { get; init; }
    public string? Department { get; init; }
    public string? Team { get; init; }
    public bool IsRemote { get; init; }
    public string? WorkplaceType { get; init; }
    public string? EmploymentType { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public bool IsListed { get; init; } = true;
    public string? JobUrl { get; init; }
    public string? DescriptionPlain { get; init; }
    public string? DescriptionHtml { get; init; }
    public AshbyCompensation? Compensation { get; init; }
}

public sealed class AshbyCompensation
{
    public string? CompensationTierSummary { get; init; }
    public string? ScrapeableCompensationSalarySummary { get; init; }
    public List<AshbyCompensationTier> CompensationTiers { get; init; } = [];
}

public sealed class AshbyCompensationTier
{
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public string? Currency { get; init; }
    public string? Interval { get; init; }
}