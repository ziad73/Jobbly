using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Persistence;
using Jobbly.Infrastructure.Pipeline;
using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.DependencyInjection;

namespace Jobbly.E2ETests;

public static class E2E
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string UniqueEmail(string prefix = "e2e") => $"{prefix}{Guid.CreateVersion7():N}@test.io";

    public sealed record TokenPair(string AccessToken, string RefreshToken, Guid UserId);

    public static async Task<TokenPair> RegisterAsync(HttpClient client, string? email = null)
    {
        email ??= UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "E2e@12345", fullName = "E2E User" });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return new TokenPair(
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!,
            body.GetProperty("user").GetProperty("id").GetGuid());
    }

    public static void Authenticate(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    // Seeds one canonical + representative job through the real normalizer so
    // tracker tests have something to save. Returns the canonical id.
    public static async Task<Guid> SeedCanonicalAsync(IServiceProvider services, string titleSuffix)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobblyDbContext>();

        var provider = await db.Providers.SingleAsync(p => p.Slug == "greenhouse");
        var externalId = $"e2e-{Guid.CreateVersion7()}";
        var raw = new RawJobDto(externalId, $"E2E Engineer {titleSuffix}", "E2E Corp", null,
            "A fine engineering role.", "https://example.com/e2e", null);

        var job = new GreenhouseJobNormalizer().Normalize(raw, provider.Id);
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        var canonical = CanonicalJob.Create(job);
        db.CanonicalJobs.Add(canonical);
        job.AttachToCanonical(canonical.Id);
        await db.SaveChangesAsync();

        return canonical.Id;
    }
}