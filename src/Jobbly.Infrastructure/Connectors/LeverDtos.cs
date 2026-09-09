namespace Jobbly.Infrastructure.Connectors;

// Response models for the public Lever Postings API (no auth):
//   GET {site base}/?mode=json  ->  JSON array of postings.
// The board is site-specific (see Providers:Sources:lever:BaseUrl); postings
// carry no company field, so the configured source Name is used instead.
// Timestamps are unix milliseconds; plaintext lives in "descriptionPlain"
// ("description" is the HTML fallback).
public sealed class LeverPosting
{
    public string? Id { get; init; }
    public string? Text { get; init; }
    public LeverCategories? Categories { get; init; }
    public string? DescriptionPlain { get; init; }
    public string? Description { get; init; }
    public string? HostedUrl { get; init; }
    public long? CreatedAt { get; init; }
    public string? WorkplaceType { get; init; }
}

public sealed class LeverCategories
{
    public string? Location { get; init; }
    public string? Commitment { get; init; }
    public string? Team { get; init; }
    public string? Department { get; init; }
}