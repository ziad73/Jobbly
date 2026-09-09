using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class ValidationE2ETests(JobblyApiFactory factory)
{
    [Fact]
    public async Task MalformedEmailReturns400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "not-an-email", password = "E2e@12345" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OutOfRangePagingReturns400()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/jobs?pageSize=999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/jobs?seniority=99")).StatusCode);
    }

    [Fact]
    public async Task InvalidProfileEnumReturns400()
    {
        var client = factory.CreateClient();
        var pair = await E2E.RegisterAsync(client);
        E2E.Authenticate(client, pair.AccessToken);

        var response = await client.PutAsJsonAsync("/api/users/me/profile",
            new { seniorityLevel = 99 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}