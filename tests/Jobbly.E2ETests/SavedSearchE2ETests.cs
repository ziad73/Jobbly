using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class SavedSearchE2ETests(JobblyApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SearchCrudAndMatchesMirrorDirectQuery()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);
        E2E.Authenticate(client, pair.AccessToken);

        var marker = Guid.CreateVersion7().ToString("N");
        await E2E.SeedCanonicalAsync(factory.Services, marker);

        var create = await client.PostAsJsonAsync("/api/saved-searches",
            new { name = "marker search", criteria = new { q = marker } });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var searchId = (await create.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        var matches = await client.GetFromJsonAsync<JsonElement>(
            $"/api/saved-searches/{searchId}/matches", Json);
        var direct = await client.GetFromJsonAsync<JsonElement>($"/api/jobs?q={marker}", Json);
        Assert.Equal(
            direct.GetProperty("totalCount").GetInt32(),
            matches.GetProperty("totalCount").GetInt32());
        Assert.True(matches.GetProperty("totalCount").GetInt32() >= 1);

        var patch = await client.PatchAsJsonAsync($"/api/saved-searches/{searchId}",
            new { name = "renamed" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var missing = await client.GetAsync(
            $"/api/saved-searches/{Guid.Empty}/matches");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var delete = await client.DeleteAsync($"/api/saved-searches/{searchId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CreateSearchWithoutNameReturns400()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);
        E2E.Authenticate(client, pair.AccessToken);

        var create = await client.PostAsJsonAsync("/api/saved-searches",
            new { criteria = new { q = "x" } });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }
}