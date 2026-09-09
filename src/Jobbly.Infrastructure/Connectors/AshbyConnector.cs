using System.Net.Http.Json;
using System.Text.Json;
using Jobbly.Application.Pipeline;
using Jobbly.Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace Jobbly.Infrastructure.Connectors;

// Fetches jobs from a single Ashby board. BaseAddress must be the full board
// postings URL including the board token, e.g.
//   https://api.ashbyhq.com/posting-api/job-board/linear
// No auth required - the public board API is open for GET. The response wraps
// postings in a { jobs: [...] } envelope; pass ?includeCompensation=true so
// employers that expose bands include them (usually absent - parsed
// defensively, salary stays null when undisclosed).
public sealed class AshbyConnector : IJobConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _companyName;
    private readonly Uri _jobsUri;

    public AshbyConnector(HttpClient httpClient, IOptions<ProvidersOptions> options)
    {
        _httpClient = httpClient;
        _companyName = options.Value.Sources["ashby"].Name;

        var baseUri = httpClient.BaseAddress
            ?? throw new InvalidOperationException("Ashby BaseAddress must be configured.");
        _jobsUri = new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}?includeCompensation=true");
    }

    public string ProviderSlug => "ashby";

    public async Task<IReadOnlyList<RawJobDto>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_jobsUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AshbyJobsResponse>(JsonOptions, cancellationToken);

        return body?.Jobs
            .Where(j => j.IsListed
                && !string.IsNullOrWhiteSpace(j.Title)
                && !string.IsNullOrWhiteSpace(j.JobUrl))
            .Select(j =>
            {
                var (salaryMin, salaryMax, currency) = ParseSalary(j.Compensation);

                return new RawJobDto(
                    j.Id ?? Guid.CreateVersion7().ToString(),
                    j.Title!,
                    _companyName,
                    j.Location,
                    !string.IsNullOrWhiteSpace(j.DescriptionPlain) ? j.DescriptionPlain : j.DescriptionHtml,
                    j.JobUrl!,
                    PostedAtToUtc(j.PublishedAt),
                    salaryMin,
                    salaryMax,
                    currency,
                    RemoteHint: j.IsRemote ? "Remote" : j.WorkplaceType);
            })
            .ToList() ?? [];
    }

    // Compensation is employer-opt-in; most boards (including the reference
    // Linear board) expose nothing, in which case salary stays undisclosed.
    private static (int? Min, int? Max, string? Currency) ParseSalary(AshbyCompensation? compensation)
    {
        var tier = compensation?.CompensationTiers
            .FirstOrDefault(t => t.Min is not null || t.Max is not null);

        if (tier is null)
        {
            return (null, null, null);
        }

        return ((int?)tier.Min, (int?)tier.Max, tier.Currency);
    }

    // Ashby timestamps are ISO 8601 with an offset. Deserialized as
    // DateTimeOffset (offset preserved) then normalized to a UTC DateTime so
    // Npgsql can write the value to a "timestamptz" column (it rejects
    // DateTimeKind.Local).
    private static DateTime? PostedAtToUtc(DateTimeOffset? value)
        => value?.ToUniversalTime().UtcDateTime;
}