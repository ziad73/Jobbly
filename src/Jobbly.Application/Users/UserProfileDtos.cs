using System.ComponentModel.DataAnnotations;
using Jobbly.Application.Validation;
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

[SalaryRangeValid]
public sealed record UpdateUserProfileRequest(
    [property: MaxLength(120)] string? Title,
    [property: EnumDataType(typeof(SeniorityLevel))] SeniorityLevel? SeniorityLevel,
    [property: Range(0, 60)] int? ExperienceYears,
    [property: MaxItems(50)] IReadOnlyList<string>? TechStack,
    [property: MaxItems(25)] IReadOnlyList<string>? Locations,
    [property: EnumDataType(typeof(RemoteType))] RemoteType? RemotePreference,
    [property: Range(0, 1_000_000)] int? SalaryExpectationMin,
    [property: Range(0, 1_000_000)] int? SalaryExpectationMax,
    [property: StringLength(3)] string? SalaryCurrency,
    [property: StringLength(20)] string? SalaryPeriod);

public sealed record ReplaceSkillsRequest(
    [property: Required, MaxItems(30)] IReadOnlyList<string>? Skills);