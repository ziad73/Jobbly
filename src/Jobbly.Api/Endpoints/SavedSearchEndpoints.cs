using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Jobbly.Application.Tracker;

namespace Jobbly.Api.Endpoints;

public static class SavedSearchEndpoints
{
    // A saved search is a named, persisted set of job-search filters belonging to a user — so they don't 
    // have to retype the same filters every time, and can see fresh results as new jobs get ingested.

    // What it stores: a name (e.g. "backend eu") plus criteria — the exact same filter object as 
    // GET /api/jobs query params (q, location, seniority, remote, salary, sort…). 
    // It's kept in the saved_searches table as jsonb, one row per user.

    // What it's for: re-running. GET /api/saved-searches/{id}/matches executes the stored filters
    // through the same search pipeline and returns current matches — that's the dashboard feed -logged-in user's personal home page with suggested jobs-: 
    // e.g. this morning it returns 12 jobs, next week after new ingestion runs it may return 18, 
    // without the user changing anything.
    public static WebApplication MapSavedSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/saved-searches")
            .WithTags("Saved Searches")
            .RequireAuthorization();

        group.MapGet("",
            async (ClaimsPrincipal user, SavedSearchService searches, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await searches.ListAsync(userId, ct));
            })
            .WithName("ListSavedSearches")
            .WithSummary("List the current user's saved searches")
            .WithDescription("Returns saved searches with their stored filter criteria.");

        group.MapPost("",
            async (ClaimsPrincipal user, SaveSearchRequest request, SavedSearchService searches, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var saved = await searches.CreateAsync(userId, request.Name, request.Criteria, ct);
                return Results.Created($"/api/saved-searches/{saved.Id}", saved);
            })
            .WithName("CreateSavedSearch")
            .WithSummary("Save a search")
            .WithDescription("Persists a named set of job-search filters for later reuse.");

        group.MapPatch("/{id:guid}",
            async (ClaimsPrincipal user, Guid id, PatchSavedSearchRequest request, SavedSearchService searches, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var saved = await searches.PatchAsync(userId, id, request.Name, request.Criteria, ct);
                return saved is null
                    ? Results.NotFound()
                    : Results.Ok(saved);
            })
            .WithName("UpdateSavedSearch")
            .WithSummary("Update a saved search")
            .WithDescription("Partially updates the name and/or stored filter criteria.");

        group.MapDelete("/{id:guid}",
            async (ClaimsPrincipal user, Guid id, SavedSearchService searches, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                await searches.DeleteAsync(userId, id, ct);
                return Results.NoContent();
            })
            .WithName("DeleteSavedSearch")
            .WithSummary("Delete a saved search")
            .WithDescription("Deletes the saved search. Idempotent.");

        group.MapGet("/{id:guid}/matches",
            async (ClaimsPrincipal user, Guid id,
                [Range(1, 10_000)] int? page,
                [Range(1, 100)] int? pageSize,
                SavedSearchService searches, CancellationToken ct) =>
            {
                if (user.GetUserId() is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var matches = await searches.RunAsync(userId, id, page ?? 1, pageSize ?? 20, ct);
                return matches is null
                    ? Results.NotFound()
                    : Results.Ok(matches);
            })
            .WithName("RunSavedSearch")
            .WithSummary("Run a saved search")
            .WithDescription("Executes the stored filters through the shared search pipeline (the dashboard feed).");

        return app;
    }
}
