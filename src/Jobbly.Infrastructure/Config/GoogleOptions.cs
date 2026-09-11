using System.ComponentModel.DataAnnotations;

namespace Jobbly.Infrastructure.Config;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>
    /// OAuth client ID from Google Cloud Console. Public value (not a secret),
    /// used as the required audience when verifying Google ID tokens.
    /// </summary>
    [Required]
    public string ClientId { get; init; } = null!;
}