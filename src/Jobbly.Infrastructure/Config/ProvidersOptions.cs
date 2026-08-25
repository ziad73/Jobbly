using System.ComponentModel.DataAnnotations;

namespace Jobbly.Infrastructure.Config;

public sealed class ProvidersOptions
{
    public const string SectionName = "Providers";

    /// <summary>Keyed by provider slug, e.g. "greenhouse", "lever".</summary>
    [Required]
    [MinLength(1)]
    public Dictionary<string, ProviderConfig> Sources { get; init; } = [];
}

public sealed class ProviderConfig
{
    [Required]
    public string Name { get; init; } = null!;

    [Required]
    [Url]
    public string BaseUrl { get; init; } = null!;

    [Range(1, 10_080)]
    public int RefreshIntervalMinutes { get; init; } = 180;
}
