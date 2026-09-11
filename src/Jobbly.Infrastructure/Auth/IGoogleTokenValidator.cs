namespace Jobbly.Infrastructure.Auth;

/// <summary>
/// Verified Google identity: email is guaranteed present and verified.
/// Name falls back to the email local part when Google has no name.
/// </summary>
public sealed record GoogleIdentity(string Email, string FullName);

/// <summary>
/// Verifies Google ID tokens. Separated from AuthService so unit tests can
/// substitute a fake instead of calling Google.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>Returns the verified identity, or null when the token is
    /// invalid, expired, aimed at another audience, or unverified.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}