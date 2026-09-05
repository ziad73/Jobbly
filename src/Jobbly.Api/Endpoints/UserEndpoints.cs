using System.Security.Claims;
using Jobbly.Application.Users;

namespace Jobbly.Api.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        // The whole group requires a valid bearer token; the caller's Id comes
        // from the token's subject claim, never from request input.
        var group = app.MapGroup("/api/users/me")
            .RequireAuthorization();

        group.MapGet("",
            async (ClaimsPrincipal user, IUserProfileService profiles, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var profile = await profiles.GetMeAsync(userId, ct);
                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(profile);
            })
            .WithName("GetCurrentUserProfile")
            .WithSummary("Fetch the current user's profile")
            .WithDescription("Returns the 1:1 profile auto-created at registration.");

        group.MapPut("/profile",
            async (ClaimsPrincipal user, UpdateUserProfileRequest request, IUserProfileService profiles, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var profile = await profiles.UpdateProfileAsync(userId, request, ct);
                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(profile);
            })
            .WithName("UpdateCurrentUserProfile")
            .WithSummary("Update the current user's profile")
            .WithDescription("Partially updates the profile - fields not supplied keep their current values.");

        group.MapPut("/skills",
            async (ClaimsPrincipal user, ReplaceSkillsRequest request, IUserProfileService profiles, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await profiles.ReplaceSkillsAsync(userId, request.Skills ?? [], ct);
                return Results.NoContent();
            })
            .WithName("ReplaceCurrentUserSkills")
            .WithSummary("Replace the current user's skill set")
            .WithDescription("Replaces all skills for the user with the supplied list (deduplicated).");

        return app;
    }
}