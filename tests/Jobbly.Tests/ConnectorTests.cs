using System.Net;
using System.Text;
using Jobbly.Infrastructure.Config;
using Jobbly.Infrastructure.Connectors;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jobbly.Tests;

public sealed class ConnectorTests
{
    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private static HttpClient ClientFor(string json, string baseAddress)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var client = new HttpClient(new StubHandler(response))
        {
            BaseAddress = new Uri(baseAddress)
        };
        return client;
    }

    private static IOptions<ProvidersOptions> OptionsFor(string slug, string name) =>
        Options.Create(new ProvidersOptions
        {
            Sources = new Dictionary<string, ProviderConfig>
            {
                [slug] = new ProviderConfig { Name = name, BaseUrl = "https://example.com/" }
            }
        });

    [Fact]
    public async Task LeverMapsPostingsPreferringPlaintext()
    {
        const string json = """
            [
              {"id": "abc-1", "text": "Backend Engineer", "categories": {"location": "Berlin"},
               "descriptionPlain": "Plain body", "description": "<p>HTML body</p>",
               "hostedUrl": "https://jobs.lever.co/testco/abc-1", "createdAt": 1788000000000},
              {"id": "abc-2", "text": "Designer", "hostedUrl": "https://jobs.lever.co/testco/abc-2"},
              {"id": "abc-3", "text": "  ", "hostedUrl": "https://jobs.lever.co/testco/abc-3"}
            ]
            """;

        using var client = ClientFor(json, "https://api.lever.co/v0/postings/testco");
        var connector = new LeverConnector(client, OptionsFor("lever", "TestCo"));

        Assert.Equal("lever", connector.ProviderSlug);
        var jobs = await connector.FetchAsync();

        Assert.Equal(2, jobs.Count);
        Assert.Equal("abc-1", jobs[0].ExternalId);
        Assert.Equal("Backend Engineer", jobs[0].Title);
        Assert.Equal("TestCo", jobs[0].CompanyName);
        Assert.Equal("Berlin", jobs[0].Location);
        Assert.Equal("Plain body", jobs[0].Description);
        Assert.Equal("https://jobs.lever.co/testco/abc-1", jobs[0].SourceUrl);
        Assert.NotNull(jobs[0].PostedAt);
        Assert.Null(jobs[1].Location);
        Assert.Null(jobs[1].Description);
    }

    [Fact]
    public async Task GreenhouseMapsBoardJobs()
    {
        const string json = """
            {"jobs": [
              {"id": 42, "title": "SRE", "absolute_url": "https://boards.greenhouse.io/t/jobs/42",
               "content": "&lt;p&gt;Hi&lt;/p&gt;", "company_name": "Acme",
               "first_published": "2026-01-05T10:00:00+00:00",
               "location": {"name": "Remote"}}
            ], "meta": {"total": 1}}
            """;

        using var client = ClientFor(json, "https://boards-api.greenhouse.io/v1/boards/testco");
        var connector = new GreenhouseConnector(client);

        Assert.Equal("greenhouse", connector.ProviderSlug);
        var jobs = await connector.FetchAsync();

        var job = Assert.Single(jobs);
        Assert.Equal("42", job.ExternalId);
        Assert.Equal("SRE", job.Title);
        Assert.Equal("Acme", job.CompanyName);
        Assert.Equal("Remote", job.Location);
        Assert.NotNull(job.PostedAt);
    }
}