namespace Jobbly.Infrastructure.Connectors;

// Response models for the RemoteOK public API (no auth):
//   GET {base}?tag={tag}  ->  top-level JSON array of postings.
// Unlike the ATS boards, this is an aggregator: the company comes per posting,
// not from config. Location is often empty (remote-only board); salary_min/max
// are 0 when undisclosed.
public sealed class RemoteOkPosting
{
    public long Id { get; init; }
    public string? Position { get; init; }
    public string? Company { get; init; }
    public string? Location { get; init; }
    public string? Description { get; init; }
    public List<string> Tags { get; init; } = [];
    public string? Url { get; init; }
    public string? ApplyUrl { get; init; }
    public DateTimeOffset? Date { get; init; }
    public int? SalaryMin { get; init; }
    public int? SalaryMax { get; init; }
}