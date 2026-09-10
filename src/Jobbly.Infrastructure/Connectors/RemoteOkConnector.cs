using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobbly.Application.Pipeline;

namespace Jobbly.Infrastructure.Connectors;

// Fetches remote jobs from the RemoteOK public API. BaseAddress is the board
// root, e.g. https://remoteok.com/api (optional ?tag= filter is left off so
// one run covers the whole board). No auth required. RawJobDto has no tags
// field, so posting tags are folded into the description payload where the
// enrichment keyword scan picks them up.
public sealed class RemoteOkConnector : IJobConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string UnknownCompany = "Unknown";

    private readonly HttpClient _httpClient;
    private readonly Uri _jobsUri;

    public RemoteOkConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;

        var baseUri = httpClient.BaseAddress
            ?? throw new InvalidOperationException("RemoteOK BaseAddress must be configured.");
        _jobsUri = baseUri;
    }

    public string ProviderSlug => "remoteok";

    public async Task<IReadOnlyList<RawJobDto>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_jobsUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<RemoteOkPosting>>(JsonOptions, cancellationToken);

        return body?
            .Where(p => !string.IsNullOrWhiteSpace(p.Position) && !string.IsNullOrWhiteSpace(p.Url ?? p.ApplyUrl))
            .Select(p => new RawJobDto(
                p.Id.ToString(),
                WebUtility.HtmlDecode(p.Position)!,
                string.IsNullOrWhiteSpace(p.Company) ? UnknownCompany : WebUtility.HtmlDecode(p.Company),
                string.IsNullOrWhiteSpace(p.Location) ? null : p.Location,
                WithTags(p.Description, p.Tags),
                p.Url ?? p.ApplyUrl!,
                PostedAtToUtc(p.Date),
                SalaryOrNull(p.SalaryMin),
                SalaryOrNull(p.SalaryMax)))
            .ToList() ?? [];
    }

    private static string? WithTags(string? description, List<string> tags)
    {
        var cleanTags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (cleanTags.Count == 0)
        {
            return description;
        }

        return $"Tags: {string.Join(", ", cleanTags)}\n\n{description}";
    }

    // RemoteOK reports 0 for undisclosed salaries - not a real zero.
    private static int? SalaryOrNull(int? value) =>
        value is null or <= 0 ? null : value;

    // ISO 8601 with offset. Deserialized as DateTimeOffset (offset preserved)
    // then normalized to a UTC DateTime so Npgsql can write the value to a
    // "timestamptz" column (it rejects DateTimeKind.Local).
    private static DateTime? PostedAtToUtc(DateTimeOffset? value)
        => value?.ToUniversalTime().UtcDateTime;
}