using Jobbly.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Jobbly.E2ETests;

// Controllable Google validator stand-in: tests decide what Google "says"
// without network calls.
public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public GoogleIdentity? Result { get; set; } = new("google-user@test.io", "Google User");

    public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
        => Task.FromResult(Result);
}

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

    /// <summary>Mutable Google stand-in shared by the Google e2e tests
    /// (collection runs sequentially, so per-test reassignment is safe).</summary>
    public FakeGoogleTokenValidator GoogleFake { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Point every connection string (EF + Hangfire storage) at the
        // container. Migrations and seeds run through the normal startup path.
        builder.UseSetting("ConnectionStrings:jobblydb", _db.GetConnectionString());

        // Never call Google from tests; the fake above decides the outcome.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator>(GoogleFake);
        });
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