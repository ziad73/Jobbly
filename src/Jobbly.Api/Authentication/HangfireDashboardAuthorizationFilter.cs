using Hangfire.Dashboard;
using Jobbly.Infrastructure.Identity;

namespace Jobbly.Api.Authentication;

/// <summary>
/// Restricts the Hangfire dashboard to users holding the Admin role.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole(ApplicationRoles.Admin);
    }
}