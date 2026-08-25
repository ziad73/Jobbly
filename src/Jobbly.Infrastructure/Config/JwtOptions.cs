using System.ComponentModel.DataAnnotations;

namespace Jobbly.Infrastructure.Config;

public sealed class JwtOptions
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32)]
    public string Key { get; init; } = null!;

    [Required]
    [Url]
    public string Issuer { get; init; } = null!;

    [Required]
    [Url]
    public string Audience { get; init; } = null!;

    /// <summary>Access token lifetime in minutes (API contract §6.1).</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days, stored hashed (schema note §5.2).</summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 7;
}
