using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Jobbly.Infrastructure.Pipeline;
using Xunit;

namespace Jobbly.Tests;

public sealed class EnrichmentTests
{
    private static Job MakeJob(string title, string description = "", string? location = null) =>
        Job.Create(Guid.CreateVersion7(), "ext-1", title, "Acme", "https://example.com/j/1",
            description, null, "fp", location);

    private static Job Enriched(string title, string description = "", string? location = null)
    {
        var job = MakeJob(title, description, location);
        new EnrichmentService().Enrich(job);
        return job;
    }

    [Theory]
    [InlineData("Senior Backend Engineer", SeniorityLevel.Senior)]
    [InlineData("SDE III - Whatsapp Voice", SeniorityLevel.Senior)]
    [InlineData("Software Engineer II", SeniorityLevel.MidLevel)]
    [InlineData("Junior Frontend Developer", SeniorityLevel.Junior)]
    [InlineData("Engineer I", SeniorityLevel.Junior)]
    [InlineData("Mid-Level Data Analyst", SeniorityLevel.MidLevel)]
    [InlineData("Staff Backend Engineer", SeniorityLevel.Staff)]
    [InlineData("Lead Product Designer", SeniorityLevel.Lead)]
    [InlineData("Engineering Manager II", SeniorityLevel.Manager)]
    [InlineData("Senior Director, Engineering", SeniorityLevel.Director)]
    [InlineData("Principal Architect", SeniorityLevel.Principal)]
    [InlineData("Graduate Software Engineer", SeniorityLevel.EntryLevel)]
    [InlineData("Software Engineer Intern", SeniorityLevel.Internship)]
    [InlineData("Account Executive, Startups", SeniorityLevel.Unknown)]
    [InlineData("Backend Engineer", SeniorityLevel.Unknown)]
    public void DetectsSeniorityFromTitle(string title, SeniorityLevel expected)
    {
        Assert.Equal(expected, Enriched(title).SeniorityLevel);
    }

    [Fact]
    public void StaffProductManagerIsManagerBySpecificityOrder()
    {
        // Both "staff" and "manager" match; Manager wins by pattern order.
        Assert.Equal(SeniorityLevel.Manager, Enriched("Staff Product Manager, Payments").SeniorityLevel);
    }

    [Theory]
    [InlineData("Backend Engineer (Remote)", null, RemoteType.Remote)]
    [InlineData("Backend Engineer", "We are remote-first. Work from home.", RemoteType.Remote)]
    [InlineData("Backend Engineer", "Hybrid role, 2 days onsite.", RemoteType.Hybrid)]
    [InlineData("Backend Engineer", "Berlin, Germany", RemoteType.OnSite)]
    public void DetectsRemoteType(string title, string? extra, RemoteType expected)
    {
        var description = extra is null ? "" : $"Great role. {extra} Apply now.";
        Assert.Equal(expected, Enriched(title, description).RemoteType);
    }

    [Theory]
    [InlineData("We use Python, Postgres and Docker daily.", new[] { "python", "postgresql", "docker" })]
    [InlineData("Node.js and Vue.js frontend.", new[] { "node", "vue" })]
    [InlineData("ASP.NET Core with React.", new[] { "dotnet", "react" })]
    [InlineData("Machine learning platform with data science team.", new[] { "machine-learning", "data-science" })]
    public void DetectsTechStack(string description, string[] expectedTags)
    {
        var stack = Enriched("Backend Engineer", description).TechStack;
        foreach (var tag in expectedTags)
        {
            Assert.Contains(tag, stack);
        }
    }

    [Theory]
    [InlineData("6-month contract role.", EmploymentType.Contract)]
    [InlineData("Temporary cover for parental leave.", EmploymentType.Temporary)]
    [InlineData("Full-time position.", EmploymentType.FullTime)]
    [InlineData("Part-time support.", EmploymentType.PartTime)]
    [InlineData("Summer internship.", EmploymentType.Internship)]
    public void DetectsEmploymentType(string description, EmploymentType expected)
    {
        Assert.Equal(expected, Enriched("Engineer", description).EmploymentType);
    }
}