using Jobbly.Application.Common;
using Jobbly.Application.Pipeline;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Connectors;
using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Jobbly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("jobblydb")
            ?? "Host=localhost;Port=5433;Database=jobbly;Username=my_user;Password=1234";

        services.AddDbContext<JobblyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(JobblyDbContext).Assembly.FullName)));

        services.AddScoped<IJobblyDbContext>(sp => sp.GetRequiredService<JobblyDbContext>());

        // Register IOptions and bind config to an object with validation  
        services.AddOptions<ProvidersOptions>()
            .BindConfiguration(ProvidersOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PipelineOptions>()
            .BindConfiguration(PipelineOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddGreenhouseConnector(services);

        return services;
    }

    private static void AddGreenhouseConnector(IServiceCollection services)
    {
        // register the connector with a typed HttpClient in DI, configured with a Polly resilience pipeline 
        // (retry on transient failures + circuit breaker) and 30-second timeout.
        services.AddHttpClient<IJobConnector, GreenhouseConnector>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<ProvidersOptions>>().Value.Sources["greenhouse"];
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Jobbly/1.0");
        })
        .AddResilienceHandler("greenhouse", (builder, context) =>
        {
            var pipeline = context.ServiceProvider
                .GetRequiredService<IOptions<PipelineOptions>>().Value;

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = pipeline.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });

            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 8
            });
        });
    }
}
