using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;

namespace Jobbly.Infrastructure.Pipeline;

// turns RawJobDto into a Job entity
// compute the dedup fingerprint, and clean/decode the HTML description
public sealed partial class GreenhouseJobNormalizer : IJobNormalizer
{
    public Job Normalize(RawJobDto raw, Guid providerId)
    {
        var fingerprint = ComputeFingerprint(raw.Title, raw.CompanyName, raw.Location);
        var description = DecodeDescription(raw.Description);

        return Job.Create(
            providerId,
            raw.ExternalId,
            raw.Title.Trim(),
            raw.CompanyName.Trim(),
            raw.SourceUrl,
            description,
            raw.PostedAt,
            fingerprint,
            raw.Location?.Trim());
    }

    public void Update(Job existing, RawJobDto raw)
    {
        var fingerprint = ComputeFingerprint(raw.Title, raw.CompanyName, raw.Location);
        var description = DecodeDescription(raw.Description);

        existing.UpdateFromSource(
            raw.Title.Trim(),
            raw.CompanyName.Trim(),
            raw.SourceUrl,
            description,
            raw.PostedAt,
            fingerprint,
            raw.Location?.Trim());
    }

    // Greenhouse content is double HTML-entity-encoded:
    //   "&lt;h2&gt;Who we are&lt;/h2&gt;" → decode 1 → "<h2>Who we are</h2>"
    //   strip tags                          → "Who we are"
    //   decode 2 (nested entities)          → "Who we are" (unchanged here)
    internal static string DecodeDescription(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(content) ?? content;
        var stripped = TagRegex().Replace(decoded, " ");
        var secondPass = WebUtility.HtmlDecode(stripped) ?? stripped;
        return CollapseWhitespaceRegex().Replace(secondPass, " ").Trim();
    }

    internal static string ComputeFingerprint(string title, string companyName, string? location)
    {
        var sb = new StringBuilder();
        sb.Append(title.ToLowerInvariant().Trim());
        sb.Append('|');
        sb.Append(companyName.ToLowerInvariant().Trim());
        sb.Append('|');
        sb.Append(location?.Trim().ToLowerInvariant() ?? string.Empty);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseWhitespaceRegex();
}
