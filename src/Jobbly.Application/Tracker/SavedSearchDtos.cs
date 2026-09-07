using System.ComponentModel.DataAnnotations;
using Jobbly.Application.Jobs;

namespace Jobbly.Application.Tracker;

public sealed record SaveSearchRequest(
    [property: Required, MinLength(1), MaxLength(100)] string Name,
    [property: Required] JobSearchQuery Criteria);

public sealed record PatchSavedSearchRequest(
    [property: MinLength(1), MaxLength(100)] string? Name,
    JobSearchQuery? Criteria);

public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    JobSearchQuery Criteria,
    DateTime CreatedAt,
    DateTime UpdatedAt);