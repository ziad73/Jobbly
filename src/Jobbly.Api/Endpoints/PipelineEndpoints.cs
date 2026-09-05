using System.ComponentModel.DataAnnotations;
using Jobbly.Api.Authentication;
using Jobbly.Application.Pipeline;

namespace Jobbly.Api.Endpoints;

public static class PipelineEndpoints
{
    public static WebApplication MapPipelineEndpoints(this WebApplication app)
    {
        app.MapPost("/api/pipeline/trigger/{providerSlug}",
            async ([Length(1, 100)] string providerSlug, RunIngestionPipeline pipeline, CancellationToken ct) =>
            {
                var result = await pipeline.ExecuteAsync(providerSlug, ct);
                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            })
            .RequireAuthorization(AddApiAuthorizationExtensions.AdminPolicy)
            .WithSummary("Trigger the ingestion pipeline for a provider")
            .WithDescription("Admin only. Runs a manual ingestion for the given provider slug.");

        return app;
    }
}