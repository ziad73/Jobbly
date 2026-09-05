using Jobbly.Domain.Enums;

namespace Jobbly.Application.Users;

public sealed record UserProfileDto(
    Guid UserId,
    string? Title,
    SeniorityLevel SeniorityLevel,
    int? ExperienceYears,
    IReadOnlyList<string> TechStack,
    IReadOnlyList<string> Locations,
    RemoteType RemotePreference,
    int? SalaryExpectationMin,
    int? SalaryExpectationMax,
    string? SalaryCurrency,
    string? SalaryPeriod);

public sealed record UpdateUserProfileRequest(
    string? Title,
    SeniorityLevel? SeniorityLevel,
    int? ExperienceYears,
    IReadOnlyList<string>? TechStack,
    IReadOnlyList<string>? Locations,
    RemoteType? RemotePreference,
    int? SalaryExpectationMin,
    int? SalaryExpectationMax,
    string? SalaryCurrency,
    string? SalaryPeriod);

public sealed record ReplaceSkillsRequest(IReadOnlyList<string>? Skills);