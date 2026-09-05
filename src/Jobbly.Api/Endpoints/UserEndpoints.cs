using Jobbly.Application.Users;

namespace Jobbly.Api.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users/me");

        // TODO(auth): read the user id from the bearer token's subject claim once JWT lands.
        // For now it comes from the query string so these can be exercised end-to-end.

        group.MapGet("",
            async (Guid userId, IUserProfileService profiles, CancellationToken ct) =>
            {
                var profile = await profiles.GetMeAsync(userId, ct);
                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(profile);
            })
            .WithName("GetCurrentUserProfile")
            .WithSummary("Fetch the current user's profile")
            .WithDescription("Returns the 1:1 profile auto-created at registration.");

        group.MapPut("/profile",
            async (Guid userId, UpdateUserProfileRequest request, IUserProfileService profiles, CancellationToken ct) =>
            {
                var profile = await profiles.UpdateProfileAsync(userId, request, ct);
                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(profile);
            })
            .WithName("UpdateCurrentUserProfile")
            .WithSummary("Update the current user's profile")
            .WithDescription("Partially updates the profile - fields not supplied keep their current values.");

        group.MapPut("/skills",
            async (Guid userId, ReplaceSkillsRequest request, IUserProfileService profiles, CancellationToken ct) =>
            {
                await profiles.ReplaceSkillsAsync(userId, request.Skills ?? [], ct);
                return Results.NoContent();
            })
            .WithName("ReplaceCurrentUserSkills")
            .WithSummary("Replace the current user's skill set")
            .WithDescription("Replaces all skills for the user with the supplied list (deduplicated).");

        return app;
    }
}