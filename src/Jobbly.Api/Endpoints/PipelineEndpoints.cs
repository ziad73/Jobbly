using Jobbly.Application.Pipeline;

namespace Jobbly.Api.Endpoints;

public static class PipelineEndpoints
{
    public static WebApplication MapPipelineEndpoints(this WebApplication app)
    {
        app.MapPost("/api/pipeline/trigger/{providerSlug}",
            async (string providerSlug, RunIngestionPipeline pipeline, CancellationToken ct) =>
            {
                var result = await pipeline.ExecuteAsync(providerSlug, ct);
                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}