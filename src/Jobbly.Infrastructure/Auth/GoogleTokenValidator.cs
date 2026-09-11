using Google.Apis.Auth;
using Jobbly.Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace Jobbly.Infrastructure.Auth;

/// <summary>
/// Verifies Google ID tokens against Google's certs: signature, issuer,
/// expiry, and that the audience is our own OAuth client id.
/// </summary>
public sealed class GoogleTokenValidator(IOptions<GoogleOptions> options) : IGoogleTokenValidator
{
    private readonly GoogleOptions _options = options.Value;

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_options.ClientId] });
        }
        // InvalidJwtException covers signature/issuer/expiry/audience failures;
        // structurally malformed tokens die earlier in Base64/argument parsing.
        // Anything else (e.g. cancellation) propagates as a 500, as it should.
        catch (Exception ex) when (ex is InvalidJwtException or FormatException or ArgumentException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload.Email) || payload.EmailVerified != true)
        {
            return null;
        }

        var fullName = string.IsNullOrWhiteSpace(payload.Name)
            ? payload.Email.Split('@')[0]
            : payload.Name;

        return new GoogleIdentity(payload.Email, fullName);
    }
}