namespace Jobbly.Application.Auth;

/// <summary>
/// Auth use cases. Issues short-lived JWT access tokens plus long-lived,
/// hash-stored refresh tokens (rotation + revocation supported).
/// </summary>
public interface IAuthService
{
    /// <summary>Creates a user + 1:1 profile and returns a token pair. The result
    /// distinguishes a duplicate email from other failures (e.g. weak password),
    /// which carry Identity's validation messages.</summary>
    Task<RegisterAttempt> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies credentials and returns a token pair (null = invalid credentials).</summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies a Google ID token, then finds-or-creates the user
    /// (auto-linking verified emails onto existing accounts) and returns our
    /// own token pair. Null = invalid, expired, or unverified token.</summary>
    Task<AuthResponse?> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);

    /// <summary>Rotates a refresh token and returns a new token pair. Replaying a revoked
    /// token revokes all the user's active tokens (suspected theft). Null = invalid/expired/revoked.</summary>
    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the given refresh token (idempotent).</summary>
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}