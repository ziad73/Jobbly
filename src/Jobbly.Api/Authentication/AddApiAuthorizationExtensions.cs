using Jobbly.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Jobbly.Api.Authentication;

public static class AddApiAuthorizationExtensions
{
    public const string AdminPolicy = "admin";

    /// <summary>
    /// Registers authorization policies on top of the JWT bearer scheme added by
    /// AddApiAuthentication. Role matching relies on RoleClaimType = "role" being
    /// set on the token validation parameters.
    /// </summary>
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireRole(ApplicationRoles.Admin));

        return services;
    }
}
