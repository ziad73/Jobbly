using Jobbly.Application.Pipeline;
using Jobbly.Infrastructure.Pipeline;
using Xunit;

namespace Jobbly.Tests;

public sealed class FingerprintTests
{
    private static string FingerprintOf(string title, string company, string? location = null)
    {
        var normalizer = new GreenhouseJobNormalizer();
        var job = normalizer.Normalize(
            new RawJobDto("ext-1", title, company, location, "desc", "https://example.com/j/1", null),
            Guid.CreateVersion7());
        return job.DedupFingerprint;
    }

    [Fact]
    public void SameJobDifferentCasingAndSpacingMatches()
    {
        Assert.Equal(
            FingerprintOf("Senior Backend Engineer", "Stripe", "Berlin"),
            FingerprintOf("  senior backend ENGINEER ", "stripe", "berlin"));
    }

    [Fact]
    public void BracketedMarkersAreIgnored()
    {
        Assert.Equal(
            FingerprintOf("Senior Backend Engineer (Remote)", "Stripe", "Berlin"),
            FingerprintOf("Senior Backend Engineer", "Stripe", "Berlin"));
    }

    [Fact]
    public void CompanySuffixesAreIgnored()
    {
        Assert.Equal(
            FingerprintOf("Backend Engineer", "Stripe, Inc.", "Berlin"),
            FingerprintOf("Backend Engineer", "Stripe", "Berlin"));
    }

    [Fact]
    public void TeamSuffixesStayDistinct()
    {
        // "Payments" vs "Risk" are different jobs - must NOT merge.
        Assert.NotEqual(
            FingerprintOf("Backend Engineer, Payments", "Stripe", "Berlin"),
            FingerprintOf("Backend Engineer, Risk", "Stripe", "Berlin"));
    }

    [Fact]
    public void LocationsStayDistinct()
    {
        Assert.NotEqual(
            FingerprintOf("Backend Engineer", "Stripe", "Berlin"),
            FingerprintOf("Backend Engineer", "Stripe", "London"));
    }
}