using System.Net.Http.Json;
using System.Text.Json;
using Jobbly.Application.Pipeline;

namespace Jobbly.Infrastructure.Connectors;

// Fetches jobs from a single Greenhouse board. BaseAddress must be the full
// board URL including the board token (plural "boards"), e.g.
//   https://boards-api.greenhouse.io/v1/boards/{board_token}
// No auth required - the public board API is open for GET. The list endpoint
// with content=true returns title, company, first_published/updated_at,
// location and the HTML description in a single response. Note: content is
// double HTML-entity-encoded; decoding is the normalizer's job (Step 4).
public sealed class GreenhouseConnector : IJobConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private const string UnknownCompany = "Unknown";

    private readonly HttpClient _httpClient;
    private readonly Uri _jobsUri;

    public GreenhouseConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Uri resolution would drop the board token segment when merging a
        // relative "jobs" path into a base ending in "{board_token}", so the
        // jobs endpoint is built explicitly here.
        var baseUri = httpClient.BaseAddress
            ?? throw new InvalidOperationException("Greenhouse BaseAddress must be configured.");
        _jobsUri = new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}/jobs?content=true");
    }

    public string ProviderSlug => "greenhouse";

    public async Task<IReadOnlyList<RawJobDto>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_jobsUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<GreenhouseJobsResponse>(JsonOptions, cancellationToken);

        return body?.Jobs
            .Where(j => !string.IsNullOrWhiteSpace(j.Title) && !string.IsNullOrWhiteSpace(j.AbsoluteUrl))
            .Select(j => new RawJobDto(
                j.Id.ToString(),
                j.Title!,
                string.IsNullOrWhiteSpace(j.CompanyName) ? UnknownCompany : j.CompanyName,
                j.Location?.Name,
                j.Content,
                j.AbsoluteUrl!,
                j.FirstPublished ?? j.UpdatedAt))
            .ToList() ?? [];
    }
}