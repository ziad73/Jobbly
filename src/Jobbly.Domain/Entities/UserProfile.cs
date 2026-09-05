using Jobbly.Domain.Enums;

namespace Jobbly.Domain.Entities;

public sealed class UserProfile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string? Title { get; private set; }
    public SeniorityLevel SeniorityLevel { get; private set; }
    public int? ExperienceYears { get; private set; }
    public IReadOnlyList<string> TechStack { get; private set; } = [];
    public IReadOnlyList<string> Locations { get; private set; } = [];
    public RemoteType RemotePreference { get; private set; }
    public int? SalaryExpectationMin { get; private set; }
    public int? SalaryExpectationMax { get; private set; }
    public string? SalaryCurrency { get; private set; }
    public string? SalaryPeriod { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserProfile()
    {
    }

    public static UserProfile Create(Guid userId)
    {
        var now = DateTime.UtcNow;

        return new UserProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string? title = null,
        SeniorityLevel? seniorityLevel = null,
        int? experienceYears = null,
        IReadOnlyList<string>? techStack = null,
        IReadOnlyList<string>? locations = null,
        RemoteType? remotePreference = null,
        int? salaryExpectationMin = null,
        int? salaryExpectationMax = null,
        string? salaryCurrency = null,
        string? salaryPeriod = null)
    {
        Title = title ?? Title;
        SeniorityLevel = seniorityLevel ?? SeniorityLevel;
        ExperienceYears = experienceYears ?? ExperienceYears;
        TechStack = techStack ?? TechStack;
        Locations = locations ?? Locations;
        RemotePreference = remotePreference ?? RemotePreference;
        SalaryExpectationMin = salaryExpectationMin ?? SalaryExpectationMin;
        SalaryExpectationMax = salaryExpectationMax ?? SalaryExpectationMax;
        SalaryCurrency = salaryCurrency ?? SalaryCurrency;
        SalaryPeriod = salaryPeriod ?? SalaryPeriod;
        UpdatedAt = DateTime.UtcNow;
    }
}