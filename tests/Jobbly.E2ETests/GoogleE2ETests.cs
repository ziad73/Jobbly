using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class GoogleE2ETests(JobblyApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GoogleLoginCreatesUserProfileAndReturnsPair()
    {
        var client = factory.CreateClient();
        factory.GoogleFake.Result = new Jobbly.Infrastructure.Auth.GoogleIdentity(
            E2E.UniqueEmail("google"), "Google Newcomer");

        var response = await client.PostAsJsonAsync("/api/auth/google",
            new { idToken = "anything-the-fake-accepts" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
        var userId = body.GetProperty("user").GetProperty("id").GetGuid();

        // Profile auto-created: the token opens the authenticated world.
        E2E.Authenticate(client, body.GetProperty("accessToken").GetString()!);
        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(userId, profile.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task GoogleLoginExistingEmailLogsIntoSameAccount()
    {
        var client = factory.CreateClient();
        var email = E2E.UniqueEmail("google-link");
        factory.GoogleFake.Result = new Jobbly.Infrastructure.Auth.GoogleIdentity(email, "Linked User");

        var first = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "t1" });
        var second = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "t2" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("user").GetProperty("id").GetGuid();
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("user").GetProperty("id").GetGuid();
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task GoogleUserCannotPasswordLogin()
    {
        var client = factory.CreateClient();
        var email = E2E.UniqueEmail("google-nopw");
        factory.GoogleFake.Result = new Jobbly.Infrastructure.Auth.GoogleIdentity(email, "No Password");

        var created = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "t" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        // No password hash was ever set: fails closed.
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Whatever@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task InvalidGoogleTokenReturns401()
    {
        var client = factory.CreateClient();
        factory.GoogleFake.Result = null;

        var response = await client.PostAsJsonAsync("/api/auth/google",
            new { idToken = "bogus" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingGoogleTokenReturns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/google", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}