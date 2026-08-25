using Jobbly.Application.Common;
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
            ?? "Host=localhost;Port=5432;Database=jobbly;Username=my_user;Password=1234";

        services.AddDbContext<JobblyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IJobblyDbContext>(sp => sp.GetRequiredService<JobblyDbContext>());

        return services;
    }
}
