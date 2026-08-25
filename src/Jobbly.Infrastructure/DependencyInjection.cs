using Jobbly.Application.Common;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
