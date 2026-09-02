using System.Text.RegularExpressions;
using Jobbly.Application.Pipeline;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;

namespace Jobbly.Infrastructure.Pipeline;

// Rule-based enrichment: tech stack, seniority, remote type, employment type.
// No I/O — pure text analysis. Called by the orchestrator after normalization.
public sealed partial class EnrichmentService : IEnrichmentService
{
    private const string DefaultCompany = "Unknown";

    private static readonly (string[] Aliases, string Tag)[] TechKeywords =
    [
        // Languages
        (["c#", "csharp", ".net", "dotnet"], "dotnet"),
        (["java"], "java"),
        (["python"], "python"),
        (["javascript", "js"], "javascript"),
        (["typescript", "ts"], "typescript"),
        (["golang"], "go"),
        (["rust"], "rust"),
        (["kotlin"], "kotlin"),
        (["swift"], "swift"),
        (["ruby"], "ruby"),
        (["php"], "php"),
        (["scala"], "scala"),
        (["elixir"], "elixir"),

        // Frameworks & libraries
        (["react", "reactjs"], "react"),
        (["vue", "vuejs"], "vue"),
        (["angular", "angularjs"], "angular"),
        (["node", "nodejs"], "node"),
        (["django"], "django"),
        (["flask"], "flask"),
        (["spring", "springboot"], "spring"),
        (["next", "nextjs"], "nextjs"),

        // Cloud & infrastructure
        (["docker"], "docker"),
        (["kubernetes", "k8s"], "kubernetes"),
        (["aws"], "aws"),
        (["azure"], "azure"),
        (["gcp", "googlecloud"], "gcp"),
        (["terraform"], "terraform"),
        (["ansible"], "ansible"),

        // Data stores & messaging
        (["postgresql", "postgres", "psql"], "postgresql"),
        (["redis"], "redis"),
        (["mongodb", "mongo"], "mongodb"),
        (["kafka"], "kafka"),
        (["elasticsearch", "elastic"], "elasticsearch"),

        // Protocols & tools
        (["graphql"], "graphql"),
        (["grpc"], "grpc"),
    ];

    // Ordered by specificity — most specific first, first match wins.
    private static readonly (SeniorityLevel Level, Regex Pattern)[] SeniorityPatterns =
    [
        (SeniorityLevel.Principal, PrincipalRegex()),
        (SeniorityLevel.Staff, StaffRegex()),
        (SeniorityLevel.Senior, SeniorRegex()),
        (SeniorityLevel.Junior, JuniorRegex()),
    ];

    private static readonly Regex[] RemotePatterns = [RemoteRegex(), HybridRegex()];
    private static readonly (EmploymentType Type, Regex Pattern)[] EmploymentPatterns =
    [
        (EmploymentType.FullTime, FullTimeRegex()),
        (EmploymentType.PartTime, PartTimeRegex()),
        (EmploymentType.Contract, ContractRegex()),
        (EmploymentType.Internship, InternRegex()),
    ];

    public void Enrich(Job job)
    {
        var text = $"{job.Title} {job.DescriptionRaw}".ToLowerInvariant();
        var title = job.Title;

        var remote = DetectRemoteType(title, job.Location);
        var seniority = DetectSeniority(title);
        var employment = DetectEmploymentType(text);
        var techStack = DetectTechStack(text);

        job.SetEnrichment(
            remote,
            seniority,
            employment,
            techStack,
            [],  // Requirements — no structured extraction yet
            []); // NiceToHaves  — no structured extraction yet
    }

    private static RemoteType DetectRemoteType(string title, string? location)
    {
        var haystack = $"{title} {location}".ToLowerInvariant();
        foreach (var pattern in RemotePatterns)
        {
            if (pattern.IsMatch(haystack))
            {
                return pattern == RemotePatterns[0] ? RemoteType.Remote : RemoteType.Hybrid;
            }
        }
        return RemoteType.OnSite;
    }

    private static SeniorityLevel DetectSeniority(string title)
    {
        var lower = title.ToLowerInvariant();
        foreach (var (level, pattern) in SeniorityPatterns)
        {
            if (pattern.IsMatch(lower))
            {
                return level;
            }
        }
        return SeniorityLevel.Unknown;
    }

    private static EmploymentType DetectEmploymentType(string text)
    {
        foreach (var (type, pattern) in EmploymentPatterns)
        {
            if (pattern.IsMatch(text))
            {
                return type;
            }
        }
        return EmploymentType.Unknown;
    }

    private static IReadOnlyList<string> DetectTechStack(string text)
    {
        var words = text.Split([' ', '/', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawWord in words)
        {
            var word = rawWord.Trim('.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '&', '"', '\'', '`', '~');
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            foreach (var (aliases, tag) in TechKeywords)
            {
                if (matched.Contains(tag))
                {
                    continue;
                }

                foreach (var alias in aliases)
                {
                    if (word.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        matched.Add(tag);
                        break;
                    }
                }
            }
        }

        return matched.ToList();
    }

    [GeneratedRegex(@"\bprincipal\b", RegexOptions.IgnoreCase)]
    private static partial Regex PrincipalRegex();

    [GeneratedRegex(@"\bstaff\b", RegexOptions.IgnoreCase)]
    private static partial Regex StaffRegex();

    [GeneratedRegex(@"\bsenior\b|\bsr\.?\b", RegexOptions.IgnoreCase)]
    private static partial Regex SeniorRegex();

    [GeneratedRegex(@"\bjunior\b|\bjr\.?\b", RegexOptions.IgnoreCase)]
    private static partial Regex JuniorRegex();

    [GeneratedRegex(@"\bremote\b", RegexOptions.IgnoreCase)]
    private static partial Regex RemoteRegex();

    [GeneratedRegex(@"\bhybrid\b", RegexOptions.IgnoreCase)]
    private static partial Regex HybridRegex();

    [GeneratedRegex(@"\bfull[\s-]?time\b", RegexOptions.IgnoreCase)]
    private static partial Regex FullTimeRegex();

    [GeneratedRegex(@"\bpart[\s-]?time\b", RegexOptions.IgnoreCase)]
    private static partial Regex PartTimeRegex();

    [GeneratedRegex(@"\bcontract(?:or|s)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContractRegex();

    [GeneratedRegex(@"\bintern(?:ship)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex InternRegex();
}