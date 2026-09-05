using Jobbly.Infrastructure.Identity;

namespace Jobbly.Infrastructure.Auth;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt, int ExpiresInSeconds) CreateAccessToken(
        ApplicationUser user, IReadOnlyCollection<string> roles);
}