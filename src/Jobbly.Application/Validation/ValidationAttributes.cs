using System.Collections;
using System.ComponentModel.DataAnnotations;
using Jobbly.Application.Users;

namespace Jobbly.Application.Validation;

/// <summary>
/// Rejects collections that contain more than <paramref name="max"/> items.
/// Applied to request properties that accept list input, so unbounded payloads
/// (e.g. a huge TechStack or Skills array) fail fast with a 400.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MaxItemsAttribute(int max) : ValidationAttribute
{
    public int Max { get; } = max;

    public override bool IsValid(object? value) =>
        value is not ICollection collection || collection.Count <= Max;
}

/// <summary>
/// Cross-field rule for profile updates: when SalaryExpectationMin and
/// SalaryExpectationMax are both present, max must be greater or equal.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SalaryRangeValidAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is UpdateUserProfileRequest request &&
            request.SalaryExpectationMin is { } min &&
            request.SalaryExpectationMax is { } max &&
            max < min)
        {
            return new ValidationResult(
                "SalaryExpectationMax must be greater than or equal to SalaryExpectationMin.",
                [nameof(UpdateUserProfileRequest.SalaryExpectationMax)]);
        }

        return ValidationResult.Success;
    }
}