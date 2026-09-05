using Jobbly.Application.Auth;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Identity;
using Jobbly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Jobbly.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JobblyDbContext dbContext,
    IJwtTokenGenerator tokenGenerator,
    RefreshTokenStore refreshTokenStore,
    IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _jwtOptions = options.Value;

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return null;
        }

        var user = ApplicationUser.Create(request.Email, request.FullName);

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        // Profiles are always present (1:1), so create one at registration.
        dbContext.UserProfiles.Add(UserProfile.Create(user.Id));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var existing = await refreshTokenStore.FindByTokenAsync(refreshToken, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        // Replay of a revoked token means the session may have been stolen:
        // invalidate every active token for that user so they must sign in again.
        if (existing.RevokedAtUtc is not null)
        {
            await refreshTokenStore.RevokeAllActiveAsync(existing.UserId, "suspected reuse", cancellationToken);
            return null;
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
        {
            return null;
        }

        // Rotate: revoke the old token, chain it to the replacement, issue a new pair.
        var newRefreshTokenValue = RefreshTokenStore.GenerateToken();
        var newRefreshToken = RefreshToken.Create(
            user.Id,
            RefreshTokenStore.HashToken(newRefreshTokenValue),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays));

        existing.Revoke("rotated");
        existing.MarkReplacedBy(newRefreshToken.Id);

        refreshTokenStore.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueAccessOnlyAsync(user, newRefreshTokenValue, cancellationToken);
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await refreshTokenStore.FindByTokenAsync(refreshToken, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.Revoke("logout");
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResponse> IssueTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var refreshTokenValue = RefreshTokenStore.GenerateToken();

        refreshTokenStore.Add(RefreshToken.Create(
            user.Id,
            RefreshTokenStore.HashToken(refreshTokenValue),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)));

        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueAccessOnlyAsync(user, refreshTokenValue, cancellationToken);
    }

    private async Task<AuthResponse> IssueAccessOnlyAsync(ApplicationUser user, string refreshTokenValue, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var (accessToken, _, expiresIn) = tokenGenerator.CreateAccessToken(user, roles);

        return new AuthResponse(accessToken, refreshTokenValue, expiresIn, ToDto(user));
    }

    private static CurrentUserDto ToDto(ApplicationUser user) => new(user.Id, user.Email!, user.FullName);
}