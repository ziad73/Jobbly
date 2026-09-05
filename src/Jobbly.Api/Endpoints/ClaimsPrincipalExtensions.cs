using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Jobbly.Api.Endpoints;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the Id of the caller from the bearer token's subject ("sub") claim.
    /// Null when the claim is missing or not a valid Guid.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}