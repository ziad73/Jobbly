using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Jobbly.E2ETests;

// Shared API host backed by a real Postgres 18 container. One container per
// test run (collection fixture); tests isolate by unique data, never by
// database resets.
public sealed class JobblyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("jobbly_test")
        .WithUsername("puser")
        .WithPassword("ppass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Point every connection string (EF + Hangfire storage) at the
        // container. Migrations and seeds run through the normal startup path.
        builder.UseSetting("ConnectionStrings:jobblydb", _db.GetConnectionString());
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<JobblyApiFactory>
{
}