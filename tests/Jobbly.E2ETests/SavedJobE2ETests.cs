using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class SavedJobE2ETests(JobblyApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SaveListPatchDeleteLifecycle()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);
        E2E.Authenticate(client, pair.AccessToken);

        var canonicalId = await E2E.SeedCanonicalAsync(factory.Services, Guid.CreateVersion7().ToString("N"));

        var save = await client.PostAsJsonAsync("/api/saved-jobs", new { canonicalJobId = canonicalId });
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var saved = await save.Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = saved.GetProperty("id").GetGuid();
        Assert.Equal(0, saved.GetProperty("status").GetInt32());

        var duplicate = await client.PostAsJsonAsync("/api/saved-jobs", new { canonicalJobId = canonicalId });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var unknown = await client.PostAsJsonAsync("/api/saved-jobs",
            new { canonicalJobId = Guid.Empty });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/saved-jobs", Json);
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());

        var patch = await client.PatchAsJsonAsync($"/api/saved-jobs/{id}",
            new { status = 1, notes = "Applied via referral" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var patched = await patch.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(1, patched.GetProperty("status").GetInt32());
        Assert.NotNull(patched.GetProperty("appliedAt").GetString());

        var delete = await client.DeleteAsync($"/api/saved-jobs/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var empty = await client.GetFromJsonAsync<JsonElement>("/api/saved-jobs", Json);
        Assert.Equal(0, empty.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task CrossUserSavedJobIsNotFound()
    {
        var owner = factory.CreateClient();
        var ownerPair = await E2E.RegisterAsync(owner);
        E2E.Authenticate(owner, ownerPair.AccessToken);
        var canonicalId = await E2E.SeedCanonicalAsync(factory.Services, Guid.CreateVersion7().ToString("N"));
        var save = await owner.PostAsJsonAsync("/api/saved-jobs", new { canonicalJobId = canonicalId });
        var id = (await save.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        var stranger = factory.CreateClient();
        var strangerPair = await E2E.RegisterAsync(stranger);
        E2E.Authenticate(stranger, strangerPair.AccessToken);

        // No existence leak: foreign ids behave as not-found.
        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.PatchAsJsonAsync($"/api/saved-jobs/{id}", new { status = 3 })).StatusCode);

        var list = await stranger.GetFromJsonAsync<JsonElement>("/api/saved-jobs", Json);
        Assert.Equal(0, list.GetProperty("totalCount").GetInt32());
    }
}