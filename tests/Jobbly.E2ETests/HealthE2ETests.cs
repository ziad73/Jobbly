using System.Net;
using Xunit;

namespace Jobbly.E2ETests;

[Collection("api")]
public sealed class HealthE2ETests(JobblyApiFactory factory)
{
    [Fact]
    public async Task LivenessIsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessIsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}