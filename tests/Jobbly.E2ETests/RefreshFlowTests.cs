using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class RefreshFlowTests(JobblyApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task RefreshRotatesPair()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = pair.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.NotEqual(pair.AccessToken, body.GetProperty("accessToken").GetString());
        Assert.NotEqual(pair.RefreshToken, body.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task ReusedRevokedTokenBurnsTheSession()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);

        var first = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = pair.RefreshToken });
        first.EnsureSuccessStatusCode();
        var rotated = (await first.Content.ReadFromJsonAsync<JsonElement>(Json))
            .GetProperty("refreshToken").GetString()!;

        // Replay the revoked token: suspected theft revokes everything.
        var replay = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = pair.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // Even the rotated token is dead now.
        var after = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = rotated });
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task LogoutRevokesToken()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);

        var logout = await client.PostAsJsonAsync("/api/auth/logout",
            new { refreshToken = pair.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = pair.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}