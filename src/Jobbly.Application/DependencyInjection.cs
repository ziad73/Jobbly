using Jobbly.Application.Jobs;
using Jobbly.Application.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Jobbly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // The ingestion orchestrator is an Application-layer service. Its
        // interfaces are implemented and registered in Infrastructure.
        services.AddScoped<RunIngestionPipeline>();
        services.AddScoped<JobSearchService>();

        return services;
    }
}
