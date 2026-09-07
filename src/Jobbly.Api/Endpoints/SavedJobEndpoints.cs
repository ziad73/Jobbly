using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Jobbly.Application.Tracker;

namespace Jobbly.Api.Endpoints;

public static class SavedJobEndpoints
{
    public static WebApplication MapSavedJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/saved-jobs")
            .WithTags("Saved Jobs")
            .RequireAuthorization();

        group.MapGet("",
            async (ClaimsPrincipal user,
                [Range(1, 10_000)] int? page,
                [Range(1, 100)] int? pageSize,
                SavedJobService savedJobs, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await savedJobs.ListAsync(userId, page ?? 1, pageSize ?? 20, ct));
            })
            .WithName("ListSavedJobs")
            .WithSummary("List the current user's saved / tracked jobs")
            .WithDescription("Returns tracked jobs with their status, notes and a compact job snapshot.");

        group.MapPost("",
            async (ClaimsPrincipal user, SaveJobRequest request, SavedJobService savedJobs, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var attempt = await savedJobs.SaveAsync(userId, request.CanonicalJobId, ct);
                return attempt.Outcome switch
                {
                    SaveJobOutcome.Created =>
                        Results.Created($"/api/saved-jobs/{attempt.SavedJob!.Id}", attempt.SavedJob),
                    SaveJobOutcome.Duplicate =>
                        Results.Conflict(new { message = "This job is already saved." }),
                    _ => Results.NotFound()
                };
            })
            .WithName("SaveJob")
            .WithSummary("Save a job to the tracker")
            .WithDescription("Starts tracking a deduplicated job in the Saved state.");

        group.MapPatch("/{id:guid}",
            async (ClaimsPrincipal user, Guid id, PatchSavedJobRequest request, SavedJobService savedJobs, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var saved = await savedJobs.PatchAsync(userId, id, request.Status, request.Notes, request.FollowUpAt, ct);
                return saved is null
                    ? Results.NotFound()
                    : Results.Ok(saved);
            })
            .WithName("UpdateSavedJob")
            .WithSummary("Update a tracked job")
            .WithDescription("Partially updates status, notes and follow-up date. Setting status to Applied records the timestamp.");

        group.MapDelete("/{id:guid}",
            async (ClaimsPrincipal user, Guid id, SavedJobService savedJobs, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await savedJobs.DeleteAsync(userId, id, ct);
                return Results.NoContent();
            })
            .WithName("DeleteSavedJob")
            .WithSummary("Remove a job from the tracker")
            .WithDescription("Stops tracking the job. Idempotent.");

        return app;
    }
}