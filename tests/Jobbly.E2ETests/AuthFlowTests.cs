using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class AuthFlowTests(JobblyApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task RegisterLoginMeProfileFlow()
    {
        var client = factory.CreateClient();
        var email = E2E.UniqueEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "E2e@12345", fullName = "E2E User" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var pair = await E2E.RegisterAsync(client);
        E2E.Authenticate(client, pair.AccessToken);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(pair.UserId, profile.GetProperty("userId").GetGuid());

        var update = await client.PutAsJsonAsync("/api/users/me/profile",
            new { title = "Backend Engineer", experienceYears = 5 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Backend Engineer", updated.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RegisterDuplicateEmailReturns409()
    {
        var client = factory.CreateClient();
        var email = E2E.UniqueEmail();
        await E2E.RegisterAsync(client, email);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "E2e@12345", fullName = "E2E User" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task RegisterShortPasswordReturns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = E2E.UniqueEmail(), password = "a1!", fullName = "E2E User" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginWrongPasswordReturns401()
    {
        var client = factory.CreateClient();
        var email = E2E.UniqueEmail();
        await E2E.RegisterAsync(client, email);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Wrong@12345" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task AnonymousMeReturns401()
    {
        var client = factory.CreateClient();

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}