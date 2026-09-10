using System.Net.Http.Json;
using System.Text.Json;
using Jobbly.Application.Pipeline;
using Jobbly.Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace Jobbly.Infrastructure.Connectors;

// Fetches jobs from a single Lever board. BaseAddress must be the full board
// postings URL including the site token, e.g.
//   https://api.lever.co/v0/postings/gohighlevel
// No auth required - the public postings API is open for GET. The response is
// a top-level JSON array (?mode=json); prefer the plaintext description and
// fall back to HTML (the normalizer strips tags either way).
public sealed class LeverConnector : IJobConnector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _companyName;
    private readonly Uri _jobsUri;

    public LeverConnector(HttpClient httpClient, IOptions<ProvidersOptions> options)
    {
        _httpClient = httpClient;
        _companyName = options.Value.Sources["lever"].Name;

        var baseUri = httpClient.BaseAddress
            ?? throw new InvalidOperationException("Lever BaseAddress must be configured.");
        _jobsUri = new Uri($"{baseUri.AbsoluteUri.TrimEnd('/')}?mode=json");
    }

    public string ProviderSlug => "lever";

    public async Task<IReadOnlyList<RawJobDto>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_jobsUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<LeverPosting>>(JsonOptions, cancellationToken);

        return body?
            .Where(p => !string.IsNullOrWhiteSpace(p.Text) && !string.IsNullOrWhiteSpace(p.HostedUrl))
            .Select(p => new RawJobDto(
                p.Id ?? Guid.CreateVersion7().ToString(),
                p.Text!,
                _companyName,
                p.Categories?.Location,
                !string.IsNullOrWhiteSpace(p.DescriptionPlain) ? p.DescriptionPlain : p.Description,
                p.HostedUrl!,
                PostedAtToUtc(p.CreatedAt),
                RemoteHint: p.WorkplaceType))
            .ToList() ?? [];
    }

    // Lever timestamps are unix milliseconds. Normalized to a UTC DateTime so
    // Npgsql can write the value to a "timestamptz" column (it rejects
    // DateTimeKind.Local).
    private static DateTime? PostedAtToUtc(long? createdAtMs) =>
        createdAtMs is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(createdAtMs.Value).UtcDateTime;
}