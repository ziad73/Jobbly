using Jobbly.Domain.Enums;

namespace Jobbly.Domain.Entities;

public sealed class Job
{
    public Guid Id { get; private set; }
    public Guid? CanonicalJobId { get; private set; }
    public CanonicalJob? CanonicalJob { get; private set; }
    public Guid ProviderId { get; private set; }
    public Provider Provider { get; private set; } = null!;
    public Guid? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string DedupFingerprint { get; private set; } = null!;

    public string ExternalId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string CompanyName { get; private set; } = null!;
    public string? Location { get; private set; }
    public RemoteType RemoteType { get; private set; }
    public SeniorityLevel SeniorityLevel { get; private set; }
    public EmploymentType EmploymentType { get; private set; }

    public IReadOnlyList<string> TechStack { get; private set; } = [];
    public int? SalaryMin { get; private set; }
    public int? SalaryMax { get; private set; }
    public string? SalaryCurrency { get; private set; }
    public string? SalaryPeriod { get; private set; }

    public string DescriptionRaw { get; private set; } = null!;
    public IReadOnlyDictionary<string, string>? DescriptionStructured { get; private set; }
    public IReadOnlyList<string> Requirements { get; private set; } = [];
    public IReadOnlyList<string> NiceToHaves { get; private set; } = [];

    public string SourceUrl { get; private set; } = null!;
    public PipelineStatus PipelineStatus { get; private set; }

    public DateTime? PostedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime IngestedAt { get; private set; }
    public DateTime? IndexedAt { get; private set; }

    private Job()
    {
    }

    public static Job Create(
        Guid providerId,
        string externalId,
        string title,
        string companyName,
        string sourceUrl,
        string descriptionRaw,
        DateTime? postedAtUtc,
        string dedupFingerprint,
        string? location = null)
    {
        return new Job
        {
            Id = Guid.CreateVersion7(),
            ProviderId = providerId,
            ExternalId = externalId,
            Title = title,
            CompanyName = companyName,
            Location = location,
            SourceUrl = sourceUrl,
            DescriptionRaw = descriptionRaw,
            PostedAt = postedAtUtc,
            DedupFingerprint = dedupFingerprint,
            PipelineStatus = PipelineStatus.Ingested,
            IngestedAt = DateTime.UtcNow
        };
    }

    public void AttachToCanonical(Guid canonicalJobId) => CanonicalJobId = canonicalJobId;

    public void SetEnrichment(
        RemoteType remoteType,
        SeniorityLevel seniorityLevel,
        EmploymentType employmentType,
        IReadOnlyList<string> techStack,
        IReadOnlyList<string> requirements,
        IReadOnlyList<string> niceToHaves,
        int? salaryMin = null,
        int? salaryMax = null,
        string? salaryCurrency = null,
        string? salaryPeriod = null)
    {
        RemoteType = remoteType;
        SeniorityLevel = seniorityLevel;
        EmploymentType = employmentType;
        TechStack = techStack;
        Requirements = requirements;
        NiceToHaves = niceToHaves;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryCurrency = salaryCurrency;
        SalaryPeriod = salaryPeriod;
        PipelineStatus = PipelineStatus.Enriched;
    }

    public void MarkIndexed() => PipelineStatus = PipelineStatus.Indexed;

    public void AssignCompany(Guid companyId) => CompanyId = companyId;
}
