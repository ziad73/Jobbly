using System.ComponentModel.DataAnnotations;
using Jobbly.Application.Users;
using Jobbly.Application.Validation;
using Xunit;

namespace Jobbly.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void MaxItemsRejectsOversizedCollections()
    {
        var attribute = new MaxItemsAttribute(3);

        Assert.True(attribute.IsValid(null));
        Assert.True(attribute.IsValid(new[] { "a", "b", "c" }));
        Assert.False(attribute.IsValid(new[] { "a", "b", "c", "d" }));
    }

    [Fact]
    public void SalaryRangeRejectsMaxBelowMin()
    {
        var attribute = new SalaryRangeValidAttribute();

        var bad = new UpdateUserProfileRequest(null, null, null, null, null, null, 100_000, 50_000, null, null);
        var good = new UpdateUserProfileRequest(null, null, null, null, null, null, 50_000, 100_000, null, null);
        var partial = new UpdateUserProfileRequest(null, null, null, null, null, null, 50_000, null, null, null);

        var badResult = attribute.GetValidationResult(bad, new ValidationContext(bad));
        Assert.NotEqual(ValidationResult.Success, badResult);
        Assert.Contains("SalaryExpectationMax", badResult!.MemberNames);

        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(good, new ValidationContext(good)));
        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(partial, new ValidationContext(partial)));
    }
}