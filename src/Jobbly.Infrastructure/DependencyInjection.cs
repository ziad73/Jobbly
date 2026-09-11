using Jobbly.Application.Auth;
using Jobbly.Application.Common;
using Jobbly.Application.Jobs;
using Jobbly.Application.Pipeline;
using Jobbly.Application.Users;
using Jobbly.Infrastructure.Auth;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Connectors;
using Jobbly.Infrastructure.Identity;
using Jobbly.Infrastructure.Persistence;
using Jobbly.Infrastructure.Pipeline;
using Jobbly.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
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
            ?? "Host=localhost;Port=5433;Database=jobbly;Username=puser;Password=ppass";

        services.AddDbContext<JobblyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(JobblyDbContext).Assembly.FullName)));

        // ASP.NET Core Identity backed by the same Postgres database.
        // Roles are enabled at the store level; User/Admin are seeded at startup
        // and "User" is assigned on registration (see DatabaseInitializer/AuthService).
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<JobblyDbContext>();

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

        services.AddOptions<GoogleOptions>()
            .BindConfiguration(GoogleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddGreenhouseConnector(services);
        AddLeverConnector(services);
        AddAshbyConnector(services);
        AddRemoteOkConnector(services);

        // Pipeline services — called by the orchestrator in Application layer
        services.AddScoped<IJobNormalizer, GreenhouseJobNormalizer>();
        services.AddScoped<IDeduplicationService, DeduplicationService>();
        services.AddScoped<IEnrichmentService, EnrichmentService>();

        // Search port implementation (Npgsql-specific full-text / location)
        services.AddScoped<IFullTextSearch, PostgresFullTextSearch>();

        // Auth + own-profile services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<RefreshTokenStore>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }

    private static void AddGreenhouseConnector(IServiceCollection services) =>
        AddProviderConnector<GreenhouseConnector>(services, "greenhouse");

    private static void AddLeverConnector(IServiceCollection services) =>
        AddProviderConnector<LeverConnector>(services, "lever");

    private static void AddAshbyConnector(IServiceCollection services) =>
        AddProviderConnector<AshbyConnector>(services, "ashby");

    private static void AddRemoteOkConnector(IServiceCollection services) =>
        AddProviderConnector<RemoteOkConnector>(services, "remoteok");

    // One named HttpClient per provider slug (a typed client named after the
    // shared IJobConnector interface would collide, with the last registration
    // winning for every provider), configured with a Polly resilience pipeline
    // (retry on transient failures + circuit breaker) and 30-second timeout.
    // Each typed client is forwarded as IJobConnector so the orchestrator's
    // IEnumerable<IJobConnector> picks up every provider.
    private static void AddProviderConnector<TConnector>(IServiceCollection services, string slug)
        where TConnector : class, IJobConnector
    {
        var http = services.AddHttpClient(slug, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<ProvidersOptions>>().Value.Sources[slug];
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Jobbly/1.0");
        });

        http.AddResilienceHandler(slug, (builder, context) =>
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

        http.AddTypedClient<TConnector>();

        services.AddTransient<IJobConnector>(sp => sp.GetRequiredService<TConnector>());
    }
}
