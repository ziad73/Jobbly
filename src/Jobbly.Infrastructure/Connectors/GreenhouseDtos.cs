namespace Jobbly.Infrastructure.Connectors;

// Response models for the public Greenhouse Job Board API (no auth).
// Property names use SnakeCaseLower to match Greenhouse's snake_case JSON.

public sealed class GreenhouseJobsResponse
{
    public List<GreenhouseJob> Jobs { get; init; } = [];
    public GreenhouseMeta? Meta { get; init; }
}

public sealed class GreenhouseMeta
{
    public int Total { get; init; }
}

public sealed class GreenhouseJob
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? AbsoluteUrl { get; init; }
    public string? Content { get; init; }
    public string? CompanyName { get; init; }
    public DateTimeOffset? FirstPublished { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public GreenhouseLocation? Location { get; init; }
    public List<GreenhouseDepartment> Departments { get; init; } = [];
    public List<GreenhouseOffice> Offices { get; init; } = [];
}

public sealed class GreenhouseLocation
{
    public string? Name { get; init; }
}

public sealed class GreenhouseDepartment
{
    public string? Name { get; init; }
}

public sealed class GreenhouseOffice
{
    public string? Name { get; init; }
}