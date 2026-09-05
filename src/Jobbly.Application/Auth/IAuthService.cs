using Jobbly.Application.Auth;

namespace Jobbly.Application.Auth;

/// <summary>
/// Auth use cases. Currently credentials-only: registration and verification.
/// Token issuance (JWT) is added in a later pass.
/// </summary>
public interface IAuthService
{
    Task<CurrentUserDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
}