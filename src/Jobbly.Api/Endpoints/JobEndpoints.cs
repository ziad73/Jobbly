using Jobbly.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Jobbly.Api.Endpoints;

public static class JobEndpoints
{
    public static WebApplication MapJobEndpoints(this WebApplication app)
    {
        app.MapGet("/api/jobs",
            async ([AsParameters] JobSearchQuery query, JobSearchService search, CancellationToken ct) =>
                Results.Ok(await search.SearchAsync(query, ct)))
            .WithName("SearchJobs")
            .WithSummary("Search deduplicated jobs")
            .WithDescription("Returns one listing per canonical job with optional filters: full-text q, tags, location, seniority, remote, salary, sort, and paging.");

        app.MapGet("/api/jobs/{canonicalId:guid}",
            async (Guid canonicalId, JobSearchService search, CancellationToken ct) =>
            {
                var detail = await search.GetByIdAsync(canonicalId, ct);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            })
            .WithName("GetJobById")
            .WithSummary("Fetch a single canonical job")
            .WithDescription("Returns full detail for one deduplicated job, or 404 if not found or archived.");

        return app;
    }
}
